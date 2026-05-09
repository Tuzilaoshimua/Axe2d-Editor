namespace Axe2DEditor.Core.Graphs;

public static class EventGraphAnalysisService
{
    public static TriggerAnalysis AnalyzeTrigger(EventGraphDefinition graph, GraphNodeDefinition trigger)
    {
        var result = new TriggerAnalysis();
        if (graph is null)
        {
            result.IsComplex = true;
            return result;
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { trigger.Id };
        AnalyzeChain(graph, trigger, trigger.Id, result, visited);
        return result;
    }

    public static void RebuildStructuredEdges(EventGraphDefinition graph, GraphNodeDefinition trigger, List<GraphNodeDefinition> conditions, List<GraphNodeDefinition> actions)
    {
        if (graph is null)
        {
            return;
        }

        var chainIds = new HashSet<string>(conditions.Select(node => node.Id).Concat(actions.Select(node => node.Id)), StringComparer.OrdinalIgnoreCase);
        graph.Edges.RemoveAll(edge => edge.FromNodeId == trigger.Id || chainIds.Contains(edge.FromNodeId) || chainIds.Contains(edge.ToNodeId));

        var orderedConditions = conditions.ToList();
        GraphNodeDefinition previous = trigger;
        for (var i = 0; i < orderedConditions.Count; i++)
        {
            var node = orderedConditions[i];
            node.X = 320 + i * 220;
            node.Y = node.Kind == NodeKinds.Condition ? 70 : 170;
            SetNodeOwner(node, trigger.Id);
            graph.Edges.Add(new GraphEdgeDefinition
            {
                FromNodeId = previous.Id,
                ToNodeId = node.Id,
                FromPort = "flowOut",
                ToPort = "flowIn",
                ValueType = NodePortValueTypes.Flow
            });
            previous = node;
        }

        var orderedActions = actions.ToList();
        var parallelIndex = orderedActions.FindIndex(IsParallelTemplate);
        var actionStartX = 320 + orderedConditions.Count * 220;
        if (parallelIndex >= 0)
        {
            var parallelNode = orderedActions[parallelIndex];
            var preParallelActions = orderedActions.Take(parallelIndex).ToList();
            var branchActions = orderedActions.Skip(parallelIndex + 1).ToList();

            foreach (var action in preParallelActions)
            {
                action.X = actionStartX;
                action.Y = 170;
                SetNodeOwner(action, trigger.Id);
                graph.Edges.Add(new GraphEdgeDefinition
                {
                    FromNodeId = previous.Id,
                    ToNodeId = action.Id,
                    FromPort = "flowOut",
                    ToPort = "flowIn",
                    ValueType = NodePortValueTypes.Flow
                });
                previous = action;
                actionStartX += 220;
            }

            parallelNode.X = actionStartX;
            parallelNode.Y = 170;
            SetNodeOwner(parallelNode, trigger.Id);
            graph.Edges.Add(new GraphEdgeDefinition
            {
                FromNodeId = previous.Id,
                ToNodeId = parallelNode.Id,
                FromPort = "flowOut",
                ToPort = "flowIn",
                ValueType = NodePortValueTypes.Flow
            });

            if (branchActions.Count == 0)
            {
                return;
            }

            for (var i = 0; i < branchActions.Count; i++)
            {
                var node = branchActions[i];
                node.X = actionStartX + 220 + (i % 3) * 220;
                node.Y = 170 + (i / 3) * 110;
                SetNodeOwner(node, trigger.Id);
                graph.Edges.Add(new GraphEdgeDefinition
                {
                    FromNodeId = parallelNode.Id,
                    ToNodeId = node.Id,
                    FromPort = "flowOut",
                    ToPort = "flowIn",
                    ValueType = NodePortValueTypes.Flow
                });
            }

            return;
        }

        GraphNodeDefinition? actionPrevious = previous;
        for (var i = 0; i < orderedActions.Count; i++)
        {
            var node = orderedActions[i];
            node.X = actionStartX + i * 220;
            node.Y = 170;
            SetNodeOwner(node, trigger.Id);
            graph.Edges.Add(new GraphEdgeDefinition
            {
                FromNodeId = actionPrevious!.Id,
                ToNodeId = node.Id,
                FromPort = "flowOut",
                ToPort = "flowIn",
                ValueType = NodePortValueTypes.Flow
            });
            actionPrevious = node;
        }
    }

    public static IEnumerable<GraphNodeDefinition> GetReachableNodes(EventGraphDefinition graph, GraphNodeDefinition? root)
    {
        if (graph is null || root is null)
        {
            return [];
        }

        var result = new List<GraphNodeDefinition>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<GraphNodeDefinition>();
        queue.Enqueue(root);
        visited.Add(root.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            foreach (var edge in graph.Edges.Where(edge => edge.FromNodeId == current.Id))
            {
                var next = graph.Nodes.FirstOrDefault(node => node.Id == edge.ToNodeId);
                if (next is not null && visited.Add(next.Id))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return result.OrderBy(node => node.Kind == NodeKinds.Trigger ? 0 : 1).ThenBy(node => node.X).ThenBy(node => node.Y).ToList();
    }

    public static IEnumerable<GraphNodeDefinition> GetDetachedOwnedNodes(EventGraphDefinition graph, GraphNodeDefinition trigger, string kind, HashSet<string> connectedIds)
    {
        if (graph is null)
        {
            return [];
        }

        return graph.Nodes
            .Where(node =>
                string.Equals(node.Kind, kind, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetNodeOwner(node), trigger.Id, StringComparison.OrdinalIgnoreCase) &&
                !connectedIds.Contains(node.Id))
            .OrderBy(node => node.X)
            .ThenBy(node => node.Y)
            .ToList();
    }

    public static bool IsEdgePortCompatible(GraphEdgeDefinition edge)
    {
        var valueType = string.IsNullOrWhiteSpace(edge.ValueType) ? NodePortValueTypes.Flow : edge.ValueType;
        if (valueType == NodePortValueTypes.Flow)
        {
            return IsFlowPort(edge.FromPort, true) && IsFlowPort(edge.ToPort, false);
        }

        return !IsFlowPort(edge.FromPort, true) && !IsFlowPort(edge.ToPort, false);
    }

    private static void AnalyzeChain(EventGraphDefinition graph, GraphNodeDefinition current, string ownerTriggerId, TriggerAnalysis result, HashSet<string> visited)
    {
        while (true)
        {
            var outgoing = graph.Edges
                .Where(edge =>
                    edge.FromNodeId == current.Id &&
                    IsFlowPort(edge.FromPort, true) &&
                    IsFlowPort(edge.ToPort, false) &&
                    string.Equals(
                        string.IsNullOrWhiteSpace(edge.ValueType) ? NodePortValueTypes.Flow : edge.ValueType,
                        NodePortValueTypes.Flow,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (outgoing.Count == 0)
            {
                return;
            }

            if (outgoing.Count > 1)
            {
                if (IsParallelNode(current))
                {
                    foreach (var branch in outgoing)
                    {
                        var branchNode = graph.Nodes.FirstOrDefault(node => node.Id == branch.ToNodeId);
                        if (branchNode is null || !visited.Add(branchNode.Id))
                        {
                            result.IsComplex = true;
                            continue;
                        }

                        if (branchNode.Kind == NodeKinds.Condition)
                        {
                            SetNodeOwner(branchNode, ownerTriggerId);
                            result.Conditions.Add(branchNode);
                        }
                        else if (branchNode.Kind == NodeKinds.Action)
                        {
                            SetNodeOwner(branchNode, ownerTriggerId);
                            result.Actions.Add(branchNode);
                        }
                        else
                        {
                            result.IsComplex = true;
                            continue;
                        }

                        AnalyzeChain(graph, branchNode, ownerTriggerId, result, visited);
                    }

                    return;
                }

                result.IsComplex = true;
                return;
            }

            var next = graph.Nodes.FirstOrDefault(node => node.Id == outgoing[0].ToNodeId);
            if (next is null || !visited.Add(next.Id))
            {
                result.IsComplex = true;
                return;
            }

            if (next.Kind == NodeKinds.Condition)
            {
                SetNodeOwner(next, ownerTriggerId);
                result.Conditions.Add(next);
            }
            else if (next.Kind == NodeKinds.Action)
            {
                SetNodeOwner(next, ownerTriggerId);
                result.Actions.Add(next);
            }
            else
            {
                result.IsComplex = true;
                return;
            }

            current = next;
        }
    }

    private static bool IsParallelTemplate(GraphNodeDefinition node)
    {
        return string.Equals(GetParameter(node.Parameters, "template", string.Empty), "parallel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsParallelNode(GraphNodeDefinition node)
    {
        return node.Kind == NodeKinds.Action && IsParallelTemplate(node);
    }

    private static bool IsFlowPort(string port, bool output)
    {
        if (string.IsNullOrWhiteSpace(port))
        {
            return true;
        }

        return output
            ? port.Equals("flowOut", StringComparison.OrdinalIgnoreCase)
            : port.Equals("flowIn", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public static string GetNodeOwner(GraphNodeDefinition node)
    {
        return GetParameter(node.Parameters, TriggerOwnerParameterKey, string.Empty);
    }

    public static void SetNodeOwner(GraphNodeDefinition node, string triggerId)
    {
        if (node.Kind == NodeKinds.Trigger || string.IsNullOrWhiteSpace(triggerId))
        {
            return;
        }

        node.Parameters[TriggerOwnerParameterKey] = triggerId;
    }

    private const string TriggerOwnerParameterKey = "__ownerTriggerId";
}

public sealed class TriggerAnalysis
{
    public bool IsComplex { get; set; }

    public List<GraphNodeDefinition> Conditions { get; } = [];

    public List<GraphNodeDefinition> Actions { get; } = [];
}
