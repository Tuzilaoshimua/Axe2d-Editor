using Axe2DEditor.Core.Actors;
using Axe2DEditor.Core.Behaviors;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Enemies;
using Axe2DEditor.Core.Effects;
using Axe2DEditor.Core.Items;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Skills;
using Axe2DEditor.Core.Stats;
using Axe2DEditor.Core.Traits;
using Axe2DEditor.Core.TowerDefense;
using Axe2DEditor.Core.Units;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    public static List<StatDefinition> CreateDefaultStats()
    {
        return
        [
            CreateStat("maxHp", "stat.maxHp.name", 120, 1, 9999, "stat.category.combat"),
            CreateStat("maxMp", "stat.maxMp.name", 60, 0, 9999, "stat.category.combat"),
            CreateStat("attack", "stat.attack.name", 18, 0, 999, "stat.category.combat"),
            CreateStat("defense", "stat.defense.name", 6, 0, 999, "stat.category.combat"),
            CreateStat("magicAttack", "stat.magicAttack.name", 10, 0, 999, "stat.category.combat"),
            CreateStat("magicDefense", "stat.magicDefense.name", 6, 0, 999, "stat.category.combat"),
            CreateStat("moveSpeed", "stat.moveSpeed.name", 120, 0, 999, "stat.category.movement"),
            CreateStat("gold", "stat.gold.name", 0, 0, 999999, "stat.category.strategy", "Integer"),
            CreateStat("wood", "stat.wood.name", 0, 0, 999999, "stat.category.strategy", "Integer"),
            CreateStat("food", "stat.food.name", 0, 0, 999999, "stat.category.strategy", "Integer"),
            CreateStat("researchPoint", "stat.researchPoint.name", 0, 0, 999999, "stat.category.strategy", "Integer"),
            CreateStat("actionPoint", "stat.actionPoint.name", 1, 0, 99, "stat.category.strategy", "Integer"),
            CreateStat("movePoint", "stat.movePoint.name", 4, 0, 99, "stat.category.movement", "Integer"),
            CreateRewardExpStat()
        ];
    }

    public static StatDefinition CreateRewardExpStat()
    {
        return CreateStat("rewardExp", "stat.rewardExp.name", 0, 0, 999999, "stat.category.reward", "Integer");
    }

    public static ActorDefinition CreateHeroActor()
    {
        var stats = CreateDefaultCombatStats();
        return new ActorDefinition
        {
            Id = "actor.hero",
            DisplayNameKey = "actor.hero.name",
            DisplayName = "主角",
            DescriptionKey = "actor.hero.description",
            Description = "默认玩家角色模板。",
            Stats = stats,
            Components =
            [
                new() { Type = "PlayerInput" },
                new()
                {
                    Type = "TopDownMovement",
                    Parameters = { ["speedStat"] = "moveSpeed" }
                },
                new() { Type = "CameraFollow" },
                new()
                {
                    Type = "Health",
                    Parameters = { ["maxHpStat"] = "maxHp" }
                }
            ],
            Tags = ["player", "human"],
            Traits = ["unit", "player"]
        };
    }

    public static SkillDefinition CreateDefaultSkill()
    {
        return new SkillDefinition
        {
            Id = "skill.basic_slash",
            DisplayNameKey = "skill.basicSlash.name",
            DisplayName = "普通斩击",
            DescriptionKey = "skill.basicSlash.description",
            Description = "基础近战攻击技能。",
            SkillType = "active",
            TargetingMode = "selfForward",
            RequiredTargetTags = ["unit", "attackable"],
            ElementId = "element.none",
            DamageTypeId = "damage.physical",
            PowerStatKey = "attack",
            PowerMultiplier = 1.0,
            BasePower = 8,
            CooldownSeconds = 0.8,
            Range = 1.5,
            FormulaId = "formula.physicalAttack",
            Effects =
            [
                new GameplayEffectReference
                {
                    EffectId = "effect.damage.physical",
                    Parameters =
                    [
                        new EffectParameterValue { Key = "formulaId", Value = "formula.physicalAttack" },
                        new EffectParameterValue { Key = "damageTypeId", Value = "damage.physical" },
                        new EffectParameterValue { Key = "basePower", Value = "8" },
                        new EffectParameterValue { Key = "powerStatKey", Value = "attack" },
                        new EffectParameterValue { Key = "powerMultiplier", Value = "1" }
                    ]
                }
            ],
            VisualEffectId = "vfx.slash",
            Tags = "attack,melee"
        };
    }

    public static ItemDefinition CreateDefaultItem()
    {
        return new ItemDefinition
        {
            Id = "item.potion_small",
            DisplayNameKey = "item.potionSmall.name",
            DisplayName = "小型治疗药",
            DescriptionKey = "item.potionSmall.description",
            Description = "恢复一定生命值。",
            EffectType = "effect.heal",
            EffectValue = 30,
            TypeId = "consumable",
            Rarity = "common",
            Price = 15,
            StackLimit = 20,
            Consumable = true,
            Effects =
            [
                new GameplayEffectReference
                {
                    EffectId = "effect.heal.hp",
                    Parameters =
                    [
                        new EffectParameterValue { Key = "formulaId", Value = "formula.heal" },
                        new EffectParameterValue { Key = "targetStatKey", Value = "maxHp" },
                        new EffectParameterValue { Key = "basePower", Value = "30" },
                        new EffectParameterValue { Key = "powerStatKey", Value = "magicAttack" },
                        new EffectParameterValue { Key = "powerMultiplier", Value = "1" }
                    ]
                }
            ],
            CustomValues =
            [
                new ItemFieldValue { Key = "useMode", Value = "instant" },
                new ItemFieldValue { Key = "castTimeSeconds", Value = "0" },
                new ItemFieldValue { Key = "cooldownSeconds", Value = "1" }
            ],
            Tags = "potion,heal"
        };
    }

    public static EnemyDefinition CreateDefaultEnemy()
    {
        var stats = CreateDefaultCombatStats();
        stats["rewardExp"] = 10;
        return new EnemyDefinition
        {
            Id = "enemy.slime",
            DisplayNameKey = "enemy.slime.name",
            DisplayName = "史莱姆",
            DescriptionKey = "enemy.slime.description",
            Description = "默认敌人模板。",
            ActorRefId = "actor.hero",
            AiPreset = "MeleeChase",
            Stats = stats,
            Tags = ["enemy", "slime"],
            Traits = ["unit", "enemy", "attackable"]
        };
    }

    public static MapDefinition CreateDefaultMap()
    {
        return new MapDefinition
        {
            Id = "map.empty",
            DisplayNameKey = "map.empty.name",
            DisplayName = "空地图",
            DescriptionKey = "map.empty.description",
            Description = "",
            ViewType = "TopDown",
            Width = 64,
            Height = 64,
            Tileset = "tileset.default"
        };
    }

    public static List<UnitDefinition> CreateDefaultUnits()
    {
        return
        [
            new UnitDefinition
            {
                Id = "unit.hero",
                DisplayNameKey = "unit.hero.name",
                DisplayName = "主角",
                DescriptionKey = "unit.hero.description",
                Description = "默认玩家角色，包含输入、移动、镜头跟随和基础生命值。",
                UnitKind = "player",
                FactionId = "faction.player",
                AIProfileId = "ai.passive",
                Stats = CreateBehaviorStats(140, 16, 8, 10, 8, 130, 0),
                Tags = ["unit", "player", "human"],
                Traits = ["unit", "player"],
                Portrait = "Assets/Templates/Units/player_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/player_sheet.png",
                    TileWidth = 32,
                    TileHeight = 48,
                    PivotX = 16,
                    PivotY = 40
                },
                Components = CreatePlayerComponents(),
                Animations = CreateFourDirectionAnimations("anim.template.player")
            },
            new UnitDefinition
            {
                Id = "unit.slime",
                DisplayNameKey = "unit.slime.name",
                DisplayName = "史莱姆",
                DescriptionKey = "unit.slime.description",
                Description = "普通近战敌人。",
                UnitKind = "enemy",
                FactionId = "faction.monster",
                AIProfileId = "ai.meleeChase",
                LootTableId = "loot.slime",
                Stats = CreateBehaviorStats(90, 12, 4, 2, 2, 95, 8),
                Tags = ["unit", "enemy", "slime", "starterEnemy"],
                Traits = ["unit", "enemy", "attackable"],
                Portrait = "Assets/Templates/Units/slime_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/slime_sheet.png",
                    TileWidth = 32,
                    TileHeight = 32,
                    PivotX = 16,
                    PivotY = 28
                },
                Components = CreateEnemyComponents("ai.meleeChase"),
                Animations = CreateSimpleCombatAnimations("anim.template.slime")
            },
            new UnitDefinition
            {
                Id = "unit.patrol_guard",
                DisplayNameKey = "unit.patrolGuard.name",
                DisplayName = "巡逻守卫",
                DescriptionKey = "unit.patrolGuard.description",
                Description = "沿路线巡逻并在发现目标后追击。",
                UnitKind = "enemy",
                FactionId = "faction.monster",
                AIProfileId = "ai.patrolGuard",
                LootTableId = "loot.guard",
                Stats = CreateBehaviorStats(120, 16, 6, 4, 4, 110, 12),
                Tags = ["unit", "enemy", "guard"],
                Traits = ["unit", "enemy", "attackable"],
                Portrait = "Assets/Templates/Units/patrol_guard_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/patrol_guard_sheet.png",
                    TileWidth = 32,
                    TileHeight = 48,
                    PivotX = 16,
                    PivotY = 40
                },
                Components = CreateEnemyComponents("ai.patrolGuard"),
                Animations = CreateSimpleCombatAnimations("anim.template.patrolGuard")
            },
            new UnitDefinition
            {
                Id = "unit.ranged_mage",
                DisplayNameKey = "unit.rangedMage.name",
                DisplayName = "远程法师",
                DescriptionKey = "unit.rangedMage.description",
                Description = "保持距离并使用火球的远程敌人。",
                UnitKind = "enemy",
                FactionId = "faction.monster",
                AIProfileId = "ai.rangedKeepDistance",
                LootTableId = "loot.guard",
                Stats = CreateBehaviorStats(80, 8, 3, 18, 8, 105, 16),
                Tags = ["unit", "enemy", "ranged", "mage", "caster"],
                Traits = ["unit", "enemy", "attackable"],
                Portrait = "Assets/Templates/Units/ranged_mage_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/ranged_mage_sheet.png",
                    TileWidth = 32,
                    TileHeight = 48,
                    PivotX = 16,
                    PivotY = 40
                },
                Components = CreateEnemyComponents("ai.rangedKeepDistance"),
                Animations = CreateCasterAnimations("anim.template.rangedMage")
            },
            new UnitDefinition
            {
                Id = "unit.trapped_villager",
                DisplayNameKey = "unit.trappedVillager.name",
                DisplayName = "受困村民",
                DescriptionKey = "unit.trappedVillager.description",
                Description = "可被事件编辑器驱动营救流程的 NPC 样例。",
                UnitKind = "npc",
                FactionId = "faction.village",
                AIProfileId = "ai.passive",
                InteractionProfileId = "interaction.rescue",
                Stats = CreateBehaviorStats(70, 2, 2, 2, 4, 90, 0),
                Tags = ["unit", "npc", "rescuable", "rescueTarget"],
                Traits = ["unit", "npc", "interactable"],
                Portrait = "Assets/Templates/Units/villager_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/villager_sheet.png",
                    TileWidth = 32,
                    TileHeight = 48,
                    PivotX = 16,
                    PivotY = 40
                },
                Components = CreateNpcComponents("interaction.rescue"),
                Animations = CreateIdleAnimations("anim.template.villager")
            },
            new UnitDefinition
            {
                Id = "unit.boss",
                DisplayNameKey = "unit.boss.name",
                DisplayName = "洞穴首领",
                DescriptionKey = "unit.boss.description",
                Description = "使用 Boss 阶段战 AI 的首领样例。",
                UnitKind = "boss",
                FactionId = "faction.monster",
                AIProfileId = "ai.bossPhases",
                LootTableId = "loot.boss",
                Stats = CreateBehaviorStats(650, 32, 14, 22, 12, 85, 80),
                Tags = ["unit", "enemy", "boss", "attackable"],
                Traits = ["unit", "enemy", "attackable", "boss"],
                Portrait = "Assets/Templates/Units/boss_portrait.png",
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Units/boss_sheet.png",
                    TileWidth = 48,
                    TileHeight = 64,
                    PivotX = 24,
                    PivotY = 56
                },
                Components = CreateEnemyComponents("ai.bossPhases"),
                Animations = CreateSimpleCombatAnimations("anim.template.boss")
            },
            new UnitDefinition
            {
                Id = "unit.td.arrow_tower",
                DisplayNameKey = "unit.td.arrowTower.name",
                DisplayName = "箭塔哨台单位",
                DescriptionKey = "unit.td.arrowTower.description",
                Description = "可建造远程哨台使用的单位载体，真正的攻击逻辑由可建造单位资产引用技能实现。",
                UnitKind = "tower",
                FactionId = "faction.player",
                AIProfileId = "ai.passive",
                Stats = CreateBehaviorStats(260, 18, 10, 2, 6, 0, 0),
                Tags = ["unit", "tower", "defense", "attackable"],
                Traits = ["unit", "attackable"],
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/TowerDefense/arrow_tower.png",
                    TileWidth = 48,
                    TileHeight = 64,
                    PivotX = 24,
                    PivotY = 56
                },
                Components =
                [
                    CreateComponent("DefenseTower", ("skillId", "skill.td.arrowShot"), ("range", 5.5), ("targetPriority", "first")),
                    CreateComponent("Health", ("maxHpStat", "maxHp"))
                ],
                Animations = CreateIdleAnimations("anim.template.td.arrowTower")
            },
            new UnitDefinition
            {
                Id = "unit.td.slow_tower",
                DisplayNameKey = "unit.td.slowTower.name",
                DisplayName = "寒霜哨台单位",
                DescriptionKey = "unit.td.slowTower.description",
                Description = "可建造控制哨台使用的单位载体，攻击时附加减速状态。",
                UnitKind = "tower",
                FactionId = "faction.player",
                AIProfileId = "ai.passive",
                Stats = CreateBehaviorStats(220, 8, 8, 18, 10, 0, 0),
                Tags = ["unit", "tower", "control", "attackable"],
                Traits = ["unit", "attackable"],
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/TowerDefense/slow_tower.png",
                    TileWidth = 48,
                    TileHeight = 64,
                    PivotX = 24,
                    PivotY = 56
                },
                Components =
                [
                    CreateComponent("DefenseTower", ("skillId", "skill.td.frostBolt"), ("range", 4.5), ("targetPriority", "fastest")),
                    CreateComponent("Health", ("maxHpStat", "maxHp"))
                ],
                Animations = CreateIdleAnimations("anim.template.td.slowTower")
            },
            new UnitDefinition
            {
                Id = "unit.trap.spike",
                DisplayNameKey = "unit.trap.spike.name",
                DisplayName = "尖刺陷阱",
                DescriptionKey = "unit.trap.spike.description",
                Description = "可被放置、触发或回收的陷阱单位样例；具体触发流程由事件编辑器或运行时处理。",
                UnitKind = "summon",
                FactionId = "faction.player",
                AIProfileId = "ai.passive",
                Stats = CreateBehaviorStats(80, 20, 4, 0, 0, 0, 0),
                Tags = ["unit", "trap", "buildable", "attackable"],
                Traits = ["unit", "trap", "attackable"],
                Sprite = new SpriteSheetConfig
                {
                    Sheet = "Assets/Templates/Traps/spike_trap.png",
                    TileWidth = 32,
                    TileHeight = 32,
                    PivotX = 16,
                    PivotY = 28
                },
                Components =
                [
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("HitboxAttack", ("attackStat", "attack"), ("range", 1))
                ],
                Animations = CreateIdleAnimations("anim.template.trap.spike")
            }
        ];
    }

    public static List<ItemDefinition> CreateDefaultItems()
    {
        return
        [
            CreateDefaultItem(),
            new ItemDefinition
            {
                Id = "item.mana_potion",
                DisplayName = "小型法力药",
                Description = "恢复一定法力值。",
                TypeId = "consumable",
                Rarity = "common",
                Price = 18,
                StackLimit = 20,
                Consumable = true,
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.restore.mp"
                    }
                ],
                Tags = "potion,mana",
                CustomValues =
                [
                    new ItemFieldValue { Key = "useMode", Value = "instant" },
                    new ItemFieldValue { Key = "castTimeSeconds", Value = "0" },
                    new ItemFieldValue { Key = "cooldownSeconds", Value = "1" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.iron_sword",
                DisplayName = "铁剑",
                Description = "基础近战武器样例。",
                TypeId = "weapon",
                Rarity = "common",
                EquipmentSlot = "mainHand",
                Price = 90,
                StackLimit = 1,
                Consumable = false,
                GrantedSkillIds = ["skill.basic_slash"],
                Tags = "weapon,sword",
                CustomValues =
                [
                    new ItemFieldValue { Key = "attack", Value = "18" },
                    new ItemFieldValue { Key = "attackInterval", Value = "0.8" },
                    new ItemFieldValue { Key = "range", Value = "1.5" },
                    new ItemFieldValue { Key = "durability", Value = "100" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.vampire_dagger",
                DisplayName = "吸血匕首",
                Description = "命中时可触发生命偷取的稀有武器。",
                TypeId = "weapon",
                Rarity = "rare",
                EquipmentSlot = "mainHand",
                Price = 420,
                StackLimit = 1,
                Consumable = false,
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.lifeSteal",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "lifeStealRatio", Value = "0.12" },
                            new EffectParameterValue { Key = "restoreStatKey", Value = "maxHp" },
                            new EffectParameterValue { Key = "targetFilterMode", Value = "excludeTags" },
                            new EffectParameterValue { Key = "blockedTargetTags", Value = "undead,bloodless" },
                            new EffectParameterValue { Key = "stackMode", Value = "replace" },
                            new EffectParameterValue { Key = "chance", Value = "1" }
                        ]
                    }
                ],
                GrantedSkillIds = ["skill.vampiric_slash"],
                Tags = "weapon,dagger,lifeSteal",
                CustomValues =
                [
                    new ItemFieldValue { Key = "attack", Value = "14" },
                    new ItemFieldValue { Key = "attackInterval", Value = "0.55" },
                    new ItemFieldValue { Key = "range", Value = "1.2" },
                    new ItemFieldValue { Key = "durability", Value = "85" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.leather_armor",
                DisplayName = "皮甲",
                Description = "基础防具样例。",
                TypeId = "armor",
                Rarity = "common",
                EquipmentSlot = "body",
                Price = 70,
                StackLimit = 1,
                Consumable = false,
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.shield",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "targetStatKey", Value = "defense" },
                            new EffectParameterValue { Key = "baseValue", Value = "8" },
                            new EffectParameterValue { Key = "stackMode", Value = "replace" }
                        ]
                    }
                ],
                Tags = "armor,light",
                CustomValues =
                [
                    new ItemFieldValue { Key = "defense", Value = "8" },
                    new ItemFieldValue { Key = "weight", Value = "1.0" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.rescue_key",
                DisplayName = "牢门钥匙",
                Description = "用于触发开门或营救交互的关键道具，具体流程由事件编辑器实现。",
                TypeId = "keyItem",
                Rarity = "quest",
                Price = 0,
                StackLimit = 1,
                Consumable = false,
                Tags = "key,rescue,quest",
                CustomValues =
                [
                    new ItemFieldValue { Key = "boundInteraction", Value = "interaction.rescue" },
                    new ItemFieldValue { Key = "consumeOnUse", Value = "false" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.herb",
                DisplayName = "草药",
                Description = "可以作为恢复道具或合成材料的基础素材。",
                TypeId = "consumable",
                Rarity = "common",
                Price = 6,
                StackLimit = 50,
                Consumable = true,
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.heal.hp",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.heal" },
                            new EffectParameterValue { Key = "targetStatKey", Value = "maxHp" },
                            new EffectParameterValue { Key = "basePower", Value = "12" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "magicAttack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "0" }
                        ]
                    }
                ],
                Tags = "material,herb,heal",
                CustomValues =
                [
                    new ItemFieldValue { Key = "useMode", Value = "instant" },
                    new ItemFieldValue { Key = "castTimeSeconds", Value = "0" },
                    new ItemFieldValue { Key = "cooldownSeconds", Value = "0.5" }
                ]
            },
            new ItemDefinition
            {
                Id = "item.ancient_relic",
                DisplayName = "古代遗物",
                Description = "事件编辑器可引用的任务物品样例。",
                TypeId = "keyItem",
                Rarity = "quest",
                Price = 0,
                StackLimit = 1,
                Consumable = false,
                Tags = "quest,relic",
                CustomValues =
                [
                    new ItemFieldValue { Key = "boundInteraction", Value = "interaction.talk" },
                    new ItemFieldValue { Key = "consumeOnUse", Value = "false" }
                ]
            }
        ];
    }

    public static List<SkillDefinition> CreateDefaultSkills()
    {
        return
        [
            CreateDefaultSkill(),
            new SkillDefinition
            {
                Id = "skill.fireball",
                DisplayName = "火球术",
                Description = "发射火球，命中后造成火焰伤害并可施加燃烧。",
                SkillType = "active",
                TargetingMode = "aimedProjectile",
                RequiredTargetTags = ["unit", "attackable"],
                ElementId = "element.fire",
                DamageTypeId = "damage.magic",
                PowerStatKey = "magicAttack",
                PowerMultiplier = 1.6,
                BasePower = 18,
                CostStatKey = "maxMp",
                CostAmount = 12,
                CastTimeSeconds = 0.35,
                CooldownSeconds = 2.4,
                Range = 8,
                ProjectileId = "projectile.fireball",
                FormulaId = "formula.magicAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.damage.fire",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.magicAttack" },
                            new EffectParameterValue { Key = "damageTypeId", Value = "damage.magic" },
                            new EffectParameterValue { Key = "elementId", Value = "element.fire" },
                            new EffectParameterValue { Key = "basePower", Value = "18" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "magicAttack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "1.6" }
                        ]
                    },
                    new GameplayEffectReference
                    {
                        EffectId = "effect.burn",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "statusId", Value = "status.burn" },
                            new EffectParameterValue { Key = "durationSeconds", Value = "5" },
                            new EffectParameterValue { Key = "chance", Value = "0.5" }
                        ]
                    }
                ],
                StatusIds = ["status.burn"],
                VisualEffectId = "vfx.fireball",
                SoundCue = "sfx.fireball",
                Tags = "magic,projectile,fire"
            },
            new SkillDefinition
            {
                Id = "skill.heal",
                DisplayName = "治疗术",
                Description = "对自己或友方目标恢复生命值。",
                SkillType = "active",
                TargetingMode = "allyUnit",
                RequiredTargetTags = ["unit"],
                ElementId = "element.none",
                DamageTypeId = "",
                PowerStatKey = "magicAttack",
                PowerMultiplier = 1.2,
                BasePower = 28,
                CostStatKey = "maxMp",
                CostAmount = 10,
                CastTimeSeconds = 0.2,
                CooldownSeconds = 4,
                Range = 5,
                FormulaId = "formula.heal",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.heal.hp",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.heal" },
                            new EffectParameterValue { Key = "targetStatKey", Value = "maxHp" },
                            new EffectParameterValue { Key = "basePower", Value = "28" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "magicAttack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "1.2" }
                        ]
                    }
                ],
                VisualEffectId = "vfx.heal",
                SoundCue = "sfx.heal",
                Tags = "heal,support"
            },
            new SkillDefinition
            {
                Id = "skill.vampiric_slash",
                DisplayName = "吸血斩",
                Description = "近战攻击造成伤害，并按伤害比例回复自身生命。",
                SkillType = "active",
                TargetingMode = "selfForward",
                RequiredTargetTags = ["unit", "attackable"],
                BlockedTargetTags = ["undead", "bloodless"],
                ElementId = "element.blood",
                DamageTypeId = "damage.physical",
                PowerStatKey = "attack",
                PowerMultiplier = 1.25,
                BasePower = 10,
                CooldownSeconds = 3.2,
                Range = 1.4,
                FormulaId = "formula.physicalAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.damage.physical",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.physicalAttack" },
                            new EffectParameterValue { Key = "damageTypeId", Value = "damage.physical" },
                            new EffectParameterValue { Key = "elementId", Value = "element.blood" },
                            new EffectParameterValue { Key = "basePower", Value = "10" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "attack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "1.25" }
                        ]
                    },
                    new GameplayEffectReference
                    {
                        EffectId = "effect.lifeSteal",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "lifeStealRatio", Value = "0.18" },
                            new EffectParameterValue { Key = "restoreStatKey", Value = "maxHp" },
                            new EffectParameterValue { Key = "targetFilterMode", Value = "excludeTags" },
                            new EffectParameterValue { Key = "blockedTargetTags", Value = "undead,bloodless" },
                            new EffectParameterValue { Key = "stackMode", Value = "replace" },
                            new EffectParameterValue { Key = "chance", Value = "1" }
                        ]
                    }
                ],
                VisualEffectId = "vfx.slash",
                SoundCue = "sfx.slash_blood",
                Tags = "attack,melee,lifeSteal"
            },
            new SkillDefinition
            {
                Id = "skill.poison_cloud",
                DisplayName = "毒云",
                Description = "在目标区域生成毒云，对范围内目标施加中毒。",
                SkillType = "active",
                TargetingMode = "groundArea",
                RequiredTargetTags = ["unit"],
                ElementId = "element.poison",
                DamageTypeId = "damage.magic",
                PowerStatKey = "magicAttack",
                PowerMultiplier = 0.8,
                BasePower = 6,
                CostStatKey = "maxMp",
                CostAmount = 16,
                CastTimeSeconds = 0.4,
                CooldownSeconds = 6,
                Range = 6,
                AreaRadius = 2.4,
                FormulaId = "formula.magicAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.poison",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "statusId", Value = "status.poison" },
                            new EffectParameterValue { Key = "durationSeconds", Value = "6" },
                            new EffectParameterValue { Key = "chance", Value = "1" }
                        ]
                    }
                ],
                StatusIds = ["status.poison"],
                VisualEffectId = "vfx.poisonCloud",
                SoundCue = "sfx.poison_cloud",
                Tags = "magic,area,poison"
            },
            new SkillDefinition
            {
                Id = "skill.dash_slash",
                DisplayName = "冲刺斩",
                Description = "向前冲刺并击退命中的目标。",
                SkillType = "active",
                TargetingMode = "dashForward",
                RequiredTargetTags = ["unit", "attackable"],
                ElementId = "element.none",
                DamageTypeId = "damage.physical",
                PowerStatKey = "attack",
                PowerMultiplier = 1.1,
                BasePower = 12,
                CooldownSeconds = 5,
                Range = 3.2,
                FormulaId = "formula.physicalAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.damage.physical",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.physicalAttack" },
                            new EffectParameterValue { Key = "damageTypeId", Value = "damage.physical" },
                            new EffectParameterValue { Key = "basePower", Value = "12" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "attack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "1.1" }
                        ]
                    },
                    new GameplayEffectReference
                    {
                        EffectId = "effect.knockback",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "distance", Value = "2.4" },
                            new EffectParameterValue { Key = "durationSeconds", Value = "0.18" },
                            new EffectParameterValue { Key = "directionMode", Value = "forward" },
                            new EffectParameterValue { Key = "stopOnObstacle", Value = "true" }
                        ]
                    }
                ],
                VisualEffectId = "vfx.dash",
                SoundCue = "sfx.dash_slash",
                Tags = "attack,melee,mobility"
            },
            new SkillDefinition
            {
                Id = "skill.summon_guard",
                DisplayName = "召唤守卫",
                Description = "召唤一个守卫单位，具体生成位置和持续时间可由事件编辑器扩展。",
                SkillType = "active",
                TargetingMode = "groundPoint",
                ElementId = "element.none",
                DamageTypeId = "",
                PowerStatKey = "magicAttack",
                PowerMultiplier = 0,
                BasePower = 0,
                CostStatKey = "maxMp",
                CostAmount = 24,
                CastTimeSeconds = 0.8,
                CooldownSeconds = 12,
                Range = 5,
                AreaRadius = 0.5,
                VisualEffectId = "vfx.summon",
                SoundCue = "sfx.summon",
                Tags = "summon,utility"
            },
            new SkillDefinition
            {
                Id = "skill.trap_reclaim",
                DisplayNameKey = "skill.trapReclaim.name",
                DisplayName = "回收陷阱",
                DescriptionKey = "skill.trapReclaim.description",
                Description = "只选择陷阱目标或范围内陷阱；返还资源、移除陷阱等流程由事件触发器或运行时处理。",
                SkillType = "active",
                TargetingMode = "areaTaggedUnits",
                RequiredTargetTags = ["trap"],
                ElementId = "element.none",
                DamageTypeId = "",
                PowerMultiplier = 0,
                BasePower = 0,
                CooldownSeconds = 1,
                Range = 4,
                AreaRadius = 2,
                VisualEffectId = "vfx.summon",
                SoundCue = "sfx.reclaim",
                Tags = "utility,trap"
            },
            new SkillDefinition
            {
                Id = "skill.td.arrowShot",
                DisplayNameKey = "skill.td.arrowShot.name",
                DisplayName = "自动箭矢",
                DescriptionKey = "skill.td.arrowShot.description",
                Description = "固定哨台或自动攻击单位使用的单体物理射击。",
                SkillType = "active",
                TargetingMode = "taggedUnit",
                RequiredTargetTags = ["unit", "enemy"],
                ElementId = "element.none",
                DamageTypeId = "damage.physical",
                PowerStatKey = "attack",
                PowerMultiplier = 1,
                BasePower = 12,
                CooldownSeconds = 0.8,
                Range = 5.5,
                ProjectileId = "projectile.td.arrow",
                FormulaId = "formula.physicalAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.damage.physical",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.physicalAttack" },
                            new EffectParameterValue { Key = "damageTypeId", Value = "damage.physical" },
                            new EffectParameterValue { Key = "basePower", Value = "12" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "attack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "1" }
                        ]
                    }
                ],
                VisualEffectId = "vfx.slash",
                SoundCue = "sfx.arrow",
                Tags = "tower,attack,projectile"
            },
            new SkillDefinition
            {
                Id = "skill.td.frostBolt",
                DisplayNameKey = "skill.td.frostBolt.name",
                DisplayName = "寒霜弹",
                DescriptionKey = "skill.td.frostBolt.description",
                Description = "固定哨台、陷阱或远程单位可复用的减速射击。",
                SkillType = "active",
                TargetingMode = "taggedUnit",
                RequiredTargetTags = ["unit", "enemy"],
                ElementId = "element.none",
                DamageTypeId = "damage.magic",
                PowerStatKey = "magicAttack",
                PowerMultiplier = 0.8,
                BasePower = 6,
                CooldownSeconds = 1.2,
                Range = 4.5,
                ProjectileId = "projectile.td.frostBolt",
                FormulaId = "formula.magicAttack",
                Effects =
                [
                    new GameplayEffectReference
                    {
                        EffectId = "effect.damage.physical",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "formulaId", Value = "formula.magicAttack" },
                            new EffectParameterValue { Key = "damageTypeId", Value = "damage.magic" },
                            new EffectParameterValue { Key = "basePower", Value = "6" },
                            new EffectParameterValue { Key = "powerStatKey", Value = "magicAttack" },
                            new EffectParameterValue { Key = "powerMultiplier", Value = "0.8" }
                        ]
                    },
                    new GameplayEffectReference
                    {
                        EffectId = "effect.applyStatus",
                        Parameters =
                        [
                            new EffectParameterValue { Key = "statusId", Value = "status.td.slow" },
                            new EffectParameterValue { Key = "durationSeconds", Value = "2.5" },
                            new EffectParameterValue { Key = "chance", Value = "1" }
                        ]
                    }
                ],
                VisualEffectId = "vfx.frost_bolt",
                SoundCue = "sfx.frost",
                Tags = "tower,projectile,control"
            }
        ];
    }

    public static List<MapDefinition> CreateDefaultMaps()
    {
        return
        [
            CreateDefaultMap()
            ,
            new MapDefinition
            {
                Id = "map.training_ground",
                DisplayNameKey = "map.trainingGround.name",
                DisplayName = "训练场",
                DescriptionKey = "map.trainingGround.description",
                Description = "用于快速测试单位、技能、路线、格子、资源和流程规则的样例地图。",
                ViewType = "TopDown",
                Width = 64,
                Height = 64,
                Tileset = "tileset.default"
            }
        ];
    }

    public static List<ItemEffectDefinition> CreateDefaultItemEffects()
    {
        return
        [
            new ItemEffectDefinition
            {
                Id = "effect.heal",
                DisplayNameKey = "item.effectType.heal",
                DisplayName = "治疗",
                DescriptionKey = "item.effectType.heal.description",
                Description = "恢复生命值。",
                BuiltIn = true
            },
            new ItemEffectDefinition
            {
                Id = "effect.damage",
                DisplayNameKey = "item.effectType.damage",
                DisplayName = "伤害",
                DescriptionKey = "item.effectType.damage.description",
                Description = "造成伤害。",
                BuiltIn = true
            },
            new ItemEffectDefinition
            {
                Id = "effect.buff",
                DisplayNameKey = "item.effectType.buff",
                DisplayName = "增益",
                DescriptionKey = "item.effectType.buff.description",
                Description = "施加正面状态。",
                BuiltIn = true
            },
            new ItemEffectDefinition
            {
                Id = "effect.debuff",
                DisplayNameKey = "item.effectType.debuff",
                DisplayName = "减益",
                DescriptionKey = "item.effectType.debuff.description",
                Description = "施加负面状态。",
                BuiltIn = true
            }
        ];
    }

    public static List<BehaviorPresetDefinition> CreateDefaultBehaviorPresets()
    {
        return
        [
            CreateBehaviorPreset(
                id: "Player",
                displayNameKey: "behavior.player.name",
                descriptionKey: "behavior.player.description",
                displayName: "玩家操控",
                description: "用于玩家直接操作的单位模板，包含输入、移动、镜头跟随和基础生命值。",
                portrait: "Assets/Templates/Units/player_portrait.png",
                sheet: "Assets/Templates/Units/player_sheet.png",
                tileWidth: 32,
                tileHeight: 48,
                pivotX: 16,
                pivotY: 40,
                stats: CreateBehaviorStats(140, 16, 8, 10, 8, 130, 0),
                tags: ["unit", "player", "human"],
                traits: ["unit", "player"],
                components:
                [
                    CreateComponent("PlayerInput"),
                    CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                    CreateComponent("CameraFollow"),
                    CreateComponent("Health", ("maxHpStat", "maxHp"))
                ],
                animations:
                [
                    ("idleDown", "anim.template.player.idle_down"),
                    ("idleLeft", "anim.template.player.idle_left"),
                    ("idleRight", "anim.template.player.idle_right"),
                    ("idleUp", "anim.template.player.idle_up"),
                    ("walkDown", "anim.template.player.walk_down"),
                    ("walkLeft", "anim.template.player.walk_left"),
                    ("walkRight", "anim.template.player.walk_right"),
                    ("walkUp", "anim.template.player.walk_up"),
                    ("attackDown", "anim.template.player.attack_down"),
                    ("hitDown", "anim.template.player.hit_down"),
                    ("dead", "anim.template.player.dead")
                ]),
            CreateBehaviorPreset(
                id: "Passive",
                displayNameKey: "behavior.passive.name",
                descriptionKey: "behavior.passive.description",
                displayName: "被动待机",
                description: "适合村民、商人、剧情 NPC 和不主动寻敌的单位。",
                portrait: "Assets/Templates/Units/passive_portrait.png",
                sheet: "Assets/Templates/Units/passive_sheet.png",
                tileWidth: 32,
                tileHeight: 48,
                pivotX: 16,
                pivotY: 40,
                stats: CreateBehaviorStats(60, 4, 2, 2, 2, 90, 0),
                tags: ["unit", "npc"],
                traits: ["unit", "npc"],
                components:
                [
                    CreateComponent("IdleBrain"),
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("Interactable", ("interactionMode", "dialogue"))
                ],
                animations:
                [
                    ("idleDown", "anim.template.passive.idle_down"),
                    ("idleLeft", "anim.template.passive.idle_left"),
                    ("idleRight", "anim.template.passive.idle_right"),
                    ("idleUp", "anim.template.passive.idle_up")
                ]),
            CreateBehaviorPreset(
                id: "MeleeChase",
                displayNameKey: "behavior.meleeChase.name",
                descriptionKey: "behavior.meleeChase.description",
                displayName: "近战追击",
                description: "追击目标并在近战范围内发起攻击，适合普通敌人模板。",
                portrait: "Assets/Templates/Units/melee_chase_portrait.png",
                sheet: "Assets/Templates/Units/melee_chase_sheet.png",
                tileWidth: 32,
                tileHeight: 48,
                pivotX: 16,
                pivotY: 40,
                stats: CreateBehaviorStats(120, 18, 6, 10, 6, 120, 10),
                tags: ["unit", "enemy", "attackable"],
                traits: ["unit", "enemy", "attackable"],
                components:
                [
                    CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                    CreateComponent("ChaseTargetAI", ("detectionRange", 8), ("loseRange", 12)),
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("HitboxAttack", ("attackStat", "attack"), ("range", 1.5))
                ],
                animations:
                [
                    ("idleDown", "anim.template.meleeChase.idle_down"),
                    ("walkDown", "anim.template.meleeChase.walk_down"),
                    ("attackDown", "anim.template.meleeChase.attack_down"),
                    ("hitDown", "anim.template.meleeChase.hit_down"),
                    ("dead", "anim.template.meleeChase.dead")
                ]),
            CreateBehaviorPreset(
                id: "Patrol",
                displayNameKey: "behavior.patrol.name",
                descriptionKey: "behavior.patrol.description",
                displayName: "巡逻守卫",
                description: "沿路线巡逻，在发现目标后切换为追击或攻击状态。",
                portrait: "Assets/Templates/Units/patrol_portrait.png",
                sheet: "Assets/Templates/Units/patrol_sheet.png",
                tileWidth: 32,
                tileHeight: 48,
                pivotX: 16,
                pivotY: 40,
                stats: CreateBehaviorStats(100, 14, 5, 8, 5, 110, 12),
                tags: ["unit", "enemy", "guard"],
                traits: ["unit", "enemy", "attackable"],
                components:
                [
                    CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                    CreateComponent("PatrolAI", ("patrolRadius", 10), ("turnDelay", 1.5)),
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("DetectionRadius", ("range", 6))
                ],
                animations:
                [
                    ("idleDown", "anim.template.patrol.idle_down"),
                    ("walkDown", "anim.template.patrol.walk_down"),
                    ("alertDown", "anim.template.patrol.alert_down"),
                    ("attackDown", "anim.template.patrol.attack_down"),
                    ("dead", "anim.template.patrol.dead")
                ]),
            CreateBehaviorPreset(
                id: "RangedGuard",
                displayNameKey: "behavior.rangedGuard.name",
                descriptionKey: "behavior.rangedGuard.description",
                displayName: "远程守卫",
                description: "保持距离并使用远程攻击，适合哨兵或法师类敌人。",
                portrait: "Assets/Templates/Units/ranged_guard_portrait.png",
                sheet: "Assets/Templates/Units/ranged_guard_sheet.png",
                tileWidth: 32,
                tileHeight: 48,
                pivotX: 16,
                pivotY: 40,
                stats: CreateBehaviorStats(90, 12, 4, 14, 6, 110, 16),
                tags: ["unit", "enemy", "ranged"],
                traits: ["unit", "enemy", "attackable"],
                components:
                [
                    CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                    CreateComponent("ProjectileShooter", ("projectileId", "projectile.template.basic"), ("cooldown", 1.2)),
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("LineOfSightAI", ("range", 8))
                ],
                animations:
                [
                    ("idleDown", "anim.template.rangedGuard.idle_down"),
                    ("walkDown", "anim.template.rangedGuard.walk_down"),
                    ("castDown", "anim.template.rangedGuard.cast_down"),
                    ("hitDown", "anim.template.rangedGuard.hit_down"),
                    ("dead", "anim.template.rangedGuard.dead")
                ]),
            CreateBehaviorPreset(
                id: "Boss",
                displayNameKey: "behavior.boss.name",
                descriptionKey: "behavior.boss.description",
                displayName: "首领模板",
                description: "高生命值、高伤害的首领单位模板，适合 Boss 或阶段战斗。",
                portrait: "Assets/Templates/Units/boss_portrait.png",
                sheet: "Assets/Templates/Units/boss_sheet.png",
                tileWidth: 48,
                tileHeight: 64,
                pivotX: 24,
                pivotY: 56,
                stats: CreateBehaviorStats(600, 30, 15, 20, 12, 85, 80),
                tags: ["unit", "enemy", "boss", "attackable"],
                traits: ["unit", "enemy", "attackable"],
                components:
                [
                    CreateComponent("BossPhaseController", ("phaseCount", 3)),
                    CreateComponent("ChaseTargetAI", ("detectionRange", 10), ("loseRange", 14)),
                    CreateComponent("Health", ("maxHpStat", "maxHp")),
                    CreateComponent("HitboxAttack", ("attackStat", "attack"), ("range", 2.2)),
                    CreateComponent("Knockback", ("force", 18))
                ],
                animations:
                [
                    ("idleDown", "anim.template.boss.idle_down"),
                    ("walkDown", "anim.template.boss.walk_down"),
                    ("attackDown", "anim.template.boss.attack_down"),
                    ("rageDown", "anim.template.boss.rage_down"),
                    ("hitDown", "anim.template.boss.hit_down"),
                    ("dead", "anim.template.boss.dead")
                ])
        ];
    }

    public static List<TraitDefinition> CreateDefaultTraits()
    {
        return
        [
            CreateTrait("unit", "trait.unit.name", "trait.unit.description", "trait.category.entity"),
            CreateTrait("player", "trait.player.name", "trait.player.description", "trait.category.entity"),
            CreateTrait("enemy", "trait.enemy.name", "trait.enemy.description", "trait.category.entity"),
            CreateTrait("attackable", "trait.attackable.name", "trait.attackable.description", "trait.category.combat"),
            CreateTrait("building", "trait.building.name", "trait.building.description", "trait.category.world"),
            CreateTrait("trap", "trait.trap.name", "trait.trap.description", "trait.category.world"),
            CreateTrait("npc", "trait.npc.name", "trait.npc.description", "trait.category.entity")
        ];
    }

    private static List<string> MergeStrings(IEnumerable<string> values, IEnumerable<string>? extraValues = null)
    {
        var result = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        if (extraValues is not null)
        {
            foreach (var value in extraValues)
            {
                var trimmed = value?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (!result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(trimmed);
                }
            }
        }

        return result;
    }

    private static SpriteSheetConfig CloneSprite(SpriteSheetConfig sprite)
    {
        return new SpriteSheetConfig
        {
            Sheet = sprite.Sheet,
            TileWidth = sprite.TileWidth,
            TileHeight = sprite.TileHeight,
            PivotX = sprite.PivotX,
            PivotY = sprite.PivotY
        };
    }

    private static List<ComponentConfig> CloneComponents(IEnumerable<ComponentConfig> components)
    {
        return components.Select(component => new ComponentConfig
        {
            Type = component.Type,
            Parameters = new Dictionary<string, object?>(component.Parameters, StringComparer.OrdinalIgnoreCase)
        }).ToList();
    }

    private static Dictionary<string, string> CloneAnimations(Dictionary<string, string> animations)
    {
        return new Dictionary<string, string>(animations, StringComparer.OrdinalIgnoreCase);
    }

    private static StatDefinition CreateStat(string key, string displayNameKey, double defaultValue, double min, double max, string categoryKey, string valueType = "Number")
    {
        return new StatDefinition
        {
            Id = $"stat.{key}",
            Key = key,
            DisplayNameKey = displayNameKey,
            ValueType = valueType,
            DefaultValue = defaultValue,
            Min = min,
            Max = max,
            Category = StatCategoryFallback(categoryKey),
            CategoryKey = categoryKey
        };
    }

    private static TraitDefinition CreateTrait(string id, string displayNameKey, string descriptionKey, string categoryKey)
    {
        return new TraitDefinition
        {
            Id = id,
            DisplayNameKey = displayNameKey,
            DisplayName = displayNameKey switch
            {
                "trait.unit.name" => "单位",
                "trait.player.name" => "玩家",
                "trait.enemy.name" => "敌人",
                "trait.attackable.name" => "可攻击",
                "trait.building.name" => "建筑",
                "trait.trap.name" => "陷阱",
                "trait.npc.name" => "NPC",
                _ => id
            },
            DescriptionKey = descriptionKey,
            Description = descriptionKey switch
            {
                "trait.unit.description" => "表示这是一个可参与规则系统的基础单位。",
                "trait.player.description" => "表示这是由玩家控制的单位。",
                "trait.enemy.description" => "表示这是会进入敌对逻辑的单位。",
                "trait.attackable.description" => "表示这个目标可以被普通攻击或伤害技能选中。",
                "trait.building.description" => "表示这是一个建筑类目标。",
                "trait.trap.description" => "表示这是一个陷阱类目标。",
                "trait.npc.description" => "表示这是一个非敌对的交互单位。",
                _ => string.Empty
            },
            Category = TraitCategoryFallback(categoryKey),
            CategoryKey = categoryKey,
            BuiltIn = true
        };
    }

    private static string StatCategoryFallback(string categoryKey)
    {
        return categoryKey switch
        {
            "stat.category.combat" => "战斗",
            "stat.category.movement" => "移动",
            "stat.category.reward" => "奖励",
            "stat.category.strategy" => "资源",
            "stat.category.general" => "通用",
            _ => "通用"
        };
    }

    private static string TraitCategoryFallback(string categoryKey)
    {
        return categoryKey switch
        {
            "trait.category.entity" => "实体",
            "trait.category.combat" => "战斗",
            "trait.category.world" => "世界",
            "trait.category.general" => "通用",
            _ => "通用"
        };
    }

    private static BehaviorPresetDefinition CreateBehaviorPreset(
        string id,
        string displayNameKey,
        string descriptionKey,
        string displayName,
        string description,
        string portrait,
        string sheet,
        int tileWidth,
        int tileHeight,
        int pivotX,
        int pivotY,
        Dictionary<string, double> stats,
        IEnumerable<string> tags,
        IEnumerable<string> traits,
        IEnumerable<ComponentConfig> components,
        IEnumerable<(string Key, string Value)> animations)
    {
        return new BehaviorPresetDefinition
        {
            Id = id,
            DisplayNameKey = displayNameKey,
            DisplayName = displayName,
            DescriptionKey = descriptionKey,
            Description = description,
            Portrait = portrait,
            Sprite = new SpriteSheetConfig
            {
                Sheet = sheet,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                PivotX = pivotX,
                PivotY = pivotY
            },
            Stats = stats,
            Tags = tags.ToList(),
            Traits = traits.ToList(),
            Components = components.ToList(),
            Animations = animations.ToDictionary(v => v.Key, v => v.Value, StringComparer.OrdinalIgnoreCase),
            BuiltIn = true
        };
    }

    private static ComponentConfig CreateComponent(string type, params (string Key, object? Value)[] parameters)
    {
        var component = new ComponentConfig
        {
            Type = type
        };

        foreach (var (key, value) in parameters)
        {
            component.Parameters[key] = value;
        }

        return component;
    }

    private static Dictionary<string, double> CreateBehaviorStats(double maxHp, double attack, double defense, double magicAttack, double magicDefense, double moveSpeed, double rewardExp)
    {
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["maxHp"] = maxHp,
            ["maxMp"] = Math.Max(0, maxHp * 0.35),
            ["attack"] = attack,
            ["defense"] = defense,
            ["magicAttack"] = magicAttack,
            ["magicDefense"] = magicDefense,
            ["moveSpeed"] = moveSpeed,
            ["rewardExp"] = rewardExp
        };
    }

    private static Dictionary<string, double> CreateDefaultCombatStats()
    {
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["maxHp"] = 120,
            ["maxMp"] = 60,
            ["attack"] = 18,
            ["defense"] = 6,
            ["magicAttack"] = 10,
            ["magicDefense"] = 6,
            ["moveSpeed"] = 120
        };
    }
}
