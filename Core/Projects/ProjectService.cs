using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Axe2DEditor.Core.Assets;
using Axe2DEditor.Core.Behaviors;
using Axe2DEditor.Core.Enemies;
using Axe2DEditor.Core.Items;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Stats;
using Axe2DEditor.Core.Units;
using Axe2DEditor.Core.Graphs;

namespace Axe2DEditor.Core.Projects;

public sealed class ProjectService
{
    public const string ProjectFileName = "project.json";
    private const string RewardExpStatKey = "rewardExp";
    private const int CurrentOptionSetCatalogVersion = 7;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ProjectContext CreateProject(string rootDirectory, string projectName)
    {
        Directory.CreateDirectory(rootDirectory);

        var project = new AxeProject
        {
            Name = projectName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        project.HierarchyTree = CreateDefaultHierarchyTree();
        project.ResourceTree = CreateDefaultResourceTree();
        project.EventGraphs = [CreateDefaultEventGraph()];
        NormalizeProject(project);

        CreateProjectFolders(rootDirectory, project.Paths);

        var context = new ProjectContext(project, rootDirectory);
        SaveProject(context);
        return context;
    }

    public ProjectContext OpenProject(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException("Project file was not found.", projectFilePath);
        }

        var json = File.ReadAllText(projectFilePath);
        var project = JsonSerializer.Deserialize<AxeProject>(json, JsonOptions)
            ?? throw new InvalidOperationException("Project file is empty or invalid.");
        NormalizeProject(project);

        var rootDirectory = Path.GetDirectoryName(projectFilePath)
            ?? throw new InvalidOperationException("Project file path has no directory.");

        CreateProjectFolders(rootDirectory, project.Paths);
        return new ProjectContext(project, rootDirectory);
    }

    public void SaveProject(ProjectContext context)
    {
        context.Project.UpdatedAt = DateTime.UtcNow;
        NormalizeProject(context.Project);
        CreateProjectFolders(context.RootDirectory, context.Project.Paths);

        var json = JsonSerializer.Serialize(context.Project, JsonOptions);
        File.WriteAllText(context.ProjectFilePath, json);
    }

