using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Projects;
using Axe2DEditor.Core.Stats;

namespace Axe2DEditor.Runtime;

internal sealed class RuntimeSession
{
    private readonly ProjectContext _context;

    public RuntimeSession(ProjectContext context)
    {
        _context = context;
        ActiveMap = _context.Project.AssetLibrary.Maps.FirstOrDefault();
        var playerNode = FindFirstPlayableNode(_context.Project.HierarchyTree);
        Player = CreatePlayerState(playerNode);
        SceneObjects = FlattenSceneObjects(_context.Project.HierarchyTree)
            .Where(node => !ReferenceEquals(node, playerNode))
            .Select(node => new RuntimeSceneObjectState(
                node.Name,
                node.Kind,
                node.Type,
                node.PositionX ?? 0f,
                node.PositionY ?? 0f,
                node.Rotation ?? 0f,
                node.Scale ?? 1f,
                node.CameraMode,
                node.CameraTarget,
                node.CameraSmooth,
                node.CameraZoom))
            .ToList();
        var sceneCamera = SceneObjects.FirstOrDefault(obj => string.Equals(obj.Category, "camera", StringComparison.OrdinalIgnoreCase));
        Camera = sceneCamera ?? new RuntimeSceneObjectState("RuntimeCamera", "item", "camera", Player.X, Player.Y, 0f, 1f, "followPlayer", string.Empty, 0f, 1f);
        HasExplicitCamera = sceneCamera is not null;
        Triggers = _context.Project.EventGraphs
            .SelectMany(graph => graph.Nodes.Where(node => string.Equals(node.Kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase))
                .Select(node => new RuntimeTriggerState(graph, node, BuildTriggerActions(graph, node))))
            .ToList();
        UpdateCamera();
        HeroStats = ResolveHeroStats();
        EnemyStats = ResolveEnemyStats();
    }

    public MapDefinition? ActiveMap { get; private set; }

    public RuntimePlayerState Player { get; }

    public IReadOnlyList<RuntimeSceneObjectState> SceneObjects { get; }

    public RuntimeSceneObjectState Camera { get; }

    public bool HasExplicitCamera { get; }

    public IReadOnlyList<RuntimeTriggerState> Triggers { get; }

