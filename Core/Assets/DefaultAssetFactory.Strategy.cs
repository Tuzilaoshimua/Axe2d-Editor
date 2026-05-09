using Axe2DEditor.Core.Strategy;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    public static List<ResourceRuleDefinition> CreateDefaultResourceRules()
    {
        return
        [
            new ResourceRuleDefinition
            {
                Id = "resource.gold",
                DisplayName = "金币",
                DisplayNameKey = "resource.gold.name",
                Description = "通用货币资源，可被建造、生产、交易、奖励和事件流程共同引用。",
                DescriptionKey = "resource.gold.description",
                ResourceKind = "currency",
                StatKey = "gold",
                StartingAmount = 200,
                StorageLimit = 999999,
                SharedByFaction = true,
                Tags = ["economy", "currency"],
                BuiltIn = true
            },
            new ResourceRuleDefinition
            {
                Id = "resource.wood",
                DisplayName = "木材",
                DisplayNameKey = "resource.wood.name",
                Description = "基础建造材料，适合建筑、陷阱、营地建设、制作或区域产出。",
                DescriptionKey = "resource.wood.description",
                ResourceKind = "material",
                StatKey = "wood",
                StartingAmount = 80,
                StorageLimit = 9999,
                SharedByFaction = true,
                Tags = ["material", "build"],
                BuiltIn = true
            },
            new ResourceRuleDefinition
            {
                Id = "resource.food",
                DisplayName = "粮食",
                DisplayNameKey = "resource.food.name",
                Description = "用于维持单位、训练人口或触发生存压力的资源。",
                DescriptionKey = "resource.food.description",
                ResourceKind = "supply",
                StatKey = "food",
                StartingAmount = 50,
                StorageLimit = 500,
                SharedByFaction = true,
                Tags = ["supply", "economy"],
                BuiltIn = true
            },
            new ResourceRuleDefinition
            {
                Id = "resource.research",
                DisplayName = "研究点",
                DisplayNameKey = "resource.research.name",
                Description = "用于解锁科技、升级单位或开放新规则的抽象发展资源。",
                DescriptionKey = "resource.research.description",
                ResourceKind = "research",
                StatKey = "researchPoint",
                StartingAmount = 0,
                StorageLimit = 9999,
                SharedByFaction = true,
                Tags = ["research", "unlock"],
                BuiltIn = true
            }
        ];
    }

    public static List<ProductionRuleDefinition> CreateDefaultProductionRules()
    {
        return
        [
            new ProductionRuleDefinition
            {
                Id = "production.train.guard",
                DisplayName = "训练守卫",
                DisplayNameKey = "production.train.guard.name",
                Description = "由建筑或事件队列生产一个守卫单位，可用于据点、城镇兵营、增援或营地支援。",
                DescriptionKey = "production.train.guard.description",
                ProducerUnitId = "unit.td.arrow_tower",
                ProducedAssetId = "unit.patrol_guard",
                ProducedAssetKind = "unit",
                BuildTimeTurns = 2,
                ResourceCosts = { ["gold"] = 80, ["food"] = 1 },
                RequiresBuildQueue = true,
                Repeatable = true,
                BuiltIn = true
            },
            new ProductionRuleDefinition
            {
                Id = "production.sawmill.wood",
                DisplayName = "木材产出",
                DisplayNameKey = "production.sawmill.wood.name",
                Description = "占有木材区域或建筑时定期产出木材，具体触发频率可由事件编辑器决定。",
                DescriptionKey = "production.sawmill.wood.description",
                ProducedAssetKind = "resource",
                ProducedAssetId = "resource.wood",
                BuildTimeTurns = 1,
                ResourceOutputs = { ["wood"] = 25 },
                RequiresBuildQueue = false,
                Repeatable = true,
                BuiltIn = true
            },
            new ProductionRuleDefinition
            {
                Id = "production.research.study",
                DisplayName = "研究积累",
                DisplayNameKey = "production.research.study.name",
                Description = "让建筑、占领点或事件每轮提供研究点，供科技规则消耗。",
                DescriptionKey = "production.research.study.description",
                ProducedAssetKind = "resource",
                ProducedAssetId = "resource.research",
                BuildTimeTurns = 1,
                ResourceOutputs = { ["researchPoint"] = 15 },
                RequiresBuildQueue = false,
                Repeatable = true,
                BuiltIn = true
            }
        ];
    }

    public static List<TechRuleDefinition> CreateDefaultTechRules()
    {
        return
        [
            new TechRuleDefinition
            {
                Id = "tech.archery",
                DisplayName = "箭术训练",
                DisplayNameKey = "tech.archery.name",
                Description = "解锁或强化远程攻击能力，可被固定哨台、弓手、战术单位共同引用。",
                DescriptionKey = "tech.archery.description",
                TechKind = "unlock",
                ResearchCosts = { ["researchPoint"] = 60, ["gold"] = 40 },
                ResearchTurns = 3,
                UnlockSkillIds = ["skill.td.arrowShot"],
                EffectIds = ["effect.damage.physical"],
                BuiltIn = true
            },
            new TechRuleDefinition
            {
                Id = "tech.logistics",
                DisplayName = "后勤学",
                DisplayNameKey = "tech.logistics.name",
                Description = "提升补给、生产或行动效率，可被据点、回合关卡和生存流程复用。",
                DescriptionKey = "tech.logistics.description",
                TechKind = "upgrade",
                ResearchCosts = { ["researchPoint"] = 80, ["food"] = 20 },
                ResearchTurns = 4,
                EffectIds = ["effect.heal.hp"],
                BuiltIn = true
            },
            new TechRuleDefinition
            {
                Id = "tech.fortification",
                DisplayName = "工事加固",
                DisplayNameKey = "tech.fortification.name",
                Description = "强化建筑、据点或占领点的防御能力，可结合建造规则和地形规则使用。",
                DescriptionKey = "tech.fortification.description",
                TechKind = "upgrade",
                PrerequisiteTechIds = ["tech.logistics"],
                ResearchCosts = { ["researchPoint"] = 100, ["wood"] = 60 },
                ResearchTurns = 4,
                UnlockBuildRuleIds = ["td.buildRule.default"],
                EffectIds = ["effect.tactics.guardBonus"],
                BuiltIn = true
            }
        ];
    }

    public static List<DiplomacyRuleDefinition> CreateDefaultDiplomacyRules()
    {
        return
        [
            new DiplomacyRuleDefinition
            {
                Id = "diplomacy.player.village",
                DisplayName = "玩家与村庄同盟",
                DisplayNameKey = "diplomacy.player.village.name",
                Description = "玩家阵营与村庄守军默认友好，允许通行、共享视野和事件交易。",
                DescriptionKey = "diplomacy.player.village.description",
                FromFactionId = "faction.player",
                ToFactionId = "faction.village",
                DiplomaticState = "allied",
                StartingTrust = 60,
                AllowsTrade = true,
                AllowsSharedVision = true,
                AllowsPassage = true,
                BuiltIn = true
            },
            new DiplomacyRuleDefinition
            {
                Id = "diplomacy.player.monster",
                DisplayName = "玩家与怪物敌对",
                DisplayNameKey = "diplomacy.player.monster.name",
                Description = "玩家与怪物阵营默认敌对，供 AI、目标选择和事件条件引用。",
                DescriptionKey = "diplomacy.player.monster.description",
                FromFactionId = "faction.player",
                ToFactionId = "faction.monster",
                DiplomaticState = "war",
                StartingTrust = -80,
                BuiltIn = true
            },
            new DiplomacyRuleDefinition
            {
                Id = "diplomacy.neutral.trade",
                DisplayName = "中立贸易关系",
                DisplayNameKey = "diplomacy.neutral.trade.name",
                Description = "中立阵营之间允许交易但不共享视野，适合商队、城镇或势力系统。",
                DescriptionKey = "diplomacy.neutral.trade.description",
                DiplomaticState = "neutral",
                StartingTrust = 0,
                AllowsTrade = true,
                AllowsPassage = true,
                BuiltIn = true
            }
        ];
    }

    public static List<TerritoryRuleDefinition> CreateDefaultTerritoryRules()
    {
        return
        [
            new TerritoryRuleDefinition
            {
                Id = "territory.village",
                DisplayName = "村庄领地",
                DisplayNameKey = "territory.village.name",
                Description = "占有村庄区域后提供金币与粮食，可用于经营、据点争夺或营地玩法。",
                DescriptionKey = "territory.village.description",
                TerritoryTag = "village",
                ControlMode = "occupyPoint",
                ControlRadius = 4,
                OwnerFactionId = "faction.village",
                ResourceYields = { ["gold"] = 25, ["food"] = 10 },
                RequiredUnitTags = ["unit"],
                BuiltIn = true
            },
            new TerritoryRuleDefinition
            {
                Id = "territory.forestCamp",
                DisplayName = "林地营地",
                DisplayNameKey = "territory.forestCamp.name",
                Description = "控制森林营地后提供木材，也可以作为巡逻、刷怪或建造区域的条件。",
                DescriptionKey = "territory.forestCamp.description",
                TerritoryTag = "forest",
                ControlMode = "areaInfluence",
                ControlRadius = 5,
                ResourceYields = { ["wood"] = 30 },
                RequiredUnitTags = ["guard"],
                BuiltIn = true
            },
            new TerritoryRuleDefinition
            {
                Id = "territory.capturePoint",
                DisplayName = "战略占领点",
                DisplayNameKey = "territory.capturePoint.name",
                Description = "被单位占领后改变所属阵营，可与目标规则、羁绊规则、事件触发器组合使用。",
                DescriptionKey = "territory.capturePoint.description",
                TerritoryTag = "capturePoint",
                ControlMode = "captureProgress",
                ControlRadius = 2,
                ResourceYields = { ["researchPoint"] = 10 },
                RequiredUnitTags = ["unit"],
                BlockedUnitTags = ["summon"],
                BuiltIn = true
            }
        ];
    }
}
