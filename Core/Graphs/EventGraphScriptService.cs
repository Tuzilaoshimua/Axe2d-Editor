using Axe2DEditor.Core.Projects;
using System.Text;
using System.Text.RegularExpressions;

namespace Axe2DEditor.Core.Graphs;

public static class EventGraphScriptService
{
    private const string ScriptPathCommentPrefix = "// 脚本路径: ";

    public static void EnsureScriptForTrigger(ProjectContext context, EventGraphDefinition? graph, GraphNodeDefinition node)
    {
        if (node.Parameters.TryGetValue("scriptPath", out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            var existingPath = ResolveScriptPath(context, existing);
            if (!string.IsNullOrWhiteSpace(existingPath) && File.Exists(existingPath) && new FileInfo(existingPath).Length > 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                var existingDirectory = Path.GetDirectoryName(existingPath);
                if (!string.IsNullOrWhiteSpace(existingDirectory))
                {
                    Directory.CreateDirectory(existingDirectory);
                }

                File.WriteAllText(existingPath, BuildScriptTemplate(graph, node), Encoding.UTF8);
                return;
            }
        }

        var scriptsDirectory = Path.Combine(context.RootDirectory, context.Project.Paths.Assets, "Scripts", "Triggers");
        Directory.CreateDirectory(scriptsDirectory);
        var safeName = ToSafeFileName(string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title);
        var filePath = EnsureUniqueScriptPath(scriptsDirectory, safeName, ".ts");
        File.WriteAllText(filePath, BuildScriptTemplate(graph, node), Encoding.UTF8);
        node.Parameters["scriptPath"] = Path.GetRelativePath(context.RootDirectory, filePath).Replace('\\', '/');
    }

    public static string BuildScriptTemplate(EventGraphDefinition? graph, GraphNodeDefinition node)
    {
        var title = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
        var eventType = GetParameter(node.Parameters, "event", string.Empty);
        var subject = GetParameter(node.Parameters, "subject", string.Empty);
        var analysis = EventGraphAnalysisService.AnalyzeTrigger(graph!, node);
        var builder = new StringBuilder();
        builder.AppendLine($"// {title}");
        builder.AppendLine($"// Event: {eventType}");
        builder.AppendLine($"// Subject: {subject}");
        builder.AppendLine();
        builder.AppendLine("const triggerMeta = {");
        builder.AppendLine($"  name: {ToScriptString(title)},");
        builder.AppendLine($"  event: {ToScriptString(eventType)},");
        builder.AppendLine($"  subject: {ToScriptString(subject)}");
        builder.AppendLine("};");
        builder.AppendLine();
        builder.AppendLine("export function onTrigger(context: unknown): void {");
        builder.AppendLine("  void context;");
        builder.AppendLine();
        builder.AppendLine("  const conditions = [");
        foreach (var condition in analysis.Conditions)
        {
            var conditionTemplate = GetParameter(condition.Parameters, "template", condition.Kind);
            var conditionDetail = GetParameter(condition.Parameters, "detail", string.Empty);
            builder.AppendLine("    {");
            builder.AppendLine($"      kind: {ToScriptString(condition.Kind)},");
            builder.AppendLine($"      template: {ToScriptString(conditionTemplate)},");
            builder.AppendLine($"      title: {ToScriptString(condition.Title)},");
            builder.AppendLine($"      detail: {ToScriptString(conditionDetail)}");
            builder.AppendLine("    },");
        }
        builder.AppendLine("  ];");
        builder.AppendLine();
        builder.AppendLine("  const actions = [");
        foreach (var action in analysis.Actions)
        {
            var actionTemplate = GetParameter(action.Parameters, "template", action.Kind);
            var actionDetail = GetParameter(action.Parameters, "detail", string.Empty);
            builder.AppendLine("    {");
            builder.AppendLine($"      kind: {ToScriptString(action.Kind)},");
            builder.AppendLine($"      template: {ToScriptString(actionTemplate)},");
            builder.AppendLine($"      title: {ToScriptString(action.Title)},");
            builder.AppendLine($"      detail: {ToScriptString(actionDetail)}");
            builder.AppendLine("    },");
        }
        builder.AppendLine("  ];");
        builder.AppendLine();
        builder.AppendLine("  if (!triggerMeta.event) {");
        builder.AppendLine("    return;");
        builder.AppendLine("  }");
        builder.AppendLine();
        builder.AppendLine("  for (const condition of conditions) {");
        builder.AppendLine("    void condition;");
        builder.AppendLine("  }");
        builder.AppendLine();
        builder.AppendLine("  for (const action of actions) {");
        builder.AppendLine("    switch (action.template) {");
        foreach (var template in analysis.Actions.Select(action => GetParameter(action.Parameters, "template", action.Kind)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"      case {ToScriptString(template)}:");
            builder.AppendLine("        break;");
        }
        builder.AppendLine("      default:");
        builder.AppendLine("        throw new Error(`Unsupported action: ${action.template}`);");
        builder.AppendLine("    }");
        builder.AppendLine("  }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/*");
        builder.AppendLine("Converted event graph snapshot:");
        builder.Append(BuildScriptSnapshot(graph, node));
        builder.AppendLine("*/");
        return builder.ToString();
    }

    public static string BuildScriptSnapshot(EventGraphDefinition? graph, GraphNodeDefinition node)
    {
        var builder = new StringBuilder();
        var triggerParameters = node.Parameters
            .Where(pair => !string.Equals(pair.Key, "scriptPath", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        AppendCommentBlock(builder, "Trigger parameters", SerializeParameters(triggerParameters));

        var analysis = EventGraphAnalysisService.AnalyzeTrigger(graph!, node);
        if (!analysis.IsComplex)
        {
            AppendNodeSnapshot(builder, "Conditions", analysis.Conditions);
            AppendNodeSnapshot(builder, "Actions", analysis.Actions);
            return builder.ToString();
        }

        var reachableNodes = EventGraphAnalysisService.GetReachableNodes(graph!, node)
            .Where(reachableNode => !string.Equals(reachableNode.Id, node.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        AppendNodeSnapshot(builder, "Reachable nodes", reachableNodes);

        if (graph is not null)
        {
            var reachableIds = reachableNodes.Select(reachableNode => reachableNode.Id)
                .Append(node.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var edgeLines = graph.Edges
                .Where(edge => reachableIds.Contains(edge.FromNodeId) && reachableIds.Contains(edge.ToNodeId))
                .Select(edge => $"{edge.FromNodeId}.{edge.FromPort} -> {edge.ToNodeId}.{edge.ToPort} ({edge.ValueType})");
            AppendCommentBlock(builder, "Edges", string.Join(Environment.NewLine, edgeLines));
        }

        return builder.ToString();
    }

    public static string ResolveScriptPath(ProjectContext context, string relativeScriptPath)
    {
        if (string.IsNullOrWhiteSpace(relativeScriptPath))
        {
            return string.Empty;
        }

        var normalized = relativeScriptPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(context.RootDirectory, normalized));
    }

    public static string BuildScriptEditorText(string scriptPath, string scriptBody)
    {
        var body = scriptBody.TrimStart('\uFEFF');
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return body;
        }

        return $"{ScriptPathCommentPrefix}{scriptPath}{Environment.NewLine}{Environment.NewLine}{body}";
    }

    public static string ExtractScriptBody(string editorText)
    {
        var text = editorText.TrimStart('\uFEFF');
        if (!text.StartsWith(ScriptPathCommentPrefix, StringComparison.Ordinal))
        {
            return editorText;
        }

        var firstLineBreak = text.IndexOfAny(['\r', '\n']);
        if (firstLineBreak < 0)
        {
            return string.Empty;
        }

        var bodyStart = SkipLineBreak(text, firstLineBreak);
        if (bodyStart < text.Length && (text[bodyStart] == '\r' || text[bodyStart] == '\n'))
        {
            bodyStart = SkipLineBreak(text, bodyStart);
        }

        return text[bodyStart..];
    }

    public static string? ValidateScriptFormat(string scriptBody)
    {
        var stack = new Stack<(char Expected, int Line, int Column)>();
        var line = 1;
        var column = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inBacktick = false;
        var inLineComment = false;
        var inBlockComment = false;
        var escaped = false;

        for (var i = 0; i < scriptBody.Length; i++)
        {
            var current = scriptBody[i];
            column++;

            if (current == '\r')
            {
                continue;
            }

            if (current == '\n')
            {
                line++;
                column = 0;
                inLineComment = false;
                continue;
            }

            if (inLineComment)
            {
                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && i + 1 < scriptBody.Length && scriptBody[i + 1] == '/')
                {
                    inBlockComment = false;
                    i++;
                    column++;
                }

                continue;
            }

            if (inSingleQuote || inDoubleQuote || inBacktick)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if ((inSingleQuote && current == '\'') ||
                    (inDoubleQuote && current == '"') ||
                    (inBacktick && current == '`'))
                {
                    inSingleQuote = false;
                    inDoubleQuote = false;
                    inBacktick = false;
                }

                continue;
            }

            if (current == '/' && i + 1 < scriptBody.Length)
            {
                if (scriptBody[i + 1] == '/')
                {
                    inLineComment = true;
                    i++;
                    column++;
                    continue;
                }

                if (scriptBody[i + 1] == '*')
                {
                    inBlockComment = true;
                    i++;
                    column++;
                    continue;
                }
            }

            switch (current)
            {
                case '\'':
                    inSingleQuote = true;
                    break;
                case '"':
                    inDoubleQuote = true;
                    break;
                case '`':
                    inBacktick = true;
                    break;
                case '(':
                    stack.Push((')', line, column));
                    break;
                case '[':
                    stack.Push((']', line, column));
                    break;
                case '{':
                    stack.Push(('}', line, column));
                    break;
                case ')':
                case ']':
                case '}':
                    if (stack.Count == 0)
                    {
                        return $"第 {line} 行，第 {column} 列附近多出“{current}”。";
                    }

                    var expected = stack.Pop();
                    if (expected.Expected != current)
                    {
                        return $"第 {line} 行，第 {column} 列附近应为“{expected.Expected}”，实际为“{current}”。";
                    }
                    break;
            }
        }

        if (inBlockComment)
        {
            return "存在未关闭的块注释。";
        }

        if (inSingleQuote || inDoubleQuote || inBacktick)
        {
            return "存在未关闭的字符串。";
        }

        if (stack.Count > 0)
        {
            var expected = stack.Peek();
            return $"第 {expected.Line} 行，第 {expected.Column} 列附近缺少“{expected.Expected}”。";
        }

        return null;
    }

    private static void AppendNodeSnapshot(StringBuilder builder, string title, IEnumerable<GraphNodeDefinition> nodes)
    {
        var nodeLines = nodes.Select(node =>
        {
            var parameters = SerializeParameters(node.Parameters);
            var summary = $"{node.Kind}: {node.Title} [{node.Id}]";
            return string.IsNullOrWhiteSpace(parameters)
                ? summary
                : $"{summary}{Environment.NewLine}{IndentLines(parameters, "  ")}";
        });
        AppendCommentBlock(builder, title, string.Join(Environment.NewLine, nodeLines));
    }

    private static void AppendCommentBlock(StringBuilder builder, string title, string content)
    {
        builder.AppendLine($"// {title}:");
        if (string.IsNullOrWhiteSpace(content))
        {
            builder.AppendLine("//   (none)");
            return;
        }

        foreach (var line in content.Replace("\r\n", "\n").Split('\n'))
        {
            builder.AppendLine($"//   {line}");
        }
    }

    private static string SerializeParameters(Dictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(Environment.NewLine, parameters.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string IndentLines(string text, string indent)
    {
        return string.Join(Environment.NewLine, text.Replace("\r\n", "\n").Split('\n').Select(line => indent + line));
    }

    private static string ToScriptString(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static string EnsureUniqueScriptPath(string directory, string fileName, string extension)
    {
        var candidate = Path.Combine(directory, fileName + extension);
        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{fileName}_{index}{extension}");
            index++;
        }

        return candidate;
    }

    private static string ToSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            builder.Append(invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch);
        }

        var result = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "TriggerScript" : result;
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static int SkipLineBreak(string text, int index)
    {
        if (index >= text.Length)
        {
            return index;
        }

        if (text[index] == '\r')
        {
            index++;
            if (index < text.Length && text[index] == '\n')
            {
                index++;
            }

            return index;
        }

        return text[index] == '\n' ? index + 1 : index;
    }
}
