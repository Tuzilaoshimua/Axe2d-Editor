using System.Text.Json;
using System.Text.Json.Serialization;
using Axe2DEditor.Core.Actors;
using Axe2DEditor.Core.Ai;
using Axe2DEditor.Core.Behaviors;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Decorations;
using Axe2DEditor.Core.Enemies;
using Axe2DEditor.Core.Effects;
using Axe2DEditor.Core.Interactions;
using Axe2DEditor.Core.Items;
using Axe2DEditor.Core.Loot;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Projectiles;
using Axe2DEditor.Core.Rules;
using Axe2DEditor.Core.Skills;
using Axe2DEditor.Core.Stats;
using Axe2DEditor.Core.Strategy;
using Axe2DEditor.Core.Tactics;
using Axe2DEditor.Core.Traits;
using Axe2DEditor.Core.TowerDefense;
using Axe2DEditor.Core.Units;

namespace Axe2DEditor.Core.Assets;

public sealed class AssetLibrary
{
    public List<AssetCategory> Categories { get; set; } = CreateDefaultCategories();

    public List<ItemTypeDefinition> ItemTypes { get; set; } = [];

    public List<StatDefinition> Stats { get; set; } = [];

    public List<TraitDefinition> Traits { get; set; } = [];

    public List<FactionDefinition> Factions { get; set; } = [];

    public List<DamageTypeDefinition> DamageTypes { get; set; } = [];

    public List<ElementDefinition> Elements { get; set; } = [];

    public List<FormulaDefinition> Formulas { get; set; } = [];

    public List<OptionSetDefinition> OptionSets { get; set; } = [];

    public bool DefaultAssetsInitialized { get; set; }

    public bool OptionSetsInitialized { get; set; }

    public int OptionSetCatalogVersion { get; set; }

    public bool BuiltInLocksReleased { get; set; }

    public List<UnitDefinition> Units { get; set; } = [];

    public List<AIProfileDefinition> AIProfiles { get; set; } = [];

    public List<ActorDefinition> Actors { get; set; } = [];

    public List<EnemyDefinition> Enemies { get; set; } = [];

    public List<ItemDefinition> Items { get; set; } = [];

    public List<ItemEffectDefinition> ItemEffects { get; set; } = [];

    public List<GameplayEffectDefinition> GameplayEffects { get; set; } = [];

    public List<StatusDefinition> Statuses { get; set; } = [];

    public List<SkillDefinition> Skills { get; set; } = [];

    public List<ProjectileDefinition> Projectiles { get; set; } = [];

    public List<VisualEffectDefinition> VisualEffects { get; set; } = [];

    public List<DecorationDefinition> Decorations { get; set; } = [];

    public List<LootTableDefinition> LootTables { get; set; } = [];

    public List<ComponentPresetDefinition> ComponentPresets { get; set; } = [];

    public List<InteractionProfileDefinition> InteractionProfiles { get; set; } = [];

    public List<MapDefinition> Maps { get; set; } = [];

    public List<BehaviorPresetDefinition> BehaviorPresets { get; set; } = [];

    public List<TowerDefensePathDefinition> Routes { get; set; } = [];

    public List<TowerDefenseWaveDefinition> SpawnWaves { get; set; } = [];

    public List<TowerDefenseRuleDefinition> LevelRules { get; set; } = [];

    public List<TowerDefenseBuildRuleDefinition> BuildRules { get; set; } = [];

    public List<TowerDefenseTowerDefinition> BuildableUnits { get; set; } = [];

    public List<TacticalGridRuleDefinition> TacticalGridRules { get; set; } = [];

    public List<TerrainRuleDefinition> TerrainRules { get; set; } = [];

    public List<TurnRuleDefinition> TurnRules { get; set; } = [];

    public List<ActionRuleDefinition> ActionRules { get; set; } = [];

    public List<TacticalRangeDefinition> TacticalRanges { get; set; } = [];

    public List<ObjectiveRuleDefinition> ObjectiveRules { get; set; } = [];

    public List<BondRuleDefinition> BondRules { get; set; } = [];

    public List<ResourceRuleDefinition> ResourceRules { get; set; } = [];

    public List<ProductionRuleDefinition> ProductionRules { get; set; } = [];

    public List<TechRuleDefinition> TechRules { get; set; } = [];

    public List<DiplomacyRuleDefinition> DiplomacyRules { get; set; } = [];

    public List<TerritoryRuleDefinition> TerritoryRules { get; set; } = [];

