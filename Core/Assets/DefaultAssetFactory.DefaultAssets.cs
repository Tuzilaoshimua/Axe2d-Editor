using Axe2DEditor.Core.Ai;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Decorations;
using Axe2DEditor.Core.Effects;
using Axe2DEditor.Core.Interactions;
using Axe2DEditor.Core.Loot;
using Axe2DEditor.Core.Projectiles;
using Axe2DEditor.Core.Rules;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    private static GameplayEffectReference EffectRef(string effectId, params (string Key, string Value)[] parameters)
    {
        return new GameplayEffectReference
        {
            EffectId = effectId,
            Parameters = parameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key))
                .Select(parameter => new EffectParameterValue
                {
                    Key = parameter.Key,
                    Value = parameter.Value
                })
                .ToList()
        };
    }

    private static EffectParameterDefinition EffectParameter(
        string key,
        string displayNameKey,
        string displayName,
        EffectParameterValueType valueType,
        string defaultValue = "",
        string optionSourceId = "",
        int order = 0,
        params string[] options)
    {
        return new EffectParameterDefinition
        {
            Key = key,
            DisplayNameKey = displayNameKey,
            DisplayName = displayName,
            ValueType = valueType,
            DefaultValue = defaultValue,
            CategoryKey = "effect.parameter.category.core",
            Category = "effect.parameter.category.core",
            OptionSourceId = optionSourceId,
            Order = order,
            Options = options.Where(option => !string.IsNullOrWhiteSpace(option)).ToList()
        };
    }

    public static List<FactionDefinition> CreateDefaultFactions()
    {
        return
        [
            new FactionDefinition
            {
                Id = "faction.player",
                DisplayName = "玩家阵营",
                DisplayNameKey = "faction.player.name",
                Description = "由玩家控制或代表玩家一方的单位。",
                DescriptionKey = "faction.player.description",
                AttitudeToPlayer = "friendly",
                BuiltIn = true
            },
            new FactionDefinition
            {
                Id = "faction.village",
                DisplayName = "村庄阵营",
                DisplayNameKey = "faction.village.name",
                Description = "村民、商人和中立 NPC 所属阵营。",
                DescriptionKey = "faction.village.description",
                AttitudeToPlayer = "friendly",
                BuiltIn = true
            },
            new FactionDefinition
            {
                Id = "faction.monster",
                DisplayName = "怪物阵营",
                DisplayNameKey = "faction.monster.name",
                Description = "默认敌对生物和怪物使用的阵营。",
                DescriptionKey = "faction.monster.description",
                AttitudeToPlayer = "hostile",
                BuiltIn = true
            },
            new FactionDefinition
            {
                Id = "faction.neutral",
                DisplayName = "中立阵营",
                DisplayNameKey = "faction.neutral.name",
                Description = "默认不会主动敌对的阵营。",
                DescriptionKey = "faction.neutral.description",
                AttitudeToPlayer = "neutral",
                BuiltIn = true
            }
        ];
    }

    public static List<DamageTypeDefinition> CreateDefaultDamageTypes()
    {
        return
        [
            new DamageTypeDefinition
            {
                Id = "damage.physical",
                DisplayName = "物理伤害",
                DisplayNameKey = "damageType.physical.name",
                Description = "使用防御属性减免的基础伤害类型。",
                DescriptionKey = "damageType.physical.description",
                DefenseStatKey = "defense",
                BuiltIn = true
            },
            new DamageTypeDefinition
            {
                Id = "damage.magic",
                DisplayName = "魔法伤害",
                DisplayNameKey = "damageType.magic.name",
                Description = "使用魔法防御减免的伤害类型。",
                DescriptionKey = "damageType.magic.description",
                DefenseStatKey = "magicDefense",
                BuiltIn = true
            }
        ];
    }

    public static List<ElementDefinition> CreateDefaultElements()
    {
        return
        [
            new ElementDefinition
            {
                Id = "element.none",
                DisplayName = "无属性",
                DisplayNameKey = "element.none.name",
                Description = "没有元素属性。",
                DescriptionKey = "element.none.description",
                ColorHex = "#B0B0B0",
                BuiltIn = true
            },
            new ElementDefinition
            {
                Id = "element.fire",
                DisplayName = "火焰",
                DisplayNameKey = "element.fire.name",
                Description = "偏向燃烧与持续伤害的火属性。",
                DescriptionKey = "element.fire.description",
                ColorHex = "#FF6A2B",
                BuiltIn = true
            },
            new ElementDefinition
            {
                Id = "element.poison",
                DisplayName = "毒素",
                DisplayNameKey = "element.poison.name",
                Description = "偏向中毒与削弱的毒属性。",
                DescriptionKey = "element.poison.description",
                ColorHex = "#7BC043",
                BuiltIn = true
            },
            new ElementDefinition
            {
                Id = "element.blood",
                DisplayName = "鲜血",
                DisplayNameKey = "element.blood.name",
                Description = "偏向吸血与生命交换的血属性。",
                DescriptionKey = "element.blood.description",
                ColorHex = "#C73E5B",
                BuiltIn = true
            }
        ];
    }

    public static List<FormulaDefinition> CreateDefaultFormulas()
    {
        return
        [
            new FormulaDefinition
            {
                Id = "formula.physicalAttack",
                DisplayName = "物理伤害公式",
                DisplayNameKey = "formula.physicalAttack.name",
                Description = "根据攻击力和技能倍率计算物理伤害。",
                DescriptionKey = "formula.physicalAttack.description",
                FormulaKind = "expression",
                Expression = "basePower + attack * multiplier",
                BuiltIn = true
            },
            new FormulaDefinition
            {
                Id = "formula.magicAttack",
                DisplayName = "魔法伤害公式",
                DisplayNameKey = "formula.magicAttack.name",
                Description = "根据魔法攻击和倍率计算法术伤害。",
                DescriptionKey = "formula.magicAttack.description",
                FormulaKind = "expression",
                Expression = "basePower + magicAttack * multiplier",
                BuiltIn = true
            },
            new FormulaDefinition
            {
                Id = "formula.heal",
                DisplayName = "治疗公式",
                DisplayNameKey = "formula.heal.name",
                Description = "计算治疗量。",
                DescriptionKey = "formula.heal.description",
                FormulaKind = "expression",
                Expression = "basePower + magicAttack * multiplier",
                BuiltIn = true
            }
        ];
    }

    public static List<AIProfileDefinition> CreateDefaultAIProfiles()
    {
        return
        [
            new AIProfileDefinition
            {
                Id = "ai.passive",
                DisplayName = "被动待机",
                DisplayNameKey = "ai.passive.name",
                Description = "站立、等待并响应交互。",
                DescriptionKey = "ai.passive.description",
                BehaviorType = "passive",
                MovementMode = "none",
                TargetSelector = "none",
                BuiltIn = true
            },
            new AIProfileDefinition
            {
                Id = "ai.meleeChase",
                DisplayName = "近战追击",
                DisplayNameKey = "ai.meleeChase.name",
                Description = "接近敌人并进行近战攻击。",
                DescriptionKey = "ai.meleeChase.description",
                BehaviorType = "meleeChase",
                MovementMode = "topDown",
                TargetSelector = "nearestHostile",
                PerceptionRange = 8,
                LeashRange = 12,
                PreferredRange = 1.5,
                BuiltIn = true
            },
            new AIProfileDefinition
            {
                Id = "ai.patrolGuard",
                DisplayName = "巡逻守卫",
                DisplayNameKey = "ai.patrolGuard.name",
                Description = "按路线巡逻，发现目标后追击。",
                DescriptionKey = "ai.patrolGuard.description",
                BehaviorType = "patrol",
                MovementMode = "topDown",
                TargetSelector = "nearestHostile",
                PatrolMode = "route",
                PerceptionRange = 8,
                LeashRange = 14,
                PreferredRange = 1.5,
                BuiltIn = true
            },
            new AIProfileDefinition
            {
                Id = "ai.rangedKeepDistance",
                DisplayName = "远程保持距离",
                DisplayNameKey = "ai.rangedKeepDistance.name",
                Description = "保持距离并优先使用远程技能。",
                DescriptionKey = "ai.rangedKeepDistance.description",
                BehaviorType = "rangedKite",
                MovementMode = "topDown",
                TargetSelector = "nearestHostile",
                PerceptionRange = 10,
                LeashRange = 16,
                PreferredRange = 6,
                BuiltIn = true
            },
            new AIProfileDefinition
            {
                Id = "ai.bossPhases",
                DisplayName = "Boss 阶段战",
                DisplayNameKey = "ai.bossPhases.name",
                Description = "多阶段 Boss 战行为。",
                DescriptionKey = "ai.bossPhases.description",
                BehaviorType = "bossPhases",
                MovementMode = "topDown",
                TargetSelector = "nearestHostile",
                PerceptionRange = 10,
                LeashRange = 18,
                PreferredRange = 2.2,
                BuiltIn = true
            }
        ];
    }

    public static List<GameplayEffectDefinition> CreateDefaultGameplayEffects()
    {
        return
        [
            new GameplayEffectDefinition
            {
                Id = "effect.damage.physical",
                DisplayName = "物理伤害",
                DisplayNameKey = "effect.damage.physical.name",
                Description = "造成物理伤害。",
                DescriptionKey = "effect.damage.physical.description",
                EffectKind = "damage",
                IconPath = "Assets/Templates/Icons/effect_damage_physical.png",
                Parameters =
                [
                    EffectParameter("formulaId", "effect.parameter.formulaId.name", "公式", EffectParameterValueType.AssetRef, "formula.physicalAttack", "formula", 10),
                    EffectParameter("damageTypeId", "effect.parameter.damageTypeId.name", "伤害类型", EffectParameterValueType.AssetRef, "damage.physical", "damageType", 20),
                    EffectParameter("elementId", "effect.parameter.elementId.name", "元素", EffectParameterValueType.AssetRef, string.Empty, "element", 30),
                    EffectParameter("basePower", "effect.parameter.basePower.name", "基础值", EffectParameterValueType.Number, "0", string.Empty, 40),
                    EffectParameter("powerStatKey", "effect.parameter.powerStatKey.name", "威力属性", EffectParameterValueType.AssetRef, "attack", "stat", 50),
                    EffectParameter("powerMultiplier", "effect.parameter.powerMultiplier.name", "倍率", EffectParameterValueType.Number, "1", string.Empty, 60)
                ],
                Tags = ["attack", "physical"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.damage.fire",
                DisplayName = "火焰伤害",
                DisplayNameKey = "effect.damage.fire.name",
                Description = "造成火焰伤害。",
                DescriptionKey = "effect.damage.fire.description",
                EffectKind = "damage",
                IconPath = "Assets/Templates/Icons/effect_damage_fire.png",
                Parameters =
                [
                    EffectParameter("formulaId", "effect.parameter.formulaId.name", "公式", EffectParameterValueType.AssetRef, "formula.magicAttack", "formula", 10),
                    EffectParameter("damageTypeId", "effect.parameter.damageTypeId.name", "伤害类型", EffectParameterValueType.AssetRef, "damage.magic", "damageType", 20),
                    EffectParameter("elementId", "effect.parameter.elementId.name", "元素", EffectParameterValueType.AssetRef, "element.fire", "element", 30),
                    EffectParameter("basePower", "effect.parameter.basePower.name", "基础值", EffectParameterValueType.Number, "0", string.Empty, 40),
                    EffectParameter("powerStatKey", "effect.parameter.powerStatKey.name", "威力属性", EffectParameterValueType.AssetRef, "magicAttack", "stat", 50),
                    EffectParameter("powerMultiplier", "effect.parameter.powerMultiplier.name", "倍率", EffectParameterValueType.Number, "1", string.Empty, 60)
                ],
                Tags = ["magic", "fire"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.heal.hp",
                DisplayName = "治疗生命",
                DisplayNameKey = "effect.heal.hp.name",
                Description = "恢复生命值。",
                DescriptionKey = "effect.heal.hp.description",
                EffectKind = "heal",
                IconPath = "Assets/Templates/Icons/effect_heal_hp.png",
                Parameters =
                [
                    EffectParameter("formulaId", "effect.parameter.formulaId.name", "公式", EffectParameterValueType.AssetRef, "formula.heal", "formula", 10),
                    EffectParameter("targetStatKey", "effect.parameter.targetStatKey.name", "目标属性", EffectParameterValueType.AssetRef, "maxHp", "stat", 20),
                    EffectParameter("basePower", "effect.parameter.basePower.name", "基础值", EffectParameterValueType.Number, "30", string.Empty, 30),
                    EffectParameter("powerStatKey", "effect.parameter.powerStatKey.name", "威力属性", EffectParameterValueType.AssetRef, "magicAttack", "stat", 40),
                    EffectParameter("powerMultiplier", "effect.parameter.powerMultiplier.name", "倍率", EffectParameterValueType.Number, "1", string.Empty, 50)
                ],
                Tags = ["heal"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.restore.mp",
                DisplayName = "恢复法力",
                DisplayNameKey = "effect.restore.mp.name",
                Description = "恢复法力值。",
                DescriptionKey = "effect.restore.mp.description",
                EffectKind = "restore",
                IconPath = "Assets/Templates/Icons/effect_restore_mp.png",
                Parameters =
                [
                    EffectParameter("formulaId", "effect.parameter.formulaId.name", "公式", EffectParameterValueType.AssetRef, "formula.heal", "formula", 10),
                    EffectParameter("targetStatKey", "effect.parameter.targetStatKey.name", "目标属性", EffectParameterValueType.AssetRef, "maxMp", "stat", 20),
                    EffectParameter("basePower", "effect.parameter.basePower.name", "基础值", EffectParameterValueType.Number, "24", string.Empty, 30),
                    EffectParameter("powerStatKey", "effect.parameter.powerStatKey.name", "威力属性", EffectParameterValueType.AssetRef, "magicAttack", "stat", 40),
                    EffectParameter("powerMultiplier", "effect.parameter.powerMultiplier.name", "倍率", EffectParameterValueType.Number, "0.8", string.Empty, 50)
                ],
                Tags = ["mana", "restore"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.lifeSteal",
                DisplayName = "吸血",
                DisplayNameKey = "effect.lifeSteal.name",
                Description = "吸血语义效果；具体比例、目标过滤、持续和叠加规则由技能、装备、投射物或事件填写。",
                DescriptionKey = "effect.lifeSteal.description",
                EffectKind = "lifeSteal",
                IconPath = "Assets/Templates/Icons/effect_life_steal.png",
                Parameters =
                [
                    EffectParameter("lifeStealRatio", "effect.parameter.lifeStealRatio.name", "吸血比例", EffectParameterValueType.Number, "0.25", string.Empty, 10),
                    EffectParameter("restoreStatKey", "effect.parameter.restoreStatKey.name", "恢复属性", EffectParameterValueType.AssetRef, "maxHp", "stat", 20),
                    EffectParameter("targetFilterMode", "effect.parameter.targetFilterMode.name", "目标规则", EffectParameterValueType.Choice, "allowAll", string.Empty, 30, "allowAll", "excludeTags", "onlyTags"),
                    EffectParameter("blockedTargetTags", "effect.parameter.blockedTargetTags.name", "禁吸标签", EffectParameterValueType.TagList, string.Empty, "tag", 40),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "0", string.Empty, 50),
                    EffectParameter("tickIntervalSeconds", "effect.parameter.tickIntervalSeconds.name", "执行间隔", EffectParameterValueType.Number, "0", string.Empty, 60),
                    EffectParameter("stackMode", "effect.parameter.stackMode.name", "叠加方式", EffectParameterValueType.Choice, "replace", string.Empty, 70, "replace", "stack", "refresh"),
                    EffectParameter("chance", "effect.parameter.chance.name", "概率", EffectParameterValueType.Number, "1", string.Empty, 80)
                ],
                Tags = ["lifeSteal"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.knockback",
                DisplayName = "击退",
                DisplayNameKey = "effect.knockback.name",
                Description = "把目标击退到远处。",
                DescriptionKey = "effect.knockback.description",
                EffectKind = "knockback",
                IconPath = "Assets/Templates/Icons/effect_knockback.png",
                Parameters =
                [
                    EffectParameter("distance", "effect.parameter.distance.name", "击退距离", EffectParameterValueType.Number, "1", string.Empty, 10),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "位移时长", EffectParameterValueType.Number, "0.2", string.Empty, 20),
                    EffectParameter("directionMode", "effect.parameter.directionMode.name", "方向", EffectParameterValueType.Choice, "fromSource", string.Empty, 30, "fromSource", "forward", "custom"),
                    EffectParameter("stopOnObstacle", "effect.parameter.stopOnObstacle.name", "遇障停止", EffectParameterValueType.Boolean, "true", string.Empty, 40)
                ],
                Tags = ["control"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.applyStatus",
                DisplayName = "施加状态",
                DisplayNameKey = "effect.applyStatus.name",
                Description = "向目标施加一个状态；状态、持续时间和概率由引用方填写。",
                DescriptionKey = "effect.applyStatus.description",
                EffectKind = "applyStatus",
                IconPath = "Assets/Templates/Icons/effect_apply_status.png",
                Parameters =
                [
                    EffectParameter("statusId", "effect.parameter.statusId.name", "状态", EffectParameterValueType.AssetRef, "status.poison", "status", 10),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "3", string.Empty, 20),
                    EffectParameter("chance", "effect.parameter.chance.name", "概率", EffectParameterValueType.Number, "1", string.Empty, 30)
                ],
                Tags = ["debuff"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.poison",
                DisplayName = "中毒",
                DisplayNameKey = "effect.poison.name",
                Description = "施加中毒状态。",
                DescriptionKey = "effect.poison.description",
                EffectKind = "applyStatus",
                IconPath = "Assets/Templates/Icons/effect_poison.png",
                Parameters =
                [
                    EffectParameter("statusId", "effect.parameter.statusId.name", "状态", EffectParameterValueType.AssetRef, "status.poison", "status", 10),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "6", string.Empty, 20),
                    EffectParameter("chance", "effect.parameter.chance.name", "概率", EffectParameterValueType.Number, "1", string.Empty, 30)
                ],
                Tags = ["poison"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.burn",
                DisplayName = "燃烧",
                DisplayNameKey = "effect.burn.name",
                Description = "施加燃烧状态。",
                DescriptionKey = "effect.burn.description",
                EffectKind = "applyStatus",
                IconPath = "Assets/Templates/Icons/effect_burn.png",
                Parameters =
                [
                    EffectParameter("statusId", "effect.parameter.statusId.name", "状态", EffectParameterValueType.AssetRef, "status.burn", "status", 10),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "5", string.Empty, 20),
                    EffectParameter("chance", "effect.parameter.chance.name", "概率", EffectParameterValueType.Number, "1", string.Empty, 30)
                ],
                Tags = ["fire"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.shield",
                DisplayName = "护盾",
                DisplayNameKey = "effect.shield.name",
                Description = "提升防御或提供护盾值。",
                DescriptionKey = "effect.shield.description",
                EffectKind = "buff",
                IconPath = "Assets/Templates/Icons/effect_shield.png",
                Parameters =
                [
                    EffectParameter("targetStatKey", "effect.parameter.targetStatKey.name", "目标属性", EffectParameterValueType.AssetRef, "defense", "stat", 10),
                    EffectParameter("baseValue", "effect.parameter.baseValue.name", "数值", EffectParameterValueType.Number, "8", string.Empty, 20),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "0", string.Empty, 30),
                    EffectParameter("stackMode", "effect.parameter.stackMode.name", "叠加方式", EffectParameterValueType.Choice, "replace", string.Empty, 40, "replace", "stack", "refresh")
                ],
                Tags = ["defense"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.tactics.guardBonus",
                DisplayName = "守护加成",
                DisplayNameKey = "effect.tactics.guardBonus.name",
                Description = "用于羁绊、阵型或地形规则的防御加成语义，数值由引用方或事件填写。",
                DescriptionKey = "effect.tactics.guardBonus.description",
                EffectKind = "buff",
                IconPath = "Assets/Templates/Icons/effect_guard_bonus.png",
                Parameters =
                [
                    EffectParameter("targetStatKey", "effect.parameter.targetStatKey.name", "目标属性", EffectParameterValueType.AssetRef, "defense", "stat", 10),
                    EffectParameter("baseValue", "effect.parameter.baseValue.name", "数值", EffectParameterValueType.Number, "6", string.Empty, 20),
                    EffectParameter("durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", EffectParameterValueType.Number, "0", string.Empty, 30),
                    EffectParameter("stackMode", "effect.parameter.stackMode.name", "叠加方式", EffectParameterValueType.Choice, "refresh", string.Empty, 40, "replace", "stack", "refresh")
                ],
                Tags = ["defense", "buff", "tactics"],
                BuiltIn = true
            },
            new GameplayEffectDefinition
            {
                Id = "effect.tactics.supportAttack",
                DisplayName = "支援攻击",
                DisplayNameKey = "effect.tactics.supportAttack.name",
                Description = "用于相邻羁绊或事件触发的支援攻击语义，具体追加攻击由事件或运行时处理。",
                DescriptionKey = "effect.tactics.supportAttack.description",
                EffectKind = "trigger",
                IconPath = "Assets/Templates/Icons/effect_support_attack.png",
                Parameters =
                [
                    EffectParameter("triggerName", "effect.parameter.triggerName.name", "触发名称", EffectParameterValueType.Text, "OnSupportAttack", string.Empty, 10),
                    EffectParameter("chance", "effect.parameter.chance.name", "概率", EffectParameterValueType.Number, "1", string.Empty, 20)
                ],
                Tags = ["attack", "support", "tactics"],
                BuiltIn = true
            }
        ];
    }

    public static List<StatusDefinition> CreateDefaultStatuses()
    {
        return
        [
            new StatusDefinition
            {
                Id = "status.burn",
                DisplayName = "燃烧",
                DisplayNameKey = "status.burn.name",
                Description = "持续受到火焰伤害。",
                DescriptionKey = "status.burn.description",
                StatusKind = "debuff",
                DurationSeconds = 5,
                MaxStacks = 3,
                TickIntervalSeconds = 1,
                OnApplyEffects =
                [
                    EffectRef("effect.damage.fire",
                        ("formulaId", "formula.magicAttack"),
                        ("damageTypeId", "damage.magic"),
                        ("elementId", "element.fire"),
                        ("basePower", "4"),
                        ("powerStatKey", "magicAttack"),
                        ("powerMultiplier", "0.2"))
                ],
                PeriodicEffects =
                [
                    EffectRef("effect.damage.fire",
                        ("formulaId", "formula.magicAttack"),
                        ("damageTypeId", "damage.magic"),
                        ("elementId", "element.fire"),
                        ("basePower", "4"),
                        ("powerStatKey", "magicAttack"),
                        ("powerMultiplier", "0.2"))
                ],
                Tags = ["fire"],
                BuiltIn = true
            },
            new StatusDefinition
            {
                Id = "status.poison",
                DisplayName = "中毒",
                DisplayNameKey = "status.poison.name",
                Description = "持续受到毒素伤害。",
                DescriptionKey = "status.poison.description",
                StatusKind = "debuff",
                DurationSeconds = 6,
                MaxStacks = 1,
                TickIntervalSeconds = 1,
                OnApplyEffects =
                [
                    EffectRef("effect.poison",
                        ("statusId", "status.poison"),
                        ("durationSeconds", "6"),
                        ("chance", "1"))
                ],
                PeriodicEffects =
                [
                    EffectRef("effect.damage.physical",
                        ("formulaId", "formula.magicAttack"),
                        ("damageTypeId", "damage.magic"),
                        ("elementId", "element.poison"),
                        ("basePower", "3"),
                        ("powerStatKey", "magicAttack"),
                        ("powerMultiplier", "0.15"))
                ],
                Tags = ["poison"],
                BuiltIn = true
            },
            new StatusDefinition
            {
                Id = "status.td.slow",
                DisplayName = "移动减速",
                DisplayNameKey = "status.td.slow.name",
                Description = "降低目标移动速度，可用于路线推进、追击、逃跑或战术移动。",
                DescriptionKey = "status.td.slow.description",
                StatusKind = "debuff",
                DurationSeconds = 2.5,
                MaxStacks = 1,
                TickIntervalSeconds = 0,
                Tags = ["control", "slow"],
                BuiltIn = true
            }
        ];
    }

    public static List<ProjectileDefinition> CreateDefaultProjectiles()
    {
        return
        [
            new ProjectileDefinition
            {
                Id = "projectile.fireball",
                DisplayName = "火球",
                DisplayNameKey = "projectile.fireball.name",
                Description = "向目标发射的火球。",
                DescriptionKey = "projectile.fireball.description",
                Speed = 420,
                LifetimeSeconds = 2.5,
                Radius = 0.35,
                VisualEffectId = "vfx.fireball",
                Effects =
                [
                    EffectRef("effect.damage.fire",
                        ("formulaId", "formula.magicAttack"),
                        ("damageTypeId", "damage.magic"),
                        ("elementId", "element.fire"),
                        ("basePower", "18"),
                        ("powerStatKey", "magicAttack"),
                        ("powerMultiplier", "1.6")),
                    EffectRef("effect.burn",
                        ("statusId", "status.burn"),
                        ("durationSeconds", "5"),
                        ("chance", "0.5"))
                ],
                BuiltIn = true
            },
            new ProjectileDefinition
            {
                Id = "projectile.template.basic",
                DisplayName = "基础投射物",
                DisplayNameKey = "projectile.basic.name",
                Description = "通用投射物。",
                DescriptionKey = "projectile.basic.description",
                Speed = 360,
                LifetimeSeconds = 2,
                Radius = 0.25,
                BuiltIn = true
            },
            new ProjectileDefinition
            {
                Id = "projectile.td.arrow",
                DisplayName = "自动箭矢投射物",
                DisplayNameKey = "projectile.td.arrow.name",
                Description = "固定哨台、陷阱或远程单位发射的高速箭矢。",
                DescriptionKey = "projectile.td.arrow.description",
                Speed = 520,
                LifetimeSeconds = 2,
                Radius = 0.2,
                VisualEffectId = "vfx.slash",
                Effects =
                [
                    EffectRef("effect.damage.physical",
                        ("formulaId", "formula.physicalAttack"),
                        ("damageTypeId", "damage.physical"),
                        ("basePower", "12"),
                        ("powerStatKey", "attack"),
                        ("powerMultiplier", "1"))
                ],
                BuiltIn = true
            },
            new ProjectileDefinition
            {
                Id = "projectile.td.frostBolt",
                DisplayName = "寒霜弹投射物",
                DisplayNameKey = "projectile.td.frostBolt.name",
                Description = "可由固定哨台、陷阱或远程单位发射的减速投射物。",
                DescriptionKey = "projectile.td.frostBolt.description",
                Speed = 380,
                LifetimeSeconds = 2.2,
                Radius = 0.28,
                VisualEffectId = "vfx.frost_bolt",
                Effects =
                [
                    EffectRef("effect.damage.physical",
                        ("formulaId", "formula.magicAttack"),
                        ("damageTypeId", "damage.magic"),
                        ("basePower", "6"),
                        ("powerStatKey", "magicAttack"),
                        ("powerMultiplier", "0.8")),
                    EffectRef("effect.applyStatus",
                        ("statusId", "status.td.slow"),
                        ("durationSeconds", "2.5"),
                        ("chance", "1"))
                ],
                BuiltIn = true
            }
        ];
    }

    public static List<VisualEffectDefinition> CreateDefaultVisualEffects()
    {
        return
        [
            new VisualEffectDefinition
            {
                Id = "vfx.slash",
                DisplayName = "斩击特效",
                DisplayNameKey = "vfx.slash.name",
                Description = "近战斩击表现。",
                DescriptionKey = "vfx.slash.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "slash",
                DurationSeconds = 0.4,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.fireball",
                DisplayName = "火球特效",
                DisplayNameKey = "vfx.fireball.name",
                Description = "火球飞行与命中特效。",
                DescriptionKey = "vfx.fireball.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "fireball",
                DurationSeconds = 0.8,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.heal",
                DisplayName = "治疗特效",
                DisplayNameKey = "vfx.heal.name",
                Description = "治疗时的光效。",
                DescriptionKey = "vfx.heal.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "heal",
                DurationSeconds = 0.6,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.poisonCloud",
                DisplayName = "毒云特效",
                DisplayNameKey = "vfx.poisonCloud.name",
                Description = "毒云持续表现。",
                DescriptionKey = "vfx.poisonCloud.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "poison",
                DurationSeconds = 1.2,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.dash",
                DisplayName = "冲刺特效",
                DisplayNameKey = "vfx.dash.name",
                Description = "冲刺时的速度残影表现。",
                DescriptionKey = "vfx.dash.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "dash",
                DurationSeconds = 0.3,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.summon",
                DisplayName = "召唤特效",
                DisplayNameKey = "vfx.summon.name",
                Description = "召唤单位时的表现。",
                DescriptionKey = "vfx.summon.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "summon",
                DurationSeconds = 1,
                BuiltIn = true
            },
            new VisualEffectDefinition
            {
                Id = "vfx.frost_bolt",
                DisplayName = "寒霜弹特效",
                DisplayNameKey = "vfx.frostBolt.name",
                Description = "寒霜投射物的飞行与命中表现。",
                DescriptionKey = "vfx.frostBolt.description",
                EffectKind = "spriteAnimation",
                AnimationKey = "frostBolt",
                DurationSeconds = 0.8,
                BuiltIn = true
            }
        ];
    }

    public static List<DecorationDefinition> CreateDefaultDecorations()
    {
        return
        [
            new DecorationDefinition
            {
                Id = "decoration.tree_pine",
                DisplayName = "松树",
                DisplayNameKey = "decoration.treePine.name",
                Description = "常见的自然装饰物。",
                DescriptionKey = "decoration.treePine.description",
                DecorationKind = "foliage",
                SpriteSheet = "Assets/Templates/Decorations/tree_pine.png",
                BlocksMovement = false,
                BuiltIn = true
            },
            new DecorationDefinition
            {
                Id = "decoration.chest_wood",
                DisplayName = "木箱",
                DisplayNameKey = "decoration.chestWood.name",
                Description = "可交互的木箱装饰物。",
                DescriptionKey = "decoration.chestWood.description",
                DecorationKind = "interactive",
                SpriteSheet = "Assets/Templates/Decorations/chest_wood.png",
                InteractionProfileId = "interaction.open",
                BuiltIn = true
            }
        ];
    }

    public static List<LootTableDefinition> CreateDefaultLootTables()
    {
        return
        [
            new LootTableDefinition
            {
                Id = "loot.slime",
                DisplayName = "史莱姆掉落",
                DisplayNameKey = "loot.slime.name",
                Description = "史莱姆的基础掉落表。",
                DescriptionKey = "loot.slime.description",
                Entries =
                [
                    new LootEntryDefinition { ItemId = "item.herb", MinQuantity = 1, MaxQuantity = 2, Chance = 0.8 },
                    new LootEntryDefinition { ItemId = "item.potion_small", MinQuantity = 1, MaxQuantity = 1, Chance = 0.15 }
                ],
                BuiltIn = true
            },
            new LootTableDefinition
            {
                Id = "loot.guard",
                DisplayName = "守卫掉落",
                DisplayNameKey = "loot.guard.name",
                Description = "守卫的基础掉落表。",
                DescriptionKey = "loot.guard.description",
                Entries =
                [
                    new LootEntryDefinition { ItemId = "item.iron_sword", MinQuantity = 1, MaxQuantity = 1, Chance = 0.05 },
                    new LootEntryDefinition { ItemId = "item.leather_armor", MinQuantity = 1, MaxQuantity = 1, Chance = 0.08 }
                ],
                BuiltIn = true
            },
            new LootTableDefinition
            {
                Id = "loot.boss",
                DisplayName = "Boss 掉落",
                DisplayNameKey = "loot.boss.name",
                Description = "Boss 的基础掉落表。",
                DescriptionKey = "loot.boss.description",
                Entries =
                [
                    new LootEntryDefinition { ItemId = "item.vampire_dagger", MinQuantity = 1, MaxQuantity = 1, Chance = 0.2 },
                    new LootEntryDefinition { ItemId = "item.ancient_relic", MinQuantity = 1, MaxQuantity = 1, Chance = 1.0 }
                ],
                BuiltIn = true
            }
        ];
    }

    public static List<ComponentPresetDefinition> CreateDefaultComponentPresets()
    {
        return
        [
            new ComponentPresetDefinition
            {
                Id = "componentPreset.playerInput",
                DisplayName = "玩家输入",
                DisplayNameKey = "componentPreset.playerInput.name",
                Description = "玩家直接操作用的输入组件。",
                DescriptionKey = "componentPreset.playerInput.description",
                Component = CreateComponent("PlayerInput"),
                BuiltIn = true
            },
            new ComponentPresetDefinition
            {
                Id = "componentPreset.health",
                DisplayName = "生命",
                DisplayNameKey = "componentPreset.health.name",
                Description = "提供生命值逻辑的组件。",
                DescriptionKey = "componentPreset.health.description",
                Component = CreateComponent("Health", ("maxHpStat", "maxHp")),
                BuiltIn = true
            },
            new ComponentPresetDefinition
            {
                Id = "componentPreset.topDownMovement",
                DisplayName = "俯视移动",
                DisplayNameKey = "componentPreset.topDownMovement.name",
                Description = "俯视角移动组件。",
                DescriptionKey = "componentPreset.topDownMovement.description",
                Component = CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                BuiltIn = true
            }
        ];
    }

    public static List<InteractionProfileDefinition> CreateDefaultInteractionProfiles()
    {
        return
        [
            new InteractionProfileDefinition
            {
                Id = "interaction.talk",
                DisplayName = "交谈",
                DisplayNameKey = "interaction.talk.name",
                Description = "基础对话交互。",
                DescriptionKey = "interaction.talk.description",
                InteractionKind = "talk",
                TriggerName = "OnInteract",
                BuiltIn = true
            },
            new InteractionProfileDefinition
            {
                Id = "interaction.rescue",
                DisplayName = "营救",
                DisplayNameKey = "interaction.rescue.name",
                Description = "营救或解救目标的交互。",
                DescriptionKey = "interaction.rescue.description",
                InteractionKind = "rescue",
                TriggerName = "OnInteract",
                BuiltIn = true
            },
            new InteractionProfileDefinition
            {
                Id = "interaction.open",
                DisplayName = "打开",
                DisplayNameKey = "interaction.open.name",
                Description = "用于门、宝箱等打开交互。",
                DescriptionKey = "interaction.open.description",
                InteractionKind = "open",
                TriggerName = "OnInteract",
                BuiltIn = true
            }
        ];
    }
}
