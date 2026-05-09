namespace Axe2DEditor.Core.Tactics;

public sealed class ActionRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ActionPointStatKey { get; set; } = "";

    public string MovePointStatKey { get; set; } = "";

    public int DefaultActionPoints { get; set; } = 1;

    public int DefaultMovePoints { get; set; } = 4;

    public bool MoveConsumesAction { get; set; }

    public bool AttackConsumesAction { get; set; } = true;

    public bool CanAttackAfterMove { get; set; } = true;

    public bool CanMoveAfterAttack { get; set; }

    public bool WaitEndsTurn { get; set; } = true;

    public bool BuiltIn { get; set; }
}
