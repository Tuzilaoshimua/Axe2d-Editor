using Axe2DEditor.Core.TowerDefense;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    public static List<TowerDefensePathDefinition> CreateDefaultTowerDefensePaths()
    {
        return
        [
            new TowerDefensePathDefinition
            {
                Id = "td.path.forest_main",
                DisplayName = "通用行进路线",
                DisplayNameKey = "td.path.forestMain.name",
                Description = "单位从入口沿路点前往目标点，可用于巡逻、护送、生成移动、撤离或防守推进。",
                DescriptionKey = "td.path.forestMain.description",
                MapId = "map.training_ground",
                SpawnPointId = "spawn.west",
                GoalPointId = "base.east",
                PathMode = "waypoints",
                Waypoints =
                [
                    new TowerDefenseWaypointDefinition { Key = "start", X = 2, Y = 16 },
                    new TowerDefenseWaypointDefinition { Key = "bend_1", X = 10, Y = 16 },
                    new TowerDefenseWaypointDefinition { Key = "bend_2", X = 14, Y = 10 },
                    new TowerDefenseWaypointDefinition { Key = "bend_3", X = 22, Y = 10 },
                    new TowerDefenseWaypointDefinition { Key = "goal", X = 29, Y = 16 }
                ],
                BuiltIn = true
            }
        ];
    }

    public static List<TowerDefenseWaveDefinition> CreateDefaultTowerDefenseWaves()
    {
        return
        [
            new TowerDefenseWaveDefinition
            {
                Id = "td.wave.slime_01",
                DisplayName = "生成波次：史莱姆小队",
                DisplayNameKey = "td.wave.slime01.name",
                Description = "基础生成波次，用于测试路线、批量生成、奖励结算和到达目标后的处理。",
                DescriptionKey = "td.wave.slime01.description",
                MapId = "map.training_ground",
                PathId = "td.path.forest_main",
                StartDelaySeconds = 3,
                SpawnIntervalSeconds = 0.8,
                RewardGold = 60,
                RewardExp = 8,
                SpawnGroups =
                [
                    new TowerDefenseSpawnGroupDefinition
                    {
                        UnitId = "unit.slime",
                        Count = 12,
                        IntervalSeconds = 0.8,
                        PathId = "td.path.forest_main"
                    }
                ],
                BuiltIn = true
            },
            new TowerDefenseWaveDefinition
            {
                Id = "td.wave.mixed_02",
                DisplayName = "生成波次：混合小队",
                DisplayNameKey = "td.wave.mixed02.name",
                Description = "近战与远程单位混合出现，用于测试生成组、路线和自动目标优先级。",
                DescriptionKey = "td.wave.mixed02.description",
                MapId = "map.training_ground",
                PathId = "td.path.forest_main",
                StartDelaySeconds = 8,
                SpawnIntervalSeconds = 0.7,
                RewardGold = 90,
                RewardExp = 16,
                SpawnGroups =
                [
                    new TowerDefenseSpawnGroupDefinition
                    {
                        UnitId = "unit.slime",
                        Count = 10,
                        IntervalSeconds = 0.7,
                        PathId = "td.path.forest_main"
                    },
                    new TowerDefenseSpawnGroupDefinition
                    {
                        UnitId = "unit.ranged_mage",
                        Count = 4,
                        IntervalSeconds = 1.2,
                        DelaySeconds = 3,
                        PathId = "td.path.forest_main"
                    }
                ],
                BuiltIn = true
            }
        ];
    }

    public static List<TowerDefenseRuleDefinition> CreateDefaultTowerDefenseRules()
    {
        return
        [
            new TowerDefenseRuleDefinition
            {
                Id = "td.rules.forest_defense",
                DisplayName = "通用关卡流程",
                DisplayNameKey = "td.rules.forestDefense.name",
                Description = "可组合关卡流程：手动启动波次、目标生命归零失败、清完波次胜利。",
                DescriptionKey = "td.rules.forestDefense.description",
                MapId = "map.training_ground",
                StartingGold = 220,
                BaseLife = 20,
                LeakDamagePerUnit = 1,
                BuildRuleId = "td.buildRule.default",
                WaveStartMode = "manual",
                VictoryCondition = "allWavesCleared",
                DefeatCondition = "baseLifeZero",
                WaveIds = ["td.wave.slime_01", "td.wave.mixed_02"],
                BuiltIn = true
            }
        ];
    }

    public static List<TowerDefenseBuildRuleDefinition> CreateDefaultTowerDefenseBuildRules()
    {
        return
        [
            new TowerDefenseBuildRuleDefinition
            {
                Id = "td.buildRule.default",
                DisplayName = "通用放置限制",
                DisplayNameKey = "td.buildRule.default.name",
                Description = "只允许在可放置区域放置单位，禁止堵死路线，允许出售和运行中升级。",
                DescriptionKey = "td.buildRule.default.description",
                BuildSurfaceTag = "buildable",
                PreventPathBlocking = true,
                AllowSell = true,
                SellRefundRatio = 0.7,
                AllowUpgradeDuringWave = true,
                CurrencyStatKey = "gold",
                BuiltIn = true
            }
        ];
    }

    public static List<TowerDefenseTowerDefinition> CreateDefaultTowerDefenseTowers()
    {
        return
        [
            new TowerDefenseTowerDefinition
            {
                Id = "td.tower.arrow",
                DisplayName = "箭矢可建造单位",
                DisplayNameKey = "td.tower.arrow.name",
                Description = "造价低、攻速快的基础单体可建造单位。",
                DescriptionKey = "td.tower.arrow.description",
                UnitId = "unit.td.arrow_tower",
                SkillId = "skill.td.arrowShot",
                TowerRole = "damage",
                BuildCost = 80,
                Range = 5.5,
                AttackIntervalSeconds = 0.8,
                TargetPriority = "first",
                Levels =
                [
                    new TowerDefenseTowerLevelDefinition { Level = 1, UpgradeCost = 0, RangeBonus = 0, DamageMultiplier = 1, AttackIntervalMultiplier = 1, SkillId = "skill.td.arrowShot" },
                    new TowerDefenseTowerLevelDefinition { Level = 2, UpgradeCost = 70, RangeBonus = 0.5, DamageMultiplier = 1.35, AttackIntervalMultiplier = 0.92, SkillId = "skill.td.arrowShot" },
                    new TowerDefenseTowerLevelDefinition { Level = 3, UpgradeCost = 120, RangeBonus = 1, DamageMultiplier = 1.8, AttackIntervalMultiplier = 0.85, SkillId = "skill.td.arrowShot" }
                ],
                BuiltIn = true
            },
            new TowerDefenseTowerDefinition
            {
                Id = "td.tower.frost",
                DisplayName = "寒霜可建造单位",
                DisplayNameKey = "td.tower.frost.name",
                Description = "伤害较低，但能持续减速敌人的控制型可建造单位。",
                DescriptionKey = "td.tower.frost.description",
                UnitId = "unit.td.slow_tower",
                SkillId = "skill.td.frostBolt",
                TowerRole = "control",
                BuildCost = 120,
                Range = 4.5,
                AttackIntervalSeconds = 1.2,
                TargetPriority = "fastest",
                Levels =
                [
                    new TowerDefenseTowerLevelDefinition { Level = 1, UpgradeCost = 0, RangeBonus = 0, DamageMultiplier = 1, AttackIntervalMultiplier = 1, SkillId = "skill.td.frostBolt" },
                    new TowerDefenseTowerLevelDefinition { Level = 2, UpgradeCost = 90, RangeBonus = 0.4, DamageMultiplier = 1.15, AttackIntervalMultiplier = 0.9, SkillId = "skill.td.frostBolt" }
                ],
                BuiltIn = true
            }
        ];
    }
}