    private static void CreateProjectFolders(string rootDirectory, ProjectPaths paths)
    {
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Assets));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Data));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Maps));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Graphs));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Builds));

        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Assets, "Units"));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Assets, "Tilesets"));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Assets, "Audio"));
        Directory.CreateDirectory(Path.Combine(rootDirectory, paths.Assets, "UI"));
    }

    private static void NormalizeProject(AxeProject project)
    {
        project.AssetLibrary ??= new Assets.AssetLibrary();
        project.AssetLibrary.Categories ??= Assets.AssetLibrary.CreateDefaultCategories();
        project.AssetLibrary.Units ??= [];
        project.AssetLibrary.Stats ??= [];
        project.AssetLibrary.Traits ??= [];
        project.AssetLibrary.Factions ??= [];
        project.AssetLibrary.DamageTypes ??= [];
        project.AssetLibrary.Elements ??= [];
        project.AssetLibrary.Formulas ??= [];
        project.AssetLibrary.OptionSets ??= [];
        project.AssetLibrary.AIProfiles ??= [];
        project.AssetLibrary.ItemTypes ??= [];
        project.AssetLibrary.ItemEffects ??= [];
        project.AssetLibrary.GameplayEffects ??= [];
        project.AssetLibrary.Statuses ??= [];
        project.AssetLibrary.Projectiles ??= [];
        project.AssetLibrary.VisualEffects ??= [];
        project.AssetLibrary.Decorations ??= [];
        project.AssetLibrary.LootTables ??= [];
        project.AssetLibrary.ComponentPresets ??= [];
        project.AssetLibrary.InteractionProfiles ??= [];
        project.AssetLibrary.BehaviorPresets ??= [];
        project.AssetLibrary.Routes ??= [];
        project.AssetLibrary.SpawnWaves ??= [];
        project.AssetLibrary.LevelRules ??= [];
        project.AssetLibrary.BuildRules ??= [];
        project.AssetLibrary.BuildableUnits ??= [];
        project.AssetLibrary.TacticalGridRules ??= [];
        project.AssetLibrary.TerrainRules ??= [];
        project.AssetLibrary.TurnRules ??= [];
        project.AssetLibrary.ActionRules ??= [];
        project.AssetLibrary.TacticalRanges ??= [];
        project.AssetLibrary.ObjectiveRules ??= [];
        project.AssetLibrary.BondRules ??= [];
        project.AssetLibrary.ResourceRules ??= [];
        project.AssetLibrary.ProductionRules ??= [];
        project.AssetLibrary.TechRules ??= [];
        project.AssetLibrary.DiplomacyRules ??= [];
        project.AssetLibrary.TerritoryRules ??= [];
        project.AssetLibrary.TowerDefensePaths ??= [];
        project.AssetLibrary.TowerDefenseWaves ??= [];
        project.AssetLibrary.TowerDefenseRules ??= [];
        project.AssetLibrary.TowerDefenseBuildRules ??= [];
        project.AssetLibrary.TowerDefenseTowers ??= [];
        project.AssetLibrary.Actors ??= [];
        project.AssetLibrary.Enemies ??= [];
        project.AssetLibrary.Items ??= [];
        project.AssetLibrary.Skills ??= [];
        project.AssetLibrary.Maps ??= [];
        project.Paths ??= new ProjectPaths();
        project.HierarchyTree ??= [];
        project.ResourceTree ??= [];
        project.EventGraphs ??= [];

        SeedDefaultAssetsIfNeeded(project.AssetLibrary);

        SeedOptionSetsIfNeeded(project.AssetLibrary);
        MigrateLegacyGameplayOptionSets(project.AssetLibrary.OptionSets);

        foreach (var optionSet in project.AssetLibrary.OptionSets)
        {
            optionSet.Values ??= [];
        }

        foreach (var preset in project.AssetLibrary.BehaviorPresets)
        {
            preset.Stats ??= [];
            preset.Tags ??= [];
            preset.Traits ??= [];
            preset.Components ??= [];
            preset.Animations ??= [];
            preset.Sprite ??= new();
            preset.DisplayName ??= string.Empty;
            preset.DisplayNameKey ??= string.Empty;
            preset.Description ??= string.Empty;
            preset.DescriptionKey ??= string.Empty;
            preset.Portrait ??= string.Empty;
        }

        MigrateLegacyAssetCategories(project.AssetLibrary.Categories);

        foreach (var defaultCategory in Assets.AssetLibrary.CreateDefaultCategories())
        {
            if (project.AssetLibrary.Categories.All(category => category.Key != defaultCategory.Key))
            {
                project.AssetLibrary.Categories.Add(defaultCategory);
            }
        }

        foreach (var category in project.AssetLibrary.Categories)
        {
            if (string.IsNullOrWhiteSpace(category.DisplayNameKey))
            {
                category.DisplayNameKey = category.Key switch
                {
                    "content.units" => "asset.units",
                    "content.items" => "asset.items",
                    "content.skills" => "asset.skills",
                    "content.effects" => "asset.effects",
                    "content.projectiles" => "asset.projectiles",
                    "content.visualEffects" => "asset.visualEffects",
                    "content.decorations" => "asset.decorations",
                    "content.maps" => "tree.maps",
                    "rules.factions" => "asset.factions",
                    "rules.stats" => "asset.stats",
                    "rules.traits" => "asset.traits",
                    "rules.damageTypes" => "asset.damageTypes",
                    "rules.elements" => "asset.elements",
                    "rules.formulas" => "asset.formulas",
                    "rules.optionSets" => "asset.optionSets",
                    "rules.aiProfiles" => "asset.aiProfiles",
                    "rules.itemTypes" => "asset.itemTypes",
                    "rules.effects" => "asset.gameplayEffects",
                    "rules.statuses" => "asset.statuses",
                    "rules.lootTables" => "asset.lootTables",
                    "rules.componentPresets" => "asset.componentPresets",
                    "rules.interactions" => "asset.interactions",
                    "rules.itemEffects" => "asset.itemEffects",
                    "rules.behaviors" => "asset.behaviors",
                    "scene.routes" => "asset.routes",
                    "unit.spawnWaves" => "asset.spawnWaves",
                    "rules.levelRules" => "asset.levelRules",
                    "rules.buildRules" => "asset.buildRules",
                    "unit.buildableUnits" => "asset.buildableUnits",
                    "scene.tacticalGridRules" => "asset.tacticalGridRules",
                    "scene.terrainRules" => "asset.terrainRules",
                    "rules.turnRules" => "asset.turnRules",
                    "rules.actionRules" => "asset.actionRules",
                    "combat.tacticalRanges" => "asset.tacticalRanges",
                    "rules.objectiveRules" => "asset.objectiveRules",
                    "rules.bondRules" => "asset.bondRules",
                    "rules.resourceRules" => "asset.resourceRules",
                    "rules.productionRules" => "asset.productionRules",
                    "rules.techRules" => "asset.techRules",
                    "rules.diplomacyRules" => "asset.diplomacyRules",
                    "scene.territoryRules" => "asset.territoryRules",
                    _ => string.Empty
                };
            }
        }

        foreach (var actor in project.AssetLibrary.Actors)
        {
            actor.Stats ??= [];
        }

        foreach (var enemy in project.AssetLibrary.Enemies)
        {
            enemy.Stats ??= [];
            enemy.Tags ??= [];
            enemy.Traits ??= [];
        }

        MigrateLegacyUnitTemplates(project);
        MigrateLegacyRewardExp(project);
        MigrateLegacyUnits(project);

        NormalizeCoreV2Assets(project);
        NormalizeMaps(project.AssetLibrary.Maps);

        foreach (var unit in project.AssetLibrary.Units)
        {
            unit.Stats ??= [];
            unit.Tags ??= [];
            unit.Traits ??= [];
            unit.Components ??= [];
            unit.Animations ??= [];
            unit.Sprite ??= new();
            unit.AIProfileId = string.IsNullOrWhiteSpace(unit.AIProfileId)
                ? MapLegacyAiPreset(unit.AiPreset, unit.UnitKind)
                : unit.AIProfileId;
            unit.AiPreset = string.IsNullOrWhiteSpace(unit.AIProfileId) ? unit.AiPreset : unit.AIProfileId;
        }

        foreach (var unit in project.AssetLibrary.Units)
        {
            unit.Stats ??= [];
            unit.Tags ??= [];
            unit.Traits ??= [];
            unit.Components ??= [];
            unit.Animations ??= [];
            unit.Sprite ??= new();
        }

        ReleaseBuiltInLocksIfNeeded(project.AssetLibrary);
        EnsureRewardExpStatDefinition(project);

        foreach (var category in project.AssetLibrary.Categories)
        {
            category.Count = category.Key switch
            {
                "content.units" => project.AssetLibrary.Units.Count,
                "content.items" => project.AssetLibrary.Items.Count,
                "content.skills" => project.AssetLibrary.Skills.Count,
                "content.projectiles" => project.AssetLibrary.Projectiles.Count,
                "content.visualEffects" => project.AssetLibrary.VisualEffects.Count,
                "content.decorations" => project.AssetLibrary.Decorations.Count,
                "content.maps" => project.AssetLibrary.Maps.Count,
                "rules.factions" => project.AssetLibrary.Factions.Count,
                "rules.stats" => project.AssetLibrary.Stats.Count,
                "rules.traits" => project.AssetLibrary.Traits.Count,
                "rules.damageTypes" => project.AssetLibrary.DamageTypes.Count,
                "rules.elements" => project.AssetLibrary.Elements.Count,
                "rules.formulas" => project.AssetLibrary.Formulas.Count,
                "rules.optionSets" => project.AssetLibrary.OptionSets.Count,
                "rules.aiProfiles" => project.AssetLibrary.AIProfiles.Count,
                    "rules.itemTypes" => project.AssetLibrary.ItemTypes.Count,
                "rules.effects" => project.AssetLibrary.GameplayEffects.Count,
                "rules.statuses" => project.AssetLibrary.Statuses.Count,
                "rules.lootTables" => project.AssetLibrary.LootTables.Count,
                "rules.componentPresets" => project.AssetLibrary.ComponentPresets.Count,
                "rules.interactions" => project.AssetLibrary.InteractionProfiles.Count,
                "rules.itemEffects" => project.AssetLibrary.ItemEffects.Count,
                "rules.behaviors" => project.AssetLibrary.BehaviorPresets.Count,
                "scene.routes" => project.AssetLibrary.Routes.Count,
                "unit.spawnWaves" => project.AssetLibrary.SpawnWaves.Count,
                "rules.levelRules" => project.AssetLibrary.LevelRules.Count,
                "rules.buildRules" => project.AssetLibrary.BuildRules.Count,
                "unit.buildableUnits" => project.AssetLibrary.BuildableUnits.Count,
                "scene.tacticalGridRules" => project.AssetLibrary.TacticalGridRules.Count,
                "scene.terrainRules" => project.AssetLibrary.TerrainRules.Count,
                "rules.turnRules" => project.AssetLibrary.TurnRules.Count,
                "rules.actionRules" => project.AssetLibrary.ActionRules.Count,
                "combat.tacticalRanges" => project.AssetLibrary.TacticalRanges.Count,
                "rules.objectiveRules" => project.AssetLibrary.ObjectiveRules.Count,
                "rules.bondRules" => project.AssetLibrary.BondRules.Count,
                "rules.resourceRules" => project.AssetLibrary.ResourceRules.Count,
                "rules.productionRules" => project.AssetLibrary.ProductionRules.Count,
                "rules.techRules" => project.AssetLibrary.TechRules.Count,
                "rules.diplomacyRules" => project.AssetLibrary.DiplomacyRules.Count,
                "scene.territoryRules" => project.AssetLibrary.TerritoryRules.Count,
                _ => category.Count
            };
        }

        if (project.HierarchyTree.Count == 0)
        {
            project.HierarchyTree = CreateDefaultHierarchyTree();
        }

        if (project.ResourceTree.Count == 0)
        {
            project.ResourceTree = CreateDefaultResourceTree();
        }

        if (project.EventGraphs.Count == 0)
        {
            project.EventGraphs.Add(CreateDefaultEventGraph());
        }
    }

    private static void MigrateLegacyAssetCategories(List<Assets.AssetCategory> categories)
    {
        foreach (var category in categories)
        {
            category.Key = category.Key switch
            {
                "towerDefense.paths" => "scene.routes",
                "towerDefense.waves" => "unit.spawnWaves",
                "towerDefense.rules" => "rules.levelRules",
                "towerDefense.buildRules" => "rules.buildRules",
                "towerDefense.towers" => "unit.buildableUnits",
                _ => category.Key
            };

            category.DisplayNameKey = category.DisplayNameKey switch
            {
                "asset.towerDefense.paths" => "asset.routes",
                "asset.towerDefense.waves" => "asset.spawnWaves",
                "asset.towerDefense.rules" => "asset.levelRules",
                "asset.towerDefense.buildRules" => "asset.buildRules",
                "asset.towerDefense.towers" => "asset.buildableUnits",
                _ => category.DisplayNameKey
            };
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = categories.Count - 1; index >= 0; index--)
        {
            if (!seen.Add(categories[index].Key))
            {
                categories.RemoveAt(index);
            }
        }
    }

    private static void MigrateLegacyUnits(AxeProject project)
    {
        var units = project.AssetLibrary.Units;

        foreach (var actor in project.AssetLibrary.Actors)
        {
            if (units.Any(v => string.Equals(v.Id, actor.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            units.Add(new UnitDefinition
            {
                Id = actor.Id,
                DisplayName = actor.DisplayName,
                DisplayNameKey = actor.DisplayNameKey,
                Description = actor.Description,
                DescriptionKey = actor.DescriptionKey,
                UnitKind = actor.Traits?.Contains("player", StringComparer.OrdinalIgnoreCase) == true ? "player" : "npc",
                FactionId = actor.Traits?.Contains("player", StringComparer.OrdinalIgnoreCase) == true ? "faction.player" : "faction.village",
                AIProfileId = "ai.passive",
                AiPreset = "ai.passive",
                Stats = actor.Stats ?? [],
                Tags = actor.Tags ?? [],
                Traits = actor.Traits ?? [],
                Portrait = actor.Portrait,
                Sprite = actor.Sprite ?? new(),
                Components = actor.Components ?? [],
                Animations = actor.Animations ?? [],
                LegacyKind = "actor"
            });
        }

        foreach (var enemy in project.AssetLibrary.Enemies)
        {
            if (units.Any(v => string.Equals(v.Id, enemy.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            units.Add(new UnitDefinition
            {
                Id = enemy.Id,
                DisplayName = enemy.DisplayName,
                DisplayNameKey = enemy.DisplayNameKey,
                Description = enemy.Description,
                DescriptionKey = enemy.DescriptionKey,
                UnitKind = enemy.Traits?.Contains("boss", StringComparer.OrdinalIgnoreCase) == true ? "boss" : "enemy",
                FactionId = "faction.monster",
                AIProfileId = MapLegacyAiPreset(enemy.AiPreset, "enemy"),
                Stats = enemy.Stats ?? [],
                Tags = enemy.Tags ?? [],
                Traits = enemy.Traits ?? [],
                AiPreset = MapLegacyAiPreset(enemy.AiPreset, "enemy"),
                LegacyKind = "enemy"
            });
        }

        project.AssetLibrary.Actors.Clear();
        project.AssetLibrary.Enemies.Clear();
    }

    private static void NormalizeCoreV2Assets(AxeProject project)
    {
        foreach (var profile in project.AssetLibrary.AIProfiles)
        {
            profile.TargetTags ??= [];
            profile.SkillIds ??= [];
            profile.Parameters ??= [];
        }

        foreach (var effect in project.AssetLibrary.GameplayEffects)
        {
            effect.Parameters ??= [];
            effect.Tags ??= [];
            effect.FormulaId = NormalizeLegacyFormulaId(effect.FormulaId);
            effect.DamageTypeId = NormalizeLegacyDamageTypeId(effect.DamageTypeId);
            effect.ElementId = NormalizeLegacyElementId(effect.ElementId);
            effect.StatusId = NormalizeLegacyStatusId(effect.StatusId);
        }

        foreach (var status in project.AssetLibrary.Statuses)
        {
            status.OnApplyEffects ??= [];
            status.PeriodicEffects ??= [];
            status.OnApplyEffectIds ??= [];
            status.PeriodicEffectIds ??= [];
            status.Tags ??= [];
        }

        foreach (var projectile in project.AssetLibrary.Projectiles)
        {
            projectile.Effects ??= [];
            projectile.EffectIds ??= [];
        }

        foreach (var decoration in project.AssetLibrary.Decorations)
        {
            decoration.Tags ??= [];
        }

        foreach (var lootTable in project.AssetLibrary.LootTables)
        {
            lootTable.Entries ??= [];
            foreach (var entry in lootTable.Entries)
            {
                if (entry.MaxQuantity < entry.MinQuantity)
                {
                    entry.MaxQuantity = entry.MinQuantity;
                }
            }
        }

        foreach (var preset in project.AssetLibrary.ComponentPresets)
        {
            preset.Component ??= new();
            preset.Component.Parameters ??= [];
        }

        foreach (var itemType in project.AssetLibrary.ItemTypes)
        {
            itemType.Fields ??= [];
            foreach (var field in itemType.Fields)
            {
                field.Options ??= [];
            }
        }

        foreach (var item in project.AssetLibrary.Items)
        {
            item.Effects ??= [];
            item.EffectIds ??= [];
            item.GrantedSkillIds ??= [];
            item.CustomValues ??= [];
        }

        foreach (var skill in project.AssetLibrary.Skills)
        {
            skill.Effects ??= [];
            skill.EffectIds ??= [];
            skill.StatusIds ??= [];
            skill.RequiredTargetTags ??= [];
            skill.BlockedTargetTags ??= [];
            skill.FormulaId = NormalizeLegacyFormulaId(skill.FormulaId);
            skill.ProjectileId = NormalizeLegacyProjectileId(skill.ProjectileId);
            skill.ElementId = NormalizeLegacyElementId(skill.ElementId);
            skill.DamageTypeId = NormalizeLegacyDamageTypeId(skill.DamageTypeId);
            skill.StatusIds = NormalizeLegacyStatusIds(skill.StatusIds);
        }

        foreach (var unit in project.AssetLibrary.Units)
        {
            unit.FactionId = NormalizeLegacyFactionId(unit.FactionId);
        }

        NormalizeTowerDefenseUnitKinds(project.AssetLibrary);
        NormalizeTowerDefenseAssets(project.AssetLibrary);
        NormalizeTacticalAssets(project.AssetLibrary);

        MigrateGameplayEffectParameters(project.AssetLibrary.GameplayEffects);
        MigrateEffectReferences(project.AssetLibrary.Skills, skill => skill.Effects, skill => skill.EffectIds);
        MigrateEffectReferences(project.AssetLibrary.Items, item => item.Effects, item => item.EffectIds);
        MigrateEffectReferences(project.AssetLibrary.Projectiles, projectile => projectile.Effects, projectile => projectile.EffectIds);
        MigrateEffectReferences(project.AssetLibrary.Statuses, status => status.OnApplyEffects, status => status.OnApplyEffectIds);
        MigrateEffectReferences(project.AssetLibrary.Statuses, status => status.PeriodicEffects, status => status.PeriodicEffectIds);
        NormalizeGameplayEffectParameterDefaults(project.AssetLibrary.GameplayEffects);
        NormalizeGameplayEffectReferenceValues(project.AssetLibrary.Skills.SelectMany(skill => skill.Effects));
        NormalizeGameplayEffectReferenceValues(project.AssetLibrary.Items.SelectMany(item => item.Effects));
        NormalizeGameplayEffectReferenceValues(project.AssetLibrary.Projectiles.SelectMany(projectile => projectile.Effects));
        NormalizeGameplayEffectReferenceValues(project.AssetLibrary.Statuses.SelectMany(status => status.OnApplyEffects));
        NormalizeGameplayEffectReferenceValues(project.AssetLibrary.Statuses.SelectMany(status => status.PeriodicEffects));

        NormalizeCrossGenrePresetReferences(project.AssetLibrary);
        NormalizeStatDefinitions(project.AssetLibrary.Stats);
        NormalizeItemTypeDefinitions(project.AssetLibrary.ItemTypes);
        NormalizeItemCustomValues(project.AssetLibrary.Items, project.AssetLibrary.ItemTypes);
    }

    private static void NormalizeCrossGenrePresetReferences(Assets.AssetLibrary library)
    {
        foreach (var production in library.ProductionRules)
        {
            if (string.Equals(production.ProducedAssetId, "unit.guard_patrol", StringComparison.OrdinalIgnoreCase))
            {
                production.ProducedAssetId = "unit.patrol_guard";
            }
        }

        foreach (var tech in library.TechRules)
        {
            ReplaceId(tech.EffectIds, "effect.heal", "effect.heal.hp");
        }
    }

    private static void ReplaceId(List<string> values, string oldValue, string newValue)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], oldValue, StringComparison.OrdinalIgnoreCase))
            {
                values[i] = newValue;
            }
        }
    }

    private static void NormalizeMaps(IEnumerable<MapDefinition> maps)
    {
        foreach (var map in maps)
        {
            MapDefaults.Normalize(map);
        }
    }

    private static void NormalizeTowerDefenseUnitKinds(Assets.AssetLibrary library)
    {
        foreach (var unit in library.Units.Where(v => v.Id.StartsWith("unit.td.", StringComparison.OrdinalIgnoreCase)))
        {
            unit.UnitKind = "tower";
        }
    }

    private static void NormalizeTowerDefenseAssets(Assets.AssetLibrary library)
    {
        foreach (var path in library.TowerDefensePaths)
        {
            path.Waypoints ??= [];
        }

        foreach (var wave in library.TowerDefenseWaves)
        {
            wave.SpawnGroups ??= [];
            if (wave.SpawnIntervalSeconds < 0)
            {
                wave.SpawnIntervalSeconds = 0;
            }

            foreach (var group in wave.SpawnGroups)
            {
                group.Count = Math.Max(1, group.Count);
                if (group.IntervalSeconds < 0)
                {
                    group.IntervalSeconds = 0;
                }
            }
        }

        foreach (var rule in library.TowerDefenseRules)
        {
            rule.WaveIds ??= [];
            rule.StartingGold = Math.Max(0, rule.StartingGold);
            rule.BaseLife = Math.Max(1, rule.BaseLife);
            rule.LeakDamagePerUnit = Math.Max(0, rule.LeakDamagePerUnit);
        }

        foreach (var buildRule in library.TowerDefenseBuildRules)
        {
            buildRule.SellRefundRatio = Math.Clamp(buildRule.SellRefundRatio, 0, 1);
        }

        foreach (var tower in library.TowerDefenseTowers)
        {
            tower.Levels ??= [];
            tower.BuildCost = Math.Max(0, tower.BuildCost);
            tower.Range = Math.Max(0, tower.Range);
            tower.AttackIntervalSeconds = Math.Max(0.01, tower.AttackIntervalSeconds);
            foreach (var level in tower.Levels)
            {
                level.Level = Math.Max(1, level.Level);
                level.UpgradeCost = Math.Max(0, level.UpgradeCost);
                level.DamageMultiplier = level.DamageMultiplier <= 0 ? 1 : level.DamageMultiplier;
                level.AttackIntervalMultiplier = level.AttackIntervalMultiplier <= 0 ? 1 : level.AttackIntervalMultiplier;
            }
        }
    }

    private static void NormalizeTacticalAssets(Assets.AssetLibrary library)
    {
        foreach (var grid in library.TacticalGridRules)
        {
            grid.TileSize = Math.Clamp(grid.TileSize, 1, 4096);
        }

        foreach (var terrain in library.TerrainRules)
        {
            terrain.AllowedUnitTags ??= [];
            terrain.ForbiddenUnitTags ??= [];
            terrain.MovementCost = Math.Max(0, terrain.MovementCost);
            terrain.DamageModifier = terrain.DamageModifier <= 0 ? 1 : terrain.DamageModifier;
        }

        foreach (var turn in library.TurnRules)
        {
            turn.MaxRounds = Math.Max(0, turn.MaxRounds);
        }

        foreach (var action in library.ActionRules)
        {
            action.DefaultActionPoints = Math.Max(0, action.DefaultActionPoints);
            action.DefaultMovePoints = Math.Max(0, action.DefaultMovePoints);
        }

        foreach (var range in library.TacticalRanges)
        {
            range.RequiredTargetTags ??= [];
            range.BlockedTargetTags ??= [];
            range.MinRange = Math.Max(0, range.MinRange);
            range.MaxRange = Math.Max(range.MinRange, range.MaxRange);
            range.AreaRadius = Math.Max(0, range.AreaRadius);
        }

        foreach (var objective in library.ObjectiveRules)
        {
            objective.TargetUnitTags ??= [];
            objective.TargetAreaTags ??= [];
            objective.RequiredCount = Math.Max(0, objective.RequiredCount);
            objective.RoundLimit = Math.Max(0, objective.RoundLimit);
        }

        foreach (var bond in library.BondRules)
        {
            bond.RequiredUnitTags ??= [];
            bond.ExcludedUnitTags ??= [];
            bond.EffectIds ??= [];
            bond.Range = Math.Max(0, bond.Range);
            bond.MinParticipants = Math.Max(1, bond.MinParticipants);
            bond.MaxParticipants = Math.Max(0, bond.MaxParticipants);
        }
    }

    private static void MigrateGameplayEffectParameters(List<Core.Effects.GameplayEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            effect.Parameters ??= [];
            if (effect.Parameters.Count > 0)
            {
                continue;
            }

            var migrated = new List<Core.Effects.EffectParameterDefinition>();
            AddLegacyParameter(migrated, "statKey", "effect.parameter.statKey.name", "属性", Core.Effects.EffectParameterValueType.AssetRef, effect.StatKey, "stat", 10);
            AddLegacyParameter(migrated, "formulaId", "effect.parameter.formulaId.name", "公式", Core.Effects.EffectParameterValueType.AssetRef, effect.FormulaId, "formula", 20);
            AddLegacyParameter(migrated, "baseValue", "effect.parameter.baseValue.name", "基础值", Core.Effects.EffectParameterValueType.Number, effect.BaseValue == 0 ? string.Empty : effect.BaseValue.ToString(CultureInfo.InvariantCulture), "", 30);
            AddLegacyParameter(migrated, "damageTypeId", "effect.parameter.damageTypeId.name", "伤害类型", Core.Effects.EffectParameterValueType.AssetRef, effect.DamageTypeId, "damageType", 40);
            AddLegacyParameter(migrated, "elementId", "effect.parameter.elementId.name", "元素", Core.Effects.EffectParameterValueType.AssetRef, effect.ElementId, "element", 50);
            AddLegacyParameter(migrated, "statusId", "effect.parameter.statusId.name", "状态", Core.Effects.EffectParameterValueType.AssetRef, effect.StatusId, "status", 60);
            AddLegacyParameter(migrated, "durationSeconds", "effect.parameter.durationSeconds.name", "持续时间", Core.Effects.EffectParameterValueType.Number, effect.DurationSeconds == 0 ? string.Empty : effect.DurationSeconds.ToString(CultureInfo.InvariantCulture), "", 70);
            AddLegacyParameter(migrated, "chance", "effect.parameter.chance.name", "概率", Core.Effects.EffectParameterValueType.Number, effect.Chance == 1 ? string.Empty : effect.Chance.ToString(CultureInfo.InvariantCulture), "", 80);
            effect.Parameters = migrated;
        }
    }

    private static void AddLegacyParameter(
        List<Core.Effects.EffectParameterDefinition> parameters,
        string key,
        string displayNameKey,
        string displayName,
        Core.Effects.EffectParameterValueType valueType,
        string defaultValue,
        string optionSourceId,
        int order)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            return;
        }

        parameters.Add(new Core.Effects.EffectParameterDefinition
        {
            Key = key,
            DisplayNameKey = displayNameKey,
            DisplayName = displayName,
            ValueType = valueType,
            DefaultValue = defaultValue,
            CategoryKey = "effect.parameter.category.core",
            Category = "effect.parameter.category.core",
            OptionSourceId = optionSourceId,
            Order = order
        });
    }

    private static void MigrateEffectReferences<T>(
        IEnumerable<T> records,
        Func<T, List<Core.Effects.GameplayEffectReference>> getReferences,
        Func<T, List<string>> getLegacyIds)
    {
        foreach (var record in records)
        {
            var references = getReferences(record);
            if (references.Count > 0)
            {
                continue;
            }

            foreach (var effectId in getLegacyIds(record).Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                references.Add(new Core.Effects.GameplayEffectReference
                {
                    EffectId = effectId
                });
            }
        }
    }

    private static void NormalizeGameplayEffectParameterDefaults(IEnumerable<Core.Effects.GameplayEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            foreach (var parameter in effect.Parameters)
            {
                if (string.Equals(parameter.OptionSourceId, "damageType", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.DefaultValue = NormalizeLegacyDamageTypeId(parameter.DefaultValue);
                }
                else if (string.Equals(parameter.OptionSourceId, "element", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.DefaultValue = NormalizeLegacyElementId(parameter.DefaultValue);
                }
                else if (string.Equals(parameter.OptionSourceId, "formula", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.DefaultValue = NormalizeLegacyFormulaId(parameter.DefaultValue);
                }
                else if (string.Equals(parameter.OptionSourceId, "status", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.DefaultValue = NormalizeLegacyStatusId(parameter.DefaultValue);
                }
                else if (string.Equals(parameter.OptionSourceId, "projectile", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.DefaultValue = NormalizeLegacyProjectileId(parameter.DefaultValue);
                }
            }
        }
    }

    private static void NormalizeGameplayEffectReferenceValues(IEnumerable<Core.Effects.GameplayEffectReference> references)
    {
        foreach (var reference in references)
        {
            foreach (var parameter in reference.Parameters)
            {
                if (string.Equals(parameter.Key, "damageTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = NormalizeLegacyDamageTypeId(parameter.Value);
                }
                else if (string.Equals(parameter.Key, "elementId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = NormalizeLegacyElementId(parameter.Value);
                }
                else if (string.Equals(parameter.Key, "formulaId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = NormalizeLegacyFormulaId(parameter.Value);
                }
                else if (string.Equals(parameter.Key, "statusId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = NormalizeLegacyStatusId(parameter.Value);
                }
                else if (string.Equals(parameter.Key, "projectileId", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = NormalizeLegacyProjectileId(parameter.Value);
                }
            }
        }
    }

    private static string NormalizeLegacyDamageTypeId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "physical" => "damage.physical",
            "magic" => "damage.magic",
            _ => normalized
        };
    }

    private static string NormalizeLegacyElementId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "none" => "element.none",
            "fire" => "element.fire",
            "poison" => "element.poison",
            "blood" => "element.blood",
            _ => normalized
        };
    }

    private static string NormalizeLegacyFormulaId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "physicalAttack" => "formula.physicalAttack",
            "magicAttack" => "formula.magicAttack",
            "heal" => "formula.heal",
            _ => normalized
        };
    }

    private static string NormalizeLegacyStatusId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "burn" => "status.burn",
            "poison" => "status.poison",
            _ => normalized
        };
    }

    private static string NormalizeLegacyProjectileId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "fireball" => "projectile.fireball",
            "basic" => "projectile.template.basic",
            "template.basic" => "projectile.template.basic",
            _ => normalized
        };
    }

    private static string NormalizeLegacyFactionId(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "" => "",
            "player" => "faction.player",
            "village" => "faction.village",
            "monster" => "faction.monster",
            "neutral" => "faction.neutral",
            _ => normalized
        };
    }

    private static List<string> NormalizeLegacyStatusIds(List<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeLegacyStatusId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void SeedOptionSetsIfNeeded(Assets.AssetLibrary library)
    {
        var defaults = Assets.DefaultAssetFactory.CreateDefaultOptionSets();
        if (!library.OptionSetsInitialized)
        {
            AddMissingByKey(library.OptionSets, defaults, v => v.Id);
            library.OptionSetsInitialized = true;
            library.OptionSetCatalogVersion = CurrentOptionSetCatalogVersion;
        }
        else if (library.OptionSetCatalogVersion < CurrentOptionSetCatalogVersion)
        {
            var migrationDefaults = defaults.Where(v => IsOptionSetCatalogMigrationGroup(v.Id)).ToList();
            AddMissingByKey(library.OptionSets, migrationDefaults, v => v.Id);
            MergeMissingOptionSetValues(library.OptionSets, migrationDefaults);
            library.OptionSetCatalogVersion = CurrentOptionSetCatalogVersion;
        }

        NormalizeOptionSetMetadata(library.OptionSets, defaults);
    }

    private static bool IsOptionSetCatalogMigrationGroup(string id)
    {
        return id is "unitKind"
            or "targetingMode"
            or "effectKind"
            or "statCategory"
            or "tag"
            or "animationKey"
            or "componentType"
            or "componentParameter"
            or "routeMode"
            or "waveStartMode"
            or "victoryCondition"
            or "defeatCondition"
            or "buildableRole"
            or "targetPriority"
            or "gridType"
            or "movementMetric"
            or "turnMode"
            or "actionRefreshMode"
            or "rangeShape"
            or "areaShape"
            or "objectiveType"
            or "bondTriggerTiming"
            or "bondDurationMode"
            or "stackingMode"
            or "resourceKind"
            or "producedAssetKind"
            or "techKind"
            or "diplomaticState"
            or "territoryControlMode";
    }

    private static void MigrateLegacyGameplayOptionSets(List<Assets.OptionSetDefinition> optionSets)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tdPathMode"] = "routeMode",
            ["tdWaveStartMode"] = "waveStartMode",
            ["tdVictoryCondition"] = "victoryCondition",
            ["tdDefeatCondition"] = "defeatCondition",
            ["tdTowerRole"] = "buildableRole",
            ["tdTargetPriority"] = "targetPriority"
        };

        foreach (var (legacyId, newId) in mappings)
        {
            var legacy = optionSets.FirstOrDefault(v => string.Equals(v.Id, legacyId, StringComparison.OrdinalIgnoreCase));
            if (legacy is null)
            {
                continue;
            }

            var target = optionSets.FirstOrDefault(v => string.Equals(v.Id, newId, StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                legacy.Id = newId;
                legacy.DisplayNameKey = $"optionSet.{newId}.name";
                continue;
            }

            target.Values ??= [];
            foreach (var (key, value) in legacy.Values ?? [])
            {
                if (!target.Values.ContainsKey(key))
                {
                    target.Values[key] = value;
                }
            }

            optionSets.Remove(legacy);
        }
    }

    private static void NormalizeOptionSetMetadata(List<Assets.OptionSetDefinition> optionSets, IReadOnlyList<Assets.OptionSetDefinition> defaults)
    {
        var defaultsById = defaults.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var optionSet in optionSets)
        {
            optionSet.Values ??= [];
            if (!defaultsById.TryGetValue(optionSet.Id, out var defaultOptionSet))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(optionSet.DisplayName))
            {
                optionSet.DisplayName = defaultOptionSet.DisplayName;
            }

            if (string.IsNullOrWhiteSpace(optionSet.DisplayNameKey))
            {
                optionSet.DisplayNameKey = defaultOptionSet.DisplayNameKey;
            }
        }
    }

    private static void MergeMissingOptionSetValues(List<Assets.OptionSetDefinition> optionSets, IReadOnlyList<Assets.OptionSetDefinition> defaults)
    {
        var optionSetsById = optionSets.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var defaultOptionSet in defaults)
        {
            if (!optionSetsById.TryGetValue(defaultOptionSet.Id, out var optionSet))
            {
                continue;
            }

            optionSet.Values ??= [];
            foreach (var (key, value) in defaultOptionSet.Values)
            {
                if (!optionSet.Values.ContainsKey(key))
                {
                    optionSet.Values[key] = value;
                }
            }
        }
    }

    private static void SeedDefaultAssetsIfNeeded(Assets.AssetLibrary library)
    {
        if (library.DefaultAssetsInitialized)
        {
            SeedTowerDefenseDefaultAssets(library);
            SeedCrossGenreDefaultAssets(library);
            return;
        }

        AddMissingByKey(library.Stats, Assets.DefaultAssetFactory.CreateDefaultStats(), v => v.Key);
        AddMissingByKey(library.Traits, Assets.DefaultAssetFactory.CreateDefaultTraits(), v => v.Id);
        AddMissingByKey(library.Factions, Assets.DefaultAssetFactory.CreateDefaultFactions(), v => v.Id);
        AddMissingByKey(library.DamageTypes, Assets.DefaultAssetFactory.CreateDefaultDamageTypes(), v => v.Id);
        AddMissingByKey(library.Elements, Assets.DefaultAssetFactory.CreateDefaultElements(), v => v.Id);
        AddMissingByKey(library.Formulas, Assets.DefaultAssetFactory.CreateDefaultFormulas(), v => v.Id);
        AddMissingByKey(library.AIProfiles, Assets.DefaultAssetFactory.CreateDefaultAIProfiles(), v => v.Id);
        AddMissingByKey(library.GameplayEffects, Assets.DefaultAssetFactory.CreateDefaultGameplayEffects(), v => v.Id);
        AddMissingByKey(library.Statuses, Assets.DefaultAssetFactory.CreateDefaultStatuses(), v => v.Id);
        AddMissingByKey(library.Projectiles, Assets.DefaultAssetFactory.CreateDefaultProjectiles(), v => v.Id);
        AddMissingByKey(library.VisualEffects, Assets.DefaultAssetFactory.CreateDefaultVisualEffects(), v => v.Id);
        AddMissingByKey(library.ItemTypes, Assets.AssetLibrary.CreateDefaultItemTypes(), v => v.Id);
        AddMissingByKey(library.ItemEffects, Assets.DefaultAssetFactory.CreateDefaultItemEffects(), v => v.Id);
        AddMissingByKey(library.Decorations, Assets.DefaultAssetFactory.CreateDefaultDecorations(), v => v.Id);
        AddMissingByKey(library.LootTables, Assets.DefaultAssetFactory.CreateDefaultLootTables(), v => v.Id);
        AddMissingByKey(library.ComponentPresets, Assets.DefaultAssetFactory.CreateDefaultComponentPresets(), v => v.Id);
        AddMissingByKey(library.InteractionProfiles, Assets.DefaultAssetFactory.CreateDefaultInteractionProfiles(), v => v.Id);
        AddMissingByKey(library.BehaviorPresets, Assets.DefaultAssetFactory.CreateDefaultBehaviorPresets(), v => v.Id);
        AddMissingByKey(library.TowerDefensePaths, Assets.DefaultAssetFactory.CreateDefaultTowerDefensePaths(), v => v.Id);
        AddMissingByKey(library.TowerDefenseWaves, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseWaves(), v => v.Id);
        AddMissingByKey(library.TowerDefenseRules, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseRules(), v => v.Id);
        AddMissingByKey(library.TowerDefenseBuildRules, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseBuildRules(), v => v.Id);
        AddMissingByKey(library.TowerDefenseTowers, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseTowers(), v => v.Id);
        SeedTacticalDefaultAssets(library);
        SeedStrategyDefaultAssets(library);
        AddMissingByKey(library.Units, Assets.DefaultAssetFactory.CreateDefaultUnits(), v => v.Id);
        AddMissingByKey(library.Items, Assets.DefaultAssetFactory.CreateDefaultItems(), v => v.Id);
        AddMissingByKey(library.Skills, Assets.DefaultAssetFactory.CreateDefaultSkills(), v => v.Id);
        AddMissingByKey(library.Maps, Assets.DefaultAssetFactory.CreateDefaultMaps(), v => v.Id);
        SeedCrossGenreDefaultAssets(library);

        library.DefaultAssetsInitialized = true;
    }

    private static void SeedTowerDefenseDefaultAssets(Assets.AssetLibrary library)
    {
        AddMissingByKey(library.Statuses, Assets.DefaultAssetFactory.CreateDefaultStatuses().Where(v => v.Id.StartsWith("status.td.", StringComparison.OrdinalIgnoreCase)), v => v.Id);
        AddMissingByKey(library.Projectiles, Assets.DefaultAssetFactory.CreateDefaultProjectiles().Where(v => v.Id.StartsWith("projectile.td.", StringComparison.OrdinalIgnoreCase)), v => v.Id);
        AddMissingByKey(library.VisualEffects, Assets.DefaultAssetFactory.CreateDefaultVisualEffects().Where(v => v.Id.StartsWith("vfx.frost", StringComparison.OrdinalIgnoreCase)), v => v.Id);
        AddMissingByKey(library.Units, Assets.DefaultAssetFactory.CreateDefaultUnits().Where(v => v.Id.StartsWith("unit.td.", StringComparison.OrdinalIgnoreCase)), v => v.Id);
        AddMissingByKey(library.Skills, Assets.DefaultAssetFactory.CreateDefaultSkills().Where(v => v.Id.StartsWith("skill.td.", StringComparison.OrdinalIgnoreCase)), v => v.Id);
        AddMissingByKey(library.TowerDefensePaths, Assets.DefaultAssetFactory.CreateDefaultTowerDefensePaths(), v => v.Id);
        AddMissingByKey(library.TowerDefenseWaves, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseWaves(), v => v.Id);
        AddMissingByKey(library.TowerDefenseRules, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseRules(), v => v.Id);
        AddMissingByKey(library.TowerDefenseBuildRules, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseBuildRules(), v => v.Id);
        AddMissingByKey(library.TowerDefenseTowers, Assets.DefaultAssetFactory.CreateDefaultTowerDefenseTowers(), v => v.Id);
        SeedTacticalDefaultAssets(library);
        SeedStrategyDefaultAssets(library);
        SeedCrossGenreDefaultAssets(library);
    }

    private static void SeedTacticalDefaultAssets(Assets.AssetLibrary library)
    {
        AddMissingByKey(library.TacticalGridRules, Assets.DefaultAssetFactory.CreateDefaultTacticalGridRules(), v => v.Id);
        AddMissingByKey(library.TerrainRules, Assets.DefaultAssetFactory.CreateDefaultTerrainRules(), v => v.Id);
        AddMissingByKey(library.TurnRules, Assets.DefaultAssetFactory.CreateDefaultTurnRules(), v => v.Id);
        AddMissingByKey(library.ActionRules, Assets.DefaultAssetFactory.CreateDefaultActionRules(), v => v.Id);
        AddMissingByKey(library.TacticalRanges, Assets.DefaultAssetFactory.CreateDefaultTacticalRanges(), v => v.Id);
        AddMissingByKey(library.ObjectiveRules, Assets.DefaultAssetFactory.CreateDefaultObjectiveRules(), v => v.Id);
        AddMissingByKey(library.BondRules, Assets.DefaultAssetFactory.CreateDefaultBondRules(), v => v.Id);
    }

    private static void SeedStrategyDefaultAssets(Assets.AssetLibrary library)
    {
        AddMissingByKey(library.Stats, Assets.DefaultAssetFactory.CreateDefaultStats()
            .Where(v => v.Key is "gold" or "wood" or "food" or "researchPoint" or "actionPoint" or "movePoint"), v => v.Key);
        AddMissingByKey(library.ResourceRules, Assets.DefaultAssetFactory.CreateDefaultResourceRules(), v => v.Id);
        AddMissingByKey(library.ProductionRules, Assets.DefaultAssetFactory.CreateDefaultProductionRules(), v => v.Id);
        AddMissingByKey(library.TechRules, Assets.DefaultAssetFactory.CreateDefaultTechRules(), v => v.Id);
        AddMissingByKey(library.DiplomacyRules, Assets.DefaultAssetFactory.CreateDefaultDiplomacyRules(), v => v.Id);
        AddMissingByKey(library.TerritoryRules, Assets.DefaultAssetFactory.CreateDefaultTerritoryRules(), v => v.Id);
    }

    private static void SeedCrossGenreDefaultAssets(Assets.AssetLibrary library)
    {
        AddMissingByKey(library.Stats, Assets.DefaultAssetFactory.CreateDefaultStats()
            .Where(v => v.Key is "actionPoint" or "movePoint"), v => v.Key);
        AddMissingByKey(library.GameplayEffects, Assets.DefaultAssetFactory.CreateDefaultGameplayEffects()
            .Where(v => v.Id is "effect.applyStatus" or "effect.tactics.guardBonus" or "effect.tactics.supportAttack"), v => v.Id);
        AddMissingByKey(library.Units, Assets.DefaultAssetFactory.CreateDefaultUnits()
            .Where(v => v.Id is "unit.trap.spike"), v => v.Id);
        AddMissingByKey(library.Skills, Assets.DefaultAssetFactory.CreateDefaultSkills()
            .Where(v => v.Id is "skill.trap_reclaim"), v => v.Id);
        AddMissingByKey(library.Maps, Assets.DefaultAssetFactory.CreateDefaultMaps()
            .Where(v => v.Id is "map.training_ground"), v => v.Id);
        NormalizeBuiltInSkillTargets(library);
    }

    private static void NormalizeBuiltInSkillTargets(Assets.AssetLibrary library)
    {
        foreach (var skill in library.Skills)
        {
            skill.RequiredTargetTags ??= [];
            skill.BlockedTargetTags ??= [];

            if (string.Equals(skill.Id, "skill.heal", StringComparison.OrdinalIgnoreCase)
                && string.Equals(skill.TargetingMode, "selfOrAlly", StringComparison.OrdinalIgnoreCase))
            {
                skill.TargetingMode = "allyUnit";
                AddMissingString(skill.RequiredTargetTags, "unit");
            }
            else if (string.Equals(skill.Id, "skill.basic_slash", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Id, "skill.fireball", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Id, "skill.dash_slash", StringComparison.OrdinalIgnoreCase))
            {
                AddMissingString(skill.RequiredTargetTags, "unit");
                AddMissingString(skill.RequiredTargetTags, "attackable");
            }
            else if (string.Equals(skill.Id, "skill.vampiric_slash", StringComparison.OrdinalIgnoreCase))
            {
                AddMissingString(skill.RequiredTargetTags, "unit");
                AddMissingString(skill.RequiredTargetTags, "attackable");
                AddMissingString(skill.BlockedTargetTags, "undead");
                AddMissingString(skill.BlockedTargetTags, "bloodless");
            }
            else if (string.Equals(skill.Id, "skill.td.arrowShot", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Id, "skill.td.frostBolt", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(skill.TargetingMode, "aimedProjectile", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(skill.TargetingMode, "towerTarget", StringComparison.OrdinalIgnoreCase))
                {
                    skill.TargetingMode = "enemyUnit";
                }

                AddMissingString(skill.RequiredTargetTags, "unit");
                AddMissingString(skill.RequiredTargetTags, "enemy");
            }
            else if (string.Equals(skill.Id, "skill.trap_reclaim", StringComparison.OrdinalIgnoreCase))
            {
                skill.TargetingMode = "areaTaggedUnits";
                AddMissingString(skill.RequiredTargetTags, "trap");
            }
        }
    }

    private static void AddMissingString(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }

    private static void ReleaseBuiltInLocksIfNeeded(Assets.AssetLibrary library)
    {
        if (library.BuiltInLocksReleased)
        {
            return;
        }

        foreach (var record in EnumerateProtectableAssetRecords(library))
        {
            SetBuiltInFlag(record, false);
        }

        library.BuiltInLocksReleased = true;
    }

    private static IEnumerable<object> EnumerateProtectableAssetRecords(Assets.AssetLibrary library)
    {
        foreach (var record in library.Traits) yield return record;
        foreach (var record in library.Factions) yield return record;
        foreach (var record in library.DamageTypes) yield return record;
        foreach (var record in library.Elements) yield return record;
        foreach (var record in library.Formulas) yield return record;
        foreach (var record in library.AIProfiles) yield return record;
        foreach (var record in library.ItemTypes) yield return record;
        foreach (var record in library.ItemEffects) yield return record;
        foreach (var record in library.GameplayEffects) yield return record;
        foreach (var record in library.Statuses) yield return record;
        foreach (var record in library.Projectiles) yield return record;
        foreach (var record in library.VisualEffects) yield return record;
        foreach (var record in library.Decorations) yield return record;
        foreach (var record in library.LootTables) yield return record;
        foreach (var record in library.ComponentPresets) yield return record;
        foreach (var record in library.InteractionProfiles) yield return record;
        foreach (var record in library.BehaviorPresets) yield return record;
        foreach (var record in library.TowerDefensePaths) yield return record;
        foreach (var record in library.TowerDefenseWaves) yield return record;
        foreach (var record in library.TowerDefenseRules) yield return record;
        foreach (var record in library.TowerDefenseBuildRules) yield return record;
        foreach (var record in library.TowerDefenseTowers) yield return record;
        foreach (var record in library.TacticalGridRules) yield return record;
        foreach (var record in library.TerrainRules) yield return record;
        foreach (var record in library.TurnRules) yield return record;
        foreach (var record in library.ActionRules) yield return record;
        foreach (var record in library.TacticalRanges) yield return record;
        foreach (var record in library.ObjectiveRules) yield return record;
        foreach (var record in library.BondRules) yield return record;
        foreach (var record in library.ResourceRules) yield return record;
        foreach (var record in library.ProductionRules) yield return record;
        foreach (var record in library.TechRules) yield return record;
        foreach (var record in library.DiplomacyRules) yield return record;
        foreach (var record in library.TerritoryRules) yield return record;
    }

    private static void SetBuiltInFlag(object record, bool value)
    {
        var property = record.GetType().GetProperty("BuiltIn");
        if (property?.CanWrite == true && property.PropertyType == typeof(bool))
        {
            property.SetValue(record, value);
        }
    }

    private static void AddMissingByKey<T>(List<T> target, IEnumerable<T> defaults, Func<T, string> getKey)
    {
        var existing = target
            .Select(getKey)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in defaults)
        {
            var key = getKey(item);
            if (string.IsNullOrWhiteSpace(key) || existing.Contains(key))
            {
                continue;
            }

            target.Add(item);
            existing.Add(key);
        }
    }

    private static string MapLegacyAiPreset(string? legacyValue, string? unitKind = null)
    {
        if (!string.IsNullOrWhiteSpace(legacyValue))
        {
            var value = legacyValue.Trim();
            if (value.StartsWith("ai.", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return value switch
            {
                "Player" => "ai.passive",
                "Passive" => "ai.passive",
                "MeleeChase" => "ai.meleeChase",
                "Patrol" => "ai.patrolGuard",
                "RangedGuard" => "ai.rangedKeepDistance",
                "Boss" => "ai.bossPhases",
                _ => value
            };
        }

        return unitKind switch
        {
            "player" => "ai.passive",
            "npc" => "ai.passive",
            "boss" => "ai.bossPhases",
            _ => "ai.meleeChase"
        };
    }

    private static void MigrateLegacyUnitTemplates(AxeProject project)
    {
        MigrateLegacyUnitTemplates(project.AssetLibrary.LegacyData, project.AssetLibrary.Units);
        MigrateLegacyUnitTemplates(project.LegacyData, project.AssetLibrary.Units);
    }

    private static void MigrateLegacyUnitTemplates(Dictionary<string, JsonElement>? legacyData, List<UnitDefinition> units)
    {
        if (legacyData is null)
        {
            return;
        }

        if (!legacyData.TryGetValue("unitTemplates", out var token) || token.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in token.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var unit = JsonSerializer.Deserialize<UnitDefinition>(item.GetRawText(), JsonOptions);
            if (unit is null || string.IsNullOrWhiteSpace(unit.Id))
            {
                continue;
            }

            unit.AIProfileId = string.IsNullOrWhiteSpace(unit.AIProfileId)
                ? MapLegacyAiPreset(unit.AiPreset, unit.UnitKind)
                : unit.AIProfileId;
            unit.Stats ??= [];
            unit.Tags ??= [];
            unit.Traits ??= [];
            unit.Components ??= [];
            unit.Animations ??= [];
            unit.Sprite ??= new();

            if (units.All(v => !string.Equals(v.Id, unit.Id, StringComparison.OrdinalIgnoreCase)))
            {
                units.Add(unit);
            }
        }

        legacyData.Remove("unitTemplates");
    }

    private static void MigrateLegacyRewardExp(AxeProject project)
    {
        foreach (var enemy in project.AssetLibrary.Enemies)
        {
            MigrateLegacyRewardExp(enemy);
        }
    }

    private static void MigrateLegacyRewardExp(EnemyDefinition enemy)
    {
        NormalizeStatKey(enemy.Stats, RewardExpStatKey);

        if (!HasStatKey(enemy.Stats, RewardExpStatKey) && enemy.LegacyRewardExp != 0)
        {
            enemy.Stats[RewardExpStatKey] = enemy.LegacyRewardExp;
        }

        enemy.LegacyRewardExp = 0;
    }

    private static void EnsureRewardExpStatDefinition(AxeProject project)
    {
        if (project.AssetLibrary.Stats.Any(v => string.Equals(v.Key, RewardExpStatKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (!HasRewardExpUsage(project))
        {
            return;
        }

        project.AssetLibrary.Stats.Add(Assets.DefaultAssetFactory.CreateRewardExpStat());
    }

    private static bool HasRewardExpUsage(AxeProject project)
    {
        return project.AssetLibrary.Units.Any(v => HasStatKey(v.Stats, RewardExpStatKey))
            || project.AssetLibrary.Enemies.Any(v => HasStatKey(v.Stats, RewardExpStatKey) || v.LegacyRewardExp != 0);
    }

    private static bool HasStatKey(Dictionary<string, double> stats, string key)
    {
        return stats.Keys.Any(v => string.Equals(v, key, StringComparison.OrdinalIgnoreCase));
    }

    private static void NormalizeStatKey(Dictionary<string, double> stats, string key)
    {
        var existingKey = stats.Keys.FirstOrDefault(v => string.Equals(v, key, StringComparison.OrdinalIgnoreCase));
        if (existingKey is null || string.Equals(existingKey, key, StringComparison.Ordinal))
        {
            return;
        }

        var value = stats[existingKey];
        stats.Remove(existingKey);
        stats[key] = value;
    }

    private static void NormalizeStatDefinitions(List<StatDefinition> stats)
    {
        foreach (var stat in stats)
        {
            if (!IsIntegerStat(stat.ValueType))
            {
                continue;
            }

            stat.DefaultValue = RoundToInteger(stat.DefaultValue);
            stat.Min = RoundToInteger(stat.Min);
            stat.Max = RoundToInteger(stat.Max);
            if (stat.Min > stat.Max)
            {
                stat.Max = stat.Min;
            }
        }
    }

    private static void NormalizeItemTypeDefinitions(List<ItemTypeDefinition> itemTypes)
    {
        foreach (var itemType in itemTypes)
        {
            itemType.Fields ??= [];
            foreach (var field in itemType.Fields)
            {
                field.Options ??= [];
                if (!IsIntegerField(field))
                {
                    continue;
                }

                field.DefaultValue = NormalizeIntegerText(field.DefaultValue);
            }
        }
    }

    private static void NormalizeItemCustomValues(List<ItemDefinition> items, List<ItemTypeDefinition> itemTypes)
    {
        var fieldLookup = itemTypes
            .SelectMany(type => type.Fields.Select(field => new { type.Id, Field = field }))
            .ToDictionary(v => (v.Id, v.Field.Key), v => v.Field, StringTupleComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            item.CustomValues ??= [];
            foreach (var value in item.CustomValues)
            {
                if (!fieldLookup.TryGetValue((item.TypeId, value.Key), out var field))
                {
                    continue;
                }

                if (IsIntegerField(field))
                {
                    value.Value = NormalizeIntegerText(value.Value);
                }
            }
        }
    }

    private static bool IsIntegerStat(string? valueType)
    {
        return string.Equals(valueType, "Integer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIntegerField(ItemFieldDefinition field)
    {
        return field.ValueType == ItemFieldValueType.Integer;
    }

    private static string NormalizeIntegerText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue.ToString(CultureInfo.InvariantCulture);
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out doubleValue))
        {
            return RoundToInteger(doubleValue).ToString(CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static double RoundToInteger(double value)
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new();

        public bool Equals((string, string) x, (string, string) y)
        {
            return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string, string) obj)
        {
            return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
        }
    }

    private static EventGraphDefinition CreateDefaultEventGraph()
    {
        return new EventGraphDefinition
        {
            Id = "event.main",
            DisplayName = "主事件图",
            Nodes = [],
            Edges = []
        };
    }

    private static List<ProjectTreeNode> CreateDefaultHierarchyTree()
    {
        return
        [
            new()
            {
                Name = "scene-001",
                Kind = "folder",
                Children =
                [
                    new()
                    {
                        Name = "Canvas",
                        Kind = "folder",
                        Children =
                        [
                            new() { Name = "Camera", Kind = "item" }
                        ]
                    }
                ]
            }
        ];
    }

    private static List<ProjectTreeNode> CreateDefaultResourceTree()
    {
        return
        [
            new()
            {
                Name = "assets",
                Kind = "folder",
                Children =
                [
                    new() { Name = "images", Kind = "folder" },
                    new() { Name = "resources", Kind = "folder" },
                    new() { Name = "scripts", Kind = "folder" },
                    new()
                    {
                        Name = "game-data",
                        Kind = "folder",
                        Children =
                        [
                            new() { Name = "units", Kind = "folder" },
                            new() { Name = "items", Kind = "folder" },
                            new() { Name = "skills", Kind = "folder" },
                            new() { Name = "maps", Kind = "folder" },
                            new() { Name = "stats", Kind = "folder" }
                        ]
                    }
                ]
            }
        ];
    }
}
