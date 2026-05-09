using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Core.Graphs;

public static class GraphNodeCatalog
{
    private static readonly IReadOnlyList<GraphFieldOptionDefinition> TriggerEventOptions =
    [
        new GraphFieldOptionDefinition { Value = "OnInteract", TextKey = "graph.trigger.event.interact" },
        new GraphFieldOptionDefinition { Value = "OnEnterArea", TextKey = "graph.trigger.event.enterArea" },
        new GraphFieldOptionDefinition { Value = "OnTouch", TextKey = "graph.trigger.event.touch" },
        new GraphFieldOptionDefinition { Value = "OnSkillCast", TextKey = "graph.trigger.event.skillCast" },
        new GraphFieldOptionDefinition { Value = "OnDamageDealt", TextKey = "graph.trigger.event.damageDealt" }
    ];

    private static readonly IReadOnlyList<GraphFieldOptionDefinition> TriggerSubjectOptions =
    [
        new GraphFieldOptionDefinition { Value = "player", TextKey = "graph.trigger.subject.player" },
        new GraphFieldOptionDefinition { Value = "any", TextKey = "graph.trigger.subject.any" },
        new GraphFieldOptionDefinition { Value = "specific", TextKey = "graph.trigger.subject.specific" }
    ];

    private static readonly IReadOnlyList<GraphFieldOptionDefinition> TriggerAreaSourceOptions =
    [
        new GraphFieldOptionDefinition { Value = "self", TextKey = "graph.trigger.area.self" },
        new GraphFieldOptionDefinition { Value = "mapTrigger", TextKey = "graph.trigger.area.mapTrigger" },
        new GraphFieldOptionDefinition { Value = "custom", TextKey = "graph.trigger.area.custom" }
    ];

    private static readonly IReadOnlyList<GraphFieldOptionDefinition> TriggerShapeOptions =
    [
        new GraphFieldOptionDefinition { Value = "box", TextKey = "graph.trigger.shape.box" },
        new GraphFieldOptionDefinition { Value = "circle", TextKey = "graph.trigger.shape.circle" }
    ];

    private static readonly IReadOnlyList<GraphFieldOptionDefinition> ConditionTemplateOptions =
    [
        new GraphFieldOptionDefinition { Value = "switch", TextKey = "graph.template.switch" },
        new GraphFieldOptionDefinition { Value = "variable", TextKey = "graph.template.variable" },
        new GraphFieldOptionDefinition { Value = "state", TextKey = "graph.template.state" },
        new GraphFieldOptionDefinition { Value = "actor", TextKey = "graph.template.actor" }
    ];

    private static readonly IReadOnlyList<GraphFieldOptionDefinition> ActionTemplateOptions =
    [
        new GraphFieldOptionDefinition { Value = "changeMap", TextKey = "graph.template.changeMap" },
        new GraphFieldOptionDefinition { Value = "animation", TextKey = "graph.template.animation" },
        new GraphFieldOptionDefinition { Value = "giveItem", TextKey = "graph.template.giveItem" },
        new GraphFieldOptionDefinition { Value = "dialogue", TextKey = "graph.template.dialogue" },
        new GraphFieldOptionDefinition { Value = "teleport", TextKey = "graph.template.teleport" },
        new GraphFieldOptionDefinition { Value = "chest", TextKey = "graph.template.chest" },
        new GraphFieldOptionDefinition { Value = "keyInteract", TextKey = "graph.template.keyInteract" },
        new GraphFieldOptionDefinition { Value = "parallel", TextKey = "graph.template.parallel" }
    ];