    public IReadOnlyDictionary<string, double> HeroStats { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> EnemyStats { get; }

    public string LastTriggeredMessage { get; private set; } = "尚未触发事件。";

    public string LastDialogueMessage { get; private set; } = "暂无对话。";

    public string LastRewardMessage { get; private set; } = "暂无奖励。";

    public PointF CameraFocus { get; private set; }

    public string CameraLabel => HasExplicitCamera ? Camera.Name : "跟随玩家";

    public string CameraModeLabel => HasExplicitCamera
        ? (string.IsNullOrWhiteSpace(Camera.CameraMode) ? "fixed" : Camera.CameraMode)
        : "followPlayer";

    public float CameraZoom => HasExplicitCamera ? Math.Clamp(Camera.CameraZoom, 0.25f, 4f) : 1f;

    public RectangleF GetWorldBounds()
    {
        if (ActiveMap is null)
        {
            return new RectangleF(0, 0, 64, 64);
        }

        return new RectangleF(0, 0, Math.Max(8, ActiveMap.Width), Math.Max(8, ActiveMap.Height));
    }

    public void UpdateCamera()
    {
        var target = ResolveCameraTarget();
        var clampedTarget = ClampCameraTarget(target);
        if (!HasExplicitCamera)
        {
            CameraFocus = clampedTarget;
            return;
        }

        var smooth = Math.Clamp(Camera.CameraSmooth, 0f, 1f);
        if (smooth <= 0.001f)
        {
            CameraFocus = clampedTarget;
            return;
        }

        var nextFocus = new PointF(
            CameraFocus.X + (clampedTarget.X - CameraFocus.X) * smooth,
            CameraFocus.Y + (clampedTarget.Y - CameraFocus.Y) * smooth);
        CameraFocus = ClampCameraTarget(nextFocus);
    }

    public string TriggerByIndex(int index)
    {
        if (index < 0 || index >= Triggers.Count)
        {
            LastTriggeredMessage = "触发失败：无效的触发器索引。";
            return LastTriggeredMessage;
        }

        var trigger = Triggers[index];
        var eventType = string.IsNullOrWhiteSpace(trigger.EventType) ? "未设置事件" : trigger.EventType;
        var subject = string.IsNullOrWhiteSpace(trigger.Subject) ? "未设置对象" : trigger.Subject;
        var actionResult = ExecuteActions(trigger.Actions);
        LastTriggeredMessage = $"已触发：{trigger.TriggerName} | 事件={eventType} | 对象={subject} | {actionResult}";
        return LastTriggeredMessage;
    }

    public string TriggerTouchEventsForPlayer()
    {
        var touchedObjects = SceneObjects
            .Where(obj => Math.Abs(obj.X - Player.X) <= 1.0f && Math.Abs(obj.Y - Player.Y) <= 1.0f)
            .Select(obj => obj.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var touchTriggers = Triggers
            .Where(trigger =>
                string.Equals(trigger.EventType, "OnTouch", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(trigger.Subject) ||
                 string.Equals(trigger.Subject, "player", StringComparison.OrdinalIgnoreCase) ||
                 touchedObjects.Contains(trigger.Subject)))
            .ToList();

        if (touchTriggers.Count == 0)
        {
            return string.Empty;
        }

        var results = new List<string>();
        foreach (var trigger in touchTriggers)
        {
            results.Add(TriggerById(trigger.TriggerId));
        }

        return string.Join(Environment.NewLine, results);
    }

    public string TriggerAreaEventsForPlayer()
    {
        var areaTriggers = Triggers
            .Where(trigger =>
                string.Equals(trigger.EventType, "OnEnterArea", StringComparison.OrdinalIgnoreCase) &&
                IsPlayerInsideTriggerArea(trigger))
            .ToList();

        if (areaTriggers.Count == 0)
        {
            return string.Empty;
        }

        var results = new List<string>();
        foreach (var trigger in areaTriggers)
        {
            results.Add(TriggerById(trigger.TriggerId));
        }

        return string.Join(Environment.NewLine, results);
    }

    public string TriggerById(string triggerId)
    {
        var index = Triggers.ToList().FindIndex(trigger => string.Equals(trigger.TriggerId, triggerId, StringComparison.OrdinalIgnoreCase));
        return TriggerByIndex(index);
    }

    public RuntimeSceneObjectState? FindSceneObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return SceneObjects.FirstOrDefault(obj => string.Equals(obj.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void ClampPlayerToBounds()
    {
        var worldBounds = GetWorldBounds();
        Player.X = Math.Clamp(Player.X, 0, Math.Max(0, worldBounds.Width - 1));
        Player.Y = Math.Clamp(Player.Y, 0, Math.Max(0, worldBounds.Height - 1));
        UpdateCamera();
    }

    private static RuntimePlayerState CreatePlayerState(ProjectTreeNode? playerNode)
    {
        var x = playerNode?.PositionX ?? 4f;
        var y = playerNode?.PositionY ?? 4f;
        return new RuntimePlayerState(playerNode?.Name ?? "测试角色", x, y);
    }

    private IReadOnlyDictionary<string, double> ResolveHeroStats()
    {
        var unit = _context.Project.AssetLibrary.Units.FirstOrDefault(v => v.Traits.Contains("player", StringComparer.OrdinalIgnoreCase))
            ?? _context.Project.AssetLibrary.Units.FirstOrDefault(v => string.Equals(v.Id, "actor.hero", StringComparison.OrdinalIgnoreCase))
            ?? _context.Project.AssetLibrary.Units.FirstOrDefault();
        if (unit is null)
        {
            return StatResolver.CreateDefaultStatMap(_context.Project.AssetLibrary.Stats);
        }

        return StatResolver.ResolveEntityStats(unit, _context.Project.AssetLibrary.Stats);
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> ResolveEnemyStats()
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in _context.Project.AssetLibrary.Units.Where(v => v.Traits.Contains("enemy", StringComparer.OrdinalIgnoreCase)))
        {
            result[unit.Id] = StatResolver.ResolveEntityStats(unit, _context.Project.AssetLibrary.Stats);
        }

        return result;
    }

    private static ProjectTreeNode? FindFirstPlayableNode(IEnumerable<ProjectTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Kind, "item", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetNodeCategory(node), "object", StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindFirstPlayableNode(node.Children);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static string GetNodeCategory(ProjectTreeNode node)
    {
        if (string.Equals(node.Type, "camera", StringComparison.OrdinalIgnoreCase))
        {
            return "camera";
        }

        if (string.Equals(node.Type, "ui", StringComparison.OrdinalIgnoreCase))
        {
            return "ui";
        }

        if (node.Name.Contains("camera", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Contains("相机", StringComparison.OrdinalIgnoreCase))
        {
            return "camera";
        }

        if (node.Name.Contains("ui", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Contains("canvas", StringComparison.OrdinalIgnoreCase))
        {
            return "ui";
        }

        return "object";
    }

    private static IEnumerable<ProjectTreeNode> FlattenSceneObjects(IEnumerable<ProjectTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Kind, "item", StringComparison.OrdinalIgnoreCase))
            {
                yield return node;
            }

            foreach (var child in FlattenSceneObjects(node.Children))
            {
                yield return child;
            }
        }
    }

    private PointF ResolveCameraTarget()
    {
        if (!HasExplicitCamera)
        {
            return new PointF(Player.X, Player.Y);
        }

        var mode = string.IsNullOrWhiteSpace(Camera.CameraMode) ? "fixed" : Camera.CameraMode;
        if (string.Equals(mode, "followPlayer", StringComparison.OrdinalIgnoreCase))
        {
            return new PointF(Player.X, Player.Y);
        }

        if (string.Equals(mode, "followTarget", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(Camera.CameraTarget, "player", StringComparison.OrdinalIgnoreCase))
            {
                return new PointF(Player.X, Player.Y);
            }

            var targetObject = FindSceneObject(Camera.CameraTarget);
            if (targetObject is not null)
            {
                return new PointF(targetObject.X, targetObject.Y);
            }
        }

        return new PointF(Camera.X, Camera.Y);
    }

    private PointF ClampCameraTarget(PointF target)
    {
        var bounds = GetWorldBounds();
        var zoom = Math.Max(0.25f, CameraZoom);
        var halfViewWidth = bounds.Width / (2f * zoom);
        var halfViewHeight = bounds.Height / (2f * zoom);
        var minX = Math.Min(bounds.Width - 1f, halfViewWidth);
        var minY = Math.Min(bounds.Height - 1f, halfViewHeight);
        var maxX = Math.Max(minX, bounds.Width - halfViewWidth);
        var maxY = Math.Max(minY, bounds.Height - halfViewHeight);
        return new PointF(
            Math.Clamp(target.X, minX, maxX),
            Math.Clamp(target.Y, minY, maxY));
    }

    private string ExecuteActions(IReadOnlyList<RuntimeActionState> actions)
    {
        if (actions.Count == 0)
        {
            return "未配置动作。";
        }

        var results = new List<string>();
        foreach (var action in actions)
        {
            var result = ExecuteAction(action);
            if (!string.IsNullOrWhiteSpace(result))
            {
                results.Add(result);
            }
        }

        return results.Count == 0 ? "动作已执行。" : string.Join("；", results);
    }

    private string ExecuteAction(RuntimeActionState action)
    {
        return action.Template switch
        {
            "dialogue" => ExecuteDialogueAction(action),
            "teleport" => ExecuteTeleportAction(action),
            "changeMap" => ExecuteChangeMapAction(action),
            "giveItem" => ExecuteGiveItemAction(action),
            "parallel" => "并行动作节点已展开执行",
            "animation" => "动画动作已记录",
            "chest" => "宝箱动作已记录",
            "keyInteract" => "按键交互动作已记录",
            _ => $"未实现模板：{action.Template}"
        };
    }

    private string ExecuteDialogueAction(RuntimeActionState action)
    {
        LastDialogueMessage = string.IsNullOrWhiteSpace(action.Detail)
            ? $"触发对话：{action.Title}"
            : $"触发对话：{action.Detail}";
        return LastDialogueMessage;
    }

    private string ExecuteTeleportAction(RuntimeActionState action)
    {
        var coordinates = GetActionParameter(action, "target", action.Detail).Replace('，', ',');
        var parts = coordinates.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 &&
            float.TryParse(parts[0], out var x) &&
            float.TryParse(parts[1], out var y))
        {
            Player.X = x;
            Player.Y = y;
            ClampPlayerToBounds();
            return $"已传送到 {Player.X:0.0},{Player.Y:0.0}";
        }

        ClampPlayerToBounds();
        return "传送动作缺少坐标，位置保持当前值。";
    }

    private string ExecuteChangeMapAction(RuntimeActionState action)
    {
        var mapToken = GetActionParameter(action, "mapId", action.Detail);
        if (string.IsNullOrWhiteSpace(mapToken))
        {
            return "切换地图动作未提供目标地图。";
        }

        var targetMap = _context.Project.AssetLibrary.Maps.FirstOrDefault(map =>
            string.Equals(map.Id, mapToken, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(map.DisplayName, mapToken, StringComparison.OrdinalIgnoreCase));
        if (targetMap is null)
        {
            return $"未找到地图：{mapToken}";
        }

        ActiveMap = targetMap;
        ClampPlayerToBounds();
        return $"已切换地图：{targetMap.DisplayName}";
    }

    private string ExecuteGiveItemAction(RuntimeActionState action)
    {
        var rewardText = GetActionParameter(action, "itemId", action.Detail);
        LastRewardMessage = string.IsNullOrWhiteSpace(rewardText)
            ? $"获得奖励：{action.Title}"
            : $"获得奖励：{rewardText}";
        return LastRewardMessage;
    }

    private static IReadOnlyList<RuntimeActionState> BuildTriggerActions(EventGraphDefinition graph, GraphNodeDefinition trigger)
    {
        var analysis = EventGraphAnalysisService.AnalyzeTrigger(graph, trigger);
        return analysis.Actions
            .Select(action => new RuntimeActionState(
                action.Id,
                string.IsNullOrWhiteSpace(action.Title) ? action.Id : action.Title,
                action.Parameters.TryGetValue("template", out var template) ? template : string.Empty,
                action.Parameters.TryGetValue("detail", out var detail) ? detail : string.Empty,
                new Dictionary<string, string>(action.Parameters, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    private bool IsPlayerInsideTriggerArea(RuntimeTriggerState trigger)
    {
        var shape = trigger.Parameters.TryGetValue("shape", out var shapeValue) ? shapeValue : "box";
        var width = ParseFloat(trigger.Parameters, "width", 96f);
        var height = ParseFloat(trigger.Parameters, "height", 96f);
        var radius = ParseFloat(trigger.Parameters, "radius", 32f);
        var centerX = Player.X;
        var centerY = Player.Y;

        if (string.Equals(trigger.Subject, "specific", StringComparison.OrdinalIgnoreCase))
        {
            var anchor = SceneObjects.FirstOrDefault(obj => string.Equals(obj.Name, trigger.TriggerName, StringComparison.OrdinalIgnoreCase));
            if (anchor is not null)
            {
                centerX = anchor.X;
                centerY = anchor.Y;
            }
        }

        if (string.Equals(shape, "circle", StringComparison.OrdinalIgnoreCase))
        {
            var dx = Player.X - centerX;
            var dy = Player.Y - centerY;
            var distanceSquared = dx * dx + dy * dy;
            var normalizedRadius = Math.Max(1f, radius / 32f);
            return distanceSquared <= normalizedRadius * normalizedRadius;
        }

        var halfWidth = Math.Max(1f, width / 64f) * 0.5f;
        var halfHeight = Math.Max(1f, height / 64f) * 0.5f;
        return Math.Abs(Player.X - centerX) <= halfWidth && Math.Abs(Player.Y - centerY) <= halfHeight;
    }

    private static float ParseFloat(IReadOnlyDictionary<string, string> parameters, string key, float fallback)
    {
        return parameters.TryGetValue(key, out var value) && float.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static string GetActionParameter(RuntimeActionState action, string key, string fallback)
    {
        return action.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}

internal sealed class RuntimePlayerState
{
    public RuntimePlayerState(string name, float x, float y)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "测试角色" : name;
        X = x;
        Y = y;
    }

    public string Name { get; }

    public float X { get; set; }

    public float Y { get; set; }
}

internal sealed class RuntimeTriggerState
{
    public RuntimeTriggerState(EventGraphDefinition graph, GraphNodeDefinition trigger, IReadOnlyList<RuntimeActionState> actions)
    {
        GraphId = graph.Id;
        GraphName = string.IsNullOrWhiteSpace(graph.DisplayName) ? graph.Id : graph.DisplayName;
        TriggerId = trigger.Id;
        TriggerName = string.IsNullOrWhiteSpace(trigger.Title) ? trigger.Id : trigger.Title;
        EventType = trigger.Parameters.TryGetValue("event", out var value) ? value : string.Empty;
        Subject = trigger.Parameters.TryGetValue("subject", out var subject) ? subject : string.Empty;
        Parameters = new Dictionary<string, string>(trigger.Parameters, StringComparer.OrdinalIgnoreCase);
        Actions = actions;
    }

    public string GraphId { get; }

    public string GraphName { get; }

    public string TriggerId { get; }

    public string TriggerName { get; }

    public string EventType { get; }

    public string Subject { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }

    public IReadOnlyList<RuntimeActionState> Actions { get; }
}

internal sealed class RuntimeSceneObjectState
{
    public RuntimeSceneObjectState(string name, string kind, string type, float x, float y, float rotation, float scale, string cameraMode, string cameraTarget, float? cameraSmooth, float? cameraZoom)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "未命名对象" : name;
        Kind = string.IsNullOrWhiteSpace(kind) ? "item" : kind;
        Type = string.IsNullOrWhiteSpace(type) ? string.Empty : type;
        X = x;
        Y = y;
        Rotation = rotation;
        Scale = scale;
        CameraMode = string.IsNullOrWhiteSpace(cameraMode) ? string.Empty : cameraMode;
        CameraTarget = string.IsNullOrWhiteSpace(cameraTarget) ? string.Empty : cameraTarget;
        CameraSmooth = cameraSmooth ?? 0f;
        CameraZoom = cameraZoom ?? 1f;
    }

    public string Name { get; }

    public string Kind { get; }

    public string Type { get; }

    public string Category =>
        string.Equals(Type, "camera", StringComparison.OrdinalIgnoreCase) ? "camera" :
        string.Equals(Type, "ui", StringComparison.OrdinalIgnoreCase) ? "ui" :
        Name.Contains("camera", StringComparison.OrdinalIgnoreCase) ? "camera" :
        Name.Contains("ui", StringComparison.OrdinalIgnoreCase) || Name.Contains("canvas", StringComparison.OrdinalIgnoreCase) ? "ui" :
        "object";

    public float X { get; }

    public float Y { get; }

    public float Rotation { get; }

    public float Scale { get; }

    public string CameraMode { get; }

    public string CameraTarget { get; }

    public float CameraSmooth { get; }

    public float CameraZoom { get; }
}

internal sealed class RuntimeActionState
{
    public RuntimeActionState(string id, string title, string template, string detail, IReadOnlyDictionary<string, string> parameters)
    {
        Id = id;
        Title = title;
        Template = string.IsNullOrWhiteSpace(template) ? "unknown" : template;
        Detail = detail;
        Parameters = parameters;
    }

    public string Id { get; }

    public string Title { get; }

    public string Template { get; }

    public string Detail { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }
}
