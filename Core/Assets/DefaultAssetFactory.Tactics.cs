using Axe2DEditor.Core.Tactics;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    public static List<TacticalGridRuleDefinition> CreateDefaultTacticalGridRules()
    {
        return
        [
            new TacticalGridRuleDefinition
            {
                Id = "tactics.grid.square",
                DisplayName = "方格移动规则",
                DisplayNameKey = "tactics.grid.square.name",
                Description = "四方向方格移动规则，可用于回合制关卡、棋盘副本、潜入巡逻或区域解谜。",
                DescriptionKey = "tactics.grid.square.description",
                GridType = "square",
                TileSize = 32,
                MovementMetric = "manhattan",
                AllowDiagonalMove = false,
                HeightEnabled = true,
                ZoneOfControlEnabled = true,
                BuiltIn = true
            },
            new TacticalGridRuleDefinition
            {
                Id = "tactics.grid.hex",
                DisplayName = "六边形移动规则",
                DisplayNameKey = "tactics.grid.hex.name",
                Description = "六边形移动规则，适合需要稳定相邻关系和无斜向歧义的地图。",
                DescriptionKey = "tactics.grid.hex.description",
                GridType = "hex",
                TileSize = 32,
                MovementMetric = "hex",
                ZoneOfControlEnabled = true,
                BuiltIn = true
            }
        ];
    }

    public static List<TerrainRuleDefinition> CreateDefaultTerrainRules()
    {
        return
        [
            new TerrainRuleDefinition
            {
                Id = "terrain.plain",
                DisplayName = "平原",
                DisplayNameKey = "terrain.plain.name",
                Description = "标准可通行地形，不提供额外修正。",
                DescriptionKey = "terrain.plain.description",
                TerrainTag = "plain",
                MovementCost = 1,
                DamageModifier = 1,
                BuiltIn = true
            },
            new TerrainRuleDefinition
            {
                Id = "terrain.forest",
                DisplayName = "森林",
                DisplayNameKey = "terrain.forest.name",
                Description = "移动稍慢，但提供防御和闪避加成。",
                DescriptionKey = "terrain.forest.description",
                TerrainTag = "forest",
                MovementCost = 2,
                DefenseBonus = 0.15,
                EvasionBonus = 0.2,
                DamageModifier = 1,
                BuiltIn = true
            },
            new TerrainRuleDefinition
            {
                Id = "terrain.mountain",
                DisplayName = "山地",
                DisplayNameKey = "terrain.mountain.name",
                Description = "移动消耗高，提供更好的防御修正。",
                DescriptionKey = "terrain.mountain.description",
                TerrainTag = "mountain",
                MovementCost = 3,
                DefenseBonus = 0.3,
                EvasionBonus = 0.1,
                DamageModifier = 1,
                BuiltIn = true
            },
            new TerrainRuleDefinition
            {
                Id = "terrain.water",
                DisplayName = "水域",
                DisplayNameKey = "terrain.water.name",
                Description = "默认阻挡移动，拥有游泳或飞行标签的单位可以由运行时放行。",
                DescriptionKey = "terrain.water.description",
                TerrainTag = "water",
                MovementCost = 2,
                BlocksMovement = true,
                DamageModifier = 1,
                AllowedUnitTags = ["swimmer", "flying"],
                BuiltIn = true
            }
        ];
    }

    public static List<TurnRuleDefinition> CreateDefaultTurnRules()
    {
        return
        [
            new TurnRuleDefinition
            {
                Id = "turn.side.standard",
                DisplayName = "阵营回合",
                DisplayNameKey = "turn.side.standard.name",
                Description = "一个阵营的单位整队行动后切换到下一个阵营。",
                DescriptionKey = "turn.side.standard.description",
                TurnMode = "sideTurn",
                MaxRounds = 0,
                ActionRefreshMode = "turnStart",
                InitiativeStatKey = "moveSpeed",
                AllowWait = true,
                AllowUndoMove = true,
                BuiltIn = true
            },
            new TurnRuleDefinition
            {
                Id = "turn.unit.speed",
                DisplayName = "速度排序行动",
                DisplayNameKey = "turn.unit.speed.name",
                Description = "按速度属性决定单位行动顺序，适合回合制 RPG 和小队战斗。",
                DescriptionKey = "turn.unit.speed.description",
                TurnMode = "speedOrder",
                ActionRefreshMode = "unitTurnStart",
                InitiativeStatKey = "moveSpeed",
                AllowWait = true,
                BuiltIn = true
            }
        ];
    }

    public static List<ActionRuleDefinition> CreateDefaultActionRules()
    {
        return
        [
            new ActionRuleDefinition
            {
                Id = "action.tactics.standard",
                DisplayName = "标准回合行动",
                DisplayNameKey = "action.tactics.standard.name",
                Description = "每回合可移动并攻击一次，攻击后结束行动。",
                DescriptionKey = "action.tactics.standard.description",
                DefaultActionPoints = 1,
                DefaultMovePoints = 4,
                MoveConsumesAction = false,
                AttackConsumesAction = true,
                CanAttackAfterMove = true,
                CanMoveAfterAttack = false,
                WaitEndsTurn = true,
                BuiltIn = true
            },
            new ActionRuleDefinition
            {
                Id = "action.tactics.ap",
                DisplayName = "行动点规则",
                DisplayNameKey = "action.tactics.ap.name",
                Description = "移动、攻击和技能都可按行动点消耗，适合更自由的回合行动。",
                DescriptionKey = "action.tactics.ap.description",
                ActionPointStatKey = "actionPoint",
                MovePointStatKey = "movePoint",
                DefaultActionPoints = 2,
                DefaultMovePoints = 5,
                MoveConsumesAction = true,
                AttackConsumesAction = true,
                CanAttackAfterMove = true,
                CanMoveAfterAttack = true,
                WaitEndsTurn = false,
                BuiltIn = true
            }
        ];
    }

    public static List<TacticalRangeDefinition> CreateDefaultTacticalRanges()
    {
        return
        [
            new TacticalRangeDefinition
            {
                Id = "range.melee.adjacent",
                DisplayName = "近战相邻",
                DisplayNameKey = "range.melee.adjacent.name",
                Description = "只能攻击相邻一格目标。",
                DescriptionKey = "range.melee.adjacent.description",
                RangeShape = "diamond",
                MinRange = 1,
                MaxRange = 1,
                AreaShape = "single",
                RequiredTargetTags = ["unit"],
                BuiltIn = true
            },
            new TacticalRangeDefinition
            {
                Id = "range.bow.standard",
                DisplayName = "弓箭射程",
                DisplayNameKey = "range.bow.standard.name",
                Description = "可攻击 2 到 3 格外目标，要求视线不被阻挡。",
                DescriptionKey = "range.bow.standard.description",
                RangeShape = "diamond",
                MinRange = 2,
                MaxRange = 3,
                AreaShape = "single",
                RequiresLineOfSight = true,
                BuiltIn = true
            },
            new TacticalRangeDefinition
            {
                Id = "range.heal.nearby",
                DisplayName = "近距离治疗",
                DisplayNameKey = "range.heal.nearby.name",
                Description = "治疗 1 到 2 格内友方，可包含自身。",
                DescriptionKey = "range.heal.nearby.description",
                RangeShape = "diamond",
                MinRange = 0,
                MaxRange = 2,
                AreaShape = "single",
                CanTargetSelf = true,
                CanTargetAlly = true,
                CanTargetEnemy = false,
                RequiredTargetTags = ["unit"],
                BuiltIn = true
            },
            new TacticalRangeDefinition
            {
                Id = "range.area.cross",
                DisplayName = "十字范围",
                DisplayNameKey = "range.area.cross.name",
                Description = "命中目标格和上下左右相邻格，适合战术范围技能。",
                DescriptionKey = "range.area.cross.description",
                RangeShape = "diamond",
                MinRange = 1,
                MaxRange = 3,
                AreaShape = "cross",
                AreaRadius = 1,
                CanTargetAlly = true,
                CanTargetEnemy = true,
                RequiredTargetTags = ["unit"],
                BuiltIn = true
            }
        ];
    }

    public static List<ObjectiveRuleDefinition> CreateDefaultObjectiveRules()
    {
        return
        [
            new ObjectiveRuleDefinition
            {
                Id = "objective.defeatAllEnemies",
                DisplayName = "全灭敌人",
                DisplayNameKey = "objective.defeatAllEnemies.name",
                Description = "击败所有带敌人标签的单位后胜利。",
                DescriptionKey = "objective.defeatAllEnemies.description",
                ObjectiveType = "defeatAll",
                IsVictoryCondition = true,
                TargetUnitTags = ["enemy"],
                BuiltIn = true
            },
            new ObjectiveRuleDefinition
            {
                Id = "objective.protectRescueTarget",
                DisplayName = "保护营救目标",
                DisplayNameKey = "objective.protectRescueTarget.name",
                Description = "营救目标死亡时失败，适合护送和救援关卡。",
                DescriptionKey = "objective.protectRescueTarget.description",
                ObjectiveType = "protectUnit",
                IsVictoryCondition = false,
                TargetUnitTags = ["rescueTarget"],
                BuiltIn = true
            },
            new ObjectiveRuleDefinition
            {
                Id = "objective.capturePoint",
                DisplayName = "占领据点",
                DisplayNameKey = "objective.capturePoint.name",
                Description = "占领指定区域后胜利，区域由地图或事件提供标签。",
                DescriptionKey = "objective.capturePoint.description",
                ObjectiveType = "capturePoint",
                IsVictoryCondition = true,
                TargetAreaTags = ["capturePoint"],
                RequiredCount = 1,
                BuiltIn = true
            },
            new ObjectiveRuleDefinition
            {
                Id = "objective.surviveRounds",
                DisplayName = "坚持回合",
                DisplayNameKey = "objective.surviveRounds.name",
                Description = "坚持到指定回合后胜利，可用于生存、护送、守点或事件驱动关卡。",
                DescriptionKey = "objective.surviveRounds.description",
                ObjectiveType = "surviveRounds",
                IsVictoryCondition = true,
                RoundLimit = 8,
                BuiltIn = true
            }
        ];
    }

    public static List<BondRuleDefinition> CreateDefaultBondRules()
    {
        return
        [
            new BondRuleDefinition
            {
                Id = "bond.shieldWall",
                DisplayName = "盾墙",
                DisplayNameKey = "bond.shieldWall.name",
                Description = "至少两个带 shield 标签的友方单位相邻时获得防御加成效果。",
                DescriptionKey = "bond.shieldWall.description",
                TriggerTiming = "whileAdjacent",
                Range = 1,
                MinParticipants = 2,
                RequireSameFaction = true,
                RequiredUnitTags = ["shield"],
                EffectIds = ["effect.tactics.guardBonus"],
                DurationMode = "whileConditionMet",
                StackingMode = "refresh",
                BuiltIn = true
            },
            new BondRuleDefinition
            {
                Id = "bond.siblingStrike",
                DisplayName = "兄妹合击",
                DisplayNameKey = "bond.siblingStrike.name",
                Description = "两个指定关系单位相邻时，攻击前可由事件触发器追加合击流程。",
                DescriptionKey = "bond.siblingStrike.description",
                TriggerTiming = "beforeAttack",
                Range = 1,
                MinParticipants = 2,
                RequireSameFaction = true,
                RequiredUnitTags = ["sibling"],
                EffectIds = ["effect.tactics.supportAttack"],
                DurationMode = "combatOnly",
                StackingMode = "unique",
                BuiltIn = true
            }
        ];
    }
}