    private static readonly GraphNodeKindDefinition TriggerDefinition = new()
    {
        Kind = NodeKinds.Trigger,
        DisplayNameKey = "graph.node.trigger",
        DisplayName = "触发器",
        AccentColor = "f4a742",
        Inputs =
        [
            new NodePortDefinition { Name = "event", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "subject", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "areaSource", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "shape", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "width", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Int },
            new NodePortDefinition { Name = "height", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Int },
            new NodePortDefinition { Name = "radius", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Int },
            new NodePortDefinition { Name = "once", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Bool },
            new NodePortDefinition { Name = "runOnce", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Bool },
            new NodePortDefinition { Name = "enabled", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Bool },
            new NodePortDefinition { Name = "scriptPath", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.AssetRef }
        ],
        Outputs =
        [
            new NodePortDefinition { Name = "flowOut", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.Flow },
            new NodePortDefinition { Name = "event", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "subject", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "enabled", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.Bool }
        ],
        Fields =
        [
            new GraphNodeFieldDefinition { Key = "event", LabelKey = "graph.trigger.eventType", Label = "事件", ValueType = NodePortValueTypes.String, CanConnectInput = true, CanConnectOutput = true, PortName = "event", OptionSet = "trigger.event" },
            new GraphNodeFieldDefinition { Key = "subject", LabelKey = "graph.trigger.subject", Label = "对象", ValueType = NodePortValueTypes.String, CanConnectInput = true, CanConnectOutput = true, PortName = "subject", OptionSet = "trigger.subject" },
            new GraphNodeFieldDefinition { Key = "areaSource", LabelKey = "graph.trigger.areaSource", Label = "区域来源", ValueType = NodePortValueTypes.String, OptionSet = "trigger.areaSource" },
            new GraphNodeFieldDefinition { Key = "shape", LabelKey = "graph.trigger.shape", Label = "形状", ValueType = NodePortValueTypes.String, OptionSet = "trigger.shape" },
            new GraphNodeFieldDefinition { Key = "width", LabelKey = "graph.trigger.width", Label = "宽度", ValueType = NodePortValueTypes.Int },
            new GraphNodeFieldDefinition { Key = "height", LabelKey = "graph.trigger.height", Label = "高度", ValueType = NodePortValueTypes.Int },
            new GraphNodeFieldDefinition { Key = "radius", LabelKey = "graph.trigger.radius", Label = "半径", ValueType = NodePortValueTypes.Int },
            new GraphNodeFieldDefinition { Key = "once", LabelKey = "graph.trigger.once", Label = "只触发一次", ValueType = NodePortValueTypes.Bool },
            new GraphNodeFieldDefinition { Key = "runOnce", LabelKey = "graph.trigger.once", Label = "执行一次", ValueType = NodePortValueTypes.Bool },
            new GraphNodeFieldDefinition { Key = "enabled", LabelKey = "common.enabled", Label = "启用", ValueType = NodePortValueTypes.Bool, CanConnectInput = true, CanConnectOutput = true, PortName = "enabled" },
            new GraphNodeFieldDefinition { Key = "scriptPath", LabelKey = "graph.field.scriptPath", Label = "脚本路径", ValueType = NodePortValueTypes.AssetRef, VisibleInScriptModeOnly = true }
        ]
    };

    private static readonly GraphNodeKindDefinition ConditionDefinition = new()
    {
        Kind = NodeKinds.Condition,
        DisplayNameKey = "graph.node.condition",
        DisplayName = "条件",
        AccentColor = "58a6ff",
        Inputs =
        [
            new NodePortDefinition { Name = "flowIn", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Flow },
            new NodePortDefinition { Name = "template", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "detail", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "enabled", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Bool }
        ],
        Outputs =
        [
            new NodePortDefinition { Name = "flowOut", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.Flow },
            new NodePortDefinition { Name = "passed", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.Bool }
        ],
        Fields =
        [
            new GraphNodeFieldDefinition { Key = "template", LabelKey = "graph.field.template", Label = "模板", ValueType = NodePortValueTypes.String, CanConnectInput = true, CanConnectOutput = true, PortName = "template", OptionSet = "condition.template" },
            new GraphNodeFieldDefinition { Key = "detail", LabelKey = "graph.field.detail", Label = "说明", ValueType = NodePortValueTypes.String },
            new GraphNodeFieldDefinition { Key = "enabled", LabelKey = "common.enabled", Label = "启用", ValueType = NodePortValueTypes.Bool }
        ]
    };

    private static readonly GraphNodeKindDefinition ActionDefinition = new()
    {
        Kind = NodeKinds.Action,
        DisplayNameKey = "graph.node.action",
        DisplayName = "动作",
        AccentColor = "4cc38a",
        Inputs =
        [
            new NodePortDefinition { Name = "flowIn", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Flow },
            new NodePortDefinition { Name = "template", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "detail", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.String },
            new NodePortDefinition { Name = "enabled", Direction = NodePortDirections.Input, ValueType = NodePortValueTypes.Bool }
        ],
        Outputs =
        [
            new NodePortDefinition { Name = "flowOut", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.Flow },
            new NodePortDefinition { Name = "result", Direction = NodePortDirections.Output, ValueType = NodePortValueTypes.String }
        ],
        Fields =
        [
            new GraphNodeFieldDefinition { Key = "template", LabelKey = "graph.field.template", Label = "模板", ValueType = NodePortValueTypes.String, CanConnectInput = true, CanConnectOutput = true, PortName = "template", OptionSet = "action.template" },
            new GraphNodeFieldDefinition { Key = "detail", LabelKey = "graph.field.detail", Label = "说明", ValueType = NodePortValueTypes.String },
            new GraphNodeFieldDefinition { Key = "enabled", LabelKey = "common.enabled", Label = "启用", ValueType = NodePortValueTypes.Bool }
        ]
    };

    public static GraphNodeKindDefinition GetDefinition(string kind)
    {
        return kind switch
        {
            NodeKinds.Trigger => TriggerDefinition,
            NodeKinds.Condition => ConditionDefinition,
            NodeKinds.Action => ActionDefinition,
            _ => new GraphNodeKindDefinition
            {
                Kind = kind,
                DisplayName = kind,
                AccentColor = "808080"
            }
        };
    }