    [JsonPropertyName("towerDefensePaths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TowerDefensePathDefinition>? LegacyTowerDefensePaths
    {
        get => null;
        set => MergeLegacyAssets(Routes, value, v => v.Id);
    }

    [JsonPropertyName("towerDefenseWaves")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TowerDefenseWaveDefinition>? LegacyTowerDefenseWaves
    {
        get => null;
        set => MergeLegacyAssets(SpawnWaves, value, v => v.Id);
    }

    [JsonPropertyName("towerDefenseRules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TowerDefenseRuleDefinition>? LegacyTowerDefenseRules
    {
        get => null;
        set => MergeLegacyAssets(LevelRules, value, v => v.Id);
    }

    [JsonPropertyName("towerDefenseBuildRules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TowerDefenseBuildRuleDefinition>? LegacyTowerDefenseBuildRules
    {
        get => null;
        set => MergeLegacyAssets(BuildRules, value, v => v.Id);
    }

    [JsonPropertyName("towerDefenseTowers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TowerDefenseTowerDefinition>? LegacyTowerDefenseTowers
    {
        get => null;
        set => MergeLegacyAssets(BuildableUnits, value, v => v.Id);
    }

    [JsonIgnore]
    public List<TowerDefensePathDefinition> TowerDefensePaths
    {
        get => Routes;
        set => Routes = value ?? [];
    }

    [JsonIgnore]
    public List<TowerDefenseWaveDefinition> TowerDefenseWaves
    {
        get => SpawnWaves;
        set => SpawnWaves = value ?? [];
    }

    [JsonIgnore]
    public List<TowerDefenseRuleDefinition> TowerDefenseRules
    {
        get => LevelRules;
        set => LevelRules = value ?? [];
    }

    [JsonIgnore]
    public List<TowerDefenseBuildRuleDefinition> TowerDefenseBuildRules
    {
        get => BuildRules;
        set => BuildRules = value ?? [];
    }

    [JsonIgnore]
    public List<TowerDefenseTowerDefinition> TowerDefenseTowers
    {
        get => BuildableUnits;
        set => BuildableUnits = value ?? [];
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? LegacyData { get; set; }

    public static List<AssetCategory> CreateDefaultCategories()
    {
        return
        [
            new() { Key = "content.units", DisplayNameKey = "asset.units" },
            new() { Key = "content.items", DisplayNameKey = "asset.items" },
            new() { Key = "content.skills", DisplayNameKey = "asset.skills" },
            new() { Key = "content.projectiles", DisplayNameKey = "asset.projectiles" },
            new() { Key = "content.visualEffects", DisplayNameKey = "asset.visualEffects" },
            new() { Key = "content.decorations", DisplayNameKey = "asset.decorations" },
            new() { Key = "content.maps", DisplayNameKey = "tree.maps" },
            new() { Key = "rules.factions", DisplayNameKey = "asset.factions" },
            new() { Key = "rules.stats", DisplayNameKey = "asset.stats" },
            new() { Key = "rules.traits", DisplayNameKey = "asset.traits" },
            new() { Key = "rules.damageTypes", DisplayNameKey = "asset.damageTypes" },
            new() { Key = "rules.elements", DisplayNameKey = "asset.elements" },
            new() { Key = "rules.formulas", DisplayNameKey = "asset.formulas" },
            new() { Key = "rules.optionSets", DisplayNameKey = "asset.optionSets" },
            new() { Key = "rules.aiProfiles", DisplayNameKey = "asset.aiProfiles" },
            new() { Key = "rules.itemTypes", DisplayNameKey = "asset.itemTypes" },
            new() { Key = "rules.effects", DisplayNameKey = "asset.gameplayEffects" },
            new() { Key = "rules.statuses", DisplayNameKey = "asset.statuses" },
            new() { Key = "rules.lootTables", DisplayNameKey = "asset.lootTables" },
            new() { Key = "rules.componentPresets", DisplayNameKey = "asset.componentPresets" },
            new() { Key = "rules.interactions", DisplayNameKey = "asset.interactions" },
            new() { Key = "rules.itemEffects", DisplayNameKey = "asset.itemEffects" },
            new() { Key = "rules.behaviors", DisplayNameKey = "asset.behaviors" },
            new() { Key = "scene.routes", DisplayNameKey = "asset.routes" },
            new() { Key = "unit.spawnWaves", DisplayNameKey = "asset.spawnWaves" },
            new() { Key = "rules.levelRules", DisplayNameKey = "asset.levelRules" },
            new() { Key = "rules.buildRules", DisplayNameKey = "asset.buildRules" },
            new() { Key = "unit.buildableUnits", DisplayNameKey = "asset.buildableUnits" },
            new() { Key = "scene.tacticalGridRules", DisplayNameKey = "asset.tacticalGridRules" },
            new() { Key = "scene.terrainRules", DisplayNameKey = "asset.terrainRules" },
            new() { Key = "rules.turnRules", DisplayNameKey = "asset.turnRules" },
            new() { Key = "rules.actionRules", DisplayNameKey = "asset.actionRules" },
            new() { Key = "combat.tacticalRanges", DisplayNameKey = "asset.tacticalRanges" },
            new() { Key = "rules.objectiveRules", DisplayNameKey = "asset.objectiveRules" },
            new() { Key = "rules.bondRules", DisplayNameKey = "asset.bondRules" },
            new() { Key = "rules.resourceRules", DisplayNameKey = "asset.resourceRules" },
            new() { Key = "rules.productionRules", DisplayNameKey = "asset.productionRules" },
            new() { Key = "rules.techRules", DisplayNameKey = "asset.techRules" },
            new() { Key = "rules.diplomacyRules", DisplayNameKey = "asset.diplomacyRules" },
            new() { Key = "scene.territoryRules", DisplayNameKey = "asset.territoryRules" }
        ];
    }

    private static void MergeLegacyAssets<T>(List<T> target, IEnumerable<T>? legacyItems, Func<T, string> getId)
    {
        if (legacyItems is null)
        {
            return;
        }

        var existing = target
            .Select(getId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in legacyItems)
        {
            var id = getId(item);
            if (string.IsNullOrWhiteSpace(id) || existing.Add(id))
            {
                target.Add(item);
            }
        }
    }

    public static List<ItemTypeDefinition> CreateDefaultItemTypes()
    {
        return
        [
            new ItemTypeDefinition
            {
                Id = "consumable",
                DisplayNameKey = "itemType.consumable.name",
                DescriptionKey = "itemType.consumable.description",
                BuiltIn = true,
                Fields =
                [
                    new ItemFieldDefinition
                    {
                        Key = "useMode",
                        DisplayNameKey = "itemField.useMode.name",
                        ValueType = ItemFieldValueType.Choice,
                        DefaultValue = "instant",
                        Options = ["instant", "targeted", "selfOnly"],
                        CategoryKey = "itemField.category.core",
                        Order = 1
                    },
                    new ItemFieldDefinition
                    {
                        Key = "castTimeSeconds",
                        DisplayNameKey = "itemField.castTimeSeconds.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "0",
                        CategoryKey = "itemField.category.core",
                        Order = 2
                    },
                    new ItemFieldDefinition
                    {
                        Key = "cooldownSeconds",
                        DisplayNameKey = "itemField.cooldownSeconds.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "0",
                        CategoryKey = "itemField.category.core",
                        Order = 3
                    }
                ]
            },
            new ItemTypeDefinition
            {
                Id = "weapon",
                DisplayNameKey = "itemType.weapon.name",
                DescriptionKey = "itemType.weapon.description",
                BuiltIn = true,
                Fields =
                [
                    new ItemFieldDefinition
                    {
                        Key = "attack",
                        DisplayNameKey = "itemField.attack.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "10",
                        CategoryKey = "itemField.category.combat",
                        Order = 1
                    },
                    new ItemFieldDefinition
                    {
                        Key = "attackInterval",
                        DisplayNameKey = "itemField.attackInterval.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "0.8",
                        CategoryKey = "itemField.category.combat",
                        Order = 2
                    },
                    new ItemFieldDefinition
                    {
                        Key = "range",
                        DisplayNameKey = "itemField.range.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "1.5",
                        CategoryKey = "itemField.category.combat",
                        Order = 3
                    },
                    new ItemFieldDefinition
                    {
                        Key = "durability",
                        DisplayNameKey = "itemField.durability.name",
                        ValueType = ItemFieldValueType.Integer,
                        DefaultValue = "100",
                        CategoryKey = "itemField.category.durability",
                        Order = 4
                    }
                ]
            },
            new ItemTypeDefinition
            {
                Id = "armor",
                DisplayNameKey = "itemType.armor.name",
                DescriptionKey = "itemType.armor.description",
                BuiltIn = true,
                Fields =
                [
                    new ItemFieldDefinition
                    {
                        Key = "defense",
                        DisplayNameKey = "itemField.defense.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "8",
                        CategoryKey = "itemField.category.combat",
                        Order = 1
                    },
                    new ItemFieldDefinition
                    {
                        Key = "weight",
                        DisplayNameKey = "itemField.weight.name",
                        ValueType = ItemFieldValueType.Number,
                        DefaultValue = "1.0",
                        CategoryKey = "itemField.category.equipment",
                        Order = 2
                    }
                ]
            },
            new ItemTypeDefinition
            {
                Id = "keyItem",
                DisplayNameKey = "itemType.keyItem.name",
                DescriptionKey = "itemType.keyItem.description",
                BuiltIn = true,
                Fields =
                [
                    new ItemFieldDefinition
                    {
                        Key = "boundInteraction",
                        DisplayNameKey = "itemField.boundInteraction.name",
                        ValueType = ItemFieldValueType.AssetRef,
                        DefaultValue = "interaction.open",
                        CategoryKey = "itemField.category.core",
                        Order = 1
                    },
                    new ItemFieldDefinition
                    {
                        Key = "consumeOnUse",
                        DisplayNameKey = "itemField.consumeOnUse.name",
                        ValueType = ItemFieldValueType.Boolean,
                        DefaultValue = "false",
                        CategoryKey = "itemField.category.core",
                        Order = 2
                    }
                ]
            }
        ];
    }
}
