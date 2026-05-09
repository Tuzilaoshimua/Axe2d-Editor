namespace Axe2DEditor.Core.Rules;

public sealed class FormulaDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string FormulaKind { get; set; } = "expression";

    public string Expression { get; set; } = "";

    public string GraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