    public static string GetKindDisplayName(LocalizationService localization, string kind)
    {
        var definition = GetDefinition(kind);
        return string.IsNullOrWhiteSpace(definition.DisplayNameKey)
            ? definition.DisplayName
            : localization.T(definition.DisplayNameKey, definition.DisplayName);
    }

    public static GraphNodeFieldDefinition? GetFieldDefinition(GraphNodeDefinition node, string fieldKey)
    {
        var definition = GetDefinition(node.Kind);
        return definition.Fields.FirstOrDefault(field =>
            string.Equals(field.Key, fieldKey, StringComparison.OrdinalIgnoreCase) &&
            (!field.VisibleInScriptModeOnly || IsScriptMode(node)));
    }

    public static IEnumerable<GraphNodeFieldDefinition> GetVisibleFields(GraphNodeDefinition node)
    {
        var definition = GetDefinition(node.Kind);
        var isScriptMode = IsScriptMode(node);
        return definition.Fields.Where(field => !field.VisibleInScriptModeOnly || isScriptMode);
    }

    public static IReadOnlyList<GraphFieldOptionDefinition> GetFieldOptions(string kind, string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return [];
        }

        if (string.Equals(kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase))
        {
            return fieldKey switch
            {
                "event" => TriggerEventOptions,
                "subject" => TriggerSubjectOptions,
                "areaSource" => TriggerAreaSourceOptions,
                "shape" => TriggerShapeOptions,
                _ => []
            };
        }

        if (string.Equals(kind, NodeKinds.Condition, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fieldKey, "template", StringComparison.OrdinalIgnoreCase))
        {
            return ConditionTemplateOptions;
        }

        if (string.Equals(kind, NodeKinds.Action, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fieldKey, "template", StringComparison.OrdinalIgnoreCase))
        {
            return ActionTemplateOptions;
        }

        return [];
    }

    public static string GetFieldDisplayValue(LocalizationService localization, string kind, string fieldKey, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var option = GetFieldOptions(kind, fieldKey).FirstOrDefault(item => string.Equals(item.Value, rawValue, StringComparison.OrdinalIgnoreCase));
        if (option is not null)
        {
            return ResolveOptionText(localization, option);
        }

        return GetDisplayValue(localization, fieldKey, rawValue);
    }

    private static bool IsScriptMode(GraphNodeDefinition node)
    {
        return string.Equals(GetParameter(node.Parameters, "mode", string.Empty), "script", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public static string GetDisplayValue(LocalizationService localization, string key, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        if (string.Equals(key, "event", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue switch
            {
                "OnInteract" => localization.T("graph.trigger.event.interact"),
                "OnEnterArea" => localization.T("graph.trigger.event.enterArea"),
                "OnTouch" => localization.T("graph.trigger.event.touch"),
                "OnSkillCast" => localization.T("graph.trigger.event.skillCast"),
                "OnDamageDealt" => localization.T("graph.trigger.event.damageDealt"),
                _ => rawValue
            };
        }

        if (string.Equals(key, "subject", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue switch
            {
                "player" => localization.T("graph.trigger.subject.player"),
                "any" => localization.T("graph.trigger.subject.any"),
                "specific" => localization.T("graph.trigger.subject.specific"),
                _ => rawValue
            };
        }

        if (string.Equals(key, "areaSource", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue switch
            {
                "self" => localization.T("graph.trigger.area.self"),
                "mapTrigger" => localization.T("graph.trigger.area.mapTrigger"),
                "custom" => localization.T("graph.trigger.area.custom"),
                _ => rawValue
            };
        }

        if (string.Equals(key, "shape", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue switch
            {
                "box" => localization.T("graph.trigger.shape.box"),
                "circle" => localization.T("graph.trigger.shape.circle"),
                _ => rawValue
            };
        }

        if (string.Equals(key, "once", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "runOnce", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "enabled", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase) ? "是" : "否";
        }

        if (string.Equals(key, "template", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue switch
            {
                "switch" => localization.T("graph.template.switch"),
                "variable" => localization.T("graph.template.variable"),
                "state" => localization.T("graph.template.state"),
                "actor" => localization.T("graph.template.actor"),
                "changeMap" => localization.T("graph.template.changeMap"),
                "animation" => localization.T("graph.template.animation"),
                "giveItem" => localization.T("graph.template.giveItem"),
                "dialogue" => localization.T("graph.template.dialogue"),
                "teleport" => localization.T("graph.template.teleport"),
                "chest" => localization.T("graph.template.chest"),
                "keyInteract" => localization.T("graph.template.keyInteract"),
                "parallel" => localization.T("graph.template.parallel"),
                _ => rawValue
            };
        }

        return rawValue;
    }

    private static string ResolveOptionText(LocalizationService localization, GraphFieldOptionDefinition option)
    {
        return string.IsNullOrWhiteSpace(option.TextKey)
            ? option.Text
            : localization.T(option.TextKey, option.Text);
    }
}
