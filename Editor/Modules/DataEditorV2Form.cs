using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Axe2DEditor.Core.Actors;
using Axe2DEditor.Core.Ai;
using Axe2DEditor.Core.Assets;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Decorations;
using Axe2DEditor.Core.Effects;
using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Core.Interactions;
using Axe2DEditor.Core.Items;
using Axe2DEditor.Core.Loot;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Projectiles;
using Axe2DEditor.Core.Projects;
using Axe2DEditor.Core.Rules;
using Axe2DEditor.Core.Skills;
using Axe2DEditor.Core.Stats;
using Axe2DEditor.Core.Strategy;
using Axe2DEditor.Core.Tactics;
using Axe2DEditor.Core.Traits;
using Axe2DEditor.Core.TowerDefense;
using Axe2DEditor.Core.Units;
using Axe2DEditor.Editor.Controls;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

public sealed class DataEditorV2Form : Form
{
    private const int LeftWidth = 300;

    private readonly ProjectContext _context;
    private readonly ProjectService _projectService;
    private readonly LocalizationService _localization;
    private readonly TreeView _domainTree = new();
    private readonly DataGridView _assetGrid = new BufferedDataGridView();
    private readonly TextBox _searchBox = new();
    private readonly Label _categoryTitleLabel = new();
    private readonly Label _categoryHelpLabel = new();
    private readonly Button _newButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _healthButton = new();

    private List<CategoryDescriptor> _categories = [];
    private CategoryDescriptor? _currentCategory;
    private CategoryDescriptor? _selectedRecordCategory;
    private object? _selectedRecord;
    private bool _suppressSelection;
    private readonly HashSet<string> _expandedNavigationGroupKeys = new(StringComparer.OrdinalIgnoreCase);

    public DataEditorV2Form(ProjectContext context, ProjectService projectService, LocalizationService localization)
    {
        _context = context;
        _projectService = projectService;
        _localization = localization;

        BuildCategories();
        BuildUi();
        PopulateNavigation();
        SelectFirstCategory();
    }

    private void BuildUi()
    {
        Text = T("module.dataEditor", "数据编辑器");
        MinimumSize = new Size(1180, 760);
        Size = new Size(1500, 920);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        TabSelectAllBehavior.InstallRecursive(this);

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(16, 10, 16, 10)
        };

        var title = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Text = T("dataEditor.v2.title", "核心数据库编辑器"),
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        topPanel.Controls.Add(title);

        ConfigureTopButton(_saveButton, T("inspector.save", "保存"), SaveProject);
        ConfigureTopButton(_healthButton, T("dataEditor.health.button", "健康检查"), RunDatabaseHealthCheck);
        ConfigureTopButton(_applyButton, T("common.edit", "编辑"), () => OpenSelectedRecordEditor());
        ConfigureTopButton(_deleteButton, T("common.delete", "删除"), DeleteRecord);
        ConfigureTopButton(_newButton, T("menu.create", "新建"), CreateRecord);
        topPanel.Controls.Add(_saveButton);
        topPanel.Controls.Add(_healthButton);
        topPanel.Controls.Add(_applyButton);
        topPanel.Controls.Add(_deleteButton);
        topPanel.Controls.Add(_newButton);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        var navTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = T("dataEditor.nav.domains", "核心原语"),
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _domainTree.Dock = DockStyle.Fill;
        _domainTree.HideSelection = false;
        _domainTree.FullRowSelect = true;
        _domainTree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is CategoryDescriptor category)
            {
                SelectCategory(category);
            }
        };
        leftPanel.Controls.Add(_domainTree);
        leftPanel.Controls.Add(navTitle);
        mainSplit.Panel1.Controls.Add(leftPanel);

        var listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };
        _categoryTitleLabel.Dock = DockStyle.Top;
        _categoryTitleLabel.Height = 28;
        _categoryTitleLabel.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _categoryHelpLabel.Dock = DockStyle.Top;
        _categoryHelpLabel.Height = 48;
        _categoryHelpLabel.ForeColor = SystemColors.GrayText;
        _categoryHelpLabel.AutoEllipsis = true;
        _searchBox.Dock = DockStyle.Top;
        _searchBox.Height = 32;
        _searchBox.Margin = new Padding(0, 0, 0, 8);
        _searchBox.PlaceholderText = T("dataEditor.search.placeholder", "全局搜索名称、ID、分类或摘要");
        _searchBox.TextChanged += (_, _) =>
        {
            PopulateNavigation();
            RefreshAssetGrid();
        };
        _assetGrid.Dock = DockStyle.Fill;
        _assetGrid.AllowUserToAddRows = false;
        _assetGrid.AllowUserToDeleteRows = false;
        _assetGrid.AllowUserToResizeRows = false;
        _assetGrid.ReadOnly = true;
        _assetGrid.MultiSelect = false;
        _assetGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _assetGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _assetGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _assetGrid.RowTemplate.Height = 28;
        _assetGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _assetGrid.RowHeadersVisible = false;
        _assetGrid.SelectionChanged += (_, _) => SelectGridRecord();
        _assetGrid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                _assetGrid.Rows[e.RowIndex].Selected = true;
                SelectGridRecord();
                OpenSelectedRecordEditor();
            }
        };
        listPanel.Controls.Add(_assetGrid);
        listPanel.Controls.Add(_searchBox);
        listPanel.Controls.Add(_categoryHelpLabel);
        listPanel.Controls.Add(_categoryTitleLabel);
        mainSplit.Panel2.Controls.Add(listPanel);
        Controls.Add(mainSplit);
        Controls.Add(topPanel);

        Shown += (_, _) => BeginInvoke(new Action(() => ApplySplitLayout(mainSplit)));
        SizeChanged += (_, _) => ApplySplitLayout(mainSplit);
    }

    private static void ApplySplitLayout(SplitContainer mainSplit)
    {
        SplitContainerLayout.ApplySafe(mainSplit, LeftWidth, 220, 720);
    }

    private static void ConfigureTopButton(Button button, string text, Action action)
    {
        button.Dock = DockStyle.Right;
        button.Text = text;
        button.Width = 118;
        button.Height = 34;
        button.Margin = new Padding(8, 0, 0, 0);
        button.UseVisualStyleBackColor = true;
        button.Click += (_, _) => action();
    }

    private void BuildCategories()
    {
        _categories =
        [
            Category("stats", "worldRules", "asset.stats", "属性", "dataEditor.help.stats", "定义所有单位、技能和公式可以引用的数值字段，例如生命、攻击、经验奖励。", lib => lib.Stats, CreateStat, r => ((StatDefinition)r).Id, r => Localized((StatDefinition)r), r => NameOrNone(((StatDefinition)r).CategoryKey, StatCategoryOptions()), r => FormatStatSummary((StatDefinition)r)),
            Category("traits", "worldRules", "asset.traits", "特性", "dataEditor.help.traits", "定义可被单位和规则引用的语义标签，例如玩家、敌人、可攻击、NPC。", lib => lib.Traits, CreateTrait, r => ((TraitDefinition)r).Id, r => Localized((TraitDefinition)r), r => NameOrNone(((TraitDefinition)r).CategoryKey, TraitCategoryOptions()), r => LocalizedDescription((TraitDefinition)r)),
            Category("factions", "worldRules", "asset.factions", "阵营", "dataEditor.help.factions", "定义单位之间的默认立场，事件编辑器可以在此基础上临时改变关系。", lib => lib.Factions, () => new FactionDefinition { Id = UniqueId("faction.custom", _context.Project.AssetLibrary.Factions.Select(v => v.Id)), DisplayName = "新阵营", AttitudeToPlayer = DefaultChoice("attitude", "neutral") }, r => ((FactionDefinition)r).Id, r => Localized((FactionDefinition)r), r => ChoiceLabel("attitude", ((FactionDefinition)r).AttitudeToPlayer), r => LocalizedDescription((FactionDefinition)r)),
            Category("damageTypes", "worldRules", "asset.damageTypes", "伤害类型", "dataEditor.help.damageTypes", "定义物理、魔法、真实伤害等伤害通道，以及它们读取哪个防御属性。", lib => lib.DamageTypes, () => new DamageTypeDefinition { Id = UniqueId("damage.custom", _context.Project.AssetLibrary.DamageTypes.Select(v => v.Id)), DisplayName = "新伤害类型", DefenseStatKey = DefaultStatKey("defense") }, r => ((DamageTypeDefinition)r).Id, r => Localized((DamageTypeDefinition)r), r => NameOrNone(((DamageTypeDefinition)r).DefenseStatKey, StatReferenceOptions()), r => LocalizedDescription((DamageTypeDefinition)r)),
            Category("elements", "worldRules", "asset.elements", "元素", "dataEditor.help.elements", "定义火焰、毒素、血契等元素语义，供技能、效果和表现颜色引用。", lib => lib.Elements, () => new ElementDefinition { Id = UniqueId("element.custom", _context.Project.AssetLibrary.Elements.Select(v => v.Id)), DisplayName = "新元素", ColorHex = "#ffffff" }, r => ((ElementDefinition)r).Id, r => Localized((ElementDefinition)r), r => ((ElementDefinition)r).ColorHex, r => LocalizedDescription((ElementDefinition)r)),
            Category("optionSets", "worldRules", "asset.optionSets", "选项集", "dataEditor.help.optionSets", "管理数据库下拉框使用的可选值，例如单位类型、AI 行为类型、技能类型、稀有度和地图视角。", lib => lib.OptionSets, CreateOptionSet, r => ((OptionSetDefinition)r).Id, r => Localized((OptionSetDefinition)r), r => ((OptionSetDefinition)r).Id, r => string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.options", "选项 {0}"), ((OptionSetDefinition)r).Values.Count)),
            Category("formulas", "worldRules", "asset.formulas", "公式", "dataEditor.help.formulas", "定义全局可复用计算公式。技能、玩法效果、状态、物品和事件只引用公式 ID，不重复写计算逻辑。", lib => lib.Formulas, () => new FormulaDefinition { Id = UniqueId("formula.custom", _context.Project.AssetLibrary.Formulas.Select(v => v.Id)), DisplayName = "新公式", Description = "自定义数值计算公式。", FormulaKind = DefaultChoice("formulaKind", "expression"), Expression = "basePower" }, r => ((FormulaDefinition)r).Id, r => Localized((FormulaDefinition)r), r => ChoiceLabel("formulaKind", ((FormulaDefinition)r).FormulaKind), r => FormatFormulaSummary((FormulaDefinition)r)),
            Category("units", "unitSystem", "asset.units", "单位", "dataEditor.help.units", "定义可放进地图或被事件生成的具体单位。单位的属性、AI、阵营、特性、组件和表现都直接配置在这里。", lib => lib.Units, CreateUnit, r => ((UnitDefinition)r).Id, r => Localized((UnitDefinition)r), r => ChoiceLabel("unitKind", ((UnitDefinition)r).UnitKind), r => FormatUnitSummary((UnitDefinition)r)),
            Category("aiProfiles", "unitSystem", "asset.aiProfiles", "AI 行为预设", "dataEditor.help.aiProfiles", "只描述单位如何行动：待机、巡逻、追击、远程保持距离、逃跑、Boss 策略。不保存属性、标签、奖励或外观。", lib => lib.AIProfiles, () => new AIProfileDefinition { Id = UniqueId("ai.custom", _context.Project.AssetLibrary.AIProfiles.Select(v => v.Id)), DisplayName = "新 AI 预设", BehaviorType = DefaultChoice("behaviorType", "passive"), TargetSelector = DefaultChoice("targetSelector", "none") }, r => ((AIProfileDefinition)r).Id, r => Localized((AIProfileDefinition)r), r => ChoiceLabel("behaviorType", ((AIProfileDefinition)r).BehaviorType), r => FormatAiSummary((AIProfileDefinition)r)),
            Category("componentPresets", "unitSystem", "asset.componentPresets", "组件预设", "dataEditor.help.componentPresets", "定义可复用能力组件，例如移动、生命、AI 控制、掉落、交互。组件负责持续能力，事件图负责触发流程。", lib => lib.ComponentPresets, () => new ComponentPresetDefinition { Id = UniqueId("component.custom", _context.Project.AssetLibrary.ComponentPresets.Select(v => v.Id)), DisplayName = "新组件预设", Component = new ComponentConfig { Type = DefaultChoice("componentType", "CustomComponent") } }, r => ((ComponentPresetDefinition)r).Id, r => Localized((ComponentPresetDefinition)r), r => ComponentLabel(((ComponentPresetDefinition)r).Component.Type), r => LocalizedDescription((ComponentPresetDefinition)r)),

            Category("skills", "combatSystem", "asset.skills", "技能", "dataEditor.help.skills", "定义技能的目标方式、消耗、冷却、效果、状态、投射物和表现引用；伤害、治疗等数值通常由玩法效果引用公式。", lib => lib.Skills, () => new SkillDefinition { Id = UniqueId("skill.custom", _context.Project.AssetLibrary.Skills.Select(v => v.Id)), DisplayName = "新技能", Effects = [new GameplayEffectReference { EffectId = DefaultOptionValue(GameplayEffectOptions(), "effect.damage.physical") }] }, r => ((SkillDefinition)r).Id, r => Localized((SkillDefinition)r), r => ChoiceLabel("targetingMode", ((SkillDefinition)r).TargetingMode), r => FormatSkillSummary((SkillDefinition)r)),
            Category("gameplayEffects", "combatSystem", "asset.gameplayEffects", "玩法效果", "dataEditor.help.gameplayEffects", "定义真正改变游戏数值或状态的效果，例如伤害、治疗、吸血、击退、奖励经验。", lib => lib.GameplayEffects, () => new GameplayEffectDefinition { Id = UniqueId("effect.custom", _context.Project.AssetLibrary.GameplayEffects.Select(v => v.Id)), DisplayName = "新玩法效果", EffectKind = DefaultChoice("effectKind", "damage") }, r => ((GameplayEffectDefinition)r).Id, r => Localized((GameplayEffectDefinition)r), r => ChoiceLabel("effectKind", ((GameplayEffectDefinition)r).EffectKind), r => FormatGameplayEffectSummary((GameplayEffectDefinition)r)),
            Category("statuses", "combatSystem", "asset.statuses", "状态", "dataEditor.help.statuses", "定义持续状态或控制效果，例如中毒、燃烧、减速、眩晕、护盾。", lib => lib.Statuses, () => new StatusDefinition { Id = UniqueId("status.custom", _context.Project.AssetLibrary.Statuses.Select(v => v.Id)), DisplayName = "新状态", StatusKind = DefaultChoice("statusKind", "buff") }, r => ((StatusDefinition)r).Id, r => Localized((StatusDefinition)r), r => ChoiceLabel("statusKind", ((StatusDefinition)r).StatusKind), r => FormatStatusSummary((StatusDefinition)r)),
            Category("projectiles", "combatSystem", "asset.projectiles", "投射物", "dataEditor.help.projectiles", "定义飞行速度、寿命、半径、命中效果和飞行表现。", lib => lib.Projectiles, () => new ProjectileDefinition { Id = UniqueId("projectile.custom", _context.Project.AssetLibrary.Projectiles.Select(v => v.Id)), DisplayName = "新投射物" }, r => ((ProjectileDefinition)r).Id, r => Localized((ProjectileDefinition)r), r => T("asset.projectiles", "投射物"), r => FormatProjectileSummary((ProjectileDefinition)r)),
            Category("visualEffects", "combatSystem", "asset.visualEffects", "视觉特效", "dataEditor.help.visualEffects", "定义视觉和声音表现，不直接改变游戏数值。玩法变化应放在玩法效果里。", lib => lib.VisualEffects, () => new VisualEffectDefinition { Id = UniqueId("vfx.custom", _context.Project.AssetLibrary.VisualEffects.Select(v => v.Id)), DisplayName = "新视觉特效" }, r => ((VisualEffectDefinition)r).Id, r => Localized((VisualEffectDefinition)r), r => ChoiceLabel("visualEffectKind", ((VisualEffectDefinition)r).EffectKind), r => FormatVisualEffectSummary((VisualEffectDefinition)r)),

            Category("itemTypes", "itemSystem", "asset.itemTypes", "物品类型", "dataEditor.help.itemTypes", "定义物品表单字段模板，例如消耗品、武器、防具、关键道具。", lib => lib.ItemTypes, () => new ItemTypeDefinition { Id = UniqueId("itemType.custom", _context.Project.AssetLibrary.ItemTypes.Select(v => v.Id)), DisplayName = "新物品类型" }, r => ((ItemTypeDefinition)r).Id, r => Localized((ItemTypeDefinition)r), r => T("asset.itemTypes", "物品类型"), r => string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.fields", "字段 {0}"), ((ItemTypeDefinition)r).Fields.Count)),
            Category("items", "itemSystem", "asset.items", "物品", "dataEditor.help.items", "定义道具、装备、关键物品及其效果引用。任务流程、给予/移除逻辑由事件编辑器驱动。", lib => lib.Items, () => new ItemDefinition { Id = UniqueId("item.custom", _context.Project.AssetLibrary.Items.Select(v => v.Id)), DisplayName = "新物品", TypeId = DefaultOptionValue(ItemTypeOptions(), "consumable"), StackLimit = 1 }, r => ((ItemDefinition)r).Id, r => Localized((ItemDefinition)r), r => GetItemTypeName(((ItemDefinition)r).TypeId), r => FormatItemSummary((ItemDefinition)r)),
            Category("lootTables", "itemSystem", "asset.lootTables", "掉落表", "dataEditor.help.lootTables", "定义死亡、破坏或事件奖励可引用的掉落候选。何时掉落由单位、装饰物或事件决定。", lib => lib.LootTables, () => new LootTableDefinition { Id = UniqueId("loot.custom", _context.Project.AssetLibrary.LootTables.Select(v => v.Id)), DisplayName = "新掉落表" }, r => ((LootTableDefinition)r).Id, r => Localized((LootTableDefinition)r), r => T("asset.lootTables", "掉落表"), r => string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.entries", "条目 {0}"), ((LootTableDefinition)r).Entries.Count)),

            Category("decorations", "sceneSystem", "asset.decorations", "装饰物", "dataEditor.help.decorations", "定义地图可放置的静态、发光、可破坏、可交互对象核心数据。触发后发生什么交给事件编辑器。", lib => lib.Decorations, () => new DecorationDefinition { Id = UniqueId("decor.custom", _context.Project.AssetLibrary.Decorations.Select(v => v.Id)), DisplayName = "新装饰物" }, r => ((DecorationDefinition)r).Id, r => Localized((DecorationDefinition)r), r => ChoiceLabel("decorationKind", ((DecorationDefinition)r).DecorationKind), r => FormatDecorationSummary((DecorationDefinition)r)),
            Category("maps", "sceneSystem", "tree.maps", "地图", "dataEditor.help.maps", "这里仅维护地图定义的基础元数据；具体绘制和放置由地图编辑器完成。", lib => lib.Maps, () => new MapDefinition { Id = UniqueId("map.custom", _context.Project.AssetLibrary.Maps.Select(v => v.Id)), DisplayName = "新地图", ViewType = DefaultChoice("viewType", "TopDown"), Width = 64, Height = 64 }, r => ((MapDefinition)r).Id, r => Localized((MapDefinition)r), r => ChoiceLabel("viewType", ((MapDefinition)r).ViewType), r => $"{((MapDefinition)r).Width} x {((MapDefinition)r).Height}"),
            Category("interactions", "sceneSystem", "asset.interactions", "交互入口", "dataEditor.help.interactions", "定义对象能向事件编辑器暴露什么触发入口，例如交互、营救、打开、传送。这里不写剧情流程。", lib => lib.InteractionProfiles, () => new InteractionProfileDefinition { Id = UniqueId("interaction.custom", _context.Project.AssetLibrary.InteractionProfiles.Select(v => v.Id)), DisplayName = "新交互入口", InteractionKind = DefaultChoice("interactionKind", "generic"), TriggerName = "OnInteract" }, r => ((InteractionProfileDefinition)r).Id, r => Localized((InteractionProfileDefinition)r), r => ChoiceLabel("interactionKind", ((InteractionProfileDefinition)r).InteractionKind), r => FormatInteractionSummary((InteractionProfileDefinition)r)),

            Category("routes", "sceneSystem", "asset.routes", "路线", "dataEditor.help.routes", "定义可复用路线：巡逻、护送、刷怪移动、逃跑路径或撤离路线都可以引用它。", lib => lib.TowerDefensePaths, CreateTowerDefensePath, r => ((TowerDefensePathDefinition)r).Id, r => Localized((TowerDefensePathDefinition)r), r => NameOrNone(((TowerDefensePathDefinition)r).MapId, MapOptions()), r => FormatTowerDefensePathSummary((TowerDefensePathDefinition)r)),
            Category("spawnWaves", "unitSystem", "asset.spawnWaves", "生成波次", "dataEditor.help.spawnWaves", "定义一组单位如何按路线和时间生成，可用于生存、竞技场、副本刷怪、增援或事件生成。", lib => lib.TowerDefenseWaves, CreateTowerDefenseWave, r => ((TowerDefenseWaveDefinition)r).Id, r => Localized((TowerDefenseWaveDefinition)r), r => NameOrNone(((TowerDefenseWaveDefinition)r).PathId, TowerDefensePathOptions()), r => FormatTowerDefenseWaveSummary((TowerDefenseWaveDefinition)r)),
            Category("levelRules", "worldRules", "asset.levelRules", "关卡流程", "dataEditor.help.levelRules", "定义一类玩法关卡的初始资源、生命/失败条件、胜利条件和波次引用。事件编辑器负责具体流程。", lib => lib.TowerDefenseRules, CreateTowerDefenseRule, r => ((TowerDefenseRuleDefinition)r).Id, r => Localized((TowerDefenseRuleDefinition)r), r => NameOrNone(((TowerDefenseRuleDefinition)r).MapId, MapOptions()), r => FormatTowerDefenseRuleSummary((TowerDefenseRuleDefinition)r)),
            Category("buildRules", "worldRules", "asset.buildRules", "放置限制", "dataEditor.help.buildRules", "定义在哪里能放置可建造单位、是否阻止堵路、是否允许出售和升级，以及使用哪个货币属性。", lib => lib.TowerDefenseBuildRules, CreateTowerDefenseBuildRule, r => ((TowerDefenseBuildRuleDefinition)r).Id, r => Localized((TowerDefenseBuildRuleDefinition)r), r => TagLabel(((TowerDefenseBuildRuleDefinition)r).BuildSurfaceTag), r => FormatTowerDefenseBuildRuleSummary((TowerDefenseBuildRuleDefinition)r)),
            Category("buildableUnits", "unitSystem", "asset.buildableUnits", "可建造单位", "dataEditor.help.buildableUnits", "定义可被建造、放置或升级的单位配置。塔、陷阱、建筑、召唤物都可以用同一套结构。", lib => lib.TowerDefenseTowers, CreateTowerDefenseTower, r => ((TowerDefenseTowerDefinition)r).Id, r => Localized((TowerDefenseTowerDefinition)r), r => ChoiceLabel("buildableRole", ((TowerDefenseTowerDefinition)r).TowerRole), r => FormatTowerDefenseTowerSummary((TowerDefenseTowerDefinition)r)),
            Category("tacticalGridRules", "sceneSystem", "asset.tacticalGridRules", "格子设置", "dataEditor.help.tacticalGridRules", "定义地图如何被战术规则读取：方格、六边形、移动距离、斜向移动、高低差和控制区。", lib => lib.TacticalGridRules, CreateTacticalGridRule, r => ((TacticalGridRuleDefinition)r).Id, r => Localized((TacticalGridRuleDefinition)r), r => ChoiceLabel("gridType", ((TacticalGridRuleDefinition)r).GridType), r => FormatTacticalGridRuleSummary((TacticalGridRuleDefinition)r)),
            Category("terrainRules", "sceneSystem", "asset.terrainRules", "地形设置", "dataEditor.help.terrainRules", "定义地形对移动、视线、防御、闪避、伤害和单位标签通行的影响。", lib => lib.TerrainRules, CreateTerrainRule, r => ((TerrainRuleDefinition)r).Id, r => Localized((TerrainRuleDefinition)r), r => TagLabel(((TerrainRuleDefinition)r).TerrainTag), r => FormatTerrainRuleSummary((TerrainRuleDefinition)r)),
            Category("turnRules", "worldRules", "asset.turnRules", "回合设置", "dataEditor.help.turnRules", "定义阵营回合、速度排序、行动点等回合制玩法的行动刷新和等待/撤销规则。", lib => lib.TurnRules, CreateTurnRule, r => ((TurnRuleDefinition)r).Id, r => Localized((TurnRuleDefinition)r), r => ChoiceLabel("turnMode", ((TurnRuleDefinition)r).TurnMode), r => FormatTurnRuleSummary((TurnRuleDefinition)r)),
            Category("actionRules", "worldRules", "asset.actionRules", "行动设置", "dataEditor.help.actionRules", "定义单位每回合能移动和行动几次、移动/攻击是否消耗行动、待机是否结束回合。", lib => lib.ActionRules, CreateActionRule, r => ((ActionRuleDefinition)r).Id, r => Localized((ActionRuleDefinition)r), r => r is ActionRuleDefinition action && action.CanAttackAfterMove ? T("common.yes", "是") : T("common.no", "否"), r => FormatActionRuleSummary((ActionRuleDefinition)r)),
            Category("tacticalRanges", "combatSystem", "asset.tacticalRanges", "战术范围", "dataEditor.help.tacticalRanges", "定义格子行动和技能使用的范围、最小/最大射程、影响区域、视线和可选目标。", lib => lib.TacticalRanges, CreateTacticalRange, r => ((TacticalRangeDefinition)r).Id, r => Localized((TacticalRangeDefinition)r), r => ChoiceLabel("rangeShape", ((TacticalRangeDefinition)r).RangeShape), r => FormatTacticalRangeSummary((TacticalRangeDefinition)r)),
            Category("objectiveRules", "worldRules", "asset.objectiveRules", "目标条件", "dataEditor.help.objectiveRules", "定义胜利或失败目标，例如全灭、保护、占领、撤离、护送、坚持回合或事件驱动目标。", lib => lib.ObjectiveRules, CreateObjectiveRule, r => ((ObjectiveRuleDefinition)r).Id, r => Localized((ObjectiveRuleDefinition)r), r => ChoiceLabel("objectiveType", ((ObjectiveRuleDefinition)r).ObjectiveType), r => FormatObjectiveRuleSummary((ObjectiveRuleDefinition)r)),
            Category("bondRules", "worldRules", "asset.bondRules", "羁绊关系", "dataEditor.help.bondRules", "定义单位相邻、标签、阵营和时机满足时可触发的关系加成；具体检查和结算交给事件或运行时。", lib => lib.BondRules, CreateBondRule, r => ((BondRuleDefinition)r).Id, r => Localized((BondRuleDefinition)r), r => ChoiceLabel("bondTriggerTiming", ((BondRuleDefinition)r).TriggerTiming), r => FormatBondRuleSummary((BondRuleDefinition)r)),
            Category("resourceRules", "worldRules", "asset.resourceRules", "资源定义", "dataEditor.help.resourceRules", "定义金币、木材、粮食、研究点等可复用资源，建造、生产、科技、事件都可以读取。", lib => lib.ResourceRules, CreateResourceRule, r => ((ResourceRuleDefinition)r).Id, r => Localized((ResourceRuleDefinition)r), r => ChoiceLabel("resourceKind", ((ResourceRuleDefinition)r).ResourceKind), r => FormatResourceRuleSummary((ResourceRuleDefinition)r)),
            Category("productionRules", "unitSystem", "asset.productionRules", "生产/产出", "dataEditor.help.productionRules", "定义单位、物品或资源如何被生产、训练、采集或周期产出，可用于城镇、据点、营地和经营玩法。", lib => lib.ProductionRules, CreateProductionRule, r => ((ProductionRuleDefinition)r).Id, r => Localized((ProductionRuleDefinition)r), r => ChoiceLabel("producedAssetKind", ((ProductionRuleDefinition)r).ProducedAssetKind), r => FormatProductionRuleSummary((ProductionRuleDefinition)r)),
            Category("techRules", "worldRules", "asset.techRules", "科技/解锁", "dataEditor.help.techRules", "定义科技、升级、解锁和研究消耗；科技产生的具体流程仍由事件编辑器或运行时系统执行。", lib => lib.TechRules, CreateTechRule, r => ((TechRuleDefinition)r).Id, r => Localized((TechRuleDefinition)r), r => ChoiceLabel("techKind", ((TechRuleDefinition)r).TechKind), r => FormatTechRuleSummary((TechRuleDefinition)r)),
            Category("diplomacyRules", "worldRules", "asset.diplomacyRules", "阵营关系", "dataEditor.help.diplomacyRules", "定义阵营之间的同盟、中立、敌对、贸易、通行和视野关系，供 AI、事件和目标规则引用。", lib => lib.DiplomacyRules, CreateDiplomacyRule, r => ((DiplomacyRuleDefinition)r).Id, r => Localized((DiplomacyRuleDefinition)r), r => ChoiceLabel("diplomaticState", ((DiplomacyRuleDefinition)r).DiplomaticState), r => FormatDiplomacyRuleSummary((DiplomacyRuleDefinition)r)),
            Category("territoryRules", "sceneSystem", "asset.territoryRules", "区域控制", "dataEditor.help.territoryRules", "定义占领点、势力范围和区域产出，可与地图标签、单位标签、目标规则和事件触发器组合。", lib => lib.TerritoryRules, CreateTerritoryRule, r => ((TerritoryRuleDefinition)r).Id, r => Localized((TerritoryRuleDefinition)r), r => TagLabel(((TerritoryRuleDefinition)r).TerritoryTag), r => FormatTerritoryRuleSummary((TerritoryRuleDefinition)r))
        ];
    }

    private CategoryDescriptor Category(
        string key,
        string domainKey,
        string displayKey,
        string fallbackName,
        string helpKey,
        string fallbackHelp,
        Func<AssetLibrary, IList> getItems,
        Func<object> createNew,
        Func<object, string> getId,
        Func<object, string> getName,
        Func<object, string> getType,
        Func<object, string> getSummary)
    {
        return new CategoryDescriptor(key, domainKey, displayKey, fallbackName, helpKey, fallbackHelp, getItems, createNew, getId, getName, getType, getSummary);
    }

    private void PopulateNavigation()
    {
        var selectedCategoryKey = _currentCategory?.Key;
        var hadTreeNodes = _domainTree.Nodes.Count > 0;
        _expandedNavigationGroupKeys.Clear();
        foreach (TreeNode node in _domainTree.Nodes)
        {
            if (node.Tag is NavigationGroupDescriptor group && node.IsExpanded)
            {
                _expandedNavigationGroupKeys.Add(group.Key);
            }
        }
        _domainTree.Nodes.Clear();
        TreeNode? nodeToSelect = null;
        var categoriesByKey = _categories.ToDictionary(category => category.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var group in GetNavigationGroups())
        {
            var groupNode = new TreeNode(T(group.DisplayKey, group.FallbackName))
            {
                NodeFont = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
                Tag = group
            };
            foreach (var categoryKey in group.CategoryKeys)
            {
                if (!categoriesByKey.TryGetValue(categoryKey, out var category))
                {
                    continue;
                }

                var count = category.GetItems(_context.Project.AssetLibrary).Count;
                var searchText = _searchBox.Text.Trim();
                var matchCount = CountSearchMatches(category, searchText);
                var nodeText = string.IsNullOrWhiteSpace(searchText)
                    ? $"{T(category.DisplayKey, category.FallbackName)}  {count}"
                    : $"{T(category.DisplayKey, category.FallbackName)}  {count} / {T("dataEditor.search.matches", "匹配")} {matchCount}";
                var categoryNode = new TreeNode(nodeText)
                {
                    Tag = category
                };
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    categoryNode.ForeColor = matchCount > 0 ? SystemColors.WindowText : SystemColors.GrayText;
                    if (_currentCategory is not null
                        && string.Equals(_currentCategory.Key, category.Key, StringComparison.OrdinalIgnoreCase)
                        && matchCount > 0)
                    {
                        categoryNode.BackColor = SystemColors.Info;
                    }
                }
                groupNode.Nodes.Add(categoryNode);
                if (!string.IsNullOrWhiteSpace(selectedCategoryKey)
                    && string.Equals(selectedCategoryKey, category.Key, StringComparison.OrdinalIgnoreCase))
                {
                    nodeToSelect = categoryNode;
                }
            }

            if (groupNode.Nodes.Count > 0)
            {
                _domainTree.Nodes.Add(groupNode);
                if (_expandedNavigationGroupKeys.Contains(group.Key))
                {
                    groupNode.Expand();
                }
                else if (!hadTreeNodes && nodeToSelect is null && _domainTree.Nodes.Count == 1)
                {
                    groupNode.Expand();
                }
                else
                {
                    groupNode.Collapse();
                }
            }
        }

        if (nodeToSelect is not null)
        {
            _domainTree.SelectedNode = nodeToSelect;
        }
    }

    private void SelectFirstCategory()
    {
        foreach (TreeNode group in _domainTree.Nodes)
        {
            var firstCategoryNode = FindFirstCategoryNode(group);
            if (firstCategoryNode is not null)
            {
                _domainTree.SelectedNode = firstCategoryNode;
                return;
            }
        }
    }

    private static TreeNode? FindFirstCategoryNode(TreeNode node)
    {
        if (node.Tag is CategoryDescriptor)
        {
            return node;
        }

        foreach (TreeNode child in node.Nodes)
        {
            var result = FindFirstCategoryNode(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private void SelectCategory(CategoryDescriptor category)
    {
        if (_currentCategory == category)
        {
            return;
        }

        _currentCategory = category;
        _selectedRecordCategory = null;
        _selectedRecord = null;
        _categoryTitleLabel.Text = T(category.DisplayKey, category.FallbackName);
        _categoryHelpLabel.Text = T(category.HelpKey, category.FallbackHelp);
        RefreshAssetGrid();
    }

    private void RefreshAssetGrid(string? selectedId = null)
    {
        _suppressSelection = true;
        _assetGrid.SuspendLayout();
        try
        {
            _assetGrid.Columns.Clear();
            _assetGrid.Rows.Clear();
            var filter = _searchBox.Text.Trim();
            var isGlobalSearch = !string.IsNullOrWhiteSpace(filter);
            _assetGrid.Columns.Add("name", T("table.assetName", "名称"));
            _assetGrid.Columns.Add("id", T("table.assetId", "ID"));
            if (isGlobalSearch)
            {
                _assetGrid.Columns.Add("node", T("dataEditor.search.node", "节点"));
            }
            _assetGrid.Columns.Add("type", T("table.category", "分类"));
            _assetGrid.Columns.Add("summary", T("table.summary", "摘要"));
            _assetGrid.Columns["summary"].FillWeight = 180;

            if (_currentCategory is null)
            {
                return;
            }

            var sourceCategories = isGlobalSearch ? _categories : [_currentCategory];
            foreach (var category in sourceCategories)
            {
                foreach (var record in category.GetItems(_context.Project.AssetLibrary).Cast<object>())
                {
                    var name = category.GetName(record);
                    var id = category.GetId(record);
                    var type = category.GetTypeText(record);
                    var summary = category.GetSummary(record);
                    if (!SearchMatchesRecord(filter, record, name, id, type, summary))
                    {
                        continue;
                    }

                    var rowIndex = isGlobalSearch
                        ? _assetGrid.Rows.Add(name, id, T(category.DisplayKey, category.FallbackName), type, summary)
                        : _assetGrid.Rows.Add(name, id, type, summary);
                    var row = _assetGrid.Rows[rowIndex];
                    row.Height = _assetGrid.RowTemplate.Height;
                    row.Tag = new AssetSearchResult(category, record);

                    if (isGlobalSearch && string.Equals(category.Key, _currentCategory.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = SystemColors.Info;
                    }

                    if (!string.IsNullOrWhiteSpace(selectedId) && string.Equals(id, selectedId, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        _assetGrid.CurrentCell = row.Cells[0];
                    }
                }
            }

            if (_assetGrid.Rows.Count > 0 && _assetGrid.SelectedRows.Count == 0)
            {
                _assetGrid.Rows[0].Selected = true;
                _assetGrid.CurrentCell = _assetGrid.Rows[0].Cells[0];
            }
        }
        finally
        {
            _assetGrid.ResumeLayout(true);
            _suppressSelection = false;
        }

        SelectGridRecord();
    }

    private int CountSearchMatches(CategoryDescriptor category, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return 0;
        }

        var count = 0;
        foreach (var record in category.GetItems(_context.Project.AssetLibrary).Cast<object>())
        {
            if (SearchMatchesRecord(filter, record, category.GetName(record), category.GetId(record), category.GetTypeText(record), category.GetSummary(record)))
            {
                count++;
            }
        }

        return count;
    }

    private static bool SearchMatchesRecord(string filter, object record, string name, string id, string type, string summary)
    {
        return string.IsNullOrWhiteSpace(filter)
            || name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || id.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || type.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || summary.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || GetRecordSearchText(record).Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRecordSearchText(object record)
    {
        try
        {
            return JsonSerializer.Serialize(record);
        }
        catch (NotSupportedException)
        {
            return record.ToString() ?? string.Empty;
        }
    }

    private void SelectGridRecord()
    {
        if (_suppressSelection)
        {
            return;
        }

        if (_assetGrid.SelectedRows.Count > 0 && _assetGrid.SelectedRows[0].Tag is AssetSearchResult result)
        {
            _selectedRecordCategory = result.Category;
            _selectedRecord = result.Record;
        }
        else
        {
            _selectedRecordCategory = null;
            _selectedRecord = null;
        }
        UpdateActionButtons();
    }

    private bool OpenSelectedRecordEditor()
    {
        if (_selectedRecordCategory is null || _selectedRecord is null)
        {
            return false;
        }

        return OpenRecordEditor(_selectedRecordCategory, _selectedRecord);
    }

    private bool OpenRecordEditor(CategoryDescriptor category, object record)
    {
        var editors = new List<FieldEditor>();
        using var dialog = CreateRecordDialog(category, record, editors);
        var result = dialog.ShowDialog(this);
        if (result != DialogResult.OK)
        {
            return false;
        }

        var selectedId = category.GetId(record);
        PopulateNavigation();
        RefreshAssetGrid(selectedId);
        return true;
    }

    private Form CreateRecordDialog(CategoryDescriptor category, object record, List<FieldEditor> editors)
    {
        var titleText = category.GetName(record);
        var dialog = new Form
        {
            Text = titleText,
            MinimumSize = new Size(880, 680),
            Size = new Size(1040, 760),
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            AutoScaleMode = AutoScaleMode.None
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 58,
            Padding = new Padding(12, 8, 12, 8)
        };
        var okButton = new Button
        {
            Text = T("dataEditor.action.apply", "应用修改"),
            Width = 118,
            Height = 36,
            UseVisualStyleBackColor = true
        };
        var cancelButton = new Button
        {
            Text = T("common.cancel", "取消"),
            Width = 96,
            Height = 36,
            DialogResult = DialogResult.Cancel,
            UseVisualStyleBackColor = true
        };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        var flow = new BufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16)
        };
        flow.Resize += (_, _) => ResizeDetailCards(flow);
        PopulateEditorFlow(flow, editors, record);

        okButton.Click += (_, _) =>
        {
            if (!ApplyEditors(editors, showErrors: true))
            {
                return;
            }

            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.Add(flow);
        dialog.Controls.Add(buttonPanel);
        return dialog;
    }

    private void PopulateEditorFlow(FlowLayoutPanel flow, List<FieldEditor> editors, object record)
    {
        flow.SuspendLayout();
        flow.Controls.Clear();

        if (record is FormulaDefinition formula)
        {
            AddFormulaEditor(flow, editors, formula);
        }
        else
        {
            foreach (var sectionGroup in BuildFieldSpecs(record).GroupBy(v => (v.SectionKey, v.SectionFallback)))
            {
                AddFieldSection(flow, editors, sectionGroup.Key.SectionKey, sectionGroup.Key.SectionFallback, sectionGroup.ToList(), record);
            }
        }

        flow.ResumeLayout(true);
        ResizeDetailCards(flow);
    }

    private void AddFieldSection(FlowLayoutPanel flow, List<FieldEditor> editors, string sectionKey, string sectionFallback, IReadOnlyList<FieldSpec> fields, object record)
    {
        var panel = CreateCard(flow);
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = T(sectionKey, sectionFallback),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold)
        };
        var fieldRows = fields.Select(field => new FieldRowState(field, record)).ToList();
        var rowLookup = fieldRows
            .GroupBy(v => v.Field.LabelKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(v => v.Key, v => v.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var row in fieldRows)
        {
            row.AttachSiblings(rowLookup);
        }
        var grid = CreateFieldSummaryGrid(fieldRows);
        editors.Add(new FieldEditor(fieldRows));

        panel.Controls.Add(grid);
        panel.Controls.Add(titleLabel);
        panel.Height = titleLabel.Height + grid.Height + panel.Padding.Vertical + 4;
        flow.Controls.Add(panel);
    }

    private void AddFormulaEditor(FlowLayoutPanel flow, List<FieldEditor> editors, FormulaDefinition formula)
    {
        var identityFields = new List<FieldSpec>();
        AddIdentityFields(identityFields, formula);
        AddFieldSection(flow, editors, "dataEditor.section.identity", "基础信息", identityFields, formula);
        AddFormulaWorkspace(flow, editors, formula);
        AddFormulaReferenceCard(flow, formula);
        AddFieldSection(
            flow,
            editors,
            "dataEditor.section.meta",
            "元数据",
            [Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => formula.BuiltIn, (_, v) => formula.BuiltIn = v, readOnly: formula.BuiltIn)],
            formula);
    }

    private void AddFormulaWorkspace(FlowLayoutPanel flow, List<FieldEditor> editors, FormulaDefinition formula)
    {
        var panel = CreateCard(flow);
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = T("dataEditor.formula.workspace", "公式编辑器"),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        AddFormulaEditorRow(
            layout,
            row++,
            T("dataEditor.field.formulaKind", "公式类型"),
            CreateRegisteredControl(editors, formula, Choice("dataEditor.section.formula", "公式", "dataEditor.field.formulaKind", "公式类型", _ => formula.FormulaKind, (_, v) => formula.FormulaKind = v, FormulaKindOptions)),
            34);

        AddFormulaEditorRow(
            layout,
            row++,
            T("dataEditor.field.graphId", "计算图 ID"),
            CreateRegisteredControl(editors, formula, TextField("dataEditor.section.formula", "公式", "dataEditor.field.graphId", "计算图 ID", _ => formula.GraphId, (_, v) => formula.GraphId = v)),
            34);

        var expressionControl = CreateRegisteredControl(
            editors,
            formula,
            Multiline("dataEditor.section.formula", "公式", "dataEditor.field.expression", "表达式", _ => formula.Expression, (_, v) => formula.Expression = v, 108));
        if (expressionControl is TextBox expressionTextBox)
        {
            expressionTextBox.Font = new Font(FontFamily.GenericMonospace, 9F);
            expressionTextBox.WordWrap = false;
            expressionTextBox.ScrollBars = ScrollBars.Both;

            var sampleInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(FontFamily.GenericMonospace, 9F),
                Text = BuildFormulaPreviewInput()
            };
            var resultLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var variableText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };

            void RefreshPreview()
            {
                variableText.Text = BuildFormulaVariableHelp(expressionTextBox.Text);
                resultLabel.Text = BuildFormulaPreviewStatus(expressionTextBox.Text, sampleInput.Text);
            }

            expressionTextBox.TextChanged += (_, _) => RefreshPreview();
            sampleInput.TextChanged += (_, _) => RefreshPreview();
            RefreshPreview();

            AddFormulaEditorRow(layout, row++, T("dataEditor.field.expression", "表达式"), expressionControl, 112);
            AddFormulaEditorRow(layout, row++, T("dataEditor.formula.previewInput", "测试变量"), sampleInput, 124);
            AddFormulaEditorRow(layout, row++, T("dataEditor.formula.previewResult", "计算结果"), resultLabel, 34);
            AddFormulaEditorRow(layout, row++, T("dataEditor.formula.variables", "变量说明"), variableText, 132);
        }

        panel.Controls.Add(layout);
        panel.Controls.Add(titleLabel);
        panel.Height = 572;
        flow.Controls.Add(panel);
    }

    private Control CreateRegisteredControl(List<FieldEditor> editors, object record, FieldSpec field)
    {
        var (control, editor) = CreateFieldControl(record, field);
        editors.Add(editor);
        return control;
    }

    private static void AddFormulaEditorRow(TableLayoutPanel layout, int row, string labelText, Control control, int height)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private void AddFormulaReferenceCard(FlowLayoutPanel flow, FormulaDefinition formula)
    {
        var panel = CreateCard(flow);
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = T("dataEditor.formula.references", "引用关系"),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold)
        };
        var references = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Text = BuildFormulaReferenceText(formula.Id)
        };
        panel.Height = 154;
        panel.Controls.Add(references);
        panel.Controls.Add(titleLabel);
        flow.Controls.Add(panel);
    }

    private string BuildFormulaPreviewInput()
    {
        return string.Join(Environment.NewLine,
        [
            "basePower = 20",
            "multiplier = 1.2",
            $"caster.attack = {GetStatValueText("attack", 30, invariant: true)}",
            $"caster.magicAttack = {GetStatValueText("magicAttack", 24, invariant: true)}",
            $"target.defense = {GetStatValueText("defense", 8, invariant: true)}",
            $"target.magicDefense = {GetStatValueText("magicDefense", 6, invariant: true)}",
            $"target.rewardExp = {GetStatValueText("rewardExp", 12, invariant: true)}"
        ]);
    }

    private string GetStatValueText(string key, double fallback, bool invariant)
    {
        var stat = _context.Project.AssetLibrary.Stats.FirstOrDefault(v => string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase));
        var value = stat?.DefaultValue ?? fallback;
        if (stat is null)
        {
            return invariant ? fallback.ToString(CultureInfo.InvariantCulture) : fallback.ToString(CultureInfo.CurrentCulture);
        }

        return invariant ? FormatStatValueInvariant(stat, value) : FormatStatValue(stat, value);
    }

    private string BuildFormulaPreviewStatus(string expression, string inputText)
    {
        try
        {
            var result = FormulaExpressionEvaluator.Evaluate(expression, ParseFormulaPreviewInputs(inputText));
            return string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.previewOk", "结果：{0}"), result.ToString("0.###", CultureInfo.CurrentCulture));
        }
        catch (Exception ex)
        {
            return string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.previewError", "无法计算：{0}"), ex.Message);
        }
    }

    private static Dictionary<string, double> ParseFormulaPreviewInputs(string text)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in ReadMeaningfulLines(text))
        {
            var parts = SplitKeyValue(line);
            if (parts is null)
            {
                throw new FormatException("测试变量格式应为 key = number。");
            }

            if (!double.TryParse(parts.Value.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && !double.TryParse(parts.Value.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                throw new FormatException($"变量 {parts.Value.Key} 不是数字。");
            }

            result[parts.Value.Key] = value;
        }

        return result;
    }

    private string BuildFormulaVariableHelp(string expression)
    {
        var detected = ExtractFormulaVariables(expression).ToList();
        var detectedText = detected.Count == 0
            ? T("dataEditor.formula.noDetectedVariables", "暂未检测到变量。")
            : string.Join(Environment.NewLine, detected.Select(v => $"  {v}"));

        var statText = _context.Project.AssetLibrary.Stats.Count == 0
            ? T("dataEditor.formula.noStats", "项目中还没有属性定义。")
            : string.Join(Environment.NewLine, _context.Project.AssetLibrary.Stats.Select(v => $"  caster.{v.Key} / target.{v.Key} = {FormatStatValue(v, v.DefaultValue)}"));

        return string.Join(Environment.NewLine,
        [
            T("dataEditor.formula.commonVariables", "常用变量："),
            string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.variableBasePower", "  {0}：技能基础值或效果基础值。"), "basePower"),
            string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.variableMultiplier", "  {0}：技能倍率或运行时传入倍率。"), "multiplier"),
            string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.variableCaster", "  {0}：施放者属性，例如 caster.attack。"), "caster.<属性Key>"),
            string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.variableTarget", "  {0}：目标属性，例如 target.defense。"), "target.<属性Key>"),
            "",
            T("dataEditor.formula.detectedVariables", "当前表达式变量："),
            detectedText,
            "",
            T("dataEditor.formula.availableStats", "可用属性："),
            statText,
            "",
            T("dataEditor.formula.functions", "函数：max、min、clamp、abs、round、floor、ceil")
        ]);
    }

    private string FormatStatValue(StatDefinition stat, double value)
    {
        var normalized = NormalizeStatValue(stat, value);
        return IsStatIntegerValueType(stat.ValueType)
            ? ((int)normalized).ToString(CultureInfo.CurrentCulture)
            : normalized.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private string FormatStatValueInvariant(StatDefinition stat, double value)
    {
        var normalized = NormalizeStatValue(stat, value);
        return IsStatIntegerValueType(stat.ValueType)
            ? ((int)normalized).ToString(CultureInfo.InvariantCulture)
            : normalized.ToString(CultureInfo.InvariantCulture);
    }

    private static double NormalizeStatValue(StatDefinition stat, double value)
    {
        return IsStatIntegerValueType(stat.ValueType) ? RoundToInteger(value) : value;
    }

    private static IEnumerable<string> ExtractFormulaVariables(string expression)
    {
        var functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "max", "min", "clamp", "abs", "round", "floor", "ceil", "ceiling" };
        return Regex.Matches(expression, @"\b[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*\b")
            .Select(v => v.Value)
            .Where(v => !functions.Contains(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase);
    }

    private string BuildFormulaReferenceText(string formulaId)
    {
        var references = new List<string>();
        references.AddRange(_context.Project.AssetLibrary.GameplayEffects
            .Where(v => string.Equals(v.FormulaId, formulaId, StringComparison.OrdinalIgnoreCase) || ReferencesFormula(v.Parameters, formulaId))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceEffect", "玩法效果：{0}（{1}）"), Localized(v), v.Id)));
        references.AddRange(_context.Project.AssetLibrary.Skills
            .Where(v => string.Equals(v.FormulaId, formulaId, StringComparison.OrdinalIgnoreCase))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceSkill", "技能直接引用：{0}（{1}）"), Localized(v), v.Id)));
        references.AddRange(_context.Project.AssetLibrary.Skills
            .Where(v => ReferencesFormula(v.Effects, formulaId))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceSkillEffect", "技能效果参数：{0}（{1}）"), Localized(v), v.Id)));
        references.AddRange(_context.Project.AssetLibrary.Items
            .Where(v => ReferencesFormula(v.Effects, formulaId))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceItemEffect", "物品效果参数：{0}（{1}）"), Localized(v), v.Id)));
        references.AddRange(_context.Project.AssetLibrary.Projectiles
            .Where(v => ReferencesFormula(v.Effects, formulaId))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceProjectileEffect", "投射物效果参数：{0}（{1}）"), Localized(v), v.Id)));
        references.AddRange(_context.Project.AssetLibrary.Statuses
            .Where(v => ReferencesFormula(v.OnApplyEffects, formulaId) || ReferencesFormula(v.PeriodicEffects, formulaId))
            .Select(v => string.Format(CultureInfo.CurrentCulture, T("dataEditor.formula.referenceStatusEffect", "状态效果参数：{0}（{1}）"), Localized(v), v.Id)));

        return references.Count == 0
            ? T("dataEditor.formula.noReferences", "当前没有资产引用这个公式。")
            : string.Join(Environment.NewLine, references);
    }

    private bool ReferencesFormula(IEnumerable<EffectParameterDefinition> parameters, string formulaId)
    {
        return parameters.Any(parameter =>
            parameter.ValueType is EffectParameterValueType.Choice or EffectParameterValueType.AssetRef
            && string.Equals(parameter.OptionSourceId, "formula", StringComparison.OrdinalIgnoreCase)
            && string.Equals(parameter.DefaultValue, formulaId, StringComparison.OrdinalIgnoreCase));
    }

    private bool ReferencesFormula(IEnumerable<GameplayEffectReference> references, string formulaId)
    {
        foreach (var reference in references)
        {
            var effect = FindGameplayEffect(reference.EffectId);
            if (effect is null)
            {
                continue;
            }

            foreach (var parameter in effect.Parameters)
            {
                if (parameter.ValueType is not (EffectParameterValueType.Choice or EffectParameterValueType.AssetRef)
                    || !string.Equals(parameter.OptionSourceId, "formula", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = reference.Parameters.FirstOrDefault(v => string.Equals(v.Key, parameter.Key, StringComparison.OrdinalIgnoreCase))?.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = parameter.DefaultValue;
                }

                if (string.Equals(value, formulaId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Panel CreateCard(FlowLayoutPanel flow)
    {
        return new Panel
        {
            Width = Math.Max(520, flow.ClientSize.Width - 40),
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(10)
        };
    }

    private static void ResizeDetailCards(FlowLayoutPanel flow)
    {
        var width = Math.Max(520, flow.ClientSize.Width - 40);
        foreach (Control control in flow.Controls)
        {
            control.Width = width;
        }
    }

    private DataGridView CreateFieldSummaryGrid(IReadOnlyList<FieldRowState> rows)
    {
        var grid = CreateEditorGrid(readOnly: true);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "label", HeaderText = T("table.assetName", "名称"), ReadOnly = true, FillWeight = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = T("dataEditor.field.value", "值"), ReadOnly = true, FillWeight = 220 });
        grid.RowTemplate.Height = 30;
        grid.ColumnHeadersHeight = 30;
        grid.Dock = DockStyle.Top;
        grid.ScrollBars = ScrollBars.None;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.Height = GetFieldSummaryGridHeight(rows.Count);

        foreach (var state in rows)
        {
            var rowIndex = grid.Rows.Add(T(state.Field.LabelKey, state.Field.LabelFallback), BuildFieldSummary(state));
            var row = grid.Rows[rowIndex];
            row.Tag = state;
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count)
            {
                return;
            }

            if (grid.Rows[e.RowIndex].Tag is not FieldRowState state || state.Field.ReadOnly)
            {
                return;
            }

            if (OpenFieldEditorDialog(state))
            {
                RefreshFieldSummaryGrid(grid);
            }
        };

        return grid;
    }

    private void RefreshFieldSummaryGrid(DataGridView grid)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is FieldRowState state)
            {
                row.Cells["value"].Value = BuildFieldSummary(state);
            }
        }
    }

    private static int GetFieldSummaryGridHeight(int rowCount)
    {
        var rows = Math.Max(1, rowCount);
        return 32 + (rows * 30) + 4;
    }

    private bool OpenFieldEditorDialog(FieldRowState state)
    {
        var dialogSize = GetFieldEditorDialogSize(
            state.Field,
            state.Field.Kind == FieldKind.MultiChoice
                ? Math.Max(state.Field.GetOptions().Count, SplitValues(state.TextValue).Count)
                : 0);
        using var dialog = new DataEditorFieldDialog
        {
            Text = T(state.Field.LabelKey, state.Field.LabelFallback),
            MinimumSize = dialogSize,
            ClientSize = dialogSize,
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            AutoScaleMode = AutoScaleMode.None
        };

        var editorControl = CreateFieldStateControl(state);
        dialog.SetEditor(editorControl);
        dialog.SetHint(T(state.Field.HintKey, state.Field.HintFallback));
        dialog.ConfirmButton.Text = T("dataEditor.action.apply", "应用修改");
        dialog.CancelActionButton.Text = T("common.cancel", "取消");

        dialog.ConfirmButton.Click += (_, _) =>
        {
            try
            {
                UpdateFieldStateFromControl(state, editorControl);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, ex.Message, dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private Size GetFieldEditorDialogSize(FieldSpec field, int optionCount)
    {
        var hasHint = !string.IsNullOrWhiteSpace(field.HintKey) || !string.IsNullOrWhiteSpace(field.HintFallback);
        return field.Kind switch
        {
            FieldKind.Boolean => new Size(320, hasHint ? 188 : 164),
            FieldKind.Number or FieldKind.Integer => new Size(360, hasHint ? 180 : 156),
            FieldKind.Choice => new Size(440, hasHint ? 186 : 162),
            FieldKind.Text => new Size(520, hasHint ? 190 : 166),
            FieldKind.MultilineText => new Size(640, Math.Max(260, field.Height + (hasHint ? 126 : 98))),
            FieldKind.MultiChoice => optionCount <= 1
                ? new Size(440, hasHint ? 170 : 144)
                : new Size(620, Math.Max(320, field.Height + (hasHint ? 144 : 116))),
            FieldKind.DoubleMap or FieldKind.StringMap or FieldKind.EffectParameters => new Size(840, Math.Max(600, field.Height + (hasHint ? 340 : 312))),
            FieldKind.Components => new Size(860, Math.Max(560, field.Height + (hasHint ? 320 : 292))),
            FieldKind.TowerDefenseWaypoints or FieldKind.TowerDefenseSpawnGroups or FieldKind.TowerDefenseWaveRefs or FieldKind.TowerDefenseTowerLevels => new Size(860, Math.Max(560, field.Height + (hasHint ? 320 : 292))),
            _ => new Size(520, hasHint ? 186 : 162)
        };
    }

    private Control CreateFieldStateControl(FieldRowState state)
    {
        Control control = state.Field.Kind switch
        {
            FieldKind.Number => IsIntegerNumericField(state)
                ? CreateIntegerBox(
                    (int)RoundToInteger(state.NumberValue),
                    Math.Min(ToIntegerBound(state.Field.NumberMin, lower: true), ToIntegerBound(state.Field.NumberMax, lower: false)),
                    Math.Max(ToIntegerBound(state.Field.NumberMin, lower: true), ToIntegerBound(state.Field.NumberMax, lower: false)))
                : CreateNumberBox(state.NumberValue, state.Field.NumberMin, state.Field.NumberMax, state.Field.DecimalPlaces),
            FieldKind.Integer => CreateIntegerBox(state.IntegerValue, state.Field.IntegerMin, state.Field.IntegerMax),
            FieldKind.Boolean => CreateBooleanCheckBox(T(state.Field.LabelKey, state.Field.LabelFallback), state.BoolValue, state.Field.ReadOnly),
            FieldKind.Choice => CreateComboBox(state.Field.GetOptions(), state.TextValue, state.Field.ReadOnly),
            FieldKind.MultiChoice => CreateCheckedList(state.Field.GetOptions(), SplitValues(state.TextValue), state.Field.ReadOnly),
            FieldKind.DoubleMap => CreateDoubleMapGrid(state.DoubleMapValue, state.Field.GetOptions(), state.Field.ReadOnly),
            FieldKind.StringMap => CreateStringMapGrid(state.StringMapValue, state.Field.GetOptions(), state.Field.ReadOnly, state.Field.AllowCustomKeys),
            FieldKind.Components => CreateComponentsGrid(state.ComponentsValue, state.Field.ReadOnly),
            FieldKind.EffectReferences => CreateGameplayEffectReferenceGrid(state.EffectReferencesValue, state.Field.ReadOnly),
            FieldKind.EffectParameters => CreateEffectParameterDefinitionGrid(state.EffectParametersValue, state.Field.ReadOnly),
            FieldKind.TowerDefenseWaypoints => CreateTowerDefenseWaypointGrid(state.TowerDefenseWaypointsValue, state.Field.ReadOnly),
            FieldKind.TowerDefenseSpawnGroups => CreateTowerDefenseSpawnGroupGrid(state.TowerDefenseSpawnGroupsValue, state.Field.ReadOnly),
            FieldKind.TowerDefenseWaveRefs => CreateTowerDefenseWaveRefGrid(state.TowerDefenseWaveRefsValue, state.Field.ReadOnly),
            FieldKind.TowerDefenseTowerLevels => CreateTowerDefenseTowerLevelGrid(state.TowerDefenseTowerLevelsValue, state.Field.ReadOnly),
            _ => CreateTextBox(state.TextValue, state.Field.Kind == FieldKind.MultilineText, state.Field.ReadOnly)
        };

        if (state.Field.Kind is FieldKind.Text or FieldKind.Number or FieldKind.Integer or FieldKind.Boolean or FieldKind.Choice)
        {
            control.Dock = DockStyle.Top;
            control.Height = state.Field.Kind == FieldKind.Boolean ? 28 : 32;
        }
        else
        {
            control.Dock = DockStyle.Fill;
        }

        control.Margin = Padding.Empty;

        return control;
    }

    private static void UpdateFieldStateFromControl(FieldRowState state, Control control)
    {
        switch (state.Field.Kind)
        {
            case FieldKind.Text:
            case FieldKind.MultilineText:
                state.TextValue = ((TextBox)control).Text;
                break;
            case FieldKind.Number:
                state.NumberValue = (double)((NumericUpDown)control).Value;
                break;
            case FieldKind.Integer:
                state.IntegerValue = (int)((NumericUpDown)control).Value;
                break;
            case FieldKind.Boolean:
                state.BoolValue = ((CheckBox)control).Checked;
                break;
            case FieldKind.Choice:
                state.TextValue = ((ComboBox)control).SelectedItem is OptionItem item ? item.Value : string.Empty;
                break;
            case FieldKind.MultiChoice:
                state.TextValue = string.Join(Environment.NewLine, ReadMultiChoiceValues(control));
                break;
            case FieldKind.DoubleMap:
                state.DoubleMapValue = ReadDoubleMapGrid(control);
                break;
            case FieldKind.StringMap:
                state.StringMapValue = ReadStringMapGrid(control);
                break;
            case FieldKind.Components:
                state.ComponentsValue = ReadComponentsGrid(control);
                break;
            case FieldKind.EffectReferences:
                state.EffectReferencesValue = ReadGameplayEffectReferenceGrid(control);
                break;
            case FieldKind.EffectParameters:
                state.EffectParametersValue = ReadEffectParameterDefinitionGrid(control);
                break;
            case FieldKind.TowerDefenseWaypoints:
                state.TowerDefenseWaypointsValue = ReadTowerDefenseWaypointGrid(control);
                break;
            case FieldKind.TowerDefenseSpawnGroups:
                state.TowerDefenseSpawnGroupsValue = ReadTowerDefenseSpawnGroupGrid(control);
                break;
            case FieldKind.TowerDefenseWaveRefs:
                state.TowerDefenseWaveRefsValue = ReadTowerDefenseWaveRefGrid(control);
                break;
            case FieldKind.TowerDefenseTowerLevels:
                state.TowerDefenseTowerLevelsValue = ReadTowerDefenseTowerLevelGrid(control);
                break;
        }
    }

    private static bool IsStatIntegerValueType(string? valueType)
    {
        return string.Equals(valueType, "Integer", StringComparison.OrdinalIgnoreCase);
    }

    private StatDefinition? TryGetStatDefinition(string key)
    {
        return _context.Project.AssetLibrary.Stats.FirstOrDefault(v => string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsIntegerStat(string key)
    {
        return IsStatIntegerValueType(TryGetStatDefinition(key)?.ValueType);
    }

    private double NormalizeStatEditorValue(string key, double value)
    {
        return IsIntegerStat(key) ? RoundToInteger(value) : value;
    }

    private string FormatStatEditorValue(string key, double value)
    {
        var normalized = NormalizeStatEditorValue(key, value);
        return IsIntegerStat(key)
            ? ((int)normalized).ToString(CultureInfo.CurrentCulture)
            : normalized.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private string FormatStatEditorValueInvariant(string key, double value)
    {
        var normalized = NormalizeStatEditorValue(key, value);
        return IsIntegerStat(key)
            ? ((int)normalized).ToString(CultureInfo.InvariantCulture)
            : normalized.ToString(CultureInfo.InvariantCulture);
    }

    private static bool IsIntegerNumericField(FieldRowState state)
    {
        if (state.Field.Kind != FieldKind.Number)
        {
            return false;
        }

        if (state.Field.UseIntegerNumericControl(state.Record))
        {
            return true;
        }

        return IsStatIntegerValueType(state.FindSibling("dataEditor.field.valueType")?.TextValue);
    }

    private static double RoundToInteger(double value)
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    private static int ToIntegerBound(decimal value, bool lower)
    {
        return lower ? (int)Math.Ceiling(value) : (int)Math.Floor(value);
    }

    private string BuildFieldSummary(FieldRowState state)
    {
        var none = T("dataEditor.option.none", "无");
        return state.Field.Kind switch
        {
            FieldKind.Number => IsIntegerNumericField(state)
                ? ((int)RoundToInteger(state.NumberValue)).ToString(CultureInfo.CurrentCulture)
                : state.NumberValue.ToString("0.###", CultureInfo.CurrentCulture),
            FieldKind.Integer => state.IntegerValue.ToString(CultureInfo.CurrentCulture),
            FieldKind.Boolean => state.BoolValue ? T("common.yes", "是") : T("common.no", "否"),
            FieldKind.Choice => NameOrNone(state.TextValue, state.Field.GetOptions()),
            FieldKind.MultiChoice => FormatLocalizedValues(SplitValues(state.TextValue), state.Field.GetOptions()),
            FieldKind.DoubleMap => FormatDoubleMapSummary(state.DoubleMapValue, state.Field.GetOptions(), none),
            FieldKind.StringMap => state.Field.LabelKey == "dataEditor.field.optionValues"
                ? FormatOptionValuesSummary(state.StringMapValue, state.Field.GetOptions(), none)
                : FormatStringMapSummary(state.StringMapValue, state.Field.GetOptions(), none),
            FieldKind.Components => FormatComponentsSummary(state.ComponentsValue, none),
            FieldKind.EffectReferences => FormatGameplayEffectReferenceSummary(state.EffectReferencesValue, none),
            FieldKind.EffectParameters => FormatEffectParameterDefinitionSummary(state.EffectParametersValue, none),
            FieldKind.TowerDefenseWaypoints => FormatCountSummary("dataEditor.summary.waypointCount", "路点 {0}", state.TowerDefenseWaypointsValue.Count),
            FieldKind.TowerDefenseSpawnGroups => FormatCountSummary("dataEditor.summary.spawnGroupCount", "刷怪组 {0}", state.TowerDefenseSpawnGroupsValue.Count),
            FieldKind.TowerDefenseWaveRefs => FormatLocalizedValues(state.TowerDefenseWaveRefsValue, TowerDefenseWaveOptions()),
            FieldKind.TowerDefenseTowerLevels => FormatCountSummary("dataEditor.summary.levelCount", "等级 {0}", state.TowerDefenseTowerLevelsValue.Count),
            _ => ShortSummary(state.Field.ReadDisplayText?.Invoke(state.Record) ?? state.TextValue, none)
        };
    }

    private string FormatCountSummary(string key, string fallback, int count)
    {
        return string.Format(CultureInfo.CurrentCulture, T(key, fallback), count);
    }

    private string FormatDoubleMapSummary(Dictionary<string, double> values, IReadOnlyList<OptionItem> options, string none)
    {
        if (values.Count == 0)
        {
            return none;
        }

        var parts = OrderedOptionKeys(values.Keys, options)
            .Where(values.ContainsKey)
            .Take(3)
            .Select(key => $"{NameOrNone(key, options)}={values[key].ToString("0.###", CultureInfo.CurrentCulture)}")
            .ToList();
        return values.Count > parts.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", parts), values.Count)
            : string.Join("、", parts);
    }

    private string FormatStringMapSummary(Dictionary<string, string> values, IReadOnlyList<OptionItem> options, string none)
    {
        var filledValues = values.Where(v => !string.IsNullOrWhiteSpace(v.Value)).ToList();
        if (filledValues.Count == 0)
        {
            return none;
        }

        var parts = OrderedOptionKeys(filledValues.Select(v => v.Key), options)
            .Where(key => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            .Take(2)
            .Select(key => $"{NameOrNone(key, options)}={values[key]}")
            .ToList();
        return filledValues.Count > parts.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", parts), filledValues.Count)
            : string.Join("、", parts);
    }

    private string FormatOptionValuesSummary(Dictionary<string, string> values, IReadOnlyList<OptionItem> options, string none)
    {
        var filledValues = values.Where(v => !string.IsNullOrWhiteSpace(v.Value)).ToList();
        if (filledValues.Count == 0)
        {
            return none;
        }

        var parts = OrderedOptionKeys(filledValues.Select(v => v.Key), options)
            .Where(key => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            .Take(3)
            .Select(key => NameOrNone(key, options))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
        return filledValues.Count > parts.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", parts), filledValues.Count)
            : string.Join("、", parts);
    }

    private string FormatComponentsSummary(IReadOnlyList<ComponentConfig> components, string none)
    {
        if (components.Count == 0)
        {
            return none;
        }

        var names = components.Select(v => ComponentLabel(v.Type)).Take(3).ToList();
        return components.Count > names.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", names), components.Count)
            : string.Join("、", names);
    }

    private string FormatGameplayEffectReferenceSummary(IReadOnlyList<GameplayEffectReference> references, string none)
    {
        if (references.Count == 0)
        {
            return none;
        }

        var names = references
            .Select(reference => NameOrNone(reference.EffectId, GameplayEffectOptions()))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(3)
            .ToList();
        return references.Count > names.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", names), references.Count)
            : string.Join("、", names);
    }

    private string FormatEffectParameterDefinitionSummary(IReadOnlyList<EffectParameterDefinition> parameters, string none)
    {
        if (parameters.Count == 0)
        {
            return none;
        }

        var parts = parameters
            .OrderBy(parameter => parameter.Order)
            .ThenBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(parameter =>
            {
                var name = T(parameter.DisplayNameKey, string.IsNullOrWhiteSpace(parameter.DisplayName) ? parameter.Key : parameter.DisplayName);
                return $"{name}={EffectParameterValueTypeLabel(parameter.ValueType)}";
            })
            .ToList();
        return parameters.Count > parts.Count
            ? string.Format(CultureInfo.CurrentCulture, T("dataEditor.summary.moreItems", "{0} 等 {1} 项"), string.Join("、", parts), parameters.Count)
            : string.Join("、", parts);
    }

    private static string ShortSummary(string text, string none)
    {
        var normalized = string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return none;
        }

        return normalized.Length > 80 ? $"{normalized[..77]}..." : normalized;
    }

    private (Control Control, FieldEditor Editor) CreateFieldControl(object record, FieldSpec field)
    {
        Control control = field.Kind switch
        {
            FieldKind.Number => field.UseIntegerNumericControl(record)
                ? CreateIntegerBox(
                    (int)RoundToInteger(field.ReadNumber(record)),
                    Math.Min(ToIntegerBound(field.NumberMin, lower: true), ToIntegerBound(field.NumberMax, lower: false)),
                    Math.Max(ToIntegerBound(field.NumberMin, lower: true), ToIntegerBound(field.NumberMax, lower: false)))
                : CreateNumberBox(field.ReadNumber(record), field.NumberMin, field.NumberMax, field.DecimalPlaces),
            FieldKind.Integer => CreateIntegerBox(field.ReadInteger(record), field.IntegerMin, field.IntegerMax),
            FieldKind.Boolean => CreateBooleanCheckBox(T(field.LabelKey, field.LabelFallback), field.ReadBool(record), field.ReadOnly),
            FieldKind.Choice => CreateComboBox(field.GetOptions(), field.ReadText(record), field.ReadOnly),
            FieldKind.MultiChoice => CreateCheckedList(field.GetOptions(), SplitValues(field.ReadText(record)), field.ReadOnly),
            FieldKind.DoubleMap => CreateDoubleMapGrid(field.ReadDoubleMap(record), field.GetOptions(), field.ReadOnly),
            FieldKind.StringMap => CreateStringMapGrid(field.ReadStringMap(record), field.GetOptions(), field.ReadOnly, field.AllowCustomKeys),
            FieldKind.Components => CreateComponentsGrid(field.ReadComponents(record), field.ReadOnly),
            FieldKind.EffectReferences => CreateGameplayEffectReferenceGrid(field.ReadEffectReferences(record), field.ReadOnly),
            FieldKind.EffectParameters => CreateEffectParameterDefinitionGrid(field.ReadEffectParameters(record), field.ReadOnly),
            FieldKind.TowerDefenseWaypoints => CreateTowerDefenseWaypointGrid(field.ReadTowerDefenseWaypoints(record), field.ReadOnly),
            FieldKind.TowerDefenseSpawnGroups => CreateTowerDefenseSpawnGroupGrid(field.ReadTowerDefenseSpawnGroups(record), field.ReadOnly),
            FieldKind.TowerDefenseWaveRefs => CreateTowerDefenseWaveRefGrid(field.ReadTowerDefenseWaveRefs(record), field.ReadOnly),
            FieldKind.TowerDefenseTowerLevels => CreateTowerDefenseTowerLevelGrid(field.ReadTowerDefenseTowerLevels(record), field.ReadOnly),
            _ => CreateTextBox(field.ReadText(record), field.Kind == FieldKind.MultilineText, field.ReadOnly)
        };

        return (control, new FieldEditor(field, record, control));
    }

    private static TextBox CreateTextBox(string text, bool multiline, bool readOnly)
    {
        var box = new TextBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            ReadOnly = readOnly,
            Margin = Padding.Empty,
            AutoSize = false
        };
        TabSelectAllBehavior.Attach(box);
        return box;
    }

    private static NumericUpDown CreateNumberBox(double value, decimal min, decimal max, int decimals)
    {
        var box = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Increment = decimals > 0 ? 0.1M : 1M,
            Value = ClampDecimal((decimal)value, min, max),
            Margin = Padding.Empty,
            AutoSize = false
        };
        TabSelectAllBehavior.Attach(box);
        return box;
    }

    private static NumericUpDown CreateIntegerBox(int value, int min, int max)
    {
        var box = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = 0,
            Value = Math.Clamp(value, min, max),
            Margin = Padding.Empty,
            AutoSize = false
        };
        TabSelectAllBehavior.Attach(box);
        return box;
    }

    private ComboBox CreateComboBox(IReadOnlyList<OptionItem> options, string value, bool readOnly, bool allowUnknownValue = true)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = !readOnly,
            Margin = Padding.Empty,
            AutoSize = false,
            IntegralHeight = false
        };
        combo.Items.AddRange(options.Cast<object>().ToArray());
        var selected = options.FirstOrDefault(v => string.Equals(v.Value, value, StringComparison.OrdinalIgnoreCase));
        if (selected is null && allowUnknownValue && !string.IsNullOrWhiteSpace(value))
        {
            selected = new OptionItem(value, UnknownOptionLabel(value));
            combo.Items.Add(selected);
        }

        combo.SelectedItem = selected ?? options.FirstOrDefault();
        return combo;
    }

    private CheckBox CreateBooleanCheckBox(string label, bool value, bool readOnly)
    {
        var checkBox = new CheckBox
        {
            Text = label,
            Checked = value,
            AutoSize = true,
            Enabled = !readOnly,
            Dock = DockStyle.Left,
            Margin = Padding.Empty
        };

        return checkBox;
    }

    private Control CreateCheckedList(IReadOnlyList<OptionItem> options, IReadOnlyCollection<string> selectedValues, bool readOnly)
    {
        var selectedSet = selectedValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderedOptions = new List<OptionItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options.Where(v => !string.IsNullOrWhiteSpace(v.Value)))
        {
            if (seen.Add(option.Value))
            {
                orderedOptions.Add(option);
            }
        }

        foreach (var value in selectedValues.Where(v => !string.IsNullOrWhiteSpace(v)).OrderBy(v => v, StringComparer.CurrentCulture))
        {
            if (seen.Add(value))
            {
                orderedOptions.Add(new OptionItem(value, UnknownOptionLabel(value)));
            }
        }

        if (orderedOptions.Count == 0)
        {
            return new Label
            {
                Text = T("dataEditor.option.none", "无"),
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ForeColor = SystemColors.GrayText
            };
        }

        var columns = Math.Min(5, Math.Max(1, orderedOptions.Count));
        var rows = (int)Math.Ceiling(orderedOptions.Count / (double)columns);
        var optionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columns,
            RowCount = rows,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        for (var i = 0; i < columns; i++)
        {
            optionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        for (var i = 0; i < rows; i++)
        {
            optionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var visualOptions = orderedOptions
            .OrderByDescending(v => selectedSet.Contains(v.Value))
            .ThenBy(v => v.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        var checkBoxes = new List<CheckBox>();
        for (var i = 0; i < visualOptions.Count; i++)
        {
            var option = visualOptions[i];
            var checkBox = new CheckBox
            {
                Text = option.DisplayName,
                Checked = selectedSet.Contains(option.Value),
                AutoSize = true,
                Enabled = !readOnly,
                Margin = new Padding(0, 2, 10, 2),
                Padding = Padding.Empty,
                Tag = option.Value
            };
            checkBoxes.Add(checkBox);
            optionPanel.Controls.Add(checkBox, i % columns, i / columns);
        }

        if (readOnly || checkBoxes.Count == 0)
        {
            return optionPanel;
        }

        var initialSelectedSet = new HashSet<string>(selectedSet, StringComparer.OrdinalIgnoreCase);
        var suppressSelectionRefresh = false;

        var toolbar = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 42,
            Padding = new Padding(0, 0, 0, 4),
            Margin = Padding.Empty
        };

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var selectAllButton = new Button
        {
            Text = T("common.selectAll", "全选"),
            Width = 72,
            Height = 30,
            Margin = new Padding(0, 0, 6, 0),
            UseVisualStyleBackColor = true
        };

        bool AreAllItemsChecked()
        {
            for (var i = 0; i < checkBoxes.Count; i++)
            {
                if (!checkBoxes[i].Checked)
                {
                    return false;
                }
            }

            return true;
        }

        void RefreshSelectAllButtonText()
        {
            selectAllButton.Text = AreAllItemsChecked()
                ? T("common.invertSelection", "反选")
                : T("common.selectAll", "全选");
        }

        void UpdateSelectionState(Action action)
        {
            suppressSelectionRefresh = true;
            try
            {
                action();
            }
            finally
            {
                suppressSelectionRefresh = false;
            }

            RefreshSelectAllButtonText();
        }

        selectAllButton.Click += (_, _) =>
        {
            if (AreAllItemsChecked())
            {
                UpdateSelectionState(() =>
                {
                    for (var i = 0; i < checkBoxes.Count; i++)
                    {
                        checkBoxes[i].Checked = !checkBoxes[i].Checked;
                    }
                });
                return;
            }

            UpdateSelectionState(() =>
            {
                for (var i = 0; i < checkBoxes.Count; i++)
                {
                    checkBoxes[i].Checked = true;
                }
            });
        };

        var defaultButton = new Button
        {
            Text = T("common.default", "默认"),
            Width = 72,
            Height = 30,
            Margin = Padding.Empty,
            UseVisualStyleBackColor = true
        };
        defaultButton.Click += (_, _) =>
        {
            UpdateSelectionState(() =>
            {
                for (var i = 0; i < checkBoxes.Count; i++)
                {
                    if (checkBoxes[i].Tag is string value)
                    {
                        checkBoxes[i].Checked = initialSelectedSet.Contains(value);
                    }
                }
            });
        };

        foreach (var checkBox in checkBoxes)
        {
            checkBox.CheckedChanged += (_, _) =>
            {
                if (!suppressSelectionRefresh)
                {
                    RefreshSelectAllButtonText();
                }
            };
        }

        buttonBar.Controls.Add(selectAllButton);
        buttonBar.Controls.Add(defaultButton);
        toolbar.Controls.Add(buttonBar);

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(6),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        optionPanel.Dock = DockStyle.Fill;
        host.Controls.Add(optionPanel, 0, 0);
        host.Controls.Add(toolbar, 0, 1);
        host.Height = host.PreferredSize.Height;
        RefreshSelectAllButtonText();
        return host;
    }

    private Control CreateDoubleMapGrid(Dictionary<string, double> values, IReadOnlyList<OptionItem> options, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "key", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = T("table.assetName", "名称"), ReadOnly = true, FillWeight = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = T("dataEditor.field.value", "数值"), ReadOnly = true, FillWeight = 80 });

        foreach (var key in OrderedOptionKeys(values.Keys, options))
        {
            var value = values.TryGetValue(key, out var currentValue) ? currentValue : 0;
            var rowIndex = grid.Rows.Add(key, NameOrNone(key, options), FormatStatEditorValue(key, value));
            grid.Rows[rowIndex].Tag = IsIntegerStat(key);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (readOnly || e.RowIndex < 0 || e.ColumnIndex != (grid.Columns["value"]?.Index ?? -1))
            {
                return;
            }

            EditNumericMapValue(grid, e.RowIndex, options);
        };

        return CreateGridEditorHost(grid, readOnly, () => AddDoubleMapRow(grid, options), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateStringMapGrid(Dictionary<string, string> values, IReadOnlyList<OptionItem> options, bool readOnly, bool allowCustomKeys)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.AllowUserToAddRows = allowCustomKeys && !readOnly;
        grid.AllowUserToDeleteRows = allowCustomKeys && !readOnly;
        var keyColumn = new DataGridViewTextBoxColumn
        {
            Name = "key",
            HeaderText = T("dataEditor.field.optionKey", T("dataEditor.field.key", "键")),
            ReadOnly = true,
            Visible = allowCustomKeys,
            FillWeight = 90
        };
        var nameColumn = new DataGridViewTextBoxColumn { Name = "name", HeaderText = T("dataEditor.field.entry", "条目"), ReadOnly = true, Visible = !allowCustomKeys, FillWeight = 120 };
        var valueColumn = new DataGridViewTextBoxColumn
        {
            Name = "value",
            HeaderText = allowCustomKeys
                ? T("dataEditor.field.optionDisplayName", T("dataEditor.field.displayName", "显示名称"))
                : T("dataEditor.field.value", "值"),
            ReadOnly = true,
            FillWeight = 180
        };
        grid.Columns.AddRange(keyColumn, nameColumn, valueColumn);

        if (allowCustomKeys)
        {
            valueColumn.DisplayIndex = 0;
            keyColumn.DisplayIndex = 1;
            nameColumn.DisplayIndex = 2;
        }
        else
        {
            nameColumn.DisplayIndex = 0;
            valueColumn.DisplayIndex = 1;
            keyColumn.DisplayIndex = 2;
        }

        foreach (var key in OrderedOptionKeys(values.Keys, options))
        {
            values.TryGetValue(key, out var currentValue);
            grid.Rows.Add(key, NameOrNone(key, options), currentValue ?? string.Empty);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (readOnly || e.RowIndex < 0)
            {
                return;
            }

            EditStringMapEntry(grid, e.RowIndex, options, allowCustomKeys);
        };

        return CreateGridEditorHost(grid, readOnly, () => AddStringMapRow(grid, options, allowCustomKeys), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateComponentsGrid(IReadOnlyList<ComponentConfig> components, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "componentIndex", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "componentType", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "parameterKey", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "componentName", HeaderText = T("dataEditor.field.component", "组件"), ReadOnly = true, FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "parameterName", HeaderText = T("dataEditor.field.parameter", "参数"), ReadOnly = true, FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = T("dataEditor.field.value", "值"), FillWeight = 150 });

        for (var componentIndex = 0; componentIndex < components.Count; componentIndex++)
        {
            var component = components[componentIndex];
            if (component.Parameters.Count == 0)
            {
                grid.Rows.Add(componentIndex, component.Type, "", ComponentLabel(component.Type), T("dataEditor.option.none", "无"), "");
                continue;
            }

            foreach (var parameter in component.Parameters.OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase))
            {
                grid.Rows.Add(componentIndex, component.Type, parameter.Key, ComponentLabel(component.Type), ComponentParameterLabel(parameter.Key), ObjectToText(parameter.Value));
            }
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (readOnly || e.RowIndex < 0)
            {
                return;
            }

            EditComponentRow(grid, e.RowIndex);
        };

        if (readOnly)
        {
            return grid;
        }

        return CreateGridEditorHost(grid, readOnly, () => AddComponentRow(grid), () => DeleteSelectedComponentRows(grid));
    }

    private DataGridView CreateEditorGrid(bool readOnly)
    {
        return new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            EditMode = DataGridViewEditMode.EditProgrammatically,
            MultiSelect = false,
            ReadOnly = readOnly,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
    }

    private Control CreateGridEditorHost(DataGridView grid, bool readOnly, Action addRow, Action deleteRow)
    {
        if (readOnly)
        {
            return grid;
        }

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(0, 0, 0, 4),
            Margin = Padding.Empty
        };

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var addButton = new Button
        {
            Text = T("common.add", "添加"),
            Width = 72,
            Height = 28,
            Margin = new Padding(0, 0, 6, 0),
            UseVisualStyleBackColor = true
        };
        addButton.Click += (_, _) => addRow();

        var deleteButton = new Button
        {
            Text = T("common.delete", "删除"),
            Width = 72,
            Height = 28,
            Margin = Padding.Empty,
            UseVisualStyleBackColor = true
        };
        deleteButton.Click += (_, _) => deleteRow();

        buttonBar.Controls.Add(addButton);
        buttonBar.Controls.Add(deleteButton);
        toolbar.Controls.Add(buttonBar);
        host.Controls.Add(grid);
        host.Controls.Add(toolbar);
        return host;
    }

    private void AddDoubleMapRow(DataGridView grid, IReadOnlyList<OptionItem> options)
    {
        grid.EndEdit();

        var existingKeys = GetGridKeys(grid);
        var availableOptions = options.Where(option => !string.IsNullOrWhiteSpace(option.Value) && !existingKeys.Contains(option.Value)).ToList();
        var promptOptions = availableOptions.Count > 0 ? availableOptions : options;
        var initialKey = availableOptions.Count > 0 ? availableOptions[0].Value : string.Empty;
        if (!TryPromptMapEntry(
                T("common.add", "添加"),
                T("dataEditor.field.key", "键"),
                T("dataEditor.field.value", "数值"),
                promptOptions,
                allowCustomKeys: true,
                initialKey,
                0,
                key => IsIntegerStat(key),
                out var key,
                out var value))
        {
            return;
        }

        if (existingKeys.Contains(key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add(key, NameOrNone(key, options), FormatStatEditorValue(key, value));
        grid.Rows[rowIndex].Tag = IsIntegerStat(key);
        SelectGridRow(grid, rowIndex);
    }

    private void AddStringMapRow(DataGridView grid, IReadOnlyList<OptionItem> options, bool allowCustomKeys)
    {
        grid.EndEdit();

        var existingKeys = GetGridKeys(grid);
        var availableOptions = options.Where(option => !string.IsNullOrWhiteSpace(option.Value) && !existingKeys.Contains(option.Value)).ToList();
        if (!allowCustomKeys && availableOptions.Count == 0)
        {
            MessageBox.Show(this, T("dataEditor.map.noAvailableKeys", "当前没有可添加的条目。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var promptOptions = availableOptions.Count > 0 ? availableOptions : options;
        var initialKey = availableOptions.Count > 0 ? availableOptions[0].Value : string.Empty;
        if (!TryPromptMapEntry(
                T("common.add", "添加"),
                T("dataEditor.field.key", "键"),
                grid.Columns["value"]?.HeaderText ?? T("dataEditor.field.value", "值"),
                promptOptions,
                allowCustomKeys,
                initialKey,
                string.Empty,
                out var key,
                out var value))
        {
            return;
        }

        if (existingKeys.Contains(key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add(key, NameOrNone(key, options), value);
        SelectGridRow(grid, rowIndex);
    }

    private void AddComponentRow(DataGridView grid)
    {
        grid.EndEdit();

        var selectedRowIndex = GetSelectedGridRowIndex(grid);
        var componentIndex = selectedRowIndex is null ? GetNextComponentIndex(grid) : ReadCellInt(grid.Rows[selectedRowIndex.Value], "componentIndex");
        var currentType = selectedRowIndex is null ? DefaultChoice("componentType", "CustomComponent") : ReadCellString(grid.Rows[selectedRowIndex.Value], "componentType");
        var usedParameterKeys = GetComponentParameterKeys(grid, componentIndex);
        var initialParameter = ComponentParameterOptions()
            .Where(option => !string.IsNullOrWhiteSpace(option.Value) && !usedParameterKeys.Contains(option.Value))
            .Select(option => option.Value)
            .FirstOrDefault()
            ?? DefaultChoice("componentParameter", "speedStat");

        if (!TryPromptComponentEntry(
                T("common.add", "添加"),
                T("dataEditor.field.componentType", "组件类型"),
                T("dataEditor.field.parameter", "参数"),
                T("dataEditor.field.value", "值"),
                currentType,
                initialParameter,
                string.Empty,
                out var componentType,
                out var parameterKey,
                out var value))
        {
            return;
        }

        if (usedParameterKeys.Contains(parameterKey))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), parameterKey), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var insertIndex = selectedRowIndex is null ? grid.Rows.Count : selectedRowIndex.Value + 1;
        grid.Rows.Insert(insertIndex, componentIndex, componentType, parameterKey, ComponentLabel(componentType), ComponentParameterLabel(parameterKey), value);
        SelectGridRow(grid, insertIndex);
    }

    private void EditComponentRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        var row = grid.Rows[rowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        grid.EndEdit();

        var componentIndex = ReadCellInt(row, "componentIndex");
        var currentType = ReadCellString(row, "componentType");
        var currentParameter = ReadCellString(row, "parameterKey");
        var currentValue = ReadCellString(row, "value");
        if (!TryPromptComponentEntry(
                T("common.edit", "编辑"),
                T("dataEditor.field.componentType", "组件类型"),
                T("dataEditor.field.parameter", "参数"),
                T("dataEditor.field.value", "值"),
                currentType,
                currentParameter,
                currentValue,
                out var componentType,
                out var parameterKey,
                out var value))
        {
            return;
        }

        var existingParameterKeys = GetComponentParameterKeys(grid, componentIndex);
        existingParameterKeys.Remove(currentParameter);
        if (existingParameterKeys.Contains(parameterKey))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), parameterKey), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (DataGridViewRow otherRow in grid.Rows)
        {
            if (otherRow.IsNewRow)
            {
                continue;
            }

            if (ReadCellInt(otherRow, "componentIndex") != componentIndex)
            {
                continue;
            }

            otherRow.Cells["componentType"].Value = componentType;
            otherRow.Cells["componentName"].Value = ComponentLabel(componentType);
        }

        row.Cells["parameterKey"].Value = parameterKey;
        row.Cells["parameterName"].Value = ComponentParameterLabel(parameterKey);
        row.Cells["value"].Value = value;
    }

    private void DeleteSelectedComponentRows(DataGridView grid)
    {
        grid.EndEdit();

        var rowIndex = GetSelectedGridRowIndex(grid);
        if (rowIndex is null)
        {
            return;
        }

        grid.Rows.RemoveAt(rowIndex.Value);

        if (grid.Rows.Count == 0)
        {
            return;
        }

        SelectGridRow(grid, Math.Min(rowIndex.Value, grid.Rows.Count - 1));
    }

    private static int GetNextComponentIndex(DataGridView grid)
    {
        var max = -1;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            max = Math.Max(max, ReadCellInt(row, "componentIndex"));
        }

        return max + 1;
    }

    private static HashSet<string> GetComponentParameterKeys(DataGridView grid, int componentIndex)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow || ReadCellInt(row, "componentIndex") != componentIndex)
            {
                continue;
            }

            var key = ReadCellString(row, "parameterKey");
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private void EditStringMapEntry(DataGridView grid, int rowIndex, IReadOnlyList<OptionItem> options, bool allowCustomKeys)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        var row = grid.Rows[rowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        grid.EndEdit();

        var currentKey = ReadCellString(row, "key");
        var currentValue = ReadCellString(row, "value");
        if (!TryPromptMapEntry(
                T("common.edit", "编辑"),
                grid.Columns["key"]?.HeaderText ?? T("dataEditor.field.optionKey", T("dataEditor.field.key", "键")),
                grid.Columns["value"]?.HeaderText ?? T("dataEditor.field.optionDisplayName", T("dataEditor.field.displayName", "显示名称")),
                options,
                allowCustomKeys,
                currentKey,
                currentValue,
                out var key,
                out var value))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show(this, T("itemField.error.emptyKey", "键不能为空。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existingKeys = GetGridKeys(grid);
        existingKeys.Remove(currentKey);
        if (existingKeys.Contains(key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        row.Cells["key"].Value = key;
        row.Cells["value"].Value = value;
        if (grid.Columns["name"]?.Visible == true)
        {
            row.Cells["name"].Value = NameOrNone(key, options);
        }

        SelectGridRow(grid, rowIndex);
    }

    private void EditNumericMapValue(DataGridView grid, int rowIndex, IReadOnlyList<OptionItem> options)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        var row = grid.Rows[rowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        grid.EndEdit();

        var currentText = ReadCellString(row, "value");
        var initialValue = double.TryParse(currentText, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed)
            || double.TryParse(currentText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : 0;

        if (!TryPromptDoubleValue(
                string.Format(CultureInfo.CurrentCulture, "{0}：{1}", grid.Columns["value"]?.HeaderText ?? T("dataEditor.field.value", "数值"), NameOrNone(ReadCellString(row, "key"), options)),
                grid.Columns["value"]?.HeaderText ?? T("dataEditor.field.value", "数值"),
                initialValue,
                row.Tag is bool integerMode && integerMode,
                out var value))
        {
            return;
        }

        row.Cells["value"].Value = FormatStatEditorValue(ReadCellString(row, "key"), value);
    }

    private bool TryPromptDoubleValue(string title, string label, double initialValue, bool integerMode, out double value)
    {
        var input = integerMode
            ? CreateIntegerBox((int)RoundToInteger(initialValue), -999999, 999999)
            : CreateNumberBox(initialValue, -999999M, 999999M, 3);
        input.Dock = DockStyle.Fill;
        var selectedValue = initialValue;

        if (!ShowPromptDialog(title, CreateLabeledPromptContent(label, input), () =>
        {
            selectedValue = (double)input.Value;
            return true;
        }))
        {
            value = initialValue;
            return false;
        }

        value = selectedValue;
        return true;
    }

    private bool TryPromptTextValue(string title, string label, string initialValue, out string value)
    {
        var input = CreateTextBox(initialValue, multiline: false, readOnly: false);
        input.Dock = DockStyle.Fill;
        var selectedValue = initialValue;

        if (!ShowPromptDialog(title, CreateLabeledPromptContent(label, input), () =>
        {
            selectedValue = input.Text;
            return true;
        }))
        {
            value = initialValue;
            return false;
        }

        value = selectedValue;
        return true;
    }

    private bool TryPromptMapEntry(string title, string keyLabel, string valueLabel, IReadOnlyList<OptionItem> options, bool allowCustomKeys, string initialKey, double initialValue, Func<string, bool>? integerModeSelector, out string key, out double value)
    {
        var keyControl = CreateOptionKeyInputControl(options, allowCustomKeys, initialKey);
        keyControl.Dock = DockStyle.Fill;
        NumericUpDown? valueControl = null;
        var selectedKey = string.Empty;
        var selectedValue = initialValue;

        var valueHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        void RebuildValueControl(string rawKey)
        {
            var integerMode = integerModeSelector?.Invoke(rawKey) == true;
            var currentValue = valueControl is not null ? (double)valueControl.Value : initialValue;
            if (valueControl is not null)
            {
                valueHost.Controls.Remove(valueControl);
                valueControl.Dispose();
            }

            valueControl = integerMode
                ? CreateIntegerBox((int)RoundToInteger(currentValue), -999999, 999999)
                : CreateNumberBox(currentValue, -999999M, 999999M, 3);
            valueControl.Dock = DockStyle.Fill;
            valueControl.Margin = Padding.Empty;
            valueHost.Controls.Add(valueControl);
        }

        RebuildValueControl(ReadOptionValue(keyControl));

        void HandleKeyChanged(object? sender, EventArgs e)
        {
            RebuildValueControl(ReadOptionValue(keyControl));
        }

        if (keyControl is ComboBox comboBox)
        {
            comboBox.SelectedIndexChanged += HandleKeyChanged;
            comboBox.TextChanged += HandleKeyChanged;
        }
        else if (keyControl is TextBox textBox)
        {
            textBox.TextChanged += HandleKeyChanged;
        }

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddPromptRow(content, 0, keyLabel, keyControl);
        AddPromptRow(content, 1, valueLabel, valueHost);

        if (!ShowPromptDialog(title, content, () =>
        {
            var rawKey = ReadOptionValue(keyControl);
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                MessageBox.Show(this, T("itemField.error.emptyKey", "键不能为空。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            selectedKey = rawKey;
            selectedValue = valueControl is not null ? (double)valueControl.Value : initialValue;
            return true;
        }))
        {
            key = initialKey;
            value = initialValue;
            return false;
        }

        key = selectedKey;
        value = selectedValue;
        return true;
    }

    private bool TryPromptMapEntry(string title, string keyLabel, string valueLabel, IReadOnlyList<OptionItem> options, bool allowCustomKeys, string initialKey, string initialValue, out string key, out string value)
    {
        var keyControl = CreateOptionKeyInputControl(options, allowCustomKeys, initialKey);
        var valueControl = CreateTextBox(initialValue, multiline: false, readOnly: false);
        keyControl.Dock = DockStyle.Fill;
        var selectedKey = string.Empty;
        var selectedValue = initialValue;

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddPromptRow(content, 0, keyLabel, keyControl);
        AddPromptRow(content, 1, valueLabel, valueControl);

        if (!ShowPromptDialog(title, content, () =>
        {
            var rawKey = ReadOptionValue(keyControl);
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                MessageBox.Show(this, T("itemField.error.emptyKey", "键不能为空。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            selectedKey = rawKey;
            selectedValue = valueControl.Text;
            return true;
        }))
        {
            key = initialKey;
            value = initialValue;
            return false;
        }

        key = selectedKey;
        value = selectedValue;
        return true;
    }

    private bool TryPromptComponentEntry(string title, string typeLabel, string parameterLabel, string valueLabel, string initialType, string initialParameter, string initialValue, out string componentType, out string parameterKey, out string value)
    {
        var typeControl = CreateKeyInputControl(ComponentTypeOptions(), allowCustomKeys: true, initialType);
        var parameterControl = CreateKeyInputControl(ComponentParameterOptions(), allowCustomKeys: true, initialParameter);
        var valueHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty };
        Control? valueControl = null;
        typeControl.Dock = DockStyle.Fill;
        parameterControl.Dock = DockStyle.Fill;

        var selectedType = initialType;
        var selectedParameter = initialParameter;
        var selectedValue = initialValue;

        void RebuildValueControl(string componentType, string parameterKey, string currentValue)
        {
            valueHost.SuspendLayout();
            if (valueControl is not null)
            {
                valueHost.Controls.Remove(valueControl);
                valueControl.Dispose();
            }

            valueControl = CreateComponentParameterValueControl(componentType, parameterKey, currentValue);
            valueControl.Dock = DockStyle.Fill;
            valueControl.Margin = Padding.Empty;
            valueHost.Controls.Add(valueControl);
            valueHost.ResumeLayout(true);
        }

        RebuildValueControl(ReadOptionValue(typeControl), ReadOptionValue(parameterControl), initialValue);

        void HandleSemanticChanged(object? sender, EventArgs e)
        {
            var currentValue = valueControl is null ? initialValue : ReadComponentParameterValue(valueControl);
            RebuildValueControl(ReadOptionValue(typeControl), ReadOptionValue(parameterControl), currentValue);
        }

        AttachOptionInputChanged(typeControl, HandleSemanticChanged);
        AttachOptionInputChanged(parameterControl, HandleSemanticChanged);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddPromptRow(content, 0, typeLabel, typeControl);
        AddPromptRow(content, 1, parameterLabel, parameterControl);
        AddPromptRow(content, 2, valueLabel, valueHost);

        if (!ShowPromptDialog(title, content, () =>
        {
            selectedType = ReadOptionValue(typeControl);
            selectedParameter = ReadOptionValue(parameterControl);
            selectedValue = valueControl is null ? string.Empty : ReadComponentParameterValue(valueControl);
            if (string.IsNullOrWhiteSpace(selectedType))
            {
                MessageBox.Show(this, T("behavior.error.invalidComponentType", "组件类型不能为空。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedParameter))
            {
                MessageBox.Show(this, T("behavior.error.invalidComponentParameterKey", "组件参数不能为空。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }))
        {
            componentType = initialType;
            parameterKey = initialParameter;
            value = initialValue;
            return false;
        }

        componentType = selectedType;
        parameterKey = selectedParameter;
        value = selectedValue;
        return true;
    }

    private static TableLayoutPanel CreatePromptTable(int rowCount, int labelWidth = 120)
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rowCount,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < rowCount; row++)
        {
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, PromptRowHeight));
        }

        return content;
    }

    private Control CreateComponentParameterValueControl(string componentType, string parameterKey, string value)
    {
        var options = GetComponentParameterValueOptions(componentType, parameterKey);
        if (options.Count > 0)
        {
            return CreateComboBox(options, value, readOnly: false);
        }

        if (IsBooleanComponentParameter(parameterKey, value))
        {
            return AttachBooleanLabelBehavior(new CheckBox
            {
                Checked = bool.TryParse(value, out var boolValue) && boolValue,
                AutoSize = true,
                Dock = DockStyle.Left
            });
        }

        if (IsIntegerComponentParameter(parameterKey, value))
        {
            return CreateIntegerBox(ParseInteger(value), -999999, 999999);
        }

        if (IsNumberComponentParameter(parameterKey, value))
        {
            return CreateNumberBox((double)ParseDecimal(value), -999999M, 999999M, 3);
        }

        return CreateTextBox(value, multiline: false, readOnly: false);
    }

    private IReadOnlyList<OptionItem> GetComponentParameterValueOptions(string componentType, string parameterKey)
    {
        if (string.IsNullOrWhiteSpace(parameterKey))
        {
            return [];
        }

        if (parameterKey.EndsWith("Stat", StringComparison.OrdinalIgnoreCase)
            || parameterKey.EndsWith("StatKey", StringComparison.OrdinalIgnoreCase))
        {
            return StatReferenceOptions();
        }

        if (string.Equals(parameterKey, "profileField", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentFieldReferenceOptions(("AIProfileId", "dataEditor.field.aiProfile", "AI 预设"));
        }

        if (string.Equals(parameterKey, "lootTableField", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentFieldReferenceOptions(("LootTableId", "dataEditor.field.lootTable", "掉落表"));
        }

        if (string.Equals(parameterKey, "skillField", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentFieldReferenceOptions(("SkillId", "dataEditor.field.skills", "技能"));
        }

        if (string.Equals(parameterKey, "projectileField", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentFieldReferenceOptions(("ProjectileId", "dataEditor.field.projectile", "投射物"));
        }

        if (string.Equals(parameterKey, "profileId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterKey, "AIProfileId", StringComparison.OrdinalIgnoreCase))
        {
            return AIProfileOptions();
        }

        if (parameterKey.Contains("lootTable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterKey, "LootTableId", StringComparison.OrdinalIgnoreCase))
        {
            return LootTableOptions();
        }

        if (parameterKey.Contains("projectile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterKey, "ProjectileId", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectileOptions();
        }

        if (parameterKey.Contains("skill", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterKey, "SkillId", StringComparison.OrdinalIgnoreCase))
        {
            return SkillOptions();
        }

        if (parameterKey.Contains("trigger", StringComparison.OrdinalIgnoreCase))
        {
            return InteractionTriggerOptions();
        }

        if (string.Equals(parameterKey, "interactionMode", StringComparison.OrdinalIgnoreCase))
        {
            return InteractionKindOptions();
        }

        return [];
    }

    private IReadOnlyList<OptionItem> ComponentFieldReferenceOptions(params (string Value, string LabelKey, string Fallback)[] fields)
    {
        return fields
            .Select(field => new OptionItem(field.Value, T(field.LabelKey, field.Fallback)))
            .ToList();
    }

    private static bool IsBooleanComponentParameter(string parameterKey, string value)
    {
        return bool.TryParse(value, out _)
            || parameterKey.StartsWith("is", StringComparison.OrdinalIgnoreCase)
            || parameterKey.StartsWith("has", StringComparison.OrdinalIgnoreCase)
            || parameterKey.StartsWith("can", StringComparison.OrdinalIgnoreCase)
            || parameterKey.EndsWith("Enabled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIntegerComponentParameter(string parameterKey, string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            && (parameterKey.Contains("count", StringComparison.OrdinalIgnoreCase)
                || parameterKey.Contains("phase", StringComparison.OrdinalIgnoreCase)
                || parameterKey.Contains("layer", StringComparison.OrdinalIgnoreCase)
                || parameterKey.Contains("level", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNumberComponentParameter(string parameterKey, string value)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            || parameterKey.Contains("range", StringComparison.OrdinalIgnoreCase)
            || parameterKey.Contains("radius", StringComparison.OrdinalIgnoreCase)
            || parameterKey.Contains("speed", StringComparison.OrdinalIgnoreCase)
            || parameterKey.Contains("cooldown", StringComparison.OrdinalIgnoreCase)
            || parameterKey.Contains("delay", StringComparison.OrdinalIgnoreCase)
            || parameterKey.Contains("force", StringComparison.OrdinalIgnoreCase);
    }

    private static void AttachOptionInputChanged(Control control, EventHandler handler)
    {
        switch (control)
        {
            case ComboBox comboBox:
                comboBox.SelectedIndexChanged += handler;
                comboBox.TextChanged += handler;
                break;
            case TextBox textBox:
                textBox.TextChanged += handler;
                break;
        }
    }

    private static string ReadComponentParameterValue(Control control)
    {
        return control switch
        {
            NumericUpDown numeric => numeric.Value.ToString(CultureInfo.InvariantCulture),
            CheckBox checkBox => checkBox.Checked ? "true" : "false",
            ComboBox comboBox when comboBox.SelectedItem is OptionItem item => item.Value,
            ComboBox comboBox => comboBox.Text,
            TextBox textBox => textBox.Text,
            _ => string.Empty
        };
    }

    private bool ShowPromptDialog(string title, Control content, Func<bool> onOk)
    {
        using var dialog = new DataEditorPromptDialog
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            AutoScaleMode = AutoScaleMode.None,
            ClientSize = new Size(PromptDialogWidth, GetPromptDialogHeight(content))
        };
        dialog.SetContent(content);
        dialog.ConfirmButton.Text = T("common.ok", "确定");
        dialog.CancelActionButton.Text = T("common.cancel", "取消");

        dialog.ConfirmButton.Click += (_, _) =>
        {
            try
            {
                if (!onOk())
                {
                    return;
                }

                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, ex.Message, dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private static Control CreateLabeledPromptContent(string labelText, Control editor)
    {
        PreparePromptEditor(editor);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, PromptRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, PromptRowHeight + 4));
        layout.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(editor, 0, 1);
        return layout;
    }

    private static void AddPromptRow(TableLayoutPanel layout, int row, string labelText, Control editor)
    {
        PreparePromptEditor(editor);
        while (layout.RowStyles.Count <= row)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, PromptRowHeight));
        }
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft
        };
        label.Margin = Padding.Empty;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(editor, 1, row);
    }

    private static void PreparePromptEditor(Control editor)
    {
        editor.Margin = Padding.Empty;
        editor.Padding = Padding.Empty;
        if (editor is Panel or TableLayoutPanel)
        {
            editor.Dock = DockStyle.Fill;
            return;
        }

        editor.Dock = DockStyle.Fill;
        editor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        editor.Margin = new Padding(0, 2, 0, 2);
        editor.MinimumSize = new Size(0, PromptEditorHeight);
        editor.Height = PromptEditorHeight;
    }

    private const int PromptRowHeight = 40;
    private const int PromptEditorHeight = 32;
    private const int PromptDialogWidth = 420;
    private const int PromptDialogMinHeight = 192;
    private const int PromptDialogPadding = 20;
    private const int PromptDialogButtonHeight = 58;
    private const int PromptDialogSafetyPadding = 8;

    private static int GetPromptDialogHeight(Control content)
    {
        var rowCount = content as TableLayoutPanel is { } tableLayoutPanel
            ? Math.Max(tableLayoutPanel.RowCount, tableLayoutPanel.RowStyles.Count)
            : 2;

        var height = (PromptDialogPadding * 2) + PromptDialogButtonHeight + (rowCount * PromptRowHeight) + PromptDialogSafetyPadding;
        return Math.Max(PromptDialogMinHeight, height);
    }

    private static Control CreateKeyInputControl(IReadOnlyList<OptionItem> options, bool allowCustomKeys, string initialValue)
    {
        if (options.Count == 0)
        {
            var textBox = CreateTextBox(initialValue, multiline: false, readOnly: false);
            textBox.Dock = DockStyle.Fill;
            textBox.Margin = Padding.Empty;
            return textBox;
        }

        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = allowCustomKeys ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
            Margin = Padding.Empty
        };
        if (allowCustomKeys)
        {
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        }
        else
        {
            combo.AutoCompleteSource = AutoCompleteSource.None;
            combo.AutoCompleteMode = AutoCompleteMode.None;
        }
        combo.Items.AddRange(options.Cast<object>().ToArray());

        var selected = options.FirstOrDefault(v => string.Equals(v.Value, initialValue, StringComparison.OrdinalIgnoreCase));
        if (selected is not null)
        {
            combo.SelectedItem = selected;
        }
        else if (allowCustomKeys)
        {
            combo.Text = initialValue;
        }
        else if (options.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        if (allowCustomKeys)
        {
            TabSelectAllBehavior.Attach(combo);
        }

        return combo;
    }

    private static Control CreateOptionKeyInputControl(IReadOnlyList<OptionItem> options, bool allowCustomKeys, string initialValue)
    {
        return CreateKeyInputControl(options, allowCustomKeys, initialValue);
    }

    private static string ReadOptionValue(Control control)
    {
        return control switch
        {
            ComboBox combo when combo.SelectedItem is OptionItem item => item.Value.Trim(),
            ComboBox combo => combo.Text.Trim(),
            TextBox textBox => textBox.Text.Trim(),
            _ => control.Text.Trim()
        };
    }

    private Control CreateGameplayEffectReferenceGrid(IReadOnlyList<GameplayEffectReference> references, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "effectId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "effectName", HeaderText = T("dataEditor.field.effect", "效果"), ReadOnly = true, FillWeight = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "effectKind", HeaderText = T("dataEditor.field.effectKind", "效果类型"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "parameters", HeaderText = T("dataEditor.field.parameters", "参数"), ReadOnly = true, FillWeight = 180 });

        foreach (var reference in references)
        {
            AddGameplayEffectReferenceRow(grid, reference);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (readOnly || e.RowIndex < 0)
            {
                return;
            }

            EditGameplayEffectReferenceRow(grid, e.RowIndex);
        };

        if (readOnly)
        {
            return grid;
        }

        return CreateGridEditorHost(grid, readOnly, () => AddGameplayEffectReferenceRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateEffectParameterDefinitionGrid(IReadOnlyList<EffectParameterDefinition> parameters, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "key", HeaderText = T("dataEditor.field.key", "Key"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "displayName", HeaderText = T("dataEditor.field.displayName", "显示名称"), ReadOnly = true, FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "valueType", HeaderText = T("dataEditor.field.valueType", "值类型"), ReadOnly = true, FillWeight = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "defaultValue", HeaderText = T("dataEditor.field.defaultValue", "默认值"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "optionSource", HeaderText = T("dataEditor.field.optionSource", "选项来源"), ReadOnly = true, FillWeight = 90 });

        foreach (var parameter in parameters.OrderBy(parameter => parameter.Order).ThenBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase))
        {
            AddEffectParameterDefinitionRow(grid, parameter);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (readOnly || e.RowIndex < 0)
            {
                return;
            }

            EditEffectParameterDefinitionRow(grid, e.RowIndex);
        };

        if (readOnly)
        {
            return grid;
        }

        return CreateGridEditorHost(grid, readOnly, () => AddEffectParameterDefinitionRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateTowerDefenseWaypointGrid(IReadOnlyList<TowerDefenseWaypointDefinition> waypoints, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "key", HeaderText = T("dataEditor.field.waypointKey", "路点 Key"), ReadOnly = true, FillWeight = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "x", HeaderText = T("dataEditor.field.x", "X"), ReadOnly = true, FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "y", HeaderText = T("dataEditor.field.y", "Y"), ReadOnly = true, FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "waitSeconds", HeaderText = T("dataEditor.field.waitSeconds", "等待秒数"), ReadOnly = true, FillWeight = 90 });

        foreach (var waypoint in waypoints)
        {
            AddTowerDefenseWaypointRow(grid, waypoint);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (!readOnly && e.RowIndex >= 0)
            {
                EditTowerDefenseWaypointRow(grid, e.RowIndex);
            }
        };

        return readOnly
            ? grid
            : CreateGridEditorHost(grid, readOnly, () => AddTowerDefenseWaypointRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateTowerDefenseSpawnGroupGrid(IReadOnlyList<TowerDefenseSpawnGroupDefinition> spawnGroups, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "unitId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "unitName", HeaderText = T("dataEditor.field.unit", "单位"), ReadOnly = true, FillWeight = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "count", HeaderText = T("dataEditor.field.count", "数量"), ReadOnly = true, FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "intervalSeconds", HeaderText = T("dataEditor.field.spawnInterval", "生成间隔"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "delaySeconds", HeaderText = T("dataEditor.field.delaySeconds", "延迟秒数"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pathId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pathName", HeaderText = T("dataEditor.field.path", "路线"), ReadOnly = true, FillWeight = 140 });

        foreach (var spawnGroup in spawnGroups)
        {
            AddTowerDefenseSpawnGroupRow(grid, spawnGroup);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (!readOnly && e.RowIndex >= 0)
            {
                EditTowerDefenseSpawnGroupRow(grid, e.RowIndex);
            }
        };

        return readOnly
            ? grid
            : CreateGridEditorHost(grid, readOnly, () => AddTowerDefenseSpawnGroupRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateTowerDefenseWaveRefGrid(IReadOnlyList<string> waveIds, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "waveId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "waveName", HeaderText = T("dataEditor.field.wave", "波次"), ReadOnly = true, FillWeight = 180 });

        foreach (var waveId in waveIds.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            AddTowerDefenseWaveRefRow(grid, waveId);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (!readOnly && e.RowIndex >= 0)
            {
                EditTowerDefenseWaveRefRow(grid, e.RowIndex);
            }
        };

        return readOnly
            ? grid
            : CreateGridEditorHost(grid, readOnly, () => AddTowerDefenseWaveRefRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private Control CreateTowerDefenseTowerLevelGrid(IReadOnlyList<TowerDefenseTowerLevelDefinition> levels, bool readOnly)
    {
        var grid = CreateEditorGrid(readOnly);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "level", HeaderText = T("dataEditor.field.level", "等级"), ReadOnly = true, FillWeight = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "upgradeCost", HeaderText = T("dataEditor.field.upgradeCost", "升级费用"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "rangeBonus", HeaderText = T("dataEditor.field.rangeBonus", "射程加成"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "damageMultiplier", HeaderText = T("dataEditor.field.damageMultiplier", "伤害倍率"), ReadOnly = true, FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "attackIntervalMultiplier", HeaderText = T("dataEditor.field.attackIntervalMultiplier", "攻击间隔倍率"), ReadOnly = true, FillWeight = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "skillId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "skillName", HeaderText = T("dataEditor.field.skill", "技能"), ReadOnly = true, FillWeight = 150 });

        foreach (var level in levels.OrderBy(value => value.Level))
        {
            AddTowerDefenseTowerLevelRow(grid, level);
        }

        grid.CellDoubleClick += (_, e) =>
        {
            if (!readOnly && e.RowIndex >= 0)
            {
                EditTowerDefenseTowerLevelRow(grid, e.RowIndex);
            }
        };

        return readOnly
            ? grid
            : CreateGridEditorHost(grid, readOnly, () => AddTowerDefenseTowerLevelRow(grid), () => DeleteSelectedGridRow(grid));
    }

    private void AddTowerDefenseWaypointRow(DataGridView grid, TowerDefenseWaypointDefinition waypoint)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseWaypointRow(row, waypoint);
        row.Tag = CloneTowerDefenseWaypoint(waypoint);
    }

    private void FillTowerDefenseWaypointRow(DataGridViewRow row, TowerDefenseWaypointDefinition waypoint)
    {
        row.Cells["key"].Value = waypoint.Key;
        row.Cells["x"].Value = waypoint.X.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["y"].Value = waypoint.Y.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["waitSeconds"].Value = waypoint.WaitSeconds.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private void AddTowerDefenseWaypointRow(DataGridView grid)
    {
        grid.EndEdit();
        var initial = new TowerDefenseWaypointDefinition
        {
            Key = UniqueSimpleKey("waypoint", ReadGridColumnValues(grid, "key"))
        };

        if (!TryPromptTowerDefenseWaypoint(T("common.add", "添加"), initial, out var waypoint))
        {
            return;
        }

        var existingKeys = ReadGridColumnValues(grid, "key").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(waypoint.Key) && existingKeys.Contains(waypoint.Key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), waypoint.Key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseWaypointRow(row, waypoint);
        row.Tag = waypoint;
        SelectGridRow(grid, rowIndex);
    }

    private void EditTowerDefenseWaypointRow(DataGridView grid, int rowIndex)
    {
        if (!TryGetEditableGridRow(grid, rowIndex, out var row))
        {
            return;
        }

        var initial = row.Tag as TowerDefenseWaypointDefinition
            ?? new TowerDefenseWaypointDefinition
            {
                Key = ReadCellString(row, "key"),
                X = ReadCellDouble(row, "x"),
                Y = ReadCellDouble(row, "y"),
                WaitSeconds = ReadCellDouble(row, "waitSeconds")
            };

        if (!TryPromptTowerDefenseWaypoint(T("common.edit", "编辑"), CloneTowerDefenseWaypoint(initial), out var updated))
        {
            return;
        }

        var existingKeys = ReadGridColumnValues(grid, "key").ToHashSet(StringComparer.OrdinalIgnoreCase);
        existingKeys.Remove(initial.Key);
        if (!string.IsNullOrWhiteSpace(updated.Key) && existingKeys.Contains(updated.Key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), updated.Key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        FillTowerDefenseWaypointRow(row, updated);
        row.Tag = updated;
        SelectGridRow(grid, rowIndex);
    }

    private bool TryPromptTowerDefenseWaypoint(string title, TowerDefenseWaypointDefinition initial, out TowerDefenseWaypointDefinition waypoint)
    {
        var keyControl = CreateTextBox(initial.Key, multiline: false, readOnly: false);
        var xControl = CreateNumberBox(initial.X, -999999M, 999999M, 3);
        var yControl = CreateNumberBox(initial.Y, -999999M, 999999M, 3);
        var waitControl = CreateNumberBox(initial.WaitSeconds, 0M, 999999M, 3);
        var content = CreatePromptTable(4);
        AddPromptRow(content, 0, T("dataEditor.field.waypointKey", "路点 Key"), keyControl);
        AddPromptRow(content, 1, T("dataEditor.field.x", "X"), xControl);
        AddPromptRow(content, 2, T("dataEditor.field.y", "Y"), yControl);
        AddPromptRow(content, 3, T("dataEditor.field.waitSeconds", "等待秒数"), waitControl);

        var selected = CloneTowerDefenseWaypoint(initial);
        if (!ShowPromptDialog(title, content, () =>
        {
            selected.Key = keyControl.Text.Trim();
            selected.X = (double)xControl.Value;
            selected.Y = (double)yControl.Value;
            selected.WaitSeconds = (double)waitControl.Value;
            return true;
        }))
        {
            waypoint = initial;
            return false;
        }

        waypoint = selected;
        return true;
    }

    private void AddTowerDefenseSpawnGroupRow(DataGridView grid, TowerDefenseSpawnGroupDefinition spawnGroup)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseSpawnGroupRow(row, spawnGroup);
        row.Tag = CloneTowerDefenseSpawnGroup(spawnGroup);
    }

    private void FillTowerDefenseSpawnGroupRow(DataGridViewRow row, TowerDefenseSpawnGroupDefinition spawnGroup)
    {
        row.Cells["unitId"].Value = spawnGroup.UnitId;
        row.Cells["unitName"].Value = NameOrNone(spawnGroup.UnitId, UnitOptions());
        row.Cells["count"].Value = spawnGroup.Count.ToString(CultureInfo.CurrentCulture);
        row.Cells["intervalSeconds"].Value = spawnGroup.IntervalSeconds.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["delaySeconds"].Value = spawnGroup.DelaySeconds.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["pathId"].Value = spawnGroup.PathId;
        row.Cells["pathName"].Value = NameOrNone(spawnGroup.PathId, TowerDefensePathOptions());
    }

    private void AddTowerDefenseSpawnGroupRow(DataGridView grid)
    {
        grid.EndEdit();
        var initial = new TowerDefenseSpawnGroupDefinition
        {
            UnitId = DefaultOptionValue(UnitOptions(), "unit.slime"),
            Count = 1,
            IntervalSeconds = 1,
            PathId = DefaultOptionValue(TowerDefensePathOptions(), "td.path.forest_main")
        };

        if (!TryPromptTowerDefenseSpawnGroup(T("common.add", "添加"), initial, out var spawnGroup))
        {
            return;
        }

        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseSpawnGroupRow(row, spawnGroup);
        row.Tag = spawnGroup;
        SelectGridRow(grid, rowIndex);
    }

    private void EditTowerDefenseSpawnGroupRow(DataGridView grid, int rowIndex)
    {
        if (!TryGetEditableGridRow(grid, rowIndex, out var row))
        {
            return;
        }

        var initial = row.Tag as TowerDefenseSpawnGroupDefinition
            ?? new TowerDefenseSpawnGroupDefinition
            {
                UnitId = ReadCellString(row, "unitId"),
                Count = ReadCellInt(row, "count"),
                IntervalSeconds = ReadCellDouble(row, "intervalSeconds"),
                DelaySeconds = ReadCellDouble(row, "delaySeconds"),
                PathId = ReadCellString(row, "pathId")
            };

        if (!TryPromptTowerDefenseSpawnGroup(T("common.edit", "编辑"), CloneTowerDefenseSpawnGroup(initial), out var updated))
        {
            return;
        }

        FillTowerDefenseSpawnGroupRow(row, updated);
        row.Tag = updated;
        SelectGridRow(grid, rowIndex);
    }

    private bool TryPromptTowerDefenseSpawnGroup(string title, TowerDefenseSpawnGroupDefinition initial, out TowerDefenseSpawnGroupDefinition spawnGroup)
    {
        var unitControl = CreateComboBox(UnitOptions(), initial.UnitId, readOnly: false);
        var countControl = CreateIntegerBox(initial.Count, 1, 999999);
        var intervalControl = CreateNumberBox(initial.IntervalSeconds, 0M, 999999M, 3);
        var delayControl = CreateNumberBox(initial.DelaySeconds, 0M, 999999M, 3);
        var pathControl = CreateComboBox(TowerDefensePathOptions(), initial.PathId, readOnly: false);
        var content = CreatePromptTable(5);
        AddPromptRow(content, 0, T("dataEditor.field.unit", "单位"), unitControl);
        AddPromptRow(content, 1, T("dataEditor.field.count", "数量"), countControl);
        AddPromptRow(content, 2, T("dataEditor.field.spawnInterval", "生成间隔"), intervalControl);
        AddPromptRow(content, 3, T("dataEditor.field.delaySeconds", "延迟秒数"), delayControl);
        AddPromptRow(content, 4, T("dataEditor.field.path", "路线"), pathControl);

        var selected = CloneTowerDefenseSpawnGroup(initial);
        if (!ShowPromptDialog(title, content, () =>
        {
            selected.UnitId = ReadOptionValue(unitControl);
            selected.Count = (int)countControl.Value;
            selected.IntervalSeconds = (double)intervalControl.Value;
            selected.DelaySeconds = (double)delayControl.Value;
            selected.PathId = ReadOptionValue(pathControl);
            return true;
        }))
        {
            spawnGroup = initial;
            return false;
        }

        spawnGroup = selected;
        return true;
    }

    private void AddTowerDefenseWaveRefRow(DataGridView grid, string waveId)
    {
        var rowIndex = grid.Rows.Add(waveId, NameOrNone(waveId, TowerDefenseWaveOptions()));
        grid.Rows[rowIndex].Tag = waveId;
    }

    private void AddTowerDefenseWaveRefRow(DataGridView grid)
    {
        grid.EndEdit();
        var existing = ReadGridColumnValues(grid, "waveId").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var options = TowerDefenseWaveOptions()
            .Where(option => !string.IsNullOrWhiteSpace(option.Value) && !existing.Contains(option.Value))
            .ToList();
        if (options.Count == 0)
        {
            MessageBox.Show(this, T("dataEditor.map.noAvailableKeys", "当前没有可添加的条目。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var initial = options[0].Value;
        if (!TryPromptTowerDefenseWaveRef(T("common.add", "添加"), initial, options, out var waveId))
        {
            return;
        }

        if (existing.Contains(waveId))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), waveId), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add(waveId, NameOrNone(waveId, TowerDefenseWaveOptions()));
        grid.Rows[rowIndex].Tag = waveId;
        SelectGridRow(grid, rowIndex);
    }

    private void EditTowerDefenseWaveRefRow(DataGridView grid, int rowIndex)
    {
        if (!TryGetEditableGridRow(grid, rowIndex, out var row))
        {
            return;
        }

        var initial = ReadCellString(row, "waveId");
        var existing = ReadGridColumnValues(grid, "waveId").ToHashSet(StringComparer.OrdinalIgnoreCase);
        existing.Remove(initial);
        var options = TowerDefenseWaveOptions()
            .Where(option => !string.IsNullOrWhiteSpace(option.Value) && !existing.Contains(option.Value))
            .ToList();
        if (!TryPromptTowerDefenseWaveRef(T("common.edit", "编辑"), initial, options, out var waveId))
        {
            return;
        }

        if (existing.Contains(waveId))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), waveId), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        row.Cells["waveId"].Value = waveId;
        row.Cells["waveName"].Value = NameOrNone(waveId, TowerDefenseWaveOptions());
        row.Tag = waveId;
        SelectGridRow(grid, rowIndex);
    }

    private bool TryPromptTowerDefenseWaveRef(string title, string initialWaveId, IReadOnlyList<OptionItem> options, out string waveId)
    {
        var waveControl = CreateComboBox(options.Count > 0 ? options : TowerDefenseWaveOptions(), initialWaveId, readOnly: false);
        var content = CreateLabeledPromptContent(T("dataEditor.field.wave", "波次"), waveControl);
        var selected = initialWaveId;
        if (!ShowPromptDialog(title, content, () =>
        {
            selected = ReadOptionValue(waveControl);
            return !string.IsNullOrWhiteSpace(selected);
        }))
        {
            waveId = initialWaveId;
            return false;
        }

        waveId = selected;
        return true;
    }

    private void AddTowerDefenseTowerLevelRow(DataGridView grid, TowerDefenseTowerLevelDefinition level)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseTowerLevelRow(row, level);
        row.Tag = CloneTowerDefenseTowerLevel(level);
    }

    private void FillTowerDefenseTowerLevelRow(DataGridViewRow row, TowerDefenseTowerLevelDefinition level)
    {
        row.Cells["level"].Value = level.Level.ToString(CultureInfo.CurrentCulture);
        row.Cells["upgradeCost"].Value = level.UpgradeCost.ToString(CultureInfo.CurrentCulture);
        row.Cells["rangeBonus"].Value = level.RangeBonus.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["damageMultiplier"].Value = level.DamageMultiplier.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["attackIntervalMultiplier"].Value = level.AttackIntervalMultiplier.ToString("0.###", CultureInfo.CurrentCulture);
        row.Cells["skillId"].Value = level.SkillId;
        row.Cells["skillName"].Value = NameOrNone(level.SkillId, SkillOptions());
    }

    private void AddTowerDefenseTowerLevelRow(DataGridView grid)
    {
        grid.EndEdit();
        var initial = new TowerDefenseTowerLevelDefinition
        {
            Level = GetNextTowerDefenseTowerLevel(grid),
            SkillId = DefaultOptionValue(SkillOptions(), "skill.td.arrowShot")
        };

        if (!TryPromptTowerDefenseTowerLevel(T("common.add", "添加"), initial, out var level))
        {
            return;
        }

        var existingLevels = ReadGridColumnValues(grid, "level").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (existingLevels.Contains(level.Level.ToString(CultureInfo.InvariantCulture)))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), level.Level), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillTowerDefenseTowerLevelRow(row, level);
        row.Tag = level;
        SelectGridRow(grid, rowIndex);
    }

    private static int GetNextTowerDefenseTowerLevel(DataGridView grid)
    {
        var maxLevel = 0;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (!row.IsNewRow)
            {
                maxLevel = Math.Max(maxLevel, ReadCellInt(row, "level"));
            }
        }

        return maxLevel + 1;
    }

    private void EditTowerDefenseTowerLevelRow(DataGridView grid, int rowIndex)
    {
        if (!TryGetEditableGridRow(grid, rowIndex, out var row))
        {
            return;
        }

        var initial = row.Tag as TowerDefenseTowerLevelDefinition
            ?? new TowerDefenseTowerLevelDefinition
            {
                Level = ReadCellInt(row, "level"),
                UpgradeCost = ReadCellInt(row, "upgradeCost"),
                RangeBonus = ReadCellDouble(row, "rangeBonus"),
                DamageMultiplier = ReadCellDouble(row, "damageMultiplier"),
                AttackIntervalMultiplier = ReadCellDouble(row, "attackIntervalMultiplier"),
                SkillId = ReadCellString(row, "skillId")
            };

        if (!TryPromptTowerDefenseTowerLevel(T("common.edit", "编辑"), CloneTowerDefenseTowerLevel(initial), out var updated))
        {
            return;
        }

        var existingLevels = ReadGridColumnValues(grid, "level").ToHashSet(StringComparer.OrdinalIgnoreCase);
        existingLevels.Remove(initial.Level.ToString(CultureInfo.InvariantCulture));
        existingLevels.Remove(initial.Level.ToString(CultureInfo.CurrentCulture));
        if (existingLevels.Contains(updated.Level.ToString(CultureInfo.InvariantCulture))
            || existingLevels.Contains(updated.Level.ToString(CultureInfo.CurrentCulture)))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), updated.Level), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        FillTowerDefenseTowerLevelRow(row, updated);
        row.Tag = updated;
        SelectGridRow(grid, rowIndex);
    }

    private bool TryPromptTowerDefenseTowerLevel(string title, TowerDefenseTowerLevelDefinition initial, out TowerDefenseTowerLevelDefinition level)
    {
        var levelControl = CreateIntegerBox(initial.Level, 1, 999);
        var upgradeCostControl = CreateIntegerBox(initial.UpgradeCost, 0, 9999999);
        var rangeBonusControl = CreateNumberBox(initial.RangeBonus, -999999M, 999999M, 3);
        var damageMultiplierControl = CreateNumberBox(initial.DamageMultiplier, 0M, 999999M, 3);
        var attackIntervalMultiplierControl = CreateNumberBox(initial.AttackIntervalMultiplier, 0M, 999999M, 3);
        var skillControl = CreateComboBox(SkillOptions(), initial.SkillId, readOnly: false);
        var content = CreatePromptTable(6);
        AddPromptRow(content, 0, T("dataEditor.field.level", "等级"), levelControl);
        AddPromptRow(content, 1, T("dataEditor.field.upgradeCost", "升级费用"), upgradeCostControl);
        AddPromptRow(content, 2, T("dataEditor.field.rangeBonus", "射程加成"), rangeBonusControl);
        AddPromptRow(content, 3, T("dataEditor.field.damageMultiplier", "伤害倍率"), damageMultiplierControl);
        AddPromptRow(content, 4, T("dataEditor.field.attackIntervalMultiplier", "攻击间隔倍率"), attackIntervalMultiplierControl);
        AddPromptRow(content, 5, T("dataEditor.field.skill", "技能"), skillControl);

        var selected = CloneTowerDefenseTowerLevel(initial);
        if (!ShowPromptDialog(title, content, () =>
        {
            selected.Level = (int)levelControl.Value;
            selected.UpgradeCost = (int)upgradeCostControl.Value;
            selected.RangeBonus = (double)rangeBonusControl.Value;
            selected.DamageMultiplier = (double)damageMultiplierControl.Value;
            selected.AttackIntervalMultiplier = (double)attackIntervalMultiplierControl.Value;
            selected.SkillId = ReadOptionValue(skillControl);
            return true;
        }))
        {
            level = initial;
            return false;
        }

        level = selected;
        return true;
    }

    private void AddEffectParameterDefinitionRow(DataGridView grid, EffectParameterDefinition parameter)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillEffectParameterDefinitionRow(row, parameter);
        row.Tag = CloneEffectParameterDefinition(parameter);
    }

    private void FillEffectParameterDefinitionRow(DataGridViewRow row, EffectParameterDefinition parameter)
    {
        row.Cells["key"].Value = parameter.Key;
        row.Cells["displayName"].Value = T(parameter.DisplayNameKey, string.IsNullOrWhiteSpace(parameter.DisplayName) ? parameter.Key : parameter.DisplayName);
        row.Cells["valueType"].Value = EffectParameterValueTypeLabel(parameter.ValueType);
        row.Cells["defaultValue"].Value = FormatEffectParameterDefinitionDefaultValue(parameter);
        row.Cells["optionSource"].Value = FormatEffectParameterOptionSource(parameter);
    }

    private void AddEffectParameterDefinitionRow(DataGridView grid)
    {
        grid.EndEdit();

        var initial = new EffectParameterDefinition
        {
            Key = UniqueSimpleKey("param", GetGridKeys(grid)),
            ValueType = EffectParameterValueType.Text,
            Order = GetNextEffectParameterOrder(grid)
        };

        if (!TryPromptEffectParameterDefinition(T("common.add", "添加"), initial, out var parameter))
        {
            return;
        }

        var existingKeys = GetGridKeys(grid);
        if (existingKeys.Contains(parameter.Key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), parameter.Key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillEffectParameterDefinitionRow(row, parameter);
        row.Tag = parameter;
        SelectGridRow(grid, rowIndex);
    }

    private void EditEffectParameterDefinitionRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        var row = grid.Rows[rowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        grid.EndEdit();
        var initial = row.Tag as EffectParameterDefinition
            ?? new EffectParameterDefinition { Key = ReadCellString(row, "key") };
        if (!TryPromptEffectParameterDefinition(T("common.edit", "编辑"), CloneEffectParameterDefinition(initial), out var updated))
        {
            return;
        }

        var existingKeys = GetGridKeys(grid);
        existingKeys.Remove(initial.Key);
        if (existingKeys.Contains(updated.Key))
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("itemField.error.duplicateKey", "键“{0}”已存在。"), updated.Key), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        FillEffectParameterDefinitionRow(row, updated);
        row.Tag = updated;
        SelectGridRow(grid, rowIndex);
    }

    private bool TryPromptEffectParameterDefinition(string title, EffectParameterDefinition initial, out EffectParameterDefinition parameter)
    {
        var keyControl = CreateTextBox(initial.Key, multiline: false, readOnly: false);
        var displayNameControl = CreateTextBox(initial.DisplayName, multiline: false, readOnly: false);
        var displayNameKeyControl = CreateTextBox(initial.DisplayNameKey, multiline: false, readOnly: false);
        var valueTypeOptions = Enum.GetValues<EffectParameterValueType>()
            .Select(valueType => new OptionItem(valueType.ToString(), T($"dataEditor.effectParameterValueType.{valueType}", valueType.ToString())))
            .ToList();
        var valueTypeControl = CreateComboBox(valueTypeOptions, initial.ValueType.ToString(), readOnly: false, allowUnknownValue: false);
        var optionSourceControl = CreateComboBox(EffectParameterOptionSourceOptions(), initial.OptionSourceId, readOnly: false, allowUnknownValue: false);
        var categoryControl = CreateComboBox(EffectParameterCategoryOptions(), string.IsNullOrWhiteSpace(initial.CategoryKey) ? "effect.parameter.category.core" : initial.CategoryKey, readOnly: false, allowUnknownValue: false);
        var orderControl = CreateIntegerBox(initial.Order, -999999, 999999);
        var optionsControl = CreateTextBox(string.Join(",", initial.Options), multiline: false, readOnly: false);
        var requiredControl = new CheckBox { Checked = initial.Required, AutoSize = true };
        var readOnlyControl = new CheckBox { Checked = initial.ReadOnly, AutoSize = true };
        var defaultValueHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        Control? defaultValueEditor = null;
        TableLayoutPanel? content = null;

        keyControl.Dock = DockStyle.Fill;
        displayNameControl.Dock = DockStyle.Fill;
        displayNameKeyControl.Dock = DockStyle.Fill;
        valueTypeControl.Dock = DockStyle.Fill;
        optionSourceControl.Dock = DockStyle.Fill;
        orderControl.Dock = DockStyle.Fill;
        optionsControl.Dock = DockStyle.Fill;

        IReadOnlyList<OptionItem> ResolveCustomOptions()
        {
            return SplitValues(optionsControl.Text)
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select(option => new OptionItem(option, T($"effect.parameter.option.{option}", option)))
                .ToList();
        }

        EffectParameterDefinition BuildEditingParameter()
        {
            var working = CloneEffectParameterDefinition(initial);
            working.DisplayName = displayNameControl.Text.Trim();
            working.DisplayNameKey = displayNameKeyControl.Text.Trim();
            working.ValueType = Enum.TryParse<EffectParameterValueType>(ReadOptionValue(valueTypeControl), ignoreCase: true, out var valueType)
                ? valueType
                : EffectParameterValueType.Text;
            working.OptionSourceId = ReadOptionValue(optionSourceControl);
            working.Options = SplitValues(optionsControl.Text);
            working.ReadOnly = readOnlyControl.Checked;
            return working;
        }

        string ReadDefaultValueEditor()
        {
            return defaultValueEditor switch
            {
                null => string.Empty,
                NumericUpDown numeric => numeric.Value.ToString(CultureInfo.InvariantCulture),
                CheckBox checkBox => checkBox.Checked ? "true" : "false",
                ComboBox comboBox when comboBox.SelectedItem is OptionItem item => item.Value,
                ComboBox comboBox => comboBox.Text.Trim(),
                TextBox textBox => textBox.Text.Trim(),
                _ => string.Join(",", ReadMultiChoiceValues(defaultValueEditor))
            };
        }

        int GetDefaultValueRowHeight(EffectParameterDefinition working)
        {
            return working.ValueType switch
            {
                EffectParameterValueType.TagList => 126,
                _ => PromptRowHeight
            };
        }

        bool UsesOptionSource(EffectParameterDefinition working)
        {
            return working.ValueType is EffectParameterValueType.Choice or EffectParameterValueType.AssetRef or EffectParameterValueType.TagList;
        }

        bool UsesCustomOptions(EffectParameterDefinition working)
        {
            return working.ValueType == EffectParameterValueType.Choice
                && string.IsNullOrWhiteSpace(working.OptionSourceId);
        }

        bool UsesDefaultValue(EffectParameterDefinition working)
        {
            return true;
        }

        int CalculateParameterDialogHeight()
        {
            if (content is null)
            {
                return 404;
            }

            var rowsHeight = 0;
            for (var i = 0; i < content.RowStyles.Count; i++)
            {
                rowsHeight += (int)Math.Max(0, content.RowStyles[i].Height);
            }

            return Math.Max(404, (PromptDialogPadding * 2) + PromptDialogButtonHeight + rowsHeight + PromptDialogSafetyPadding);
        }

        void SetRowVisible(int rowIndex, bool visible)
        {
            if (content is null || rowIndex < 0 || rowIndex >= content.RowStyles.Count)
            {
                return;
            }

            foreach (Control child in content.Controls)
            {
                if (content.GetRow(child) == rowIndex)
                {
                    child.Visible = visible;
                }
            }

            content.RowStyles[rowIndex].Height = visible ? PromptRowHeight : 0;
        }

        void RefreshSemanticRows(EffectParameterDefinition working)
        {
            if (content is null)
            {
                return;
            }

            var showOptionSource = UsesOptionSource(working);
            var showOptions = UsesCustomOptions(working);
            var showDefaultValue = UsesDefaultValue(working);

            SetRowVisible(4, showOptionSource);
            SetRowVisible(5, showDefaultValue);
            SetRowVisible(8, showOptions);

            if (showOptionSource)
            {
                content.RowStyles[4].Height = PromptRowHeight;
            }

            if (showDefaultValue)
            {
                content.RowStyles[5].Height = GetDefaultValueRowHeight(working);
            }

            if (showOptions)
            {
                content.RowStyles[8].Height = PromptRowHeight;
            }

            if (!showOptions)
            {
                optionsControl.Text = string.Empty;
            }

            if (showOptionSource && !showOptions && working.ValueType == EffectParameterValueType.TagList && !string.Equals(working.OptionSourceId, "tag", StringComparison.OrdinalIgnoreCase))
            {
                var tagOption = EffectParameterOptionSourceOptions().FirstOrDefault(option => string.Equals(option.Value, "tag", StringComparison.OrdinalIgnoreCase));
                if (tagOption is not null)
                {
                    optionSourceControl.SelectedItem = tagOption;
                }
            }
        }

        void RebuildDefaultValueEditor(string preferredValue)
        {
            var working = BuildEditingParameter();
            var previousEditor = defaultValueEditor;
            RefreshSemanticRows(working);
            working.DefaultValue = NormalizeDraftDefaultValueForEditor(working, preferredValue, previousEditor);

            defaultValueHost.SuspendLayout();
            defaultValueHost.Controls.Clear();
            if (content.RowStyles.Count > 5)
            {
                content.RowStyles[5].Height = GetDefaultValueRowHeight(working);
            }
            defaultValueEditor = CreateEffectParameterControl(working, working.DefaultValue);
            defaultValueEditor.Margin = Padding.Empty;
            defaultValueEditor.Dock = DockStyle.Fill;
            defaultValueHost.Controls.Add(defaultValueEditor);
            defaultValueHost.ResumeLayout(true);
        }

        content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddPromptRow(content, 0, T("dataEditor.field.key", "Key"), keyControl);
        AddPromptRow(content, 1, T("dataEditor.field.displayName", "显示名称"), displayNameControl);
        AddPromptRow(content, 2, T("dataEditor.field.displayNameKey", "显示名称 Key"), displayNameKeyControl);
        AddPromptRow(content, 3, T("dataEditor.field.valueType", "值类型"), valueTypeControl);
        AddPromptRow(content, 4, T("dataEditor.field.optionSource", "选项来源"), optionSourceControl);
        AddPromptRow(content, 5, T("dataEditor.field.defaultValue", "默认值"), defaultValueHost);
        AddPromptRow(content, 6, T("dataEditor.field.category", "分类"), categoryControl);
        AddPromptRow(content, 7, T("dataEditor.field.order", "排序"), orderControl);
        AddPromptRow(content, 8, T("dataEditor.field.options", "选项"), optionsControl);
        AddPromptRow(content, 9, T("dataEditor.field.flags", "标记"), CreateInlineCheckEditor(requiredControl, T("dataEditor.field.required", "必填"), readOnlyControl, T("dataEditor.field.readOnly", "只读")));
        RebuildDefaultValueEditor(initial.DefaultValue);

        void RefreshDefaultEditorFromCurrent()
        {
            var currentValue = ReadDefaultValueEditor();
            RebuildDefaultValueEditor(currentValue);
        }

        valueTypeControl.SelectedIndexChanged += (_, _) => RefreshDefaultEditorFromCurrent();
        optionSourceControl.SelectedIndexChanged += (_, _) => RefreshDefaultEditorFromCurrent();
        optionsControl.TextChanged += (_, _) =>
        {
            var usesCustomOptions = BuildEditingParameter().Options.Count > 0;
            if (usesCustomOptions)
            {
                RefreshDefaultEditorFromCurrent();
            }
        };

        var selected = CloneEffectParameterDefinition(initial);
        using var dialog = new DataEditorPromptDialog
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            AutoScaleMode = AutoScaleMode.None,
            ClientSize = new Size(560, CalculateParameterDialogHeight())
        };
        dialog.SetContent(content);
        dialog.ConfirmButton.Text = T("common.ok", "确定");
        dialog.CancelActionButton.Text = T("common.cancel", "取消");
        dialog.ConfirmButton.Click += (_, _) =>
        {
            var key = keyControl.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show(dialog, T("itemField.error.emptyKey", "键不能为空。"), dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.Key = key;
            selected.DisplayName = displayNameControl.Text.Trim();
            selected.DisplayNameKey = displayNameKeyControl.Text.Trim();
            selected.ValueType = Enum.TryParse<EffectParameterValueType>(ReadOptionValue(valueTypeControl), ignoreCase: true, out var valueType)
                ? valueType
                : EffectParameterValueType.Text;
            selected.DefaultValue = NormalizeEffectParameterDefaultValue(BuildEditingParameter(), ReadDefaultValueEditor());
            selected.OptionSourceId = ReadOptionValue(optionSourceControl);
            selected.CategoryKey = ReadOptionValue(categoryControl);
            selected.Category = T(selected.CategoryKey, selected.CategoryKey);
            selected.Order = (int)orderControl.Value;
            selected.Options = ResolveCustomOptions().Select(option => option.Value).ToList();
            selected.Required = requiredControl.Checked;
            selected.ReadOnly = readOnlyControl.Checked;
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            parameter = initial;
            return false;
        }

        parameter = selected;
        return true;
    }

    private Control CreateInlineCheckEditor(CheckBox leftCheckBox, string leftText, CheckBox rightCheckBox, string rightText)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var checks = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.None,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        leftCheckBox.Text = leftText;
        rightCheckBox.Text = rightText;
        leftCheckBox.Margin = new Padding(0, 0, 12, 0);
        rightCheckBox.Margin = Padding.Empty;
        checks.Controls.Add(leftCheckBox);
        checks.Controls.Add(rightCheckBox);
        host.Controls.Add(checks);
        host.Layout += (_, _) =>
        {
            checks.Location = new Point(0, Math.Max(0, (host.ClientSize.Height - checks.PreferredSize.Height) / 2));
        };
        return host;
    }

    private IReadOnlyList<OptionItem> EffectParameterOptionSourceOptions()
    {
        return
        [
            new OptionItem(string.Empty, T("dataEditor.option.none", "无")),
            new OptionItem("tag", T("dataEditor.effectParameterOptionSource.tag", "标签")),
            new OptionItem("stat", T("dataEditor.effectParameterOptionSource.stat", "属性")),
            new OptionItem("formula", T("dataEditor.effectParameterOptionSource.formula", "公式")),
            new OptionItem("status", T("dataEditor.effectParameterOptionSource.status", "状态")),
            new OptionItem("skill", T("dataEditor.effectParameterOptionSource.skill", "技能")),
            new OptionItem("projectile", T("dataEditor.effectParameterOptionSource.projectile", "投射物")),
            new OptionItem("element", T("dataEditor.effectParameterOptionSource.element", "元素")),
            new OptionItem("damageType", T("dataEditor.effectParameterOptionSource.damageType", "伤害类型"))
        ];
    }

    private string FormatEffectParameterDefinitionDefaultValue(EffectParameterDefinition parameter)
    {
        return string.IsNullOrWhiteSpace(parameter.DefaultValue)
            ? T("dataEditor.option.none", "无")
            : FormatEffectParameterValue(parameter, parameter.DefaultValue);
    }

    private string EffectParameterValueTypeLabel(EffectParameterValueType valueType)
    {
        return T($"dataEditor.effectParameterValueType.{valueType}", valueType.ToString());
    }

    private string FormatEffectParameterOptionSource(EffectParameterDefinition parameter)
    {
        if (parameter.Options.Count > 0)
        {
            return T("dataEditor.effectParameterOptionSource.custom", "自定义选项");
        }

        return NameOrNone(parameter.OptionSourceId, EffectParameterOptionSourceOptions());
    }

    private string EffectParameterCategoryLabel(string categoryKey)
    {
        return string.IsNullOrWhiteSpace(categoryKey)
            ? T("dataEditor.option.none", "无")
            : T(categoryKey, categoryKey);
    }

    private IReadOnlyList<OptionItem> EffectParameterCategoryOptions()
    {
        return
        [
            new OptionItem("effect.parameter.category.core", T("effect.parameter.category.core", "核心参数")),
            new OptionItem("effect.parameter.category.target", T("effect.parameter.category.target", "目标参数")),
            new OptionItem("effect.parameter.category.filter", T("effect.parameter.category.filter", "筛选参数")),
            new OptionItem("effect.parameter.category.timing", T("effect.parameter.category.timing", "时间参数")),
            new OptionItem("effect.parameter.category.presentation", T("effect.parameter.category.presentation", "表现参数"))
        ];
    }

    private string NormalizeEffectParameterDefaultValue(EffectParameterDefinition parameter, string rawValue)
    {
        var validOptions = EffectParameterOptions(parameter);
        if (parameter.ValueType is not (EffectParameterValueType.Choice or EffectParameterValueType.AssetRef))
        {
            return rawValue.Trim();
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return validOptions.Any(option => string.Equals(option.Value, rawValue, StringComparison.OrdinalIgnoreCase))
            ? rawValue.Trim()
            : string.Empty;
    }

    private static int GetNextEffectParameterOrder(DataGridView grid)
    {
        var maxOrder = -1;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow || row.Tag is not EffectParameterDefinition parameter)
            {
                continue;
            }

            maxOrder = Math.Max(maxOrder, parameter.Order);
        }

        return maxOrder + 10;
    }

    private static List<EffectParameterDefinition> ReadEffectParameterDefinitionGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到效果参数模板表格控件。");
        var parameters = new List<EffectParameterDefinition>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            if (row.Tag is EffectParameterDefinition parameter)
            {
                parameters.Add(CloneEffectParameterDefinition(parameter));
            }
        }

        return parameters
            .OrderBy(parameter => parameter.Order)
            .ThenBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void AddGameplayEffectReferenceRow(DataGridView grid, GameplayEffectReference reference)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillGameplayEffectReferenceRow(row, reference);
        row.Tag = CloneGameplayEffectReference(reference);
    }

    private void FillGameplayEffectReferenceRow(DataGridViewRow row, GameplayEffectReference reference)
    {
        row.Cells["effectId"].Value = reference.EffectId;
        row.Cells["effectName"].Value = NameOrNone(reference.EffectId, GameplayEffectOptions());

        var effect = FindGameplayEffect(reference.EffectId);
        row.Cells["effectKind"].Value = effect is null
            ? T("dataEditor.option.none", "无")
            : ChoiceLabel("effectKind", effect.EffectKind);
        row.Cells["parameters"].Value = FormatGameplayEffectReferenceParameters(reference, effect);
    }

    private void AddGameplayEffectReferenceRow(DataGridView grid)
    {
        grid.EndEdit();

        if (!TryPromptGameplayEffectReference(
                T("common.add", "添加"),
                new GameplayEffectReference(),
                out var reference))
        {
            return;
        }

        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        FillGameplayEffectReferenceRow(row, reference);
        row.Tag = reference;
        SelectGridRow(grid, rowIndex);
    }

    private void EditGameplayEffectReferenceRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        var row = grid.Rows[rowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        grid.EndEdit();
        var initial = row.Tag as GameplayEffectReference
            ?? new GameplayEffectReference { EffectId = ReadCellString(row, "effectId") };
        if (!TryPromptGameplayEffectReference(
                T("common.edit", "编辑"),
                CloneGameplayEffectReference(initial),
                out var updated))
        {
            return;
        }

        FillGameplayEffectReferenceRow(row, updated);
        row.Tag = updated;
    }

    private bool TryPromptGameplayEffectReference(string title, GameplayEffectReference initial, out GameplayEffectReference reference)
    {
        var effectControl = CreateComboBox(GameplayEffectOptions(), initial.EffectId, readOnly: false);
        effectControl.Dock = DockStyle.Fill;
        var parameterHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var selected = CloneGameplayEffectReference(initial);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, PromptRowHeight));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddPromptRow(content, 0, T("dataEditor.field.effect", "效果"), effectControl);
        parameterHost.MinimumSize = new Size(0, 220);
        AddPromptRow(content, 1, T("dataEditor.field.parameters", "参数列表"), parameterHost);

        void RebuildParameterEditor(string effectId, Dictionary<string, string>? currentValues = null)
        {
            parameterHost.SuspendLayout();
            parameterHost.Controls.Clear();
            var effect = FindGameplayEffect(effectId);
            var values = currentValues is null
                ? BuildEffectParameterMap(initial.Parameters)
                : new Dictionary<string, string>(currentValues, StringComparer.OrdinalIgnoreCase);
            var editor = CreateGameplayEffectParameterEditor(effect, values);
            editor.Dock = DockStyle.Fill;
            parameterHost.Controls.Add(editor);
            parameterHost.Tag = editor;
            parameterHost.ResumeLayout(true);
        }

        RebuildParameterEditor(initial.EffectId);

        effectControl.SelectedIndexChanged += (_, _) =>
        {
            var values = ReadGameplayEffectParameterMap(parameterHost.Tag as Control);
            RebuildParameterEditor(ReadOptionValue(effectControl), values);
        };

        using var dialog = new DataEditorPromptDialog
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            AutoScaleMode = AutoScaleMode.None,
            ClientSize = new Size(560, GetGameplayEffectReferenceDialogHeight(parameterHost.Tag as Control))
        };
        dialog.SetContent(content);
        dialog.ConfirmButton.Text = T("common.ok", "确定");
        dialog.CancelActionButton.Text = T("common.cancel", "取消");

        void ResizeEffectReferenceDialog()
        {
            var editorHeight = GetGameplayEffectParameterEditorPreferredHeight(parameterHost.Tag as Control);
            parameterHost.MinimumSize = new Size(0, editorHeight);
            content.RowStyles[1].SizeType = SizeType.Absolute;
            content.RowStyles[1].Height = editorHeight;
            dialog.ClientSize = new Size(560, GetGameplayEffectReferenceDialogHeight(parameterHost.Tag as Control));
        }

        ResizeEffectReferenceDialog();

        effectControl.SelectedIndexChanged += (_, _) => ResizeEffectReferenceDialog();
        dialog.ConfirmButton.Click += (_, _) =>
        {
            var effectId = ReadOptionValue(effectControl);
            if (string.IsNullOrWhiteSpace(effectId))
            {
                MessageBox.Show(dialog, T("dataEditor.effect.error.emptyEffect", "效果不能为空。"), dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.EffectId = effectId;
            selected.Parameters = BuildEffectParameterValues(ReadGameplayEffectParameterMap(parameterHost.Tag as Control));
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            reference = initial;
            return false;
        }

        reference = selected;
        return true;
    }

    private static int GetGameplayEffectReferenceDialogHeight(Control? editor)
    {
        return Math.Clamp(
            (PromptDialogPadding * 2) + PromptDialogButtonHeight + PromptRowHeight + GetGameplayEffectParameterEditorPreferredHeight(editor) + PromptDialogSafetyPadding,
            260,
            720);
    }

    private static int GetGameplayEffectParameterEditorPreferredHeight(Control? editor)
    {
        if (editor is null)
        {
            return 180;
        }

        if (editor.Tag is int taggedHeight)
        {
            return Math.Clamp(taggedHeight, 120, 560);
        }

        return Math.Clamp(editor.PreferredSize.Height, 120, 560);
    }

    private Control CreateGameplayEffectParameterEditor(GameplayEffectDefinition? effect, Dictionary<string, string> values)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            Padding = new Padding(0),
            Margin = Padding.Empty
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        if (effect is null || effect.Parameters.Count == 0)
        {
            panel.RowCount = 1;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.Tag = 40;
            panel.Controls.Add(new Label
            {
                Text = T("dataEditor.effect.noParameters", "该效果没有参数。"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            }, 1, 0);
            return panel;
        }

        var orderedParameters = effect.Parameters.OrderBy(parameter => parameter.Order).ThenBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase).ToList();
        panel.RowCount = orderedParameters.Count;
        var preferredHeight = 0;
        for (var row = 0; row < orderedParameters.Count; row++)
        {
            var parameter = orderedParameters[row];
            var rowHeight = GetEffectParameterEditorRowHeight(parameter);
            preferredHeight += rowHeight;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            var label = new Label
            {
                Text = T(parameter.DisplayNameKey, string.IsNullOrWhiteSpace(parameter.DisplayName) ? parameter.Key : parameter.DisplayName),
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var currentValue = values.TryGetValue(parameter.Key, out var existingValue)
                ? existingValue
                : parameter.DefaultValue;
            var editor = CreateEffectParameterControl(parameter, currentValue);
            editor.Margin = Padding.Empty;
            editor.Dock = DockStyle.Fill;
            editor.Tag = parameter.Key;
            panel.Controls.Add(label, 0, row);
            panel.Controls.Add(editor, 1, row);
        }

        panel.Tag = Math.Max(preferredHeight + 8, panel.PreferredSize.Height + 8);
        return panel;
    }

    private int GetEffectParameterEditorRowHeight(EffectParameterDefinition parameter)
    {
        if (parameter.ValueType != EffectParameterValueType.TagList)
        {
            return 34;
        }

        var editor = CreateCheckedList(EffectParameterOptions(parameter), [], readOnly: true);
        return Math.Max(34, editor.PreferredSize.Height + 8);
    }

    private Control CreateEffectParameterControl(EffectParameterDefinition parameter, string value)
    {
        Control editor = parameter.ValueType switch
        {
            EffectParameterValueType.Number => CreateNumberBox((double)ParseDecimal(value), -999999M, 999999M, 3),
            EffectParameterValueType.Integer => CreateIntegerBox(ParseInteger(value), -999999, 999999),
            EffectParameterValueType.Boolean => new CheckBox
            {
                Text = bool.TryParse(value, out var boolValue) && boolValue
                    ? T("common.yes", "是")
                    : T("common.no", "否"),
                Checked = bool.TryParse(value, out var checkedValue) && checkedValue,
                AutoSize = true,
                Enabled = !parameter.ReadOnly,
                Dock = DockStyle.Left
            },
            EffectParameterValueType.Choice => CreateComboBox(EffectParameterOptions(parameter), value, parameter.ReadOnly),
            EffectParameterValueType.TagList => CreateCheckedList(EffectParameterOptions(parameter), SplitValues(value), parameter.ReadOnly),
            EffectParameterValueType.AssetRef => CreateComboBox(EffectParameterOptions(parameter), value, parameter.ReadOnly),
            _ => CreateTextBox(value, multiline: false, parameter.ReadOnly)
        };

        if (parameter.ValueType == EffectParameterValueType.Boolean && editor is CheckBox booleanEditor)
        {
            return AttachBooleanLabelBehavior(booleanEditor);
        }

        return editor;
    }

    private CheckBox AttachBooleanLabelBehavior(CheckBox checkBox)
    {
        void RefreshText()
        {
            checkBox.Text = checkBox.Checked
                ? T("common.yes", "是")
                : T("common.no", "否");
        }

        RefreshText();
        checkBox.CheckedChanged += (_, _) => RefreshText();
        return checkBox;
    }

    private string NormalizeDraftDefaultValueForEditor(EffectParameterDefinition parameter, string rawValue, Control? previousEditor)
    {
        rawValue = rawValue?.Trim() ?? string.Empty;

        return parameter.ValueType switch
        {
            EffectParameterValueType.Text => previousEditor is TextBox ? rawValue : string.Empty,
            EffectParameterValueType.Number => decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentNumber)
                || decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out currentNumber)
                    ? currentNumber.ToString(CultureInfo.InvariantCulture)
                    : "0",
            EffectParameterValueType.Integer => int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var currentInteger)
                || int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentInteger)
                    ? currentInteger.ToString(CultureInfo.InvariantCulture)
                    : "0",
            EffectParameterValueType.Boolean => bool.TryParse(rawValue, out var currentBoolean) && currentBoolean ? "true" : "false",
            EffectParameterValueType.TagList => string.Join(",",
                SplitValues(rawValue)
                    .Where(value => EffectParameterOptions(parameter).Any(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)),
            EffectParameterValueType.Choice or EffectParameterValueType.AssetRef => EffectParameterOptions(parameter)
                .Any(option => string.Equals(option.Value, rawValue, StringComparison.OrdinalIgnoreCase))
                    ? rawValue
                    : string.Empty,
            _ => rawValue
        };
    }

    private IReadOnlyList<OptionItem> EffectParameterOptions(EffectParameterDefinition parameter)
    {
        if (parameter.Options.Count > 0)
        {
            return parameter.Options.Select(option => new OptionItem(option, T($"effect.parameter.option.{option}", option))).ToList();
        }

        return parameter.OptionSourceId switch
        {
            "tag" => TargetTagOptions(),
            "stat" => StatReferenceOptions(),
            "formula" => FormulaOptions(),
            "status" => StatusOptions(),
            "skill" => SkillOptions(),
            "projectile" => ProjectileOptions(),
            "element" => ElementOptions(),
            "damageType" => DamageTypeOptions(),
            _ => EmptyOption()
        };
    }

    private Dictionary<string, string> ReadGameplayEffectParameterMap(Control? control)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (control is not TableLayoutPanel panel)
        {
            return values;
        }

        foreach (Control child in panel.Controls)
        {
            if (child.Tag is not string key || string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            values[key] = child switch
            {
                NumericUpDown numeric => numeric.Value.ToString(CultureInfo.InvariantCulture),
                CheckBox checkBox => checkBox.Checked ? "true" : "false",
                ComboBox comboBox when comboBox.SelectedItem is OptionItem item => item.Value,
                ComboBox comboBox => comboBox.Text,
                TextBox textBox => textBox.Text,
                _ => string.Join(",", ReadMultiChoiceValues(child))
            };
        }

        return values;
    }

    private static Dictionary<string, string> BuildEffectParameterMap(IEnumerable<EffectParameterValue> parameters)
    {
        return parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key))
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static List<EffectParameterValue> BuildEffectParameterValues(Dictionary<string, string> values)
    {
        return values
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => new EffectParameterValue { Key = entry.Key, Value = entry.Value })
            .ToList();
    }

    private GameplayEffectDefinition? FindGameplayEffect(string effectId)
    {
        return _context.Project.AssetLibrary.GameplayEffects.FirstOrDefault(effect =>
            string.Equals(effect.Id, effectId, StringComparison.OrdinalIgnoreCase));
    }

    private string FormatGameplayEffectReferenceParameters(GameplayEffectReference reference, GameplayEffectDefinition? effect)
    {
        if (effect is null)
        {
            return reference.Parameters.Count == 0
                ? T("dataEditor.option.none", "无")
                : string.Format(
                    CultureInfo.CurrentCulture,
                    T("dataEditor.summary.parameterCount", "已配置 {0} 项参数"),
                    reference.Parameters.Count);
        }

        var template = effect.Parameters.ToDictionary(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase);
        var parts = new List<string>();
        foreach (var parameter in reference.Parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Key) || string.IsNullOrWhiteSpace(parameter.Value))
            {
                continue;
            }

            var name = template.TryGetValue(parameter.Key, out var definition)
                ? T(definition.DisplayNameKey, string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Key : definition.DisplayName)
                : parameter.Key;
            var value = template.TryGetValue(parameter.Key, out var optionDefinition)
                ? FormatEffectParameterValue(optionDefinition, parameter.Value)
                : parameter.Value;
            parts.Add($"{name}={value}");
        }

        return parts.Count == 0 ? T("dataEditor.option.none", "无") : string.Join(" | ", parts);
    }

    private string FormatEffectParameterValue(EffectParameterDefinition parameter, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return T("dataEditor.option.none", "无");
        }

        if (parameter.ValueType == EffectParameterValueType.TagList)
        {
            return FormatLocalizedValues(SplitValues(value), EffectParameterOptions(parameter));
        }

        if (parameter.ValueType is EffectParameterValueType.Choice or EffectParameterValueType.AssetRef)
        {
            return NameOrNone(value, EffectParameterOptions(parameter));
        }

        if (parameter.ValueType == EffectParameterValueType.Boolean && bool.TryParse(value, out var boolValue))
        {
            return boolValue ? T("common.yes", "是") : T("common.no", "否");
        }

        return value;
    }

    private static GameplayEffectReference CloneGameplayEffectReference(GameplayEffectReference reference)
    {
        return new GameplayEffectReference
        {
            EffectId = reference.EffectId,
            Parameters = reference.Parameters
                .Select(parameter => new EffectParameterValue
                {
                    Key = parameter.Key,
                    Value = parameter.Value
                })
                .ToList()
        };
    }

    private static EffectParameterDefinition CloneEffectParameterDefinition(EffectParameterDefinition parameter)
    {
        return new EffectParameterDefinition
        {
            Key = parameter.Key,
            DisplayName = parameter.DisplayName,
            DisplayNameKey = parameter.DisplayNameKey,
            ValueType = parameter.ValueType,
            DefaultValue = parameter.DefaultValue,
            Required = parameter.Required,
            ReadOnly = parameter.ReadOnly,
            Category = parameter.Category,
            CategoryKey = parameter.CategoryKey,
            Order = parameter.Order,
            Options = [.. parameter.Options],
            OptionSourceId = parameter.OptionSourceId
        };
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)
                ? parsed
                : 0M;
    }

    private static int ParseInteger(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed)
                ? parsed
                : 0;
    }

    private void DeleteSelectedGridRow(DataGridView grid)
    {
        grid.EndEdit();

        var rowIndex = GetSelectedGridRowIndex(grid);
        if (rowIndex is null)
        {
            return;
        }

        grid.Rows.RemoveAt(rowIndex.Value);
        if (grid.Rows.Count == 0)
        {
            return;
        }

        SelectGridRow(grid, Math.Min(rowIndex.Value, grid.Rows.Count - 1));
    }

    private static HashSet<string> GetGridKeys(DataGridView grid)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var key = ReadCellString(row, "key");
            if (!string.IsNullOrWhiteSpace(key))
            {
                result.Add(key);
            }
        }

        return result;
    }

    private static int? GetSelectedGridRowIndex(DataGridView grid)
    {
        if (grid.CurrentRow is not null && !grid.CurrentRow.IsNewRow)
        {
            return grid.CurrentRow.Index;
        }

        if (grid.SelectedRows.Count > 0 && !grid.SelectedRows[0].IsNewRow)
        {
            return grid.SelectedRows[0].Index;
        }

        return null;
    }

    private static void SelectGridRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        grid.ClearSelection();
        var row = grid.Rows[rowIndex];
        row.Selected = true;

        var preferredCell = grid.Columns.Contains("value") ? row.Cells["value"] : null;
        var cell = preferredCell?.Visible == true ? preferredCell : row.Cells.Cast<DataGridViewCell>().FirstOrDefault(v => v.Visible) ?? row.Cells[0];
        grid.CurrentCell = cell;
    }

    private static DataGridView? FindEditorGrid(Control control)
    {
        if (control is DataGridView grid)
        {
            return grid;
        }

        foreach (Control child in control.Controls)
        {
            var found = FindEditorGrid(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static CheckBox? FindCheckBox(Control control)
    {
        if (control is CheckBox checkBox)
        {
            return checkBox;
        }

        foreach (Control child in control.Controls)
        {
            var found = FindCheckBox(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static IEnumerable<string> ReadMultiChoiceValues(Control control)
    {
        var values = new List<string>();
        CollectCheckedValues(control, values);
        return values;
    }

    private static void CollectCheckedValues(Control control, ICollection<string> values)
    {
        if (control is CheckBox checkBox && checkBox.Checked && checkBox.Tag is string value && !string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }

        foreach (Control child in control.Controls)
        {
            CollectCheckedValues(child, values);
        }
    }

    private void CreateRecord()
    {
        if (_currentCategory is null)
        {
            return;
        }

        var record = _currentCategory.CreateNew();
        _currentCategory.GetItems(_context.Project.AssetLibrary).Add(record);
        PopulateNavigation();
        RefreshAssetGrid(_currentCategory.GetId(record));
        if (!OpenRecordEditor(_currentCategory, record))
        {
            _currentCategory.GetItems(_context.Project.AssetLibrary).Remove(record);
            _selectedRecordCategory = null;
            _selectedRecord = null;
            PopulateNavigation();
            RefreshAssetGrid();
        }
    }

    private void DeleteRecord()
    {
        if (_selectedRecordCategory is null || _selectedRecord is null)
        {
            return;
        }

        if (IsBuiltIn(_selectedRecord))
        {
            MessageBox.Show(this, T("common.cannotDeleteBuiltin", "内置内容不能删除。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var name = _selectedRecordCategory.GetName(_selectedRecord);
        var confirm = MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("common.deleteConfirm", "确定删除“{0}”吗？"), name), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        _selectedRecordCategory.GetItems(_context.Project.AssetLibrary).Remove(_selectedRecord);
        _selectedRecordCategory = null;
        _selectedRecord = null;
        PopulateNavigation();
        RefreshAssetGrid();
    }

    private void ApplyAndRefresh()
    {
        OpenSelectedRecordEditor();
    }

    private void SaveProject()
    {
        _projectService.SaveProject(_context);
        var selectedId = _selectedRecord is not null && _selectedRecordCategory is not null ? _selectedRecordCategory.GetId(_selectedRecord) : null;
        PopulateNavigation();
        RefreshAssetGrid(selectedId);
        MessageBox.Show(this, T("dataEditor.save.success", "核心数据库已保存。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RunDatabaseHealthCheck()
    {
        var report = BuildDatabaseHealthReport();
        using var dialog = new Form
        {
            Text = T("dataEditor.health.title", "数据库健康检查"),
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(900, 640),
            MinimumSize = new Size(760, 520),
            Font = Font
        };

        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Text = report,
            Font = new Font(FontFamily.GenericMonospace, 9F)
        };
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 52,
            Padding = new Padding(12, 8, 12, 8)
        };
        var okButton = new Button
        {
            Text = T("common.ok", "确定"),
            Width = 96,
            Height = 32,
            DialogResult = DialogResult.OK,
            UseVisualStyleBackColor = true
        };
        buttonPanel.Controls.Add(okButton);
        dialog.Controls.Add(textBox);
        dialog.Controls.Add(buttonPanel);
        dialog.AcceptButton = okButton;
        dialog.ShowDialog(this);
    }

    private string BuildDatabaseHealthReport()
    {
        var issues = new List<string>();
        var library = _context.Project.AssetLibrary;
        var unitIds = IdSet(library.Units.Select(unit => unit.Id));
        var skillIds = IdSet(library.Skills.Select(skill => skill.Id));
        var effectIds = IdSet(library.GameplayEffects.Select(effect => effect.Id));
        var statusIds = IdSet(library.Statuses.Select(status => status.Id));
        var projectileIds = IdSet(library.Projectiles.Select(projectile => projectile.Id));
        var visualEffectIds = IdSet(library.VisualEffects.Select(vfx => vfx.Id));
        var formulaIds = IdSet(library.Formulas.Select(formula => formula.Id));
        var damageTypeIds = IdSet(library.DamageTypes.Select(damageType => damageType.Id));
        var elementIds = IdSet(library.Elements.Select(element => element.Id));
        var factionIds = IdSet(library.Factions.Select(faction => faction.Id));
        var aiProfileIds = IdSet(library.AIProfiles.Select(profile => profile.Id));
        var lootTableIds = IdSet(library.LootTables.Select(loot => loot.Id));
        var itemIds = IdSet(library.Items.Select(item => item.Id));
        var itemTypeIds = IdSet(library.ItemTypes.Select(itemType => itemType.Id));
        var mapIds = IdSet(library.Maps.Select(map => map.Id));
        var routeIds = IdSet(library.TowerDefensePaths.Select(path => path.Id));
        var waveIds = IdSet(library.TowerDefenseWaves.Select(wave => wave.Id));
        var buildRuleIds = IdSet(library.TowerDefenseBuildRules.Select(rule => rule.Id));
        var techIds = IdSet(library.TechRules.Select(tech => tech.Id));
        var resourceRuleIds = IdSet(library.ResourceRules.Select(resource => resource.Id));
        var statKeys = IdSet(library.Stats.Select(stat => stat.Key));
        var tagKeys = RegisteredTagKeys();
        var eventGraphIds = IdSet(_context.Project.EventGraphs.Select(graph => graph.Id));

        AddDuplicateIssues(issues, "asset.units", "单位", library.Units.Select(unit => unit.Id));
        AddDuplicateIssues(issues, "asset.skills", "技能", library.Skills.Select(skill => skill.Id));
        AddDuplicateIssues(issues, "asset.gameplayEffects", "玩法效果", library.GameplayEffects.Select(effect => effect.Id));
        AddDuplicateIssues(issues, "asset.items", "物品", library.Items.Select(item => item.Id));
        AddDuplicateIssues(issues, "tree.maps", "地图", library.Maps.Select(map => map.Id));
        AddDuplicateIssues(issues, "asset.stats", "属性", library.Stats.Select(stat => stat.Key));
        AddDuplicateIssues(issues, "asset.traits", "特性", library.Traits.Select(trait => trait.Id));
        AddDuplicateIssues(issues, "asset.optionSets", "选项集", library.OptionSets.Select(optionSet => optionSet.Id));

        foreach (var stat in library.Stats)
        {
            if (string.IsNullOrWhiteSpace(stat.Key))
            {
                AddIssue(issues, "asset.stats", "属性", stat.Id, T("dataEditor.health.emptyStatKey", "属性 Key 不能为空。"));
            }

            CheckOptionValue(issues, "asset.stats", "属性", stat.Id, "dataEditor.field.valueType", "值类型", stat.ValueType, ValueTypeOptions());
            CheckOptionValue(issues, "asset.stats", "属性", stat.Id, "dataEditor.field.category", "分类", stat.CategoryKey, StatCategoryOptions(), optional: true);
        }

        foreach (var trait in library.Traits)
        {
            CheckOptionValue(issues, "asset.traits", "特性", trait.Id, "dataEditor.field.category", "分类", trait.CategoryKey, TraitCategoryOptions(), optional: true);
            if (!string.IsNullOrWhiteSpace(trait.Id) && !tagKeys.Contains(trait.Id))
            {
                AddIssue(issues, "asset.traits", "特性", trait.Id, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.traitNotTag", "特性未登记为可选标签：{0}"), trait.Id));
            }
        }

        foreach (var faction in library.Factions)
        {
            CheckOptionValue(issues, "asset.factions", "阵营", faction.Id, "dataEditor.field.attitude", "立场", faction.AttitudeToPlayer, AttitudeOptions());
        }

        foreach (var damageType in library.DamageTypes)
        {
            CheckRef(issues, "asset.damageTypes", "伤害类型", damageType.Id, "dataEditor.field.defenseStat", "防御属性", damageType.DefenseStatKey, statKeys, optional: true);
        }

        foreach (var formula in library.Formulas)
        {
            CheckOptionValue(issues, "asset.formulas", "公式", formula.Id, "dataEditor.field.formulaKind", "公式类型", formula.FormulaKind, FormulaKindOptions());
        }

        foreach (var itemType in library.ItemTypes)
        {
            CheckItemTypeFields(issues, itemType, statKeys, tagKeys);
        }

        foreach (var profile in library.AIProfiles)
        {
            CheckOptionValue(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, "dataEditor.field.behaviorType", "行为类型", profile.BehaviorType, BehaviorTypeOptions());
            CheckOptionValue(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, "dataEditor.field.movementMode", "移动模式", profile.MovementMode, MovementModeOptions());
            CheckOptionValue(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, "dataEditor.field.targetSelector", "目标选择", profile.TargetSelector, TargetSelectorOptions());
            CheckOptionValue(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, "dataEditor.field.patrolMode", "巡逻模式", profile.PatrolMode, PatrolModeOptions());
            CheckTags(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, profile.TargetTags, tagKeys);
            CheckRefs(issues, "asset.aiProfiles", "AI 行为预设", profile.Id, "dataEditor.field.skills", "技能", profile.SkillIds, skillIds);
        }

        foreach (var unit in library.Units)
        {
            CheckOptionValue(issues, "asset.units", "单位", unit.Id, "dataEditor.field.unitKind", "单位类型", unit.UnitKind, UnitKindOptions());
            CheckRef(issues, "asset.units", "单位", unit.Id, "dataEditor.field.faction", "阵营", unit.FactionId, factionIds);
            CheckRef(issues, "asset.units", "单位", unit.Id, "dataEditor.field.aiProfile", "AI 预设", unit.AIProfileId, aiProfileIds, optional: true);
            CheckRef(issues, "asset.units", "单位", unit.Id, "dataEditor.field.lootTable", "掉落表", unit.LootTableId, lootTableIds, optional: true);
            CheckTags(issues, "asset.units", "单位", unit.Id, unit.Tags.Concat(unit.Traits), tagKeys);
            CheckStatMap(issues, "asset.units", "单位", unit.Id, unit.Stats, statKeys);
            CheckComponents(issues, "asset.units", "单位", unit.Id, unit.Components, skillIds, projectileIds, aiProfileIds, lootTableIds);
        }

        foreach (var skill in library.Skills)
        {
            CheckOptionValue(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.skillType", "技能类型", skill.SkillType, SkillTypeOptions());
            CheckOptionValue(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.targetingMode", "目标方式", skill.TargetingMode, TargetingModeOptions());
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.element", "元素", skill.ElementId, elementIds, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.damageType", "伤害类型", skill.DamageTypeId, damageTypeIds, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.powerStat", "威力属性", skill.PowerStatKey, statKeys, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.costStat", "消耗属性", skill.CostStatKey, statKeys, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.projectile", "投射物", skill.ProjectileId, projectileIds, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.formula", "公式", skill.FormulaId, formulaIds, optional: true);
            CheckRef(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.visualEffect", "视觉特效", skill.VisualEffectId, visualEffectIds, optional: true);
            CheckRefs(issues, "asset.skills", "技能", skill.Id, "dataEditor.field.statuses", "状态", skill.StatusIds, statusIds);
            CheckTags(issues, "asset.skills", "技能", skill.Id, SplitValues(skill.Tags).Concat(skill.RequiredTargetTags).Concat(skill.BlockedTargetTags), tagKeys);
            CheckEffectReferences(issues, "asset.skills", "技能", skill.Id, skill.Effects, effectIds, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys);
        }

        foreach (var effect in library.GameplayEffects)
        {
            CheckOptionValue(issues, "asset.gameplayEffects", "玩法效果", effect.Id, "dataEditor.field.effectKind", "效果类型", effect.EffectKind, EffectKindOptions());
            CheckTags(issues, "asset.gameplayEffects", "玩法效果", effect.Id, effect.Tags, tagKeys);
            CheckEffectParameterDefaults(issues, "asset.gameplayEffects", "玩法效果", effect.Id, effect.Parameters, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys, tagKeys);
        }

        foreach (var status in library.Statuses)
        {
            CheckOptionValue(issues, "asset.statuses", "状态", status.Id, "dataEditor.field.statusKind", "状态类型", status.StatusKind, StatusKindOptions());
            CheckTags(issues, "asset.statuses", "状态", status.Id, status.Tags, tagKeys);
            CheckEffectReferences(issues, "asset.statuses", "状态", status.Id, status.OnApplyEffects, effectIds, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys);
            CheckEffectReferences(issues, "asset.statuses", "状态", status.Id, status.PeriodicEffects, effectIds, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys);
        }

        foreach (var projectile in library.Projectiles)
        {
            CheckRef(issues, "asset.projectiles", "投射物", projectile.Id, "dataEditor.field.visualEffect", "视觉特效", projectile.VisualEffectId, visualEffectIds, optional: true);
            CheckEffectReferences(issues, "asset.projectiles", "投射物", projectile.Id, projectile.Effects, effectIds, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys);
        }

        foreach (var visualEffect in library.VisualEffects)
        {
            CheckOptionValue(issues, "asset.visualEffects", "视觉特效", visualEffect.Id, "dataEditor.field.effectKind", "表现类型", visualEffect.EffectKind, VisualEffectKindOptions());
            CheckOptionValue(issues, "asset.visualEffects", "视觉特效", visualEffect.Id, "dataEditor.field.animationKey", "动画键", visualEffect.AnimationKey, AnimationKeyOptions(), optional: true);
        }

        foreach (var item in library.Items)
        {
            CheckRef(issues, "asset.items", "物品", item.Id, "dataEditor.field.itemType", "物品类型", item.TypeId, itemTypeIds);
            CheckOptionValue(issues, "asset.items", "物品", item.Id, "dataEditor.field.rarity", "稀有度", item.Rarity, RarityOptions());
            CheckOptionValue(issues, "asset.items", "物品", item.Id, "dataEditor.field.equipmentSlot", "装备槽", item.EquipmentSlot, EquipmentSlotOptions(), optional: true);
            CheckRefs(issues, "asset.items", "物品", item.Id, "dataEditor.field.grantedSkills", "授予技能", item.GrantedSkillIds, skillIds);
            CheckTags(issues, "asset.items", "物品", item.Id, SplitValues(item.Tags), tagKeys);
            CheckItemCustomValues(issues, item, library.ItemTypes, statKeys, tagKeys);
            CheckEffectReferences(issues, "asset.items", "物品", item.Id, item.Effects, effectIds, formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys);
        }

        foreach (var decoration in library.Decorations)
        {
            CheckOptionValue(issues, "asset.decorations", "装饰物", decoration.Id, "dataEditor.field.decorationKind", "装饰物类型", decoration.DecorationKind, DecorationKindOptions());
            CheckOptionValue(issues, "asset.decorations", "装饰物", decoration.Id, "dataEditor.field.animationKey", "动画键", decoration.AnimationKey, AnimationKeyOptions(), optional: true);
            CheckRef(issues, "asset.decorations", "装饰物", decoration.Id, "dataEditor.field.lootTable", "掉落表", decoration.LootTableId, lootTableIds, optional: true);
            CheckRef(issues, "asset.decorations", "装饰物", decoration.Id, "dataEditor.field.interactionProfile", "交互入口", decoration.InteractionProfileId, IdSet(library.InteractionProfiles.Select(profile => profile.Id)), optional: true);
            CheckTags(issues, "asset.decorations", "装饰物", decoration.Id, decoration.Tags, tagKeys);
        }

        foreach (var interaction in library.InteractionProfiles)
        {
            CheckOptionValue(issues, "asset.interactions", "交互入口", interaction.Id, "dataEditor.field.interactionKind", "交互类型", interaction.InteractionKind, InteractionKindOptions());
            CheckRef(issues, "asset.interactions", "交互入口", interaction.Id, "dataEditor.field.eventGraph", "事件图", interaction.EventGraphId, eventGraphIds, optional: true);
        }

        foreach (var loot in library.LootTables)
        {
            CheckRefs(issues, "asset.lootTables", "掉落表", loot.Id, "asset.items", "物品", loot.Entries.Select(entry => entry.ItemId), itemIds);
        }

        foreach (var map in library.Maps)
        {
            CheckOptionValue(issues, "tree.maps", "地图", map.Id, "dataEditor.field.viewType", "地图视角", map.ViewType, ViewTypeOptions());
        }

        foreach (var path in library.TowerDefensePaths)
        {
            CheckRef(issues, "asset.routes", "路线", path.Id, "dataEditor.field.map", "地图", path.MapId, mapIds, optional: true);
            CheckOptionValue(issues, "asset.routes", "路线", path.Id, "dataEditor.field.pathMode", "路线模式", path.PathMode, TowerDefensePathModeOptions());
        }

        foreach (var wave in library.TowerDefenseWaves)
        {
            CheckRef(issues, "asset.spawnWaves", "生成波次", wave.Id, "dataEditor.field.map", "地图", wave.MapId, mapIds, optional: true);
            CheckRef(issues, "asset.spawnWaves", "生成波次", wave.Id, "dataEditor.field.path", "路线", wave.PathId, routeIds, optional: true);
            foreach (var group in wave.SpawnGroups)
            {
                CheckRef(issues, "asset.spawnWaves", "生成波次", wave.Id, "dataEditor.field.unit", "单位", group.UnitId, unitIds);
                CheckRef(issues, "asset.spawnWaves", "生成波次", wave.Id, "dataEditor.field.path", "路线", group.PathId, routeIds, optional: true);
            }
        }

        foreach (var rule in library.TowerDefenseRules)
        {
            CheckRef(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.map", "地图", rule.MapId, mapIds, optional: true);
            CheckRef(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.buildRule", "建造规则", rule.BuildRuleId, buildRuleIds, optional: true);
            CheckRefs(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.waves", "波次", rule.WaveIds, waveIds);
            CheckOptionValue(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.waveStartMode", "波次启动方式", rule.WaveStartMode, TowerDefenseWaveStartModeOptions());
            CheckOptionValue(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.victoryCondition", "胜利条件", rule.VictoryCondition, TowerDefenseVictoryConditionOptions());
            CheckOptionValue(issues, "asset.levelRules", "关卡规则", rule.Id, "dataEditor.field.defeatCondition", "失败条件", rule.DefeatCondition, TowerDefenseDefeatConditionOptions());
        }

        foreach (var buildRule in library.TowerDefenseBuildRules)
        {
            CheckRef(issues, "asset.buildRules", "建造规则", buildRule.Id, "dataEditor.field.currencyStat", "货币属性", buildRule.CurrencyStatKey, statKeys, optional: true);
            CheckTags(issues, "asset.buildRules", "建造规则", buildRule.Id, [buildRule.BuildSurfaceTag], tagKeys);
        }

        foreach (var buildable in library.TowerDefenseTowers)
        {
            CheckOptionValue(issues, "asset.buildableUnits", "可建造单位", buildable.Id, "dataEditor.field.buildableRole", "定位", buildable.TowerRole, TowerDefenseTowerRoleOptions());
            CheckOptionValue(issues, "asset.buildableUnits", "可建造单位", buildable.Id, "dataEditor.field.targetPriority", "目标优先级", buildable.TargetPriority, TowerDefenseTargetPriorityOptions());
            CheckRef(issues, "asset.buildableUnits", "可建造单位", buildable.Id, "dataEditor.field.unit", "单位", buildable.UnitId, unitIds);
            CheckRef(issues, "asset.buildableUnits", "可建造单位", buildable.Id, "dataEditor.field.skill", "技能", buildable.SkillId, skillIds, optional: true);
            CheckRefs(issues, "asset.buildableUnits", "可建造单位", buildable.Id, "dataEditor.field.skill", "技能", buildable.Levels.Select(level => level.SkillId), skillIds);
        }

        foreach (var grid in library.TacticalGridRules)
        {
            CheckOptionValue(issues, "asset.tacticalGridRules", "格子设置", grid.Id, "dataEditor.field.gridType", "格子类型", grid.GridType, GridTypeOptions());
            CheckOptionValue(issues, "asset.tacticalGridRules", "格子设置", grid.Id, "dataEditor.field.movementMetric", "移动计算方式", grid.MovementMetric, MovementMetricOptions());
        }

        foreach (var turn in library.TurnRules)
        {
            CheckOptionValue(issues, "asset.turnRules", "回合设置", turn.Id, "dataEditor.field.turnMode", "回合模式", turn.TurnMode, TurnModeOptions());
            CheckOptionValue(issues, "asset.turnRules", "回合设置", turn.Id, "dataEditor.field.actionRefreshMode", "行动刷新方式", turn.ActionRefreshMode, ActionRefreshModeOptions());
            CheckRef(issues, "asset.turnRules", "回合规则", turn.Id, "dataEditor.field.initiativeStat", "行动顺序属性", turn.InitiativeStatKey, statKeys, optional: true);
        }

        foreach (var action in library.ActionRules)
        {
            CheckRef(issues, "asset.actionRules", "行动规则", action.Id, "dataEditor.field.actionPointStat", "行动点属性", action.ActionPointStatKey, statKeys, optional: true);
            CheckRef(issues, "asset.actionRules", "行动规则", action.Id, "dataEditor.field.movePointStat", "移动力属性", action.MovePointStatKey, statKeys, optional: true);
        }

        foreach (var range in library.TacticalRanges)
        {
            CheckOptionValue(issues, "asset.tacticalRanges", "战术范围", range.Id, "dataEditor.field.rangeShape", "范围形状", range.RangeShape, RangeShapeOptions());
            CheckOptionValue(issues, "asset.tacticalRanges", "战术范围", range.Id, "dataEditor.field.areaShape", "影响区域形状", range.AreaShape, AreaShapeOptions());
            CheckTags(issues, "asset.tacticalRanges", "战术范围", range.Id, range.RequiredTargetTags.Concat(range.BlockedTargetTags), tagKeys);
        }

        foreach (var terrain in library.TerrainRules)
        {
            CheckTags(issues, "asset.terrainRules", "地形规则", terrain.Id, [terrain.TerrainTag, .. terrain.AllowedUnitTags, .. terrain.ForbiddenUnitTags], tagKeys);
        }

        foreach (var objective in library.ObjectiveRules)
        {
            CheckOptionValue(issues, "asset.objectiveRules", "目标条件", objective.Id, "dataEditor.field.objectiveType", "目标类型", objective.ObjectiveType, ObjectiveTypeOptions());
            CheckTags(issues, "asset.objectiveRules", "目标规则", objective.Id, objective.TargetUnitTags.Concat(objective.TargetAreaTags), tagKeys);
            CheckRef(issues, "asset.objectiveRules", "目标条件", objective.Id, "dataEditor.field.eventGraph", "事件图", objective.EventGraphId, eventGraphIds, optional: true);
        }

        foreach (var bond in library.BondRules)
        {
            CheckOptionValue(issues, "asset.bondRules", "羁绊关系", bond.Id, "dataEditor.field.triggerTiming", "触发时机", bond.TriggerTiming, BondTriggerTimingOptions());
            CheckOptionValue(issues, "asset.bondRules", "羁绊关系", bond.Id, "dataEditor.field.durationMode", "持续方式", bond.DurationMode, BondDurationModeOptions());
            CheckOptionValue(issues, "asset.bondRules", "羁绊关系", bond.Id, "dataEditor.field.stackingMode", "叠加方式", bond.StackingMode, StackingModeOptions());
            CheckTags(issues, "asset.bondRules", "羁绊规则", bond.Id, bond.RequiredUnitTags.Concat(bond.ExcludedUnitTags), tagKeys);
            CheckRefs(issues, "asset.bondRules", "羁绊规则", bond.Id, "dataEditor.field.effects", "效果", bond.EffectIds, effectIds);
        }

        foreach (var resource in library.ResourceRules)
        {
            CheckOptionValue(issues, "asset.resourceRules", "资源定义", resource.Id, "dataEditor.field.resourceKind", "资源类型", resource.ResourceKind, ResourceKindOptions());
            CheckRef(issues, "asset.resourceRules", "资源规则", resource.Id, "dataEditor.field.stat", "属性", resource.StatKey, statKeys, optional: true);
            CheckTags(issues, "asset.resourceRules", "资源规则", resource.Id, resource.Tags, tagKeys);
        }

        foreach (var production in library.ProductionRules)
        {
            CheckOptionValue(issues, "asset.productionRules", "生产/产出", production.Id, "dataEditor.field.producedAssetKind", "产出类型", production.ProducedAssetKind, ProducedAssetKindOptions());
            CheckRef(issues, "asset.productionRules", "生产规则", production.Id, "dataEditor.field.producerUnit", "生产者单位", production.ProducerUnitId, unitIds, optional: true);
            CheckProducedAssetRef(issues, production, unitIds, itemIds, resourceRuleIds, skillIds);
            CheckResourceMap(issues, "asset.productionRules", "生产规则", production.Id, production.ResourceCosts, statKeys);
            CheckResourceMap(issues, "asset.productionRules", "生产规则", production.Id, production.ResourceOutputs, statKeys);
            CheckRef(issues, "asset.productionRules", "生产/产出", production.Id, "dataEditor.field.eventGraph", "事件图", production.EventGraphId, eventGraphIds, optional: true);
        }

        foreach (var tech in library.TechRules)
        {
            CheckOptionValue(issues, "asset.techRules", "科技/解锁", tech.Id, "dataEditor.field.techKind", "科技类型", tech.TechKind, TechKindOptions());
            CheckRefs(issues, "asset.techRules", "科技规则", tech.Id, "dataEditor.field.prerequisiteTechs", "前置科技", tech.PrerequisiteTechIds, techIds);
            CheckRefs(issues, "asset.techRules", "科技规则", tech.Id, "dataEditor.field.unlockUnits", "解锁单位", tech.UnlockUnitIds, unitIds);
            CheckRefs(issues, "asset.techRules", "科技规则", tech.Id, "dataEditor.field.unlockSkills", "解锁技能", tech.UnlockSkillIds, skillIds);
            CheckRefs(issues, "asset.techRules", "科技规则", tech.Id, "dataEditor.field.unlockBuildRules", "解锁建造规则", tech.UnlockBuildRuleIds, buildRuleIds);
            CheckRefs(issues, "asset.techRules", "科技规则", tech.Id, "dataEditor.field.effects", "效果", tech.EffectIds, effectIds);
            CheckResourceMap(issues, "asset.techRules", "科技规则", tech.Id, tech.ResearchCosts, statKeys);
            CheckRef(issues, "asset.techRules", "科技/解锁", tech.Id, "dataEditor.field.eventGraph", "事件图", tech.EventGraphId, eventGraphIds, optional: true);
        }

        foreach (var diplomacy in library.DiplomacyRules)
        {
            CheckOptionValue(issues, "asset.diplomacyRules", "阵营关系", diplomacy.Id, "dataEditor.field.diplomaticState", "外交状态", diplomacy.DiplomaticState, DiplomaticStateOptions());
            CheckRef(issues, "asset.diplomacyRules", "外交规则", diplomacy.Id, "dataEditor.field.fromFaction", "来源阵营", diplomacy.FromFactionId, factionIds, optional: true);
            CheckRef(issues, "asset.diplomacyRules", "外交规则", diplomacy.Id, "dataEditor.field.toFaction", "目标阵营", diplomacy.ToFactionId, factionIds, optional: true);
            CheckRef(issues, "asset.diplomacyRules", "阵营关系", diplomacy.Id, "dataEditor.field.eventGraph", "事件图", diplomacy.EventGraphId, eventGraphIds, optional: true);
        }

        foreach (var territory in library.TerritoryRules)
        {
            CheckOptionValue(issues, "asset.territoryRules", "区域控制", territory.Id, "dataEditor.field.controlMode", "控制方式", territory.ControlMode, TerritoryControlModeOptions());
            CheckRef(issues, "asset.territoryRules", "领地规则", territory.Id, "dataEditor.field.ownerFaction", "所属阵营", territory.OwnerFactionId, factionIds, optional: true);
            CheckTags(issues, "asset.territoryRules", "领地规则", territory.Id, [territory.TerritoryTag, .. territory.RequiredUnitTags, .. territory.BlockedUnitTags], tagKeys);
            CheckResourceMap(issues, "asset.territoryRules", "领地规则", territory.Id, territory.ResourceYields, statKeys);
            CheckRef(issues, "asset.territoryRules", "区域控制", territory.Id, "dataEditor.field.eventGraph", "事件图", territory.EventGraphId, eventGraphIds, optional: true);
        }

        var header = issues.Count == 0
            ? T("dataEditor.health.ok", "未发现明显问题。")
            : string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.issueCount", "发现 {0} 个问题："), issues.Count);
        return header + Environment.NewLine + Environment.NewLine + (issues.Count == 0
            ? T("dataEditor.health.okDetail", "引用关系、默认预设和常用标签检查通过。")
            : string.Join(Environment.NewLine, issues));
    }

    private bool ApplyEditors(IEnumerable<FieldEditor> editors, bool showErrors)
    {
        foreach (var editor in editors)
        {
            if (!editor.Apply(out var error))
            {
                if (showErrors)
                {
                    MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        return true;
    }

    private HashSet<string> IdSet(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private HashSet<string> RegisteredTagKeys()
    {
        return _context.Project.AssetLibrary.OptionSets
            .FirstOrDefault(optionSet => string.Equals(optionSet.Id, "tag", StringComparison.OrdinalIgnoreCase))
            ?.Values
            .Keys
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    private void AddDuplicateIssues(List<string> issues, string categoryKey, string categoryFallback, IEnumerable<string> ids)
    {
        foreach (var duplicate in ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            AddIssue(issues, categoryKey, categoryFallback, duplicate, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.duplicateId", "重复 ID：{0}"), duplicate));
        }
    }

    private void CheckRef(List<string> issues, string categoryKey, string categoryFallback, string ownerId, string fieldKey, string fieldFallback, string value, HashSet<string> validIds, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!optional)
            {
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.emptyRequiredRef", "{0}不能为空。"), T(fieldKey, fieldFallback)));
            }

            return;
        }

        if (!validIds.Contains(value))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.missingRef", "{0}引用不存在：{1}"), T(fieldKey, fieldFallback), value));
        }
    }

    private void CheckRefs(List<string> issues, string categoryKey, string categoryFallback, string ownerId, string fieldKey, string fieldFallback, IEnumerable<string> values, HashSet<string> validIds)
    {
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            CheckRef(issues, categoryKey, categoryFallback, ownerId, fieldKey, fieldFallback, value, validIds, optional: false);
        }
    }

    private void CheckOptionValue(List<string> issues, string categoryKey, string categoryFallback, string ownerId, string fieldKey, string fieldFallback, string value, IReadOnlyList<OptionItem> options, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!optional)
            {
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.emptyRequiredRef", "{0}不能为空。"), T(fieldKey, fieldFallback)));
            }

            return;
        }

        if (options.Any(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.invalidOptionValue", "{0}选项不存在：{1}"), T(fieldKey, fieldFallback), value));
    }

    private void CheckTags(List<string> issues, string categoryKey, string categoryFallback, string ownerId, IEnumerable<string> tags, HashSet<string> validTags)
    {
        foreach (var tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!validTags.Contains(tag))
            {
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.unknownTag", "标签未在选项集中定义：{0}"), tag));
            }
        }
    }

    private void CheckItemTypeFields(List<string> issues, ItemTypeDefinition itemType, HashSet<string> statKeys, HashSet<string> tagKeys)
    {
        foreach (var field in itemType.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key))
            {
                AddIssue(issues, "asset.itemTypes", "物品类型", itemType.Id, T("itemField.error.emptyKey", "键不能为空。"));
                continue;
            }

            CheckItemFieldValue(issues, "asset.itemTypes", "物品类型", itemType.Id, field, field.DefaultValue, statKeys, tagKeys);
        }
    }

    private void CheckItemCustomValues(List<string> issues, ItemDefinition item, IEnumerable<ItemTypeDefinition> itemTypes, HashSet<string> statKeys, HashSet<string> tagKeys)
    {
        var itemType = itemTypes.FirstOrDefault(type => string.Equals(type.Id, item.TypeId, StringComparison.OrdinalIgnoreCase));
        if (itemType is null)
        {
            return;
        }

        var fieldLookup = itemType.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Key))
            .ToDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var value in item.CustomValues)
        {
            if (string.IsNullOrWhiteSpace(value.Key))
            {
                continue;
            }

            if (!fieldLookup.TryGetValue(value.Key, out var field))
            {
                AddIssue(issues, "asset.items", "物品", item.Id, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.unknownItemField", "物品字段不存在：{0}"), value.Key));
                continue;
            }

            CheckItemFieldValue(issues, "asset.items", "物品", item.Id, field, value.Value, statKeys, tagKeys);
        }
    }

    private void CheckItemFieldValue(List<string> issues, string categoryKey, string categoryFallback, string ownerId, ItemFieldDefinition field, string value, HashSet<string> statKeys, HashSet<string> tagKeys)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.Required)
            {
                var name = T(field.DisplayNameKey, string.IsNullOrWhiteSpace(field.DisplayName) ? field.Key : field.DisplayName);
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.emptyRequiredRef", "{0}不能为空。"), name));
            }

            return;
        }

        if (field.ValueType == ItemFieldValueType.Integer && !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.invalidIntegerValue", "字段“{0}”必须是整数：{1}"), FieldDisplayName(field), value));
        }
        else if (field.ValueType == ItemFieldValueType.Number
            && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            && !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out _))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.invalidNumberValue", "字段“{0}”必须是数字：{1}"), FieldDisplayName(field), value));
        }
        else if (field.ValueType == ItemFieldValueType.Boolean && !bool.TryParse(value, out _))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.invalidBooleanValue", "字段“{0}”必须是布尔值：{1}"), FieldDisplayName(field), value));
        }
        else if (field.ValueType == ItemFieldValueType.Choice && field.Options.Count > 0 && !field.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.invalidOptionValue", "{0}选项不存在：{1}"), FieldDisplayName(field), value));
        }
        else if (field.ValueType == ItemFieldValueType.StatRef && !statKeys.Contains(value))
        {
            AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.missingParameterRef", "参数“{0}”引用不存在：{1}"), FieldDisplayName(field), value));
        }
        else if (field.ValueType == ItemFieldValueType.TagList)
        {
            CheckTags(issues, categoryKey, categoryFallback, ownerId, SplitValues(value), tagKeys);
        }
    }

    private string FieldDisplayName(ItemFieldDefinition field)
    {
        return T(field.DisplayNameKey, string.IsNullOrWhiteSpace(field.DisplayName) ? field.Key : field.DisplayName);
    }

    private void CheckStatMap(List<string> issues, string categoryKey, string categoryFallback, string ownerId, Dictionary<string, double> stats, HashSet<string> statKeys)
    {
        foreach (var key in stats.Keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (!statKeys.Contains(key))
            {
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.unknownStat", "属性不存在：{0}"), key));
            }
        }
    }

    private void CheckResourceMap(List<string> issues, string categoryKey, string categoryFallback, string ownerId, Dictionary<string, double> values, HashSet<string> statKeys)
    {
        CheckStatMap(issues, categoryKey, categoryFallback, ownerId, values, statKeys);
    }

    private void CheckComponents(List<string> issues, string categoryKey, string categoryFallback, string ownerId, IEnumerable<ComponentConfig> components, HashSet<string> skillIds, HashSet<string> projectileIds, HashSet<string> aiProfileIds, HashSet<string> lootTableIds)
    {
        foreach (var component in components)
        {
            foreach (var (key, rawValue) in component.Parameters)
            {
                var value = ObjectToText(rawValue);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (key.Equals("skillId", StringComparison.OrdinalIgnoreCase) || key.Equals("SkillId", StringComparison.OrdinalIgnoreCase))
                {
                    CheckRef(issues, categoryKey, categoryFallback, ownerId, "dataEditor.field.skill", "技能", value, skillIds);
                }
                else if (key.Equals("projectileId", StringComparison.OrdinalIgnoreCase) || key.Equals("ProjectileId", StringComparison.OrdinalIgnoreCase))
                {
                    CheckRef(issues, categoryKey, categoryFallback, ownerId, "dataEditor.field.projectile", "投射物", value, projectileIds);
                }
                else if (key.Equals("AIProfileId", StringComparison.OrdinalIgnoreCase) || key.Equals("profileId", StringComparison.OrdinalIgnoreCase))
                {
                    CheckRef(issues, categoryKey, categoryFallback, ownerId, "dataEditor.field.aiProfile", "AI 预设", value, aiProfileIds);
                }
                else if (key.Equals("LootTableId", StringComparison.OrdinalIgnoreCase))
                {
                    CheckRef(issues, categoryKey, categoryFallback, ownerId, "dataEditor.field.lootTable", "掉落表", value, lootTableIds);
                }
            }
        }
    }

    private void CheckEffectReferences(
        List<string> issues,
        string categoryKey,
        string categoryFallback,
        string ownerId,
        IEnumerable<GameplayEffectReference> references,
        HashSet<string> effectIds,
        HashSet<string> formulaIds,
        HashSet<string> statusIds,
        HashSet<string> projectileIds,
        HashSet<string> damageTypeIds,
        HashSet<string> elementIds,
        HashSet<string> statKeys)
    {
        foreach (var reference in references)
        {
            CheckRef(issues, categoryKey, categoryFallback, ownerId, "dataEditor.field.effect", "效果", reference.EffectId, effectIds);
            var effect = FindGameplayEffect(reference.EffectId);
            if (effect is null)
            {
                continue;
            }

            CheckEffectParameterValues(issues, categoryKey, categoryFallback, ownerId, effect.Parameters, BuildEffectParameterMap(reference.Parameters), formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys, RegisteredTagKeys());
        }
    }

    private void CheckEffectParameterDefaults(
        List<string> issues,
        string categoryKey,
        string categoryFallback,
        string ownerId,
        IEnumerable<EffectParameterDefinition> parameters,
        HashSet<string> formulaIds,
        HashSet<string> statusIds,
        HashSet<string> projectileIds,
        HashSet<string> damageTypeIds,
        HashSet<string> elementIds,
        HashSet<string> statKeys,
        HashSet<string> tagKeys)
    {
        CheckEffectParameterValues(issues, categoryKey, categoryFallback, ownerId, parameters, parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.DefaultValue, StringComparer.OrdinalIgnoreCase), formulaIds, statusIds, projectileIds, damageTypeIds, elementIds, statKeys, tagKeys);
    }

    private void CheckEffectParameterValues(
        List<string> issues,
        string categoryKey,
        string categoryFallback,
        string ownerId,
        IEnumerable<EffectParameterDefinition> parameters,
        IReadOnlyDictionary<string, string> values,
        HashSet<string> formulaIds,
        HashSet<string> statusIds,
        HashSet<string> projectileIds,
        HashSet<string> damageTypeIds,
        HashSet<string> elementIds,
        HashSet<string> statKeys,
        HashSet<string> tagKeys)
    {
        foreach (var parameter in parameters)
        {
            if (!values.TryGetValue(parameter.Key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var validIds = parameter.OptionSourceId switch
            {
                "formula" => formulaIds,
                "status" => statusIds,
                "projectile" => projectileIds,
                "damageType" => damageTypeIds,
                "element" => elementIds,
                "stat" => statKeys,
                _ => null
            };

            if (validIds is not null && !validIds.Contains(value))
            {
                var name = T(parameter.DisplayNameKey, string.IsNullOrWhiteSpace(parameter.DisplayName) ? parameter.Key : parameter.DisplayName);
                AddIssue(issues, categoryKey, categoryFallback, ownerId, string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.missingParameterRef", "参数“{0}”引用不存在：{1}"), name, value));
            }

            if (parameter.ValueType == EffectParameterValueType.TagList || string.Equals(parameter.OptionSourceId, "tag", StringComparison.OrdinalIgnoreCase))
            {
                CheckTags(issues, categoryKey, categoryFallback, ownerId, SplitValues(value), tagKeys);
            }
        }
    }

    private void CheckProducedAssetRef(List<string> issues, ProductionRuleDefinition production, HashSet<string> unitIds, HashSet<string> itemIds, HashSet<string> resourceRuleIds, HashSet<string> skillIds)
    {
        var targetIds = production.ProducedAssetKind switch
        {
            "unit" => unitIds,
            "item" => itemIds,
            "resource" => resourceRuleIds,
            "skill" => skillIds,
            "event" => null,
            _ => null
        };

        if (targetIds is not null)
        {
            CheckRef(issues, "asset.productionRules", "生产规则", production.Id, "dataEditor.field.producedAsset", "产出内容", production.ProducedAssetId, targetIds, optional: false);
        }
    }

    private void AddIssue(List<string> issues, string categoryKey, string categoryFallback, string ownerId, string message)
    {
        issues.Add(string.Format(CultureInfo.CurrentCulture, T("dataEditor.health.issueLine", "[{0}] {1}: {2}"), T(categoryKey, categoryFallback), string.IsNullOrWhiteSpace(ownerId) ? T("dataEditor.unnamed", "未命名") : ownerId, message));
    }

    private void UpdateActionButtons()
    {
        _deleteButton.Enabled = _selectedRecord is not null && !IsBuiltIn(_selectedRecord);
        _applyButton.Enabled = _selectedRecord is not null;
    }

    private List<FieldSpec> BuildFieldSpecs(object record)
    {
        var fields = new List<FieldSpec>();
        AddIdentityFields(fields, record);

        switch (record)
        {
            case StatDefinition stat:
                fields.Add(TextField("dataEditor.section.core", "核心设置", "dataEditor.field.statKey", "属性 Key", _ => stat.Key, (_, v) => stat.Key = v));
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.valueType", "值类型", _ => stat.ValueType, (_, v) => stat.ValueType = v, ValueTypeOptions));
                var defaultValueField = Number("dataEditor.section.core", "核心设置", "dataEditor.field.defaultValue", "默认值", _ => stat.DefaultValue, (_, v) => stat.DefaultValue = v);
                defaultValueField.UseIntegerNumericControl = obj => obj is FieldRowState state && IsStatIntegerValueType(state.FindSibling("dataEditor.field.valueType")?.TextValue);
                fields.Add(defaultValueField);

                var minValueField = Number("dataEditor.section.core", "核心设置", "dataEditor.field.min", "最小值", _ => stat.Min, (_, v) => stat.Min = v);
                minValueField.UseIntegerNumericControl = obj => obj is FieldRowState state && IsStatIntegerValueType(state.FindSibling("dataEditor.field.valueType")?.TextValue);
                fields.Add(minValueField);

                var maxValueField = Number("dataEditor.section.core", "核心设置", "dataEditor.field.max", "最大值", _ => stat.Max, (_, v) => stat.Max = v);
                maxValueField.UseIntegerNumericControl = obj => obj is FieldRowState state && IsStatIntegerValueType(state.FindSibling("dataEditor.field.valueType")?.TextValue);
                fields.Add(maxValueField);
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.category", "分类", _ => stat.CategoryKey, (_, v) => { stat.CategoryKey = v; stat.Category = T(v, v); }, StatCategoryOptions));
                break;
            case TraitDefinition trait:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.category", "分类", _ => trait.CategoryKey, (_, v) => { trait.CategoryKey = v; trait.Category = T(v, v); }, TraitCategoryOptions));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => trait.BuiltIn, (_, v) => trait.BuiltIn = v, readOnly: trait.BuiltIn));
                break;
            case FactionDefinition faction:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.attitudeToPlayer", "对玩家立场", _ => faction.AttitudeToPlayer, (_, v) => faction.AttitudeToPlayer = v, AttitudeOptions));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => faction.BuiltIn, (_, v) => faction.BuiltIn = v, readOnly: faction.BuiltIn));
                break;
            case DamageTypeDefinition damage:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.defenseStat", "防御属性", _ => damage.DefenseStatKey, (_, v) => damage.DefenseStatKey = v, StatReferenceOptions));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => damage.BuiltIn, (_, v) => damage.BuiltIn = v, readOnly: damage.BuiltIn));
                break;
            case ElementDefinition element:
                fields.Add(TextField("dataEditor.section.core", "核心设置", "dataEditor.field.colorHex", "颜色", _ => element.ColorHex, (_, v) => element.ColorHex = v));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => element.BuiltIn, (_, v) => element.BuiltIn = v, readOnly: element.BuiltIn));
                break;
            case OptionSetDefinition optionSet:
                fields.Add(StringMap("dataEditor.section.options", "选项", "dataEditor.field.optionValues", "选项列表", _ => optionSet.Values, (_, v) => optionSet.Values = v, () => OptionSetEntryOptions(optionSet), 220, allowCustomKeys: true));
                break;
            case UnitDefinition unit:
                AddUnitSharedFields(fields, unit, unit.UnitKind, (r, v) => unit.UnitKind = v, unit.FactionId, (r, v) => unit.FactionId = v, unit.AIProfileId, (r, v) => unit.AIProfileId = v, unit.LootTableId, (r, v) => unit.LootTableId = v, unit.InteractionProfileId, (r, v) => unit.InteractionProfileId = v, unit.Stats, v => unit.Stats = v, unit.Tags, v => unit.Tags = v, unit.Traits, v => unit.Traits = v, unit.Portrait, (r, v) => unit.Portrait = v, unit.Sprite, unit.Components, v => unit.Components = v, unit.Animations, v => unit.Animations = v);
                break;
            case AIProfileDefinition ai:
                fields.Add(Choice("dataEditor.section.ai", "AI 行为", "dataEditor.field.behaviorType", "行为类型", _ => ai.BehaviorType, (_, v) => ai.BehaviorType = v, BehaviorTypeOptions));
                fields.Add(Choice("dataEditor.section.ai", "AI 行为", "dataEditor.field.movementMode", "移动模式", _ => ai.MovementMode, (_, v) => ai.MovementMode = v, MovementModeOptions));
                fields.Add(Choice("dataEditor.section.ai", "AI 行为", "dataEditor.field.targetSelector", "目标选择", _ => ai.TargetSelector, (_, v) => ai.TargetSelector = v, TargetSelectorOptions));
        fields.Add(MultiChoice("dataEditor.section.ai", "AI 行为", "dataEditor.field.targetTags", "目标标签", _ => FormatList(ai.TargetTags), (_, v) => ai.TargetTags = SplitValues(v), TargetTagOptions, 112));
                fields.Add(MultiChoice("dataEditor.section.ai", "AI 行为", "dataEditor.field.skills", "可用技能", _ => FormatList(ai.SkillIds), (_, v) => ai.SkillIds = SplitValues(v), SkillOptions, 112));
                fields.Add(Number("dataEditor.section.ai", "AI 行为", "dataEditor.field.perceptionRange", "感知范围", _ => ai.PerceptionRange, (_, v) => ai.PerceptionRange = v));
                fields.Add(Number("dataEditor.section.ai", "AI 行为", "dataEditor.field.leashRange", "脱战距离", _ => ai.LeashRange, (_, v) => ai.LeashRange = v));
                fields.Add(Number("dataEditor.section.ai", "AI 行为", "dataEditor.field.preferredRange", "偏好距离", _ => ai.PreferredRange, (_, v) => ai.PreferredRange = v));
                fields.Add(Number("dataEditor.section.ai", "AI 行为", "dataEditor.field.fleeHealth", "逃跑血量比例", _ => ai.FleeHealthPercent, (_, v) => ai.FleeHealthPercent = v, min: 0, max: 1));
                fields.Add(Choice("dataEditor.section.ai", "AI 行为", "dataEditor.field.patrolMode", "巡逻模式", _ => ai.PatrolMode, (_, v) => ai.PatrolMode = v, PatrolModeOptions));
                fields.Add(Multiline("dataEditor.section.parameters", "参数", "dataEditor.field.parameters", "参数", _ => FormatStringMap(ai.Parameters), (_, v) => ai.Parameters = ParseStringMap(v), 92, "dataEditor.hint.keyValue", "每行一个 key = value。"));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => ai.BuiltIn, (_, v) => ai.BuiltIn = v, readOnly: ai.BuiltIn));
                break;
            case SkillDefinition skill:
                AddSkillFields(fields, skill);
                break;
            case GameplayEffectDefinition effect:
                AddGameplayEffectFields(fields, effect);
                break;
            case StatusDefinition status:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.statusKind", "状态类型", _ => status.StatusKind, (_, v) => status.StatusKind = v, StatusKindOptions));
                fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.duration", "持续时间", _ => status.DurationSeconds, (_, v) => status.DurationSeconds = v, min: 0));
                fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.maxStacks", "最大层数", _ => status.MaxStacks, (_, v) => status.MaxStacks = v, min: 1, max: 999));
                fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.tickInterval", "间隔触发", _ => status.TickIntervalSeconds, (_, v) => status.TickIntervalSeconds = v, min: 0));
                fields.Add(EffectRefs("dataEditor.section.effects", "效果引用", "dataEditor.field.onApplyEffects", "添加时效果", _ => status.OnApplyEffects, (_, v) => status.OnApplyEffects = v, 180));
                fields.Add(EffectRefs("dataEditor.section.effects", "效果引用", "dataEditor.field.periodicEffects", "周期效果", _ => status.PeriodicEffects, (_, v) => status.PeriodicEffects = v, 180));
                fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(status.Tags), (_, v) => status.Tags = SplitValues(v), StatusTagOptions, 112));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => status.BuiltIn, (_, v) => status.BuiltIn = v, readOnly: status.BuiltIn));
                break;
            case ProjectileDefinition projectile:
                fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.speed", "速度", _ => projectile.Speed, (_, v) => projectile.Speed = v, min: 0, max: 99999));
                fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.lifetime", "存在时间", _ => projectile.LifetimeSeconds, (_, v) => projectile.LifetimeSeconds = v, min: 0));
                fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.radius", "半径", _ => projectile.Radius, (_, v) => projectile.Radius = v, min: 0));
                fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.piercing", "穿透", _ => projectile.Piercing, (_, v) => projectile.Piercing = v));
                fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.visualEffect", "视觉特效", _ => projectile.VisualEffectId, (_, v) => projectile.VisualEffectId = v, VisualEffectOptions));
                fields.Add(EffectRefs("dataEditor.section.effects", "效果引用", "dataEditor.field.effects", "命中效果", _ => projectile.Effects, (_, v) => projectile.Effects = v, 180));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => projectile.BuiltIn, (_, v) => projectile.BuiltIn = v, readOnly: projectile.BuiltIn));
                break;
            case VisualEffectDefinition vfx:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.visualEffectKind", "表现类型", _ => vfx.EffectKind, (_, v) => vfx.EffectKind = v, VisualEffectKindOptions));
                fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.spriteSheet", "精灵表", _ => vfx.SpriteSheet, (_, v) => vfx.SpriteSheet = v));
                fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.animationKey", "动画 Key", _ => vfx.AnimationKey, (_, v) => vfx.AnimationKey = v));
                fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.soundCue", "音效 Cue", _ => vfx.SoundCue, (_, v) => vfx.SoundCue = v));
                fields.Add(Number("dataEditor.section.presentation", "表现资源", "dataEditor.field.duration", "持续时间", _ => vfx.DurationSeconds, (_, v) => vfx.DurationSeconds = v, min: 0));
                fields.Add(Bool("dataEditor.section.presentation", "表现资源", "dataEditor.field.loop", "循环", _ => vfx.Loop, (_, v) => vfx.Loop = v));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => vfx.BuiltIn, (_, v) => vfx.BuiltIn = v, readOnly: vfx.BuiltIn));
                break;
            case ItemDefinition item:
                AddItemFields(fields, item);
                break;
            case ItemTypeDefinition itemType:
                fields.Add(Multiline("dataEditor.section.fields", "字段模板", "dataEditor.field.itemTypeFields", "字段定义", _ => FormatItemFields(itemType.Fields), (_, v) => itemType.Fields = ParseItemFields(v), 180, "dataEditor.hint.itemTypeFields", "格式：key | name=显示名 | type=Text | default=值 | required=false | readonly=false | category=分类Key | order=1 | options=a,b"));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => itemType.BuiltIn, (_, v) => itemType.BuiltIn = v, readOnly: itemType.BuiltIn));
                break;
            case LootTableDefinition loot:
                fields.Add(Multiline("dataEditor.section.entries", "掉落条目", "dataEditor.field.lootEntries", "掉落条目", _ => FormatLootEntries(loot.Entries), (_, v) => loot.Entries = ParseLootEntries(v), 180, "dataEditor.hint.lootEntries", "格式：item.id | min=1 | max=2 | chance=0.5"));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => loot.BuiltIn, (_, v) => loot.BuiltIn = v, readOnly: loot.BuiltIn));
                break;
            case ComponentPresetDefinition componentPreset:
                fields.Add(TextField("dataEditor.section.component", "组件", "dataEditor.field.componentType", "组件类型", _ => componentPreset.Component.Type, (_, v) => componentPreset.Component.Type = v));
                fields.Add(Multiline("dataEditor.section.component", "组件", "dataEditor.field.parameters", "参数", _ => FormatObjectMap(componentPreset.Component.Parameters), (_, v) => componentPreset.Component.Parameters = ParseObjectMap(v), 100, "dataEditor.hint.keyValue", "每行一个 key = value。"));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => componentPreset.BuiltIn, (_, v) => componentPreset.BuiltIn = v, readOnly: componentPreset.BuiltIn));
                break;
            case DecorationDefinition decor:
                AddDecorationFields(fields, decor);
                break;
            case InteractionProfileDefinition interaction:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.interactionKind", "交互类型", _ => interaction.InteractionKind, (_, v) => interaction.InteractionKind = v, InteractionKindOptions));
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.triggerName", "触发名称", _ => interaction.TriggerName, (_, v) => interaction.TriggerName = v, InteractionTriggerOptions));
                fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => interaction.EventGraphId, (_, v) => interaction.EventGraphId = v, EventGraphOptions));
                fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.onceOnly", "只触发一次", _ => interaction.OnceOnly, (_, v) => interaction.OnceOnly = v));
                fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => interaction.BuiltIn, (_, v) => interaction.BuiltIn = v, readOnly: interaction.BuiltIn));
                break;
            case MapDefinition map:
                fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.viewType", "视角类型", _ => map.ViewType, (_, v) => map.ViewType = v, ViewTypeOptions));
                fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.width", "宽度", _ => map.Width, (_, v) => map.Width = v, min: 8, max: 512));
                fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.height", "高度", _ => map.Height, (_, v) => map.Height = v, min: 8, max: 512));
                fields.Add(TextField("dataEditor.section.core", "核心设置", "dataEditor.field.tileset", "图块集", _ => map.Tileset, (_, v) => map.Tileset = v));
                break;
            case TowerDefensePathDefinition path:
                AddTowerDefensePathFields(fields, path);
                break;
            case TowerDefenseWaveDefinition wave:
                AddTowerDefenseWaveFields(fields, wave);
                break;
            case TowerDefenseRuleDefinition rule:
                AddTowerDefenseRuleFields(fields, rule);
                break;
            case TowerDefenseBuildRuleDefinition buildRule:
                AddTowerDefenseBuildRuleFields(fields, buildRule);
                break;
            case TowerDefenseTowerDefinition tower:
                AddTowerDefenseTowerFields(fields, tower);
                break;
            case TacticalGridRuleDefinition tacticalGridRule:
                AddTacticalGridRuleFields(fields, tacticalGridRule);
                break;
            case TerrainRuleDefinition terrainRule:
                AddTerrainRuleFields(fields, terrainRule);
                break;
            case TurnRuleDefinition turnRule:
                AddTurnRuleFields(fields, turnRule);
                break;
            case ActionRuleDefinition actionRule:
                AddActionRuleFields(fields, actionRule);
                break;
            case TacticalRangeDefinition tacticalRange:
                AddTacticalRangeFields(fields, tacticalRange);
                break;
            case ObjectiveRuleDefinition objectiveRule:
                AddObjectiveRuleFields(fields, objectiveRule);
                break;
            case BondRuleDefinition bondRule:
                AddBondRuleFields(fields, bondRule);
                break;
            case ResourceRuleDefinition resourceRule:
                AddResourceRuleFields(fields, resourceRule);
                break;
            case ProductionRuleDefinition productionRule:
                AddProductionRuleFields(fields, productionRule);
                break;
            case TechRuleDefinition techRule:
                AddTechRuleFields(fields, techRule);
                break;
            case DiplomacyRuleDefinition diplomacyRule:
                AddDiplomacyRuleFields(fields, diplomacyRule);
                break;
            case TerritoryRuleDefinition territoryRule:
                AddTerritoryRuleFields(fields, territoryRule);
                break;
        }

        return fields;
    }

    private void AddIdentityFields(List<FieldSpec> fields, object record)
    {
        var idProperty = record.GetType().GetProperty("Id");
        if (idProperty is not null)
        {
            fields.Add(TextField("dataEditor.section.identity", "基础信息", "table.assetId", "ID", _ => GetString(record, "Id"), (_, v) => SetString(record, "Id", v)));
        }
        var nameField = TextField("dataEditor.section.identity", "基础信息", "inspector.entity.name", "名称", _ => GetString(record, "DisplayName"), (_, v) => SetString(record, "DisplayName", v));
        nameField.ReadDisplayText = Localized;
        fields.Add(nameField);
        fields.Add(TextField("dataEditor.section.identity", "基础信息", "inspector.entity.nameKey", "名称 Key", _ => GetString(record, "DisplayNameKey"), (_, v) => SetString(record, "DisplayNameKey", v)));
        if (record.GetType().GetProperty("Description") is not null)
        {
            var descriptionField = Multiline("dataEditor.section.identity", "基础信息", "inspector.entity.description", "描述", _ => GetString(record, "Description"), (_, v) => SetString(record, "Description", v), 68);
            descriptionField.ReadDisplayText = LocalizedDescription;
            fields.Add(descriptionField);
        }
        if (record.GetType().GetProperty("DescriptionKey") is not null)
        {
            fields.Add(TextField("dataEditor.section.identity", "基础信息", "inspector.entity.descriptionKey", "描述 Key", _ => GetString(record, "DescriptionKey"), (_, v) => SetString(record, "DescriptionKey", v)));
        }
    }

    private void AddUnitSharedFields(
        List<FieldSpec> fields,
        object record,
        string unitKind,
        Action<object, string> setUnitKind,
        string factionId,
        Action<object, string> setFaction,
        string aiProfileId,
        Action<object, string> setAi,
        string lootTableId,
        Action<object, string> setLoot,
        string interactionId,
        Action<object, string> setInteraction,
        Dictionary<string, double> stats,
        Action<Dictionary<string, double>> setStats,
        List<string> tags,
        Action<List<string>> setTags,
        List<string> traits,
        Action<List<string>> setTraits,
        string portrait,
        Action<object, string> setPortrait,
        SpriteSheetConfig sprite,
        List<ComponentConfig> components,
        Action<List<ComponentConfig>> setComponents,
        Dictionary<string, string> animations,
        Action<Dictionary<string, string>> setAnimations)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.unitKind", "单位类型", _ => unitKind, setUnitKind, UnitKindOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.faction", "阵营", _ => factionId, setFaction, FactionOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.aiProfile", "AI 预设", _ => aiProfileId, setAi, AIProfileOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.lootTable", "掉落表", _ => lootTableId, setLoot, LootTableOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.interactionProfile", "交互入口", _ => interactionId, setInteraction, InteractionOptions));
        fields.Add(DoubleMap("dataEditor.section.stats", "属性", "inspector.entity.stats", "属性值", _ => BuildStatEditorValues(stats), (_, v) => setStats(v), StatReferenceOptions, 190));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(tags), (_, v) => setTags(SplitValues(v)), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.traits", "特性", _ => FormatList(traits), (_, v) => setTraits(SplitValues(v)), TraitOptions, 112));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.portrait", "头像/立绘", _ => portrait, setPortrait));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.spriteSheet", "精灵表", _ => sprite.Sheet, (_, v) => sprite.Sheet = v));
        fields.Add(Integer("dataEditor.section.presentation", "表现资源", "dataEditor.field.tileWidth", "单元格宽度", _ => sprite.TileWidth, (_, v) => sprite.TileWidth = v, min: 1, max: 4096));
        fields.Add(Integer("dataEditor.section.presentation", "表现资源", "dataEditor.field.tileHeight", "单元格高度", _ => sprite.TileHeight, (_, v) => sprite.TileHeight = v, min: 1, max: 4096));
        fields.Add(Integer("dataEditor.section.presentation", "表现资源", "dataEditor.field.pivotX", "枢轴 X", _ => sprite.PivotX, (_, v) => sprite.PivotX = v, min: -4096, max: 4096));
        fields.Add(Integer("dataEditor.section.presentation", "表现资源", "dataEditor.field.pivotY", "枢轴 Y", _ => sprite.PivotY, (_, v) => sprite.PivotY = v, min: -4096, max: 4096));
        fields.Add(Components("dataEditor.section.components", "能力组件", "dataEditor.field.components", "组件", _ => components, (_, v) => setComponents(v), 170));
        fields.Add(StringMap("dataEditor.section.presentation", "表现资源", "dataEditor.field.animations", "动画映射", _ => animations, (_, v) => setAnimations(v), AnimationKeyOptions, 154));
    }

    private void AddSkillFields(List<FieldSpec> fields, SkillDefinition skill)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.skillType", "技能类型", _ => skill.SkillType, (_, v) => skill.SkillType = v, SkillTypeOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.targetingMode", "目标方式", _ => skill.TargetingMode, (_, v) => skill.TargetingMode = v, TargetingModeOptions));
        fields.Add(MultiChoice("dataEditor.section.targeting", "目标筛选", "dataEditor.field.requiredTargetTags", "必须目标标签", _ => FormatList(skill.RequiredTargetTags), (_, v) => skill.RequiredTargetTags = SplitValues(v), TargetTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.targeting", "目标筛选", "dataEditor.field.blockedTargetTags", "排除目标标签", _ => FormatList(skill.BlockedTargetTags), (_, v) => skill.BlockedTargetTags = SplitValues(v), TargetTagOptions, 112));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.element", "元素", _ => skill.ElementId, (_, v) => skill.ElementId = v, ElementOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.damageType", "伤害类型", _ => skill.DamageTypeId, (_, v) => skill.DamageTypeId = v, DamageTypeOptions));
        fields.Add(Choice("dataEditor.section.numbers", "数值", "dataEditor.field.powerStat", "威力属性", _ => skill.PowerStatKey, (_, v) => skill.PowerStatKey = v, StatReferenceOptions));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.powerMultiplier", "倍率", _ => skill.PowerMultiplier, (_, v) => skill.PowerMultiplier = v, min: -9999, max: 9999));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.basePower", "基础值", _ => skill.BasePower, (_, v) => skill.BasePower = v, min: -999999, max: 999999));
        fields.Add(Choice("dataEditor.section.numbers", "数值", "dataEditor.field.costStat", "消耗属性", _ => skill.CostStatKey, (_, v) => skill.CostStatKey = v, StatReferenceOptions));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.costAmount", "消耗量", _ => skill.CostAmount, (_, v) => skill.CostAmount = v, min: 0, max: 999999));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.castTime", "施法时间", _ => skill.CastTimeSeconds, (_, v) => skill.CastTimeSeconds = v, min: 0));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.cooldown", "冷却", _ => skill.CooldownSeconds, (_, v) => skill.CooldownSeconds = v, min: 0));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.range", "射程", _ => skill.Range, (_, v) => skill.Range = v, min: 0));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.areaRadius", "范围半径", _ => skill.AreaRadius, (_, v) => skill.AreaRadius = v, min: 0));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.projectile", "投射物", _ => skill.ProjectileId, (_, v) => skill.ProjectileId = v, ProjectileOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.formula", "公式", _ => skill.FormulaId, (_, v) => skill.FormulaId = v, FormulaOptions));
        fields.Add(EffectRefs("dataEditor.section.effects", "效果引用", "dataEditor.field.effects", "效果", _ => skill.Effects, (_, v) => skill.Effects = v, 220));
        fields.Add(MultiChoice("dataEditor.section.effects", "效果引用", "dataEditor.field.statuses", "状态", _ => FormatList(skill.StatusIds), (_, v) => skill.StatusIds = SplitValues(v), StatusOptions, 112));
        fields.Add(Choice("dataEditor.section.presentation", "表现资源", "dataEditor.field.visualEffect", "视觉特效", _ => skill.VisualEffectId, (_, v) => skill.VisualEffectId = v, VisualEffectOptions));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.soundCue", "音效 Cue", _ => skill.SoundCue, (_, v) => skill.SoundCue = v));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(SplitValues(skill.Tags)), (_, v) => skill.Tags = FormatTagString(v), SkillTagOptions, 112));
    }

    private void AddGameplayEffectFields(List<FieldSpec> fields, GameplayEffectDefinition effect)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.effectKind", "效果类型", _ => effect.EffectKind, (_, v) => effect.EffectKind = v, EffectKindOptions));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.iconPath", "图标", _ => effect.IconPath, (_, v) => effect.IconPath = v));
        fields.Add(EffectParameters("dataEditor.section.fields", "参数模板", "dataEditor.field.effectParameters", "参数模板", _ => effect.Parameters, (_, v) => effect.Parameters = v, 220));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(effect.Tags), (_, v) => effect.Tags = SplitValues(v), EffectTagOptions, 112));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => effect.BuiltIn, (_, v) => effect.BuiltIn = v, readOnly: effect.BuiltIn));
    }

    private void AddItemFields(List<FieldSpec> fields, ItemDefinition item)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.itemType", "物品类型", _ => item.TypeId, (_, v) => item.TypeId = v, ItemTypeOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.rarity", "稀有度", _ => item.Rarity, (_, v) => item.Rarity = v, RarityOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.equipmentSlot", "装备槽", _ => item.EquipmentSlot, (_, v) => item.EquipmentSlot = v, EquipmentSlotOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.price", "价格", _ => item.Price, (_, v) => item.Price = v, min: 0, max: 9999999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.stackLimit", "堆叠上限", _ => item.StackLimit, (_, v) => item.StackLimit = v, min: 1, max: 99999));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.consumable", "可消耗", _ => item.Consumable, (_, v) => item.Consumable = v));
        fields.Add(EffectRefs("dataEditor.section.effects", "效果引用", "dataEditor.field.effects", "效果", _ => item.Effects, (_, v) => item.Effects = v, 220));
        fields.Add(MultiChoice("dataEditor.section.effects", "效果引用", "dataEditor.field.grantedSkills", "授予技能", _ => FormatList(item.GrantedSkillIds), (_, v) => item.GrantedSkillIds = SplitValues(v), SkillOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(SplitValues(item.Tags)), (_, v) => item.Tags = FormatTagString(v), ItemTagOptions, 112));
        fields.Add(StringMap(
            "dataEditor.section.parameters",
            "参数",
            "dataEditor.field.customValues",
            "类型字段值",
            _ => BuildItemCustomValueMap(item.CustomValues),
            (_, v) => item.CustomValues = BuildItemCustomValues(v, item.TypeId),
            () => ItemFieldOptions(item.TypeId),
            180));
    }

    private void AddDecorationFields(List<FieldSpec> fields, DecorationDefinition decor)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.decorationKind", "装饰类型", _ => decor.DecorationKind, (_, v) => decor.DecorationKind = v, DecorationKindOptions));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.spriteSheet", "精灵表", _ => decor.SpriteSheet, (_, v) => decor.SpriteSheet = v));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.animationKey", "动画 Key", _ => decor.AnimationKey, (_, v) => decor.AnimationKey = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.blocksMovement", "阻挡移动", _ => decor.BlocksMovement, (_, v) => decor.BlocksMovement = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.destructible", "可破坏", _ => decor.Destructible, (_, v) => decor.Destructible = v));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.lootTable", "掉落表", _ => decor.LootTableId, (_, v) => decor.LootTableId = v, LootTableOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.interactionProfile", "交互入口", _ => decor.InteractionProfileId, (_, v) => decor.InteractionProfileId = v, InteractionOptions));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(decor.Tags), (_, v) => decor.Tags = SplitValues(v), DecorationTagOptions, 112));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => decor.BuiltIn, (_, v) => decor.BuiltIn = v, readOnly: decor.BuiltIn));
    }

    private void AddTowerDefensePathFields(List<FieldSpec> fields, TowerDefensePathDefinition path)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.map", "地图", _ => path.MapId, (_, v) => path.MapId = v, MapOptions));
        fields.Add(TextField("dataEditor.section.core", "核心设置", "dataEditor.field.spawnPoint", "出生点", _ => path.SpawnPointId, (_, v) => path.SpawnPointId = v));
        fields.Add(TextField("dataEditor.section.core", "核心设置", "dataEditor.field.goalPoint", "终点", _ => path.GoalPointId, (_, v) => path.GoalPointId = v));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.pathMode", "路线模式", _ => path.PathMode, (_, v) => path.PathMode = v, TowerDefensePathModeOptions));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowBranching", "允许分支", _ => path.AllowBranching, (_, v) => path.AllowBranching = v));
        fields.Add(TowerDefenseWaypoints("dataEditor.section.path", "路线", "dataEditor.field.waypoints", "路点", _ => path.Waypoints, (_, v) => path.Waypoints = v, 220));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => path.BuiltIn, (_, v) => path.BuiltIn = v, readOnly: path.BuiltIn));
    }

    private void AddTowerDefenseWaveFields(List<FieldSpec> fields, TowerDefenseWaveDefinition wave)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.map", "地图", _ => wave.MapId, (_, v) => wave.MapId = v, MapOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.path", "路线", _ => wave.PathId, (_, v) => wave.PathId = v, TowerDefensePathOptions));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.startDelay", "开始延迟", _ => wave.StartDelaySeconds, (_, v) => wave.StartDelaySeconds = v, min: 0));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.spawnInterval", "生成间隔", _ => wave.SpawnIntervalSeconds, (_, v) => wave.SpawnIntervalSeconds = v, min: 0));
        fields.Add(Integer("dataEditor.section.rewards", "奖励", "dataEditor.field.rewardGold", "金币奖励", _ => wave.RewardGold, (_, v) => wave.RewardGold = v, min: 0, max: 9999999));
        fields.Add(Integer("dataEditor.section.rewards", "奖励", "dataEditor.field.rewardExp", "经验奖励", _ => wave.RewardExp, (_, v) => wave.RewardExp = v, min: 0, max: 9999999));
        fields.Add(TowerDefenseSpawnGroups("dataEditor.section.wave", "波次", "dataEditor.field.spawnGroups", "刷怪组", _ => wave.SpawnGroups, (_, v) => wave.SpawnGroups = v, 220));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => wave.BuiltIn, (_, v) => wave.BuiltIn = v, readOnly: wave.BuiltIn));
    }

    private void AddTowerDefenseRuleFields(List<FieldSpec> fields, TowerDefenseRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.map", "地图", _ => rule.MapId, (_, v) => rule.MapId = v, MapOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.startingGold", "初始金币", _ => rule.StartingGold, (_, v) => rule.StartingGold = v, min: 0, max: 9999999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.baseLife", "基地生命", _ => rule.BaseLife, (_, v) => rule.BaseLife = v, min: 1, max: 9999999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.leakDamage", "漏怪伤害", _ => rule.LeakDamagePerUnit, (_, v) => rule.LeakDamagePerUnit = v, min: 0, max: 9999999));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.buildRule", "建造规则", _ => rule.BuildRuleId, (_, v) => rule.BuildRuleId = v, TowerDefenseBuildRuleOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.waveStartMode", "开波方式", _ => rule.WaveStartMode, (_, v) => rule.WaveStartMode = v, TowerDefenseWaveStartModeOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.victoryCondition", "胜利条件", _ => rule.VictoryCondition, (_, v) => rule.VictoryCondition = v, TowerDefenseVictoryConditionOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.defeatCondition", "失败条件", _ => rule.DefeatCondition, (_, v) => rule.DefeatCondition = v, TowerDefenseDefeatConditionOptions));
        fields.Add(TowerDefenseWaveRefs("dataEditor.section.wave", "波次", "dataEditor.field.waves", "波次列表", _ => rule.WaveIds, (_, v) => rule.WaveIds = v, 220));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTowerDefenseBuildRuleFields(List<FieldSpec> fields, TowerDefenseBuildRuleDefinition buildRule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.buildSurfaceTag", "建造区域标签", _ => buildRule.BuildSurfaceTag, (_, v) => buildRule.BuildSurfaceTag = v, AreaTagOptions));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.preventPathBlocking", "禁止堵路", _ => buildRule.PreventPathBlocking, (_, v) => buildRule.PreventPathBlocking = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowSell", "允许出售", _ => buildRule.AllowSell, (_, v) => buildRule.AllowSell = v));
        fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.sellRefundRatio", "出售返还比例", _ => buildRule.SellRefundRatio, (_, v) => buildRule.SellRefundRatio = v, min: 0, max: 1));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowUpgradeDuringWave", "波次中允许升级", _ => buildRule.AllowUpgradeDuringWave, (_, v) => buildRule.AllowUpgradeDuringWave = v));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.currencyStat", "货币属性", _ => buildRule.CurrencyStatKey, (_, v) => buildRule.CurrencyStatKey = v, StatReferenceOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => buildRule.BuiltIn, (_, v) => buildRule.BuiltIn = v, readOnly: buildRule.BuiltIn));
    }

    private void AddTowerDefenseTowerFields(List<FieldSpec> fields, TowerDefenseTowerDefinition tower)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.unit", "单位", _ => tower.UnitId, (_, v) => tower.UnitId = v, UnitOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.skill", "技能", _ => tower.SkillId, (_, v) => tower.SkillId = v, SkillOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.buildableRole", "可建造单位定位", _ => tower.TowerRole, (_, v) => tower.TowerRole = v, TowerDefenseTowerRoleOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.buildCost", "建造费用", _ => tower.BuildCost, (_, v) => tower.BuildCost = v, min: 0, max: 9999999));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.range", "射程", _ => tower.Range, (_, v) => tower.Range = v, min: 0));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.attackInterval", "攻击间隔", _ => tower.AttackIntervalSeconds, (_, v) => tower.AttackIntervalSeconds = v, min: 0));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.targetPriority", "目标优先级", _ => tower.TargetPriority, (_, v) => tower.TargetPriority = v, TowerDefenseTargetPriorityOptions));
        fields.Add(TowerDefenseTowerLevels("dataEditor.section.levels", "升级等级", "dataEditor.field.levels", "等级", _ => tower.Levels, (_, v) => tower.Levels = v, 220));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => tower.BuiltIn, (_, v) => tower.BuiltIn = v, readOnly: tower.BuiltIn));
    }

    private void AddTacticalGridRuleFields(List<FieldSpec> fields, TacticalGridRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.gridType", "格子类型", _ => rule.GridType, (_, v) => rule.GridType = v, GridTypeOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.tileSize", "格子尺寸", _ => rule.TileSize, (_, v) => rule.TileSize = v, min: 1, max: 4096));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.movementMetric", "移动计算", _ => rule.MovementMetric, (_, v) => rule.MovementMetric = v, MovementMetricOptions));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowDiagonalMove", "允许斜向移动", _ => rule.AllowDiagonalMove, (_, v) => rule.AllowDiagonalMove = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.heightEnabled", "启用高低差", _ => rule.HeightEnabled, (_, v) => rule.HeightEnabled = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.zoneOfControlEnabled", "启用控制区", _ => rule.ZoneOfControlEnabled, (_, v) => rule.ZoneOfControlEnabled = v));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTerrainRuleFields(List<FieldSpec> fields, TerrainRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.terrainTag", "地形标签", _ => rule.TerrainTag, (_, v) => rule.TerrainTag = v, TerrainTagOptions));
        fields.Add(Number("dataEditor.section.core", "核心设置", "dataEditor.field.movementCost", "移动消耗", _ => rule.MovementCost, (_, v) => rule.MovementCost = v, min: 0, max: 999));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.blocksMovement", "阻挡移动", _ => rule.BlocksMovement, (_, v) => rule.BlocksMovement = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.blocksLineOfSight", "阻挡视线", _ => rule.BlocksLineOfSight, (_, v) => rule.BlocksLineOfSight = v));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.defenseBonus", "防御加成", _ => rule.DefenseBonus, (_, v) => rule.DefenseBonus = v, min: -10, max: 10));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.evasionBonus", "闪避加成", _ => rule.EvasionBonus, (_, v) => rule.EvasionBonus = v, min: -10, max: 10));
        fields.Add(Number("dataEditor.section.numbers", "数值", "dataEditor.field.damageModifier", "伤害修正", _ => rule.DamageModifier, (_, v) => rule.DamageModifier = v, min: 0, max: 100));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.allowedUnitTags", "允许单位标签", _ => FormatList(rule.AllowedUnitTags), (_, v) => rule.AllowedUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.forbiddenUnitTags", "禁止单位标签", _ => FormatList(rule.ForbiddenUnitTags), (_, v) => rule.ForbiddenUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTurnRuleFields(List<FieldSpec> fields, TurnRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.turnMode", "回合模式", _ => rule.TurnMode, (_, v) => rule.TurnMode = v, TurnModeOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.maxRounds", "最大回合", _ => rule.MaxRounds, (_, v) => rule.MaxRounds = v, min: 0, max: 9999));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.actionRefreshMode", "行动刷新", _ => rule.ActionRefreshMode, (_, v) => rule.ActionRefreshMode = v, ActionRefreshModeOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.initiativeStat", "先攻属性", _ => rule.InitiativeStatKey, (_, v) => rule.InitiativeStatKey = v, StatReferenceOptions));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowWait", "允许待机", _ => rule.AllowWait, (_, v) => rule.AllowWait = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowUndoMove", "允许撤销移动", _ => rule.AllowUndoMove, (_, v) => rule.AllowUndoMove = v));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddActionRuleFields(List<FieldSpec> fields, ActionRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.actionPointStat", "行动点属性", _ => rule.ActionPointStatKey, (_, v) => rule.ActionPointStatKey = v, StatReferenceOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.movePointStat", "移动力属性", _ => rule.MovePointStatKey, (_, v) => rule.MovePointStatKey = v, StatReferenceOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.defaultActionPoints", "默认行动点", _ => rule.DefaultActionPoints, (_, v) => rule.DefaultActionPoints = v, min: 0, max: 99));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.defaultMovePoints", "默认移动力", _ => rule.DefaultMovePoints, (_, v) => rule.DefaultMovePoints = v, min: 0, max: 99));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.moveConsumesAction", "移动消耗行动", _ => rule.MoveConsumesAction, (_, v) => rule.MoveConsumesAction = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.attackConsumesAction", "攻击消耗行动", _ => rule.AttackConsumesAction, (_, v) => rule.AttackConsumesAction = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canAttackAfterMove", "移动后可攻击", _ => rule.CanAttackAfterMove, (_, v) => rule.CanAttackAfterMove = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canMoveAfterAttack", "攻击后可移动", _ => rule.CanMoveAfterAttack, (_, v) => rule.CanMoveAfterAttack = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.waitEndsTurn", "待机结束回合", _ => rule.WaitEndsTurn, (_, v) => rule.WaitEndsTurn = v));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTacticalRangeFields(List<FieldSpec> fields, TacticalRangeDefinition range)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.rangeShape", "范围形状", _ => range.RangeShape, (_, v) => range.RangeShape = v, RangeShapeOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.minRange", "最小射程", _ => range.MinRange, (_, v) => range.MinRange = v, min: 0, max: 999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.maxRange", "最大射程", _ => range.MaxRange, (_, v) => range.MaxRange = v, min: 0, max: 999));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.areaShape", "影响区域", _ => range.AreaShape, (_, v) => range.AreaShape = v, AreaShapeOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.areaRadius", "区域半径", _ => range.AreaRadius, (_, v) => range.AreaRadius = v, min: 0, max: 999));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.requiresLineOfSight", "需要视线", _ => range.RequiresLineOfSight, (_, v) => range.RequiresLineOfSight = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canTargetSelf", "可选自身", _ => range.CanTargetSelf, (_, v) => range.CanTargetSelf = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canTargetAlly", "可选友方", _ => range.CanTargetAlly, (_, v) => range.CanTargetAlly = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canTargetEnemy", "可选敌方", _ => range.CanTargetEnemy, (_, v) => range.CanTargetEnemy = v));
        fields.Add(MultiChoice("dataEditor.section.targeting", "目标筛选", "dataEditor.field.requiredTargetTags", "必须目标标签", _ => FormatList(range.RequiredTargetTags), (_, v) => range.RequiredTargetTags = SplitValues(v), TargetTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.targeting", "目标筛选", "dataEditor.field.blockedTargetTags", "排除目标标签", _ => FormatList(range.BlockedTargetTags), (_, v) => range.BlockedTargetTags = SplitValues(v), TargetTagOptions, 112));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.terrainBlocked", "受地形阻挡", _ => range.TerrainBlocked, (_, v) => range.TerrainBlocked = v));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => range.BuiltIn, (_, v) => range.BuiltIn = v, readOnly: range.BuiltIn));
    }

    private void AddObjectiveRuleFields(List<FieldSpec> fields, ObjectiveRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.objectiveType", "目标类型", _ => rule.ObjectiveType, (_, v) => rule.ObjectiveType = v, ObjectiveTypeOptions));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.isVictoryCondition", "胜利条件", _ => rule.IsVictoryCondition, (_, v) => rule.IsVictoryCondition = v));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.targetUnitTags", "目标单位标签", _ => FormatList(rule.TargetUnitTags), (_, v) => rule.TargetUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.targetAreaTags", "目标区域标签", _ => FormatList(rule.TargetAreaTags), (_, v) => rule.TargetAreaTags = SplitValues(v), AreaTagOptions, 112));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.requiredCount", "需要数量", _ => rule.RequiredCount, (_, v) => rule.RequiredCount = v, min: 0, max: 999999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.roundLimit", "回合限制", _ => rule.RoundLimit, (_, v) => rule.RoundLimit = v, min: 0, max: 9999));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => rule.EventGraphId, (_, v) => rule.EventGraphId = v, EventGraphOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddBondRuleFields(List<FieldSpec> fields, BondRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.bondTriggerTiming", "触发时机", _ => rule.TriggerTiming, (_, v) => rule.TriggerTiming = v, BondTriggerTimingOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.bondRange", "羁绊范围", _ => rule.Range, (_, v) => rule.Range = v, min: 0, max: 999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.minParticipants", "最少参与者", _ => rule.MinParticipants, (_, v) => rule.MinParticipants = v, min: 1, max: 99));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.maxParticipants", "最多参与者", _ => rule.MaxParticipants, (_, v) => rule.MaxParticipants = v, min: 0, max: 99));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.requireSameFaction", "要求同阵营", _ => rule.RequireSameFaction, (_, v) => rule.RequireSameFaction = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.requireLineOfSight", "要求视线", _ => rule.RequireLineOfSight, (_, v) => rule.RequireLineOfSight = v));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.requiredUnitTags", "需要单位标签", _ => FormatList(rule.RequiredUnitTags), (_, v) => rule.RequiredUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.excludedUnitTags", "排除单位标签", _ => FormatList(rule.ExcludedUnitTags), (_, v) => rule.ExcludedUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.effects", "效果引用", "dataEditor.field.effects", "效果", _ => FormatList(rule.EffectIds), (_, v) => rule.EffectIds = SplitValues(v), GameplayEffectOptions, 112));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.bondDurationMode", "持续方式", _ => rule.DurationMode, (_, v) => rule.DurationMode = v, BondDurationModeOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.stackingMode", "叠加方式", _ => rule.StackingMode, (_, v) => rule.StackingMode = v, StackingModeOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddResourceRuleFields(List<FieldSpec> fields, ResourceRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.resourceKind", "资源类型", _ => rule.ResourceKind, (_, v) => rule.ResourceKind = v, ResourceKindOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.statKey", "属性 Key", _ => rule.StatKey, (_, v) => rule.StatKey = v, StatReferenceOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.startingAmount", "初始数量", _ => rule.StartingAmount, (_, v) => rule.StartingAmount = v, min: -999999, max: 999999999));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.storageLimit", "存储上限", _ => rule.StorageLimit, (_, v) => rule.StorageLimit = v, min: 0, max: 999999999));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.sharedByFaction", "阵营共享", _ => rule.SharedByFaction, (_, v) => rule.SharedByFaction = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.canGoNegative", "允许负数", _ => rule.CanGoNegative, (_, v) => rule.CanGoNegative = v));
        fields.Add(TextField("dataEditor.section.presentation", "表现资源", "dataEditor.field.iconPath", "图标", _ => rule.IconPath, (_, v) => rule.IconPath = v));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.tags", "标签", _ => FormatList(rule.Tags), (_, v) => rule.Tags = SplitValues(v), ResourceTagOptions, 112));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddProductionRuleFields(List<FieldSpec> fields, ProductionRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.producerUnit", "生产者单位", _ => rule.ProducerUnitId, (_, v) => rule.ProducerUnitId = v, UnitOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.producedAssetKind", "产出类型", _ => rule.ProducedAssetKind, (_, v) => rule.ProducedAssetKind = v, ProducedAssetKindOptions));
        fields.Add(TextField("dataEditor.section.references", "引用关系", "dataEditor.field.producedAssetId", "产出资产 ID", _ => rule.ProducedAssetId, (_, v) => rule.ProducedAssetId = v));
        fields.Add(Integer("dataEditor.section.timing", "时间与范围", "dataEditor.field.buildTimeTurns", "生产回合", _ => rule.BuildTimeTurns, (_, v) => rule.BuildTimeTurns = v, min: 0, max: 9999));
        fields.Add(Number("dataEditor.section.timing", "时间与范围", "dataEditor.field.buildTimeSeconds", "生产秒数", _ => rule.BuildTimeSeconds, (_, v) => rule.BuildTimeSeconds = v, min: 0, max: 999999));
        fields.Add(DoubleMap("dataEditor.section.costs", "消耗", "dataEditor.field.resourceCosts", "资源消耗", _ => rule.ResourceCosts, (_, v) => rule.ResourceCosts = v, ResourceStatOptions, 154));
        fields.Add(DoubleMap("dataEditor.section.rewards", "奖励", "dataEditor.field.resourceOutputs", "资源产出", _ => rule.ResourceOutputs, (_, v) => rule.ResourceOutputs = v, ResourceStatOptions, 154));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.requiresBuildQueue", "需要队列", _ => rule.RequiresBuildQueue, (_, v) => rule.RequiresBuildQueue = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.repeatable", "可重复", _ => rule.Repeatable, (_, v) => rule.Repeatable = v));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => rule.EventGraphId, (_, v) => rule.EventGraphId = v, EventGraphOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTechRuleFields(List<FieldSpec> fields, TechRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.techKind", "科技类型", _ => rule.TechKind, (_, v) => rule.TechKind = v, TechKindOptions));
        fields.Add(MultiChoice("dataEditor.section.references", "引用关系", "dataEditor.field.prerequisiteTechs", "前置科技", _ => FormatList(rule.PrerequisiteTechIds), (_, v) => rule.PrerequisiteTechIds = SplitValues(v), TechRuleOptions, 112));
        fields.Add(DoubleMap("dataEditor.section.costs", "消耗", "dataEditor.field.researchCosts", "研究消耗", _ => rule.ResearchCosts, (_, v) => rule.ResearchCosts = v, ResourceStatOptions, 154));
        fields.Add(Integer("dataEditor.section.timing", "时间与范围", "dataEditor.field.researchTurns", "研究回合", _ => rule.ResearchTurns, (_, v) => rule.ResearchTurns = v, min: 0, max: 9999));
        fields.Add(MultiChoice("dataEditor.section.unlocks", "解锁", "dataEditor.field.unlockUnits", "解锁单位", _ => FormatList(rule.UnlockUnitIds), (_, v) => rule.UnlockUnitIds = SplitValues(v), UnitOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.unlocks", "解锁", "dataEditor.field.unlockSkills", "解锁技能", _ => FormatList(rule.UnlockSkillIds), (_, v) => rule.UnlockSkillIds = SplitValues(v), SkillOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.unlocks", "解锁", "dataEditor.field.unlockBuildRules", "解锁建造规则", _ => FormatList(rule.UnlockBuildRuleIds), (_, v) => rule.UnlockBuildRuleIds = SplitValues(v), TowerDefenseBuildRuleOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.effects", "效果引用", "dataEditor.field.effects", "效果", _ => FormatList(rule.EffectIds), (_, v) => rule.EffectIds = SplitValues(v), GameplayEffectOptions, 112));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => rule.EventGraphId, (_, v) => rule.EventGraphId = v, EventGraphOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddDiplomacyRuleFields(List<FieldSpec> fields, DiplomacyRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.fromFaction", "来源阵营", _ => rule.FromFactionId, (_, v) => rule.FromFactionId = v, FactionOptions));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.toFaction", "目标阵营", _ => rule.ToFactionId, (_, v) => rule.ToFactionId = v, FactionOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.diplomaticState", "外交状态", _ => rule.DiplomaticState, (_, v) => rule.DiplomaticState = v, DiplomaticStateOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.startingTrust", "初始信任", _ => rule.StartingTrust, (_, v) => rule.StartingTrust = v, min: -100, max: 100));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowsTrade", "允许交易", _ => rule.AllowsTrade, (_, v) => rule.AllowsTrade = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowsSharedVision", "共享视野", _ => rule.AllowsSharedVision, (_, v) => rule.AllowsSharedVision = v));
        fields.Add(Bool("dataEditor.section.core", "核心设置", "dataEditor.field.allowsPassage", "允许通行", _ => rule.AllowsPassage, (_, v) => rule.AllowsPassage = v));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => rule.EventGraphId, (_, v) => rule.EventGraphId = v, EventGraphOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private void AddTerritoryRuleFields(List<FieldSpec> fields, TerritoryRuleDefinition rule)
    {
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.territoryTag", "领地标签", _ => rule.TerritoryTag, (_, v) => rule.TerritoryTag = v, AreaTagOptions));
        fields.Add(Choice("dataEditor.section.core", "核心设置", "dataEditor.field.controlMode", "控制方式", _ => rule.ControlMode, (_, v) => rule.ControlMode = v, TerritoryControlModeOptions));
        fields.Add(Integer("dataEditor.section.core", "核心设置", "dataEditor.field.controlRadius", "控制半径", _ => rule.ControlRadius, (_, v) => rule.ControlRadius = v, min: 0, max: 999));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.ownerFaction", "所属阵营", _ => rule.OwnerFactionId, (_, v) => rule.OwnerFactionId = v, FactionOptions));
        fields.Add(DoubleMap("dataEditor.section.rewards", "奖励", "dataEditor.field.resourceYields", "资源产出", _ => rule.ResourceYields, (_, v) => rule.ResourceYields = v, ResourceStatOptions, 154));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.requiredUnitTags", "需要单位标签", _ => FormatList(rule.RequiredUnitTags), (_, v) => rule.RequiredUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(MultiChoice("dataEditor.section.tags", "标签与特性", "dataEditor.field.blockedUnitTags", "阻止单位标签", _ => FormatList(rule.BlockedUnitTags), (_, v) => rule.BlockedUnitTags = SplitValues(v), UnitTagOptions, 112));
        fields.Add(Choice("dataEditor.section.references", "引用关系", "dataEditor.field.eventGraph", "事件图", _ => rule.EventGraphId, (_, v) => rule.EventGraphId = v, EventGraphOptions));
        fields.Add(Bool("dataEditor.section.meta", "元数据", "dataEditor.field.builtIn", "内置", _ => rule.BuiltIn, (_, v) => rule.BuiltIn = v, readOnly: rule.BuiltIn));
    }

    private StatDefinition CreateStat()
    {
        var key = UniqueSimpleKey("customStat", _context.Project.AssetLibrary.Stats.Select(v => v.Key));
        return new StatDefinition
        {
            Id = $"stat.{key}",
            Key = key,
            DisplayName = "新属性",
            CategoryKey = DefaultChoice("statCategory", "stat.category.general"),
            Category = ChoiceLabel("statCategory", DefaultChoice("statCategory", "stat.category.general"))
        };
    }

    private TraitDefinition CreateTrait()
    {
        var id = UniqueSimpleKey("customTrait", _context.Project.AssetLibrary.Traits.Select(v => v.Id));
        return new TraitDefinition
        {
            Id = id,
            DisplayName = "新特性",
            CategoryKey = DefaultChoice("traitCategory", "trait.category.general"),
            Category = ChoiceLabel("traitCategory", DefaultChoice("traitCategory", "trait.category.general"))
        };
    }

    private OptionSetDefinition CreateOptionSet()
    {
        return new OptionSetDefinition
        {
            Id = UniqueSimpleKey("customOptionSet", _context.Project.AssetLibrary.OptionSets.Select(v => v.Id)),
            DisplayName = "新选项集",
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["newOption"] = "新选项"
            }
        };
    }

    private TowerDefensePathDefinition CreateTowerDefensePath()
    {
        return new TowerDefensePathDefinition
        {
            Id = UniqueId("td.path.custom", _context.Project.AssetLibrary.TowerDefensePaths.Select(v => v.Id)),
            DisplayName = "新路线",
            MapId = DefaultOptionValue(MapOptions(), "map.training_ground"),
            PathMode = DefaultChoice("routeMode", "waypoints"),
            SpawnPointId = "spawn",
            GoalPointId = "base",
            Waypoints =
            [
                new TowerDefenseWaypointDefinition { Key = "start", X = 0, Y = 0 },
                new TowerDefenseWaypointDefinition { Key = "goal", X = 10, Y = 0 }
            ]
        };
    }

    private TowerDefenseWaveDefinition CreateTowerDefenseWave()
    {
        return new TowerDefenseWaveDefinition
        {
            Id = UniqueId("td.wave.custom", _context.Project.AssetLibrary.TowerDefenseWaves.Select(v => v.Id)),
            DisplayName = "新生成波次",
            MapId = DefaultOptionValue(MapOptions(), "map.training_ground"),
            PathId = DefaultOptionValue(TowerDefensePathOptions(), "td.path.forest_main"),
            StartDelaySeconds = 3,
            SpawnIntervalSeconds = 1,
            RewardGold = 50,
            RewardExp = 5,
            SpawnGroups =
            [
                new TowerDefenseSpawnGroupDefinition
                {
                    UnitId = DefaultOptionValue(UnitOptions(), "unit.slime"),
                    Count = 8,
                    IntervalSeconds = 1,
                    PathId = DefaultOptionValue(TowerDefensePathOptions(), "td.path.forest_main")
                }
            ]
        };
    }

    private TowerDefenseRuleDefinition CreateTowerDefenseRule()
    {
        return new TowerDefenseRuleDefinition
        {
            Id = UniqueId("td.rules.custom", _context.Project.AssetLibrary.TowerDefenseRules.Select(v => v.Id)),
            DisplayName = "新关卡规则",
            MapId = DefaultOptionValue(MapOptions(), "map.training_ground"),
            StartingGold = 200,
            BaseLife = 20,
            LeakDamagePerUnit = 1,
            BuildRuleId = DefaultOptionValue(TowerDefenseBuildRuleOptions(), "td.buildRule.default"),
            WaveStartMode = DefaultChoice("waveStartMode", "manual"),
            VictoryCondition = DefaultChoice("victoryCondition", "allWavesCleared"),
            DefeatCondition = DefaultChoice("defeatCondition", "baseLifeZero"),
            WaveIds = _context.Project.AssetLibrary.TowerDefenseWaves.Take(2).Select(v => v.Id).ToList()
        };
    }

    private TowerDefenseBuildRuleDefinition CreateTowerDefenseBuildRule()
    {
        return new TowerDefenseBuildRuleDefinition
        {
            Id = UniqueId("td.buildRule.custom", _context.Project.AssetLibrary.TowerDefenseBuildRules.Select(v => v.Id)),
            DisplayName = "新建造规则",
            BuildSurfaceTag = DefaultOptionValue(TagOptions(), "buildable"),
            PreventPathBlocking = true,
            AllowSell = true,
            SellRefundRatio = 0.7,
            AllowUpgradeDuringWave = true,
            CurrencyStatKey = DefaultStatKey("gold")
        };
    }

    private TowerDefenseTowerDefinition CreateTowerDefenseTower()
    {
        return new TowerDefenseTowerDefinition
        {
            Id = UniqueId("td.tower.custom", _context.Project.AssetLibrary.TowerDefenseTowers.Select(v => v.Id)),
            DisplayName = "新可建造单位",
            UnitId = DefaultOptionValue(UnitOptions(), "unit.td.arrow_tower"),
            SkillId = DefaultOptionValue(SkillOptions(), "skill.td.arrowShot"),
            TowerRole = DefaultChoice("buildableRole", "damage"),
            BuildCost = 100,
            Range = 5,
            AttackIntervalSeconds = 1,
            TargetPriority = DefaultChoice("targetPriority", "first"),
            Levels =
            [
                new TowerDefenseTowerLevelDefinition { Level = 1, SkillId = DefaultOptionValue(SkillOptions(), "skill.td.arrowShot") }
            ]
        };
    }

    private TacticalGridRuleDefinition CreateTacticalGridRule()
    {
        return new TacticalGridRuleDefinition
        {
            Id = UniqueId("tactics.grid.custom", _context.Project.AssetLibrary.TacticalGridRules.Select(v => v.Id)),
            DisplayName = "新格子规则",
            GridType = DefaultChoice("gridType", "square"),
            TileSize = 32,
            MovementMetric = DefaultChoice("movementMetric", "manhattan"),
            ZoneOfControlEnabled = true
        };
    }

    private TerrainRuleDefinition CreateTerrainRule()
    {
        return new TerrainRuleDefinition
        {
            Id = UniqueId("terrain.custom", _context.Project.AssetLibrary.TerrainRules.Select(v => v.Id)),
            DisplayName = "新地形规则",
            TerrainTag = DefaultOptionValue(TagOptions(), "plain"),
            MovementCost = 1,
            DamageModifier = 1
        };
    }

    private TurnRuleDefinition CreateTurnRule()
    {
        return new TurnRuleDefinition
        {
            Id = UniqueId("turn.custom", _context.Project.AssetLibrary.TurnRules.Select(v => v.Id)),
            DisplayName = "新回合规则",
            TurnMode = DefaultChoice("turnMode", "sideTurn"),
            ActionRefreshMode = DefaultChoice("actionRefreshMode", "turnStart"),
            InitiativeStatKey = DefaultStatKey("moveSpeed"),
            AllowWait = true,
            AllowUndoMove = true
        };
    }

    private ActionRuleDefinition CreateActionRule()
    {
        return new ActionRuleDefinition
        {
            Id = UniqueId("action.custom", _context.Project.AssetLibrary.ActionRules.Select(v => v.Id)),
            DisplayName = "新行动规则",
            DefaultActionPoints = 1,
            DefaultMovePoints = 4,
            AttackConsumesAction = true,
            CanAttackAfterMove = true,
            WaitEndsTurn = true
        };
    }

    private TacticalRangeDefinition CreateTacticalRange()
    {
        return new TacticalRangeDefinition
        {
            Id = UniqueId("range.custom", _context.Project.AssetLibrary.TacticalRanges.Select(v => v.Id)),
            DisplayName = "新战术范围",
            RangeShape = DefaultChoice("rangeShape", "diamond"),
            MinRange = 1,
            MaxRange = 1,
            AreaShape = DefaultChoice("areaShape", "single"),
            CanTargetAlly = true,
            CanTargetEnemy = true,
            TerrainBlocked = true
        };
    }

    private ObjectiveRuleDefinition CreateObjectiveRule()
    {
        return new ObjectiveRuleDefinition
        {
            Id = UniqueId("objective.custom", _context.Project.AssetLibrary.ObjectiveRules.Select(v => v.Id)),
            DisplayName = "新目标规则",
            ObjectiveType = DefaultChoice("objectiveType", "defeatAll"),
            IsVictoryCondition = true,
            TargetUnitTags = DefaultOptionValues(UnitTagOptions(), "enemy"),
            RequiredCount = 1
        };
    }

    private BondRuleDefinition CreateBondRule()
    {
        return new BondRuleDefinition
        {
            Id = UniqueId("bond.custom", _context.Project.AssetLibrary.BondRules.Select(v => v.Id)),
            DisplayName = "新羁绊规则",
            TriggerTiming = DefaultChoice("bondTriggerTiming", "whileAdjacent"),
            Range = 1,
            MinParticipants = 2,
            RequireSameFaction = true,
            DurationMode = DefaultChoice("bondDurationMode", "whileConditionMet"),
            StackingMode = DefaultChoice("stackingMode", "refresh")
        };
    }

    private ResourceRuleDefinition CreateResourceRule()
    {
        return new ResourceRuleDefinition
        {
            Id = UniqueId("resource.custom", _context.Project.AssetLibrary.ResourceRules.Select(v => v.Id)),
            DisplayName = "新资源规则",
            ResourceKind = DefaultChoice("resourceKind", "currency"),
            StatKey = DefaultStatKey("gold"),
            StorageLimit = 9999,
            SharedByFaction = true
        };
    }

    private ProductionRuleDefinition CreateProductionRule()
    {
        return new ProductionRuleDefinition
        {
            Id = UniqueId("production.custom", _context.Project.AssetLibrary.ProductionRules.Select(v => v.Id)),
            DisplayName = "新生产规则",
            ProducerUnitId = DefaultOptionValue(UnitOptions(), "unit.td.arrow_tower"),
            ProducedAssetKind = DefaultChoice("producedAssetKind", "unit"),
            ProducedAssetId = DefaultOptionValue(UnitOptions(), "unit.guard_patrol"),
            BuildTimeTurns = 1,
            ResourceCosts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultStatKey("gold")] = 50
            },
            RequiresBuildQueue = true,
            Repeatable = true
        };
    }

    private TechRuleDefinition CreateTechRule()
    {
        return new TechRuleDefinition
        {
            Id = UniqueId("tech.custom", _context.Project.AssetLibrary.TechRules.Select(v => v.Id)),
            DisplayName = "新科技规则",
            TechKind = DefaultChoice("techKind", "unlock"),
            ResearchCosts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultStatKey("researchPoint")] = 50
            },
            ResearchTurns = 3
        };
    }

    private DiplomacyRuleDefinition CreateDiplomacyRule()
    {
        return new DiplomacyRuleDefinition
        {
            Id = UniqueId("diplomacy.custom", _context.Project.AssetLibrary.DiplomacyRules.Select(v => v.Id)),
            DisplayName = "新外交规则",
            FromFactionId = DefaultOptionValue(FactionOptions(), "faction.player"),
            ToFactionId = DefaultOptionValue(FactionOptions(), "faction.neutral"),
            DiplomaticState = DefaultChoice("diplomaticState", "neutral"),
            AllowsPassage = true
        };
    }

    private TerritoryRuleDefinition CreateTerritoryRule()
    {
        return new TerritoryRuleDefinition
        {
            Id = UniqueId("territory.custom", _context.Project.AssetLibrary.TerritoryRules.Select(v => v.Id)),
            DisplayName = "新领地规则",
            TerritoryTag = DefaultOptionValue(TagOptions(), "capturePoint"),
            ControlMode = DefaultChoice("territoryControlMode", "occupyPoint"),
            ControlRadius = 3,
            OwnerFactionId = DefaultOptionValue(FactionOptions(), "faction.neutral"),
            RequiredUnitTags = DefaultOptionValues(UnitTagOptions(), "unit")
        };
    }

    private string DefaultChoice(string group, params string[] preferredValues)
    {
        return DefaultOptionValue(OptionSetOptions(group), preferredValues);
    }

    private static string DefaultOptionValue(IReadOnlyList<OptionItem> options, params string[] preferredValues)
    {
        var available = options.Where(v => !string.IsNullOrWhiteSpace(v.Value)).ToList();
        foreach (var preferredValue in preferredValues)
        {
            if (available.Any(v => string.Equals(v.Value, preferredValue, StringComparison.OrdinalIgnoreCase)))
            {
                return preferredValue;
            }
        }

        return available.FirstOrDefault()?.Value ?? string.Empty;
    }

    private string DefaultStatKey(params string[] preferredKeys)
    {
        return DefaultOptionValue(StatReferenceOptions(), preferredKeys);
    }

    private List<string> DefaultOptionValues(IReadOnlyList<OptionItem> options, params string[] preferredValues)
    {
        var available = options
            .Where(v => !string.IsNullOrWhiteSpace(v.Value))
            .Select(v => v.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return preferredValues.Where(available.Contains).ToList();
    }

    private static List<string> DefaultSingleOptionValue(IReadOnlyList<OptionItem> options, params string[] preferredValues)
    {
        var value = DefaultOptionValue(options, preferredValues);
        return string.IsNullOrWhiteSpace(value) ? [] : [value];
    }

    private ComponentConfig CreateDefaultComponent(string preferredType, params (string Key, object Value)[] parameters)
    {
        var component = new ComponentConfig { Type = DefaultChoice("componentType", preferredType) };
        foreach (var parameter in parameters)
        {
            var key = DefaultChoice("componentParameter", parameter.Key);
            if (!string.IsNullOrWhiteSpace(key))
            {
                component.Parameters[key] = parameter.Value;
            }
        }

        return component;
    }

    private UnitDefinition CreateUnit()
    {
        return new UnitDefinition
        {
            Id = UniqueId("unit.custom", _context.Project.AssetLibrary.Units.Select(v => v.Id)),
            DisplayName = "新单位",
            DisplayNameKey = "",
            Description = "新单位。",
            DescriptionKey = "",
            UnitKind = DefaultChoice("unitKind", "enemy"),
            FactionId = DefaultOptionValue(FactionOptions(), "faction.monster"),
            AIProfileId = DefaultOptionValue(AIProfileOptions(), "ai.meleeChase"),
            Stats = CreateDefaultStatValues(),
            Tags = DefaultOptionValues(UnitTagOptions(), "unit", "enemy"),
            Traits = DefaultOptionValues(TraitOptions(), "unit", "enemy", "attackable"),
            Portrait = "",
            Sprite = new SpriteSheetConfig(),
            Components =
            [
                CreateDefaultComponent("TopDownMovement", ("speedStat", DefaultStatKey("moveSpeed"))),
                CreateDefaultComponent("Health", ("maxHpStat", DefaultStatKey("maxHp"))),
                CreateDefaultComponent("AIController", ("profileField", "AIProfileId"))
            ],
            Animations = []
        };
    }

    private Dictionary<string, double> CreateDefaultStatValues()
    {
        return _context.Project.AssetLibrary.Stats.ToDictionary(v => v.Key, v => NormalizeStatValue(v, v.DefaultValue), StringComparer.OrdinalIgnoreCase);
    }

    private string BuildValidationText(object record)
    {
        var messages = new List<string>();
        switch (record)
        {
            case AIProfileDefinition:
                messages.Add(T("dataEditor.validation.aiBoundary", "AI 预设只控制行动策略；属性、标签、奖励和外观请直接放在单位中。"));
                break;
            case UnitDefinition unit:
                messages.Add(string.Format(CultureInfo.CurrentCulture, T("dataEditor.validation.unitRefs", "类型：{0}；AI：{1}；阵营：{2}。"), ChoiceLabel("unitKind", unit.UnitKind), NameOrNone(unit.AIProfileId, AIProfileOptions()), NameOrNone(unit.FactionId, FactionOptions())));
                break;
            case SkillDefinition:
                messages.Add(T("dataEditor.validation.skillBoundary", "技能主要定义施放方式、消耗和引用关系；伤害、治疗、吸血等数值优先放在玩法效果和公式里。"));
                break;
            case InteractionProfileDefinition:
                messages.Add(T("dataEditor.validation.interactionBoundary", "交互入口只声明可触发的入口；对话、营救、开箱、传送流程由事件编辑器实现。"));
                break;
            case FormulaDefinition formula:
                messages.Add(T("dataEditor.validation.formulaBoundary", "公式是全局可复用的计算资产，技能和玩法效果只引用它，不重复写计算逻辑。"));
                if (string.Equals(formula.FormulaKind, "nodeGraph", StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(T("dataEditor.validation.formulaGraph", "当前公式类型是计算节点图，表达式可作为兼容预览，真正执行以图或运行时实现为准。"));
                }
                break;
            case VisualEffectDefinition:
                messages.Add(T("dataEditor.validation.vfxBoundary", "视觉特效只负责表现，不直接造成伤害或修改状态。"));
                break;
            case GameplayEffectDefinition:
                messages.Add(T("dataEditor.validation.effectBoundary", "玩法效果负责改变数值或状态，可被技能、物品、投射物和状态引用。"));
                break;
        }

        return string.Join(Environment.NewLine, messages);
    }

    private bool IsBuiltIn(object record)
    {
        var property = record.GetType().GetProperty("BuiltIn");
        return property?.PropertyType == typeof(bool) && (bool)(property.GetValue(record) ?? false);
    }

    private string Localized(object record)
    {
        return Localized(GetString(record, "DisplayName"), GetString(record, "DisplayNameKey"));
    }

    private string LocalizedDescription(object record)
    {
        return Localized(GetString(record, "Description"), GetString(record, "DescriptionKey"));
    }

    private string Localized(string text, string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            var value = _localization.T(key);
            if (!string.Equals(value, key, StringComparison.Ordinal))
            {
                return value;
            }
        }

        return string.IsNullOrWhiteSpace(text) ? T("dataEditor.unnamed", "未命名") : text;
    }

    private string FormatStatSummary(StatDefinition stat)
    {
        return $"{T(stat.CategoryKey, stat.Category)} | {T("dataEditor.field.defaultValue", "默认值")} {FormatStatValue(stat, stat.DefaultValue)}";
    }

    private string FormatFormulaSummary(FormulaDefinition formula)
    {
        var expression = string.IsNullOrWhiteSpace(formula.Expression)
            ? T("dataEditor.option.none", "无")
            : formula.Expression;
        return $"{ChoiceLabel("formulaKind", formula.FormulaKind)} | {expression}";
    }

    private string FormatUnitSummary(UnitDefinition unit)
    {
        return $"{ChoiceLabel("unitKind", unit.UnitKind)} | {NameOrNone(unit.AIProfileId, AIProfileOptions())} | {FormatLocalizedValues(unit.Traits, TraitOptions())}";
    }

    private string FormatAiSummary(AIProfileDefinition ai)
    {
        return $"{ChoiceLabel("behaviorType", ai.BehaviorType)} | {T("dataEditor.field.skills", "技能")} {ai.SkillIds.Count}";
    }

    private string FormatSkillSummary(SkillDefinition skill)
    {
        var formula = NameOrNone(skill.FormulaId, FormulaOptions());
        var targetTags = skill.RequiredTargetTags.Count == 0
            ? T("dataEditor.option.none", "无")
            : FormatLocalizedValues(skill.RequiredTargetTags, TargetTagOptions());
        return $"{ChoiceLabel("targetingMode", skill.TargetingMode)} | {T("dataEditor.field.requiredTargetTags", "必须目标标签")} {targetTags} | {T("dataEditor.field.effects", "效果")} {skill.Effects.Count} | {T("dataEditor.field.formula", "公式")} {formula}";
    }

    private string FormatGameplayEffectSummary(GameplayEffectDefinition effect)
    {
        var formulaCount = effect.Parameters.Count(parameter =>
            parameter.ValueType is EffectParameterValueType.Choice or EffectParameterValueType.AssetRef
            && string.Equals(parameter.OptionSourceId, "formula", StringComparison.OrdinalIgnoreCase));
        return $"{ChoiceLabel("effectKind", effect.EffectKind)} | {T("dataEditor.field.effectParameters", "参数模板")} {effect.Parameters.Count} | {T("dataEditor.field.formula", "公式")} {formulaCount}";
    }

    private string FormatStatusSummary(StatusDefinition status)
    {
        return $"{ChoiceLabel("statusKind", status.StatusKind)} | {status.DurationSeconds:0.##}s";
    }

    private string FormatProjectileSummary(ProjectileDefinition projectile)
    {
        return $"{T("dataEditor.field.speed", "速度")} {projectile.Speed:0.##} | {T("dataEditor.field.effects", "效果")} {projectile.Effects.Count}";
    }

    private string FormatVisualEffectSummary(VisualEffectDefinition vfx)
    {
        return $"{vfx.SpriteSheet} | {vfx.DurationSeconds:0.##}s";
    }

    private string FormatItemSummary(ItemDefinition item)
    {
        return $"{ChoiceLabel("rarity", item.Rarity)} | {T("dataEditor.field.effects", "效果")} {item.Effects.Count}";
    }

    private string FormatDecorationSummary(DecorationDefinition decoration)
    {
        var flags = new List<string>();
        if (decoration.BlocksMovement) flags.Add(T("dataEditor.field.blocksMovement", "阻挡移动"));
        if (decoration.Destructible) flags.Add(T("dataEditor.field.destructible", "可破坏"));
        if (!string.IsNullOrWhiteSpace(decoration.InteractionProfileId)) flags.Add(NameOrNone(decoration.InteractionProfileId, InteractionOptions()));
        return flags.Count == 0 ? LocalizedDescription(decoration) : string.Join(" | ", flags);
    }

    private string FormatInteractionSummary(InteractionProfileDefinition interaction)
    {
        var triggerName = string.IsNullOrWhiteSpace(interaction.TriggerName)
            ? T("dataEditor.option.none", "无")
            : GraphNodeCatalog.GetDisplayValue(_localization, "event", interaction.TriggerName);
        return $"{ChoiceLabel("interactionKind", interaction.InteractionKind)} | {triggerName}";
    }

    private string FormatTowerDefensePathSummary(TowerDefensePathDefinition path)
    {
        return $"{ChoiceLabel("routeMode", path.PathMode)} | {T("dataEditor.field.waypoints", "路点")} {path.Waypoints.Count}";
    }

    private string FormatTowerDefenseWaveSummary(TowerDefenseWaveDefinition wave)
    {
        return $"{NameOrNone(wave.PathId, TowerDefensePathOptions())} | {T("dataEditor.field.spawnGroups", "刷怪组")} {wave.SpawnGroups.Count} | {T("dataEditor.field.rewardGold", "金币奖励")} {wave.RewardGold}";
    }

    private string FormatTowerDefenseRuleSummary(TowerDefenseRuleDefinition rule)
    {
        return $"{ChoiceLabel("victoryCondition", rule.VictoryCondition)} | {T("dataEditor.field.waves", "波次列表")} {rule.WaveIds.Count} | {T("dataEditor.field.baseLife", "基地生命")} {rule.BaseLife}";
    }

    private string FormatTowerDefenseBuildRuleSummary(TowerDefenseBuildRuleDefinition buildRule)
    {
        var sell = buildRule.AllowSell ? T("common.yes", "是") : T("common.no", "否");
        return $"{T("dataEditor.field.buildSurfaceTag", "建造区域标签")} {TagLabel(buildRule.BuildSurfaceTag)} | {T("dataEditor.field.allowSell", "允许出售")} {sell}";
    }

    private string FormatTowerDefenseTowerSummary(TowerDefenseTowerDefinition tower)
    {
        return $"{NameOrNone(tower.SkillId, SkillOptions())} | {T("dataEditor.field.buildCost", "建造费用")} {tower.BuildCost} | {T("dataEditor.field.levels", "等级")} {tower.Levels.Count}";
    }

    private string FormatTacticalGridRuleSummary(TacticalGridRuleDefinition rule)
    {
        return $"{ChoiceLabel("gridType", rule.GridType)} | {ChoiceLabel("movementMetric", rule.MovementMetric)} | {T("dataEditor.field.tileSize", "格子尺寸")} {rule.TileSize}";
    }

    private string FormatTerrainRuleSummary(TerrainRuleDefinition rule)
    {
        return $"{T("dataEditor.field.movementCost", "移动消耗")} {rule.MovementCost:0.##} | {T("dataEditor.field.defenseBonus", "防御加成")} {rule.DefenseBonus:0.##}";
    }

    private string FormatTurnRuleSummary(TurnRuleDefinition rule)
    {
        return $"{ChoiceLabel("turnMode", rule.TurnMode)} | {ChoiceLabel("actionRefreshMode", rule.ActionRefreshMode)} | {T("dataEditor.field.maxRounds", "最大回合")} {rule.MaxRounds}";
    }

    private string FormatActionRuleSummary(ActionRuleDefinition rule)
    {
        return $"{T("dataEditor.field.defaultActionPoints", "默认行动点")} {rule.DefaultActionPoints} | {T("dataEditor.field.defaultMovePoints", "默认移动力")} {rule.DefaultMovePoints}";
    }

    private string FormatTacticalRangeSummary(TacticalRangeDefinition range)
    {
        return $"{range.MinRange}-{range.MaxRange} | {ChoiceLabel("areaShape", range.AreaShape)} | {T("dataEditor.field.areaRadius", "区域半径")} {range.AreaRadius}";
    }

    private string FormatObjectiveRuleSummary(ObjectiveRuleDefinition rule)
    {
        var condition = rule.IsVictoryCondition ? T("dataEditor.field.victoryCondition", "胜利条件") : T("dataEditor.field.defeatCondition", "失败条件");
        return $"{condition} | {ChoiceLabel("objectiveType", rule.ObjectiveType)} | {T("dataEditor.field.requiredCount", "需要数量")} {rule.RequiredCount}";
    }

    private string FormatBondRuleSummary(BondRuleDefinition rule)
    {
        return $"{ChoiceLabel("bondTriggerTiming", rule.TriggerTiming)} | {T("dataEditor.field.bondRange", "羁绊范围")} {rule.Range} | {T("dataEditor.field.effects", "效果")} {rule.EffectIds.Count}";
    }

    private string FormatResourceRuleSummary(ResourceRuleDefinition rule)
    {
        return $"{ChoiceLabel("resourceKind", rule.ResourceKind)} | {NameOrNone(rule.StatKey, StatReferenceOptions())} | {T("dataEditor.field.startingAmount", "初始数量")} {rule.StartingAmount}";
    }

    private string FormatProductionRuleSummary(ProductionRuleDefinition rule)
    {
        return $"{ChoiceLabel("producedAssetKind", rule.ProducedAssetKind)} | {T("dataEditor.field.buildTimeTurns", "生产回合")} {rule.BuildTimeTurns} | {T("dataEditor.field.resourceCosts", "资源消耗")} {rule.ResourceCosts.Count}";
    }

    private string FormatTechRuleSummary(TechRuleDefinition rule)
    {
        return $"{ChoiceLabel("techKind", rule.TechKind)} | {T("dataEditor.field.researchTurns", "研究回合")} {rule.ResearchTurns} | {T("dataEditor.field.unlocks", "解锁")} {rule.UnlockUnitIds.Count + rule.UnlockSkillIds.Count + rule.UnlockBuildRuleIds.Count}";
    }

    private string FormatDiplomacyRuleSummary(DiplomacyRuleDefinition rule)
    {
        return $"{NameOrNone(rule.FromFactionId, FactionOptions())} -> {NameOrNone(rule.ToFactionId, FactionOptions())} | {ChoiceLabel("diplomaticState", rule.DiplomaticState)}";
    }

    private string FormatTerritoryRuleSummary(TerritoryRuleDefinition rule)
    {
        return $"{ChoiceLabel("territoryControlMode", rule.ControlMode)} | {T("dataEditor.field.controlRadius", "控制半径")} {rule.ControlRadius} | {T("dataEditor.field.resourceYields", "资源产出")} {rule.ResourceYields.Count}";
    }

    private string GetItemTypeName(string id) => NameOrNone(id, ItemTypeOptions());

    private IReadOnlyList<NavigationGroupDescriptor> GetNavigationGroups()
    {
        return
        [
            new("definitions", "dataEditor.nav.definitions", "基础定义", ["stats", "traits", "optionSets", "formulas", "factions", "damageTypes", "elements"]),
            new("entities", "dataEditor.nav.entities", "实体对象", ["units", "items", "decorations", "componentPresets", "aiProfiles", "interactions"]),
            new("abilities", "dataEditor.nav.abilities", "行为能力", ["skills", "gameplayEffects", "statuses", "projectiles", "visualEffects", "tacticalRanges"]),
            new("space", "dataEditor.nav.space", "空间与场景", ["maps", "routes", "territoryRules", "terrainRules", "tacticalGridRules", "buildRules"]),
            new("resources", "dataEditor.nav.resources", "资源与生产", ["resourceRules", "itemTypes", "lootTables", "productionRules", "buildableUnits"]),
            new("flow", "dataEditor.nav.flow", "流程与目标", ["spawnWaves", "objectiveRules", "levelRules", "turnRules", "actionRules", "bondRules", "diplomacyRules", "techRules"])
        ];
    }

    private IReadOnlyList<OptionItem> UnitKindOptions() => OptionSetOptions("unitKind");
    private IReadOnlyList<OptionItem> AttitudeOptions() => OptionSetOptions("attitude");
    private IReadOnlyList<OptionItem> ValueTypeOptions() => OptionSetOptions("valueType");
    private IReadOnlyList<OptionItem> FormulaKindOptions() => OptionSetOptions("formulaKind");
    private IReadOnlyList<OptionItem> BehaviorTypeOptions() => OptionSetOptions("behaviorType");
    private IReadOnlyList<OptionItem> MovementModeOptions() => OptionSetOptions("movementMode");
    private IReadOnlyList<OptionItem> TargetSelectorOptions() => OptionSetOptions("targetSelector");
    private IReadOnlyList<OptionItem> PatrolModeOptions() => OptionSetOptions("patrolMode");
    private IReadOnlyList<OptionItem> SkillTypeOptions() => OptionSetOptions("skillType");
    private IReadOnlyList<OptionItem> TargetingModeOptions() => OptionSetOptions("targetingMode");
    private IReadOnlyList<OptionItem> EffectKindOptions() => OptionSetOptions("effectKind");
    private IReadOnlyList<OptionItem> StatusKindOptions() => OptionSetOptions("statusKind");
    private IReadOnlyList<OptionItem> VisualEffectKindOptions() => OptionSetOptions("visualEffectKind");
    private IReadOnlyList<OptionItem> RarityOptions() => OptionSetOptions("rarity");
    private IReadOnlyList<OptionItem> EquipmentSlotOptions() => EmptyOption().Concat(OptionSetOptions("equipmentSlot")).ToList();
    private IReadOnlyList<OptionItem> DecorationKindOptions() => OptionSetOptions("decorationKind");
    private IReadOnlyList<OptionItem> InteractionKindOptions() => OptionSetOptions("interactionKind");
    private IReadOnlyList<OptionItem> InteractionTriggerOptions()
    {
        return EmptyOption()
            .Concat(new[]
            {
                new OptionItem("OnInteract", GraphNodeCatalog.GetDisplayValue(_localization, "event", "OnInteract")),
                new OptionItem("OnEnterArea", GraphNodeCatalog.GetDisplayValue(_localization, "event", "OnEnterArea")),
                new OptionItem("OnTouch", GraphNodeCatalog.GetDisplayValue(_localization, "event", "OnTouch")),
                new OptionItem("OnSkillCast", GraphNodeCatalog.GetDisplayValue(_localization, "event", "OnSkillCast")),
                new OptionItem("OnDamageDealt", GraphNodeCatalog.GetDisplayValue(_localization, "event", "OnDamageDealt"))
            })
            .ToList();
    }
    private IReadOnlyList<OptionItem> ViewTypeOptions() => OptionSetOptions("viewType");
    private IReadOnlyList<OptionItem> StatCategoryOptions() => OptionSetOptions("statCategory");
    private IReadOnlyList<OptionItem> TraitCategoryOptions() => OptionSetOptions("traitCategory");
    private IReadOnlyList<OptionItem> GridTypeOptions() => OptionSetOptions("gridType");
    private IReadOnlyList<OptionItem> MovementMetricOptions() => OptionSetOptions("movementMetric");
    private IReadOnlyList<OptionItem> TurnModeOptions() => OptionSetOptions("turnMode");
    private IReadOnlyList<OptionItem> ActionRefreshModeOptions() => OptionSetOptions("actionRefreshMode");
    private IReadOnlyList<OptionItem> RangeShapeOptions() => OptionSetOptions("rangeShape");
    private IReadOnlyList<OptionItem> AreaShapeOptions() => OptionSetOptions("areaShape");
    private IReadOnlyList<OptionItem> ObjectiveTypeOptions() => OptionSetOptions("objectiveType");
    private IReadOnlyList<OptionItem> BondTriggerTimingOptions() => OptionSetOptions("bondTriggerTiming");
    private IReadOnlyList<OptionItem> BondDurationModeOptions() => OptionSetOptions("bondDurationMode");
    private IReadOnlyList<OptionItem> StackingModeOptions() => OptionSetOptions("stackingMode");
    private IReadOnlyList<OptionItem> ResourceKindOptions() => OptionSetOptions("resourceKind");
    private IReadOnlyList<OptionItem> ProducedAssetKindOptions() => OptionSetOptions("producedAssetKind");
    private IReadOnlyList<OptionItem> TechKindOptions() => OptionSetOptions("techKind");
    private IReadOnlyList<OptionItem> DiplomaticStateOptions() => OptionSetOptions("diplomaticState");
    private IReadOnlyList<OptionItem> TerritoryControlModeOptions() => OptionSetOptions("territoryControlMode");

    private IReadOnlyList<OptionItem> StatReferenceOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Stats.Select(v => new OptionItem(v.Key, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> TraitOptions() => _context.Project.AssetLibrary.Traits.Select(v => new OptionItem(v.Id, Localized(v))).ToList();
    private IReadOnlyList<OptionItem> TagOptions() => MergeOptionSetOptions("tag", CollectTagKeys());
    private IReadOnlyList<OptionItem> UnitTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Unit));
    private IReadOnlyList<OptionItem> TargetTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Target));
    private IReadOnlyList<OptionItem> SkillTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Skill));
    private IReadOnlyList<OptionItem> EffectTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Effect));
    private IReadOnlyList<OptionItem> StatusTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Status));
    private IReadOnlyList<OptionItem> ItemTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Item));
    private IReadOnlyList<OptionItem> DecorationTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Decoration));
    private IReadOnlyList<OptionItem> TerrainTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Terrain));
    private IReadOnlyList<OptionItem> AreaTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Area));
    private IReadOnlyList<OptionItem> ResourceTagOptions() => ContextualOptionSetOptions("tag", CollectContextualTagKeys(TagContext.Resource));
    private IReadOnlyList<OptionItem> AnimationKeyOptions() => MergeOptionSetOptions("animationKey", CollectAnimationKeys());
    private IReadOnlyList<OptionItem> FactionOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Factions.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> DamageTypeOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.DamageTypes.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> ElementOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Elements.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> FormulaOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Formulas.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> AIProfileOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.AIProfiles.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> SkillOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Skills.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> GameplayEffectOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.GameplayEffects.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> StatusOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Statuses.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> ProjectileOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Projectiles.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> VisualEffectOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.VisualEffects.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> UnitOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Units.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> MapOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.Maps.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> TowerDefensePathOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.TowerDefensePaths.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> TowerDefenseWaveOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.TowerDefenseWaves.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> TowerDefenseBuildRuleOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.TowerDefenseBuildRules.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> ResourceRuleOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.ResourceRules.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> TechRuleOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.TechRules.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> ResourceStatOptions()
    {
        var resourceStats = _context.Project.AssetLibrary.ResourceRules
            .Select(v => v.StatKey)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return EmptyOption()
            .Concat(_context.Project.AssetLibrary.Stats
                .Where(v => resourceStats.Contains(v.Key))
                .Select(v => new OptionItem(v.Key, Localized(v))))
            .ToList();
    }
    private IReadOnlyList<OptionItem> ItemTypeOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.ItemTypes.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> ItemFieldOptions(string itemTypeId)
    {
        var itemType = _context.Project.AssetLibrary.ItemTypes.FirstOrDefault(v => string.Equals(v.Id, itemTypeId, StringComparison.OrdinalIgnoreCase));
        if (itemType is null)
        {
            return [];
        }

        return itemType.Fields
            .OrderBy(v => v.Order)
            .ThenBy(v => Localized(v), StringComparer.CurrentCulture)
            .Select(v => new OptionItem(v.Key, Localized(v)))
            .ToList();
    }
    private IReadOnlyList<OptionItem> LootTableOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.LootTables.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> InteractionOptions() => EmptyOption().Concat(_context.Project.AssetLibrary.InteractionProfiles.Select(v => new OptionItem(v.Id, Localized(v)))).ToList();
    private IReadOnlyList<OptionItem> EventGraphOptions() => EmptyOption().Concat(_context.Project.EventGraphs.Select(v => new OptionItem(v.Id, v.DisplayName))).ToList();
    private IReadOnlyList<OptionItem> TowerDefensePathModeOptions() => OptionSetOptions("routeMode");
    private IReadOnlyList<OptionItem> TowerDefenseWaveStartModeOptions() => OptionSetOptions("waveStartMode");
    private IReadOnlyList<OptionItem> TowerDefenseVictoryConditionOptions() => OptionSetOptions("victoryCondition");
    private IReadOnlyList<OptionItem> TowerDefenseDefeatConditionOptions() => OptionSetOptions("defeatCondition");
    private IReadOnlyList<OptionItem> TowerDefenseTowerRoleOptions() => OptionSetOptions("buildableRole");
    private IReadOnlyList<OptionItem> TowerDefenseTargetPriorityOptions() => OptionSetOptions("targetPriority");

    private IReadOnlyList<OptionItem> OptionSetOptions(string group)
    {
        return _context.Project.AssetLibrary.OptionSets
            .FirstOrDefault(v => string.Equals(v.Id, group, StringComparison.OrdinalIgnoreCase))
            ?.Values
            .Where(v => !string.IsNullOrWhiteSpace(v.Key))
            .Select(v => new OptionItem(v.Key, ChoiceLabel(group, v.Key)))
            .ToList()
            ?? [];
    }

    private IReadOnlyList<OptionItem> MergeOptionSetOptions(string group, IEnumerable<string> discoveredValues)
    {
        var result = new List<OptionItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in OptionSetOptions(group))
        {
            if (seen.Add(option.Value))
            {
                result.Add(option);
            }
        }

        foreach (var value in discoveredValues.Where(v => !string.IsNullOrWhiteSpace(v)).OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
        {
            if (seen.Add(value))
            {
                result.Add(new OptionItem(value, ChoiceLabel(group, value)));
            }
        }

        return result;
    }

    private IReadOnlyList<OptionItem> ContextualOptionSetOptions(string group, IEnumerable<string> values)
    {
        var result = new List<OptionItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)).OrderBy(v => ChoiceLabel(group, v), StringComparer.CurrentCulture))
        {
            if (seen.Add(value))
            {
                result.Add(new OptionItem(value, ChoiceLabel(group, value)));
            }
        }

        return result;
    }

    private IReadOnlyList<OptionItem> OptionSetEntryOptions(OptionSetDefinition optionSet)
    {
        return optionSet.Values.Keys
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => new OptionItem(v, ChoiceLabel(optionSet.Id, v)))
            .ToList();
    }

    private IReadOnlyList<OptionItem> ComponentTypeOptions() => OptionSetOptions("componentType");
    private IReadOnlyList<OptionItem> ComponentParameterOptions() => OptionSetOptions("componentParameter");

    private IReadOnlyList<OptionItem> EmptyOption()
    {
        return [new OptionItem("", T("dataEditor.option.none", "无"))];
    }

    private string ChoiceLabel(string group, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return T("dataEditor.option.none", "无");
        }

        var configuredLabel = _context.Project.AssetLibrary.OptionSets
            .FirstOrDefault(v => string.Equals(v.Id, group, StringComparison.OrdinalIgnoreCase))
            ?.Values
            .FirstOrDefault(v => string.Equals(v.Key, value, StringComparison.OrdinalIgnoreCase))
            .Value;
        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            return T($"dataEditor.option.{group}.{value}", configuredLabel);
        }

        return T($"dataEditor.option.{group}.{value}", value);
    }

    private IEnumerable<string> CollectTagKeys()
    {
        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "unit", "player", "human", "npc", "enemy", "boss", "summon", "attackable", "interactable", "rescuable",
            "tower", "buildable", "path", "base", "guard", "slime", "ranged", "mage", "building", "trap", "undead", "bloodless", "mechanical", "damageOverTime", "control", "defense", "lifeSteal",
            "nature", "light", "mineral", "breakable", "chest", "attack", "melee", "magic", "projectile",
            "fire", "poison", "slow", "heal", "support", "mobility", "utility", "potion", "mana", "weapon", "sword",
            "dagger", "armor", "key", "rescue", "quest", "material", "herb", "relic",
            "strategy", "economy", "currency", "supply", "research", "build", "territory", "village", "capturePoint"
        };

        foreach (var trait in _context.Project.AssetLibrary.Traits)
        {
            AddTag(tags, trait.Id);
        }

        foreach (var unit in _context.Project.AssetLibrary.Units)
        {
            AddTags(tags, unit.Tags);
            AddTags(tags, unit.Traits);
        }

        foreach (var profile in _context.Project.AssetLibrary.AIProfiles)
        {
            AddTags(tags, profile.TargetTags);
        }

        foreach (var status in _context.Project.AssetLibrary.Statuses)
        {
            AddTags(tags, status.Tags);
        }

        foreach (var effect in _context.Project.AssetLibrary.GameplayEffects)
        {
            AddTags(tags, effect.Tags);
        }

        foreach (var decoration in _context.Project.AssetLibrary.Decorations)
        {
            AddTags(tags, decoration.Tags);
        }

        foreach (var skill in _context.Project.AssetLibrary.Skills)
        {
            AddTags(tags, SplitValues(skill.Tags));
        }

        foreach (var item in _context.Project.AssetLibrary.Items)
        {
            AddTags(tags, SplitValues(item.Tags));
        }

        foreach (var resource in _context.Project.AssetLibrary.ResourceRules)
        {
            AddTags(tags, resource.Tags);
        }

        foreach (var territory in _context.Project.AssetLibrary.TerritoryRules)
        {
            AddTag(tags, territory.TerritoryTag);
            AddTags(tags, territory.RequiredUnitTags);
            AddTags(tags, territory.BlockedUnitTags);
        }

        return tags;
    }

    private IEnumerable<string> CollectContextualTagKeys(TagContext context)
    {
        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in GetBuiltInTagsForContext(context))
        {
            AddTag(tags, value);
        }

        switch (context)
        {
            case TagContext.Unit:
                foreach (var trait in _context.Project.AssetLibrary.Traits)
                {
                    AddTag(tags, trait.Id);
                }

                foreach (var unit in _context.Project.AssetLibrary.Units)
                {
                    AddTags(tags, unit.Tags);
                    AddTags(tags, unit.Traits);
                }

                break;

            case TagContext.Target:
                foreach (var unit in _context.Project.AssetLibrary.Units)
                {
                    AddTags(tags, unit.Tags);
                    AddTags(tags, unit.Traits);
                }

                foreach (var profile in _context.Project.AssetLibrary.AIProfiles)
                {
                    AddTags(tags, profile.TargetTags);
                }
                break;

            case TagContext.Skill:
                foreach (var skill in _context.Project.AssetLibrary.Skills)
                {
                    AddTags(tags, SplitValues(skill.Tags));
                }
                break;

            case TagContext.Effect:
                foreach (var effect in _context.Project.AssetLibrary.GameplayEffects)
                {
                    AddTags(tags, effect.Tags);
                }
                break;

            case TagContext.Status:
                foreach (var status in _context.Project.AssetLibrary.Statuses)
                {
                    AddTags(tags, status.Tags);
                }
                break;

            case TagContext.Item:
                foreach (var item in _context.Project.AssetLibrary.Items)
                {
                    AddTags(tags, SplitValues(item.Tags));
                }
                break;

            case TagContext.Decoration:
                foreach (var decoration in _context.Project.AssetLibrary.Decorations)
                {
                    AddTags(tags, decoration.Tags);
                }
                break;

            case TagContext.Terrain:
                foreach (var terrain in _context.Project.AssetLibrary.TerrainRules)
                {
                    AddTag(tags, terrain.TerrainTag);
                }
                break;

            case TagContext.Area:
                foreach (var buildRule in _context.Project.AssetLibrary.TowerDefenseBuildRules)
                {
                    AddTag(tags, buildRule.BuildSurfaceTag);
                }

                foreach (var terrain in _context.Project.AssetLibrary.TerrainRules)
                {
                    AddTag(tags, terrain.TerrainTag);
                }

                foreach (var objective in _context.Project.AssetLibrary.ObjectiveRules)
                {
                    AddTags(tags, objective.TargetAreaTags);
                }

                foreach (var territory in _context.Project.AssetLibrary.TerritoryRules)
                {
                    AddTag(tags, territory.TerritoryTag);
                }
                break;

            case TagContext.Resource:
                foreach (var resource in _context.Project.AssetLibrary.ResourceRules)
                {
                    AddTags(tags, resource.Tags);
                }
                break;
        }

        return tags;
    }

    private static IEnumerable<string> GetBuiltInTagsForContext(TagContext context)
    {
        return context switch
        {
            TagContext.Unit => ["unit", "player", "human", "npc", "enemy", "boss", "summon", "tower", "building", "trap", "attackable", "interactable", "rescuable", "guard", "slime", "ranged", "mage"],
            TagContext.Target => ["unit", "player", "human", "npc", "enemy", "boss", "summon", "tower", "building", "trap", "attackable", "interactable", "rescuable", "undead", "bloodless", "mechanical"],
            TagContext.Skill => ["attack", "melee", "ranged", "magic", "projectile", "fire", "poison", "heal", "support", "mobility", "utility", "lifeSteal", "control"],
            TagContext.Effect => ["attack", "damageOverTime", "control", "defense", "lifeSteal", "heal", "restore", "knockback", "reward", "physical", "magic", "fire", "poison"],
            TagContext.Status => ["buff", "debuff", "control", "damageOverTime", "defense", "poison", "fire", "slow", "heal", "lifeSteal"],
            TagContext.Item => ["potion", "mana", "weapon", "sword", "dagger", "armor", "key", "quest", "material", "herb", "relic", "lifeSteal"],
            TagContext.Decoration => ["interactable", "breakable", "chest", "nature", "light", "mineral", "rescue", "quest"],
            TagContext.Terrain => ["plain", "forest", "mountain", "water", "path", "base", "buildable", "capturePoint", "territory", "village"],
            TagContext.Area => ["plain", "forest", "mountain", "water", "path", "base", "buildable", "capturePoint", "territory", "village"],
            TagContext.Resource => ["currency", "material", "supply", "research", "economy", "reward", "unlock"],
            _ => []
        };
    }

    private IEnumerable<string> CollectAnimationKeys()
    {
        var keys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "idle", "idleDown", "idleLeft", "idleRight", "idleUp",
            "walkDown", "walkLeft", "walkRight", "walkUp",
            "attackDown", "attackLeft", "attackRight", "attackUp",
            "castDown", "castLeft", "castRight", "castUp",
            "hitDown", "hitLeft", "hitRight", "hitUp",
            "dead", "spawn", "despawn", "open", "closed", "burn"
        };

        foreach (var unit in _context.Project.AssetLibrary.Units)
        {
            AddTags(keys, unit.Animations.Keys);
        }

        foreach (var actor in _context.Project.AssetLibrary.Actors)
        {
            AddTags(keys, actor.Animations.Keys);
        }

        return keys;
    }

    private static void AddTags(ISet<string> destination, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            AddTag(destination, value);
        }
    }

    private static void AddTag(ISet<string> destination, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            destination.Add(value.Trim());
        }
    }

    private string NameOrNone(string id, IReadOnlyList<OptionItem> options)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return T("dataEditor.option.none", "无");
        }

        return options.FirstOrDefault(v => string.Equals(v.Value, id, StringComparison.OrdinalIgnoreCase))?.DisplayName
            ?? UnknownOptionLabel(id);
    }

    private string UnknownOptionLabel(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? T("dataEditor.option.none", "无")
            : T("dataEditor.option.unknown", "未识别项");
    }

    private IEnumerable<string> OrderedOptionKeys(IEnumerable<string> existingKeys, IReadOnlyList<OptionItem> options)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options.Where(v => !string.IsNullOrWhiteSpace(v.Value)))
        {
            if (seen.Add(option.Value))
            {
                result.Add(option.Value);
            }
        }

        foreach (var key in existingKeys.Where(v => !string.IsNullOrWhiteSpace(v)).OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
        {
            if (seen.Add(key))
            {
                result.Add(key);
            }
        }

        return result;
    }

    private Dictionary<string, double> BuildStatEditorValues(Dictionary<string, double> source)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var stat in _context.Project.AssetLibrary.Stats)
        {
            var value = source.TryGetValue(stat.Key, out var currentValue) ? currentValue : stat.DefaultValue;
            result[stat.Key] = IsStatIntegerValueType(stat.ValueType) ? RoundToInteger(value) : value;
        }

        foreach (var pair in source)
        {
            result.TryAdd(pair.Key, pair.Value);
        }

        return result;
    }

    private string TagLabel(string tag)
    {
        var trait = _context.Project.AssetLibrary.Traits.FirstOrDefault(v => string.Equals(v.Id, tag, StringComparison.OrdinalIgnoreCase));
        if (trait is not null)
        {
            return Localized(trait);
        }

        return ChoiceLabel("tag", tag);
    }

    private string AnimationKeyLabel(string key)
    {
        return ChoiceLabel("animationKey", key);
    }

    private string ComponentLabel(string type)
    {
        var preset = _context.Project.AssetLibrary.ComponentPresets.FirstOrDefault(v => string.Equals(v.Component.Type, type, StringComparison.OrdinalIgnoreCase));
        if (preset is not null)
        {
            return Localized(preset);
        }

        return ChoiceLabel("componentType", type);
    }

    private string ComponentParameterLabel(string key)
    {
        return ChoiceLabel("componentParameter", key);
    }

    private string FormatLocalizedValues(IEnumerable<string> values, IReadOnlyList<OptionItem> options)
    {
        var names = values.Select(v => NameOrNone(v, options)).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        return names.Count == 0 ? T("dataEditor.option.none", "无") : string.Join("、", names);
    }

    private static string GetString(object record, string propertyName)
    {
        return record.GetType().GetProperty(propertyName)?.GetValue(record)?.ToString() ?? string.Empty;
    }

    private static void SetString(object record, string propertyName, string value)
    {
        record.GetType().GetProperty(propertyName)?.SetValue(record, value);
    }

    private string T(string key, string fallback) => _localization.T(key, fallback);

    private static string FormatList(IEnumerable<string> values)
    {
        return string.Join(Environment.NewLine, values.Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private static List<string> SplitValues(string text)
    {
        return text.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatTagString(string text)
    {
        return string.Join(",", SplitValues(text));
    }

    private static Dictionary<string, double> ReadDoubleMapGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到映射表格控件。");
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var key = ReadCellString(row, "key");
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var valueText = ReadCellString(row, "value");
            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
                && !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new FormatException($"属性“{ReadCellString(row, "name")}”的数值无效。");
            }

            if (row.Tag is bool integerMode && integerMode)
            {
                value = RoundToInteger(value);
            }

            result[key] = value;
        }

        return result;
    }

    private static Dictionary<string, string> ReadStringMapGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到映射表格控件。");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var key = ReadCellString(row, "key");
            var value = ReadCellString(row, "value");
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static List<ComponentConfig> ReadComponentsGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到组件表格控件。");
        var components = new SortedDictionary<int, ComponentConfig>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var componentType = ReadCellString(row, "componentType");
            if (string.IsNullOrWhiteSpace(componentType))
            {
                continue;
            }

            var index = ReadCellInt(row, "componentIndex");
            if (!components.TryGetValue(index, out var component))
            {
                component = new ComponentConfig { Type = componentType };
                components[index] = component;
            }

            var parameterKey = ReadCellString(row, "parameterKey");
            if (!string.IsNullOrWhiteSpace(parameterKey))
            {
                component.Parameters[parameterKey] = ParseObjectValue(ReadCellString(row, "value"));
            }
        }

        return components.Values.ToList();
    }

    private static List<TowerDefenseWaypointDefinition> ReadTowerDefenseWaypointGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到路线点表格控件。");
        var waypoints = new List<TowerDefenseWaypointDefinition>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            waypoints.Add(row.Tag is TowerDefenseWaypointDefinition waypoint
                ? CloneTowerDefenseWaypoint(waypoint)
                : new TowerDefenseWaypointDefinition
                {
                    Key = ReadCellString(row, "key"),
                    X = ReadCellDouble(row, "x"),
                    Y = ReadCellDouble(row, "y"),
                    WaitSeconds = ReadCellDouble(row, "waitSeconds")
                });
        }

        return waypoints;
    }

    private static List<TowerDefenseSpawnGroupDefinition> ReadTowerDefenseSpawnGroupGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到生成组表格控件。");
        var spawnGroups = new List<TowerDefenseSpawnGroupDefinition>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            spawnGroups.Add(row.Tag is TowerDefenseSpawnGroupDefinition spawnGroup
                ? CloneTowerDefenseSpawnGroup(spawnGroup)
                : new TowerDefenseSpawnGroupDefinition
                {
                    UnitId = ReadCellString(row, "unitId"),
                    Count = Math.Max(1, ReadCellInt(row, "count")),
                    IntervalSeconds = Math.Max(0, ReadCellDouble(row, "intervalSeconds")),
                    DelaySeconds = Math.Max(0, ReadCellDouble(row, "delaySeconds")),
                    PathId = ReadCellString(row, "pathId")
                });
        }

        return spawnGroups;
    }

    private static List<string> ReadTowerDefenseWaveRefGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到波次引用表格控件。");
        return ReadGridColumnValues(grid, "waveId")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<TowerDefenseTowerLevelDefinition> ReadTowerDefenseTowerLevelGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到升级等级表格控件。");
        var levels = new List<TowerDefenseTowerLevelDefinition>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            levels.Add(row.Tag is TowerDefenseTowerLevelDefinition level
                ? CloneTowerDefenseTowerLevel(level)
                : new TowerDefenseTowerLevelDefinition
                {
                    Level = Math.Max(1, ReadCellInt(row, "level")),
                    UpgradeCost = Math.Max(0, ReadCellInt(row, "upgradeCost")),
                    RangeBonus = ReadCellDouble(row, "rangeBonus"),
                    DamageMultiplier = Math.Max(0, ReadCellDouble(row, "damageMultiplier")),
                    AttackIntervalMultiplier = Math.Max(0, ReadCellDouble(row, "attackIntervalMultiplier")),
                    SkillId = ReadCellString(row, "skillId")
                });
        }

        return levels
            .OrderBy(level => level.Level)
            .ToList();
    }

    private static List<ComponentConfig> CloneComponents(IEnumerable<ComponentConfig> components)
    {
        return components.Select(component => new ComponentConfig
        {
            Type = component.Type,
            Parameters = new Dictionary<string, object?>(component.Parameters, StringComparer.OrdinalIgnoreCase)
        }).ToList();
    }

    private static List<TowerDefenseWaypointDefinition> CloneTowerDefenseWaypoints(IEnumerable<TowerDefenseWaypointDefinition> waypoints)
    {
        return waypoints.Select(CloneTowerDefenseWaypoint).ToList();
    }

    private static TowerDefenseWaypointDefinition CloneTowerDefenseWaypoint(TowerDefenseWaypointDefinition waypoint)
    {
        return new TowerDefenseWaypointDefinition
        {
            Key = waypoint.Key,
            X = waypoint.X,
            Y = waypoint.Y,
            WaitSeconds = waypoint.WaitSeconds
        };
    }

    private static List<TowerDefenseSpawnGroupDefinition> CloneTowerDefenseSpawnGroups(IEnumerable<TowerDefenseSpawnGroupDefinition> spawnGroups)
    {
        return spawnGroups.Select(CloneTowerDefenseSpawnGroup).ToList();
    }

    private static TowerDefenseSpawnGroupDefinition CloneTowerDefenseSpawnGroup(TowerDefenseSpawnGroupDefinition spawnGroup)
    {
        return new TowerDefenseSpawnGroupDefinition
        {
            UnitId = spawnGroup.UnitId,
            Count = spawnGroup.Count,
            IntervalSeconds = spawnGroup.IntervalSeconds,
            DelaySeconds = spawnGroup.DelaySeconds,
            PathId = spawnGroup.PathId
        };
    }

    private static List<TowerDefenseTowerLevelDefinition> CloneTowerDefenseTowerLevels(IEnumerable<TowerDefenseTowerLevelDefinition> levels)
    {
        return levels.Select(CloneTowerDefenseTowerLevel).ToList();
    }

    private static TowerDefenseTowerLevelDefinition CloneTowerDefenseTowerLevel(TowerDefenseTowerLevelDefinition level)
    {
        return new TowerDefenseTowerLevelDefinition
        {
            Level = level.Level,
            UpgradeCost = level.UpgradeCost,
            RangeBonus = level.RangeBonus,
            DamageMultiplier = level.DamageMultiplier,
            AttackIntervalMultiplier = level.AttackIntervalMultiplier,
            SkillId = level.SkillId
        };
    }

    private static string ReadCellString(DataGridViewRow row, string columnName)
    {
        return row.Cells[columnName].Value?.ToString()?.Trim() ?? string.Empty;
    }

    private static int ReadCellInt(DataGridViewRow row, string columnName)
    {
        return int.TryParse(row.Cells[columnName].Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || int.TryParse(row.Cells[columnName].Value?.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            ? value
            : 0;
    }

    private static double ReadCellDouble(DataGridViewRow row, string columnName)
    {
        return double.TryParse(row.Cells[columnName].Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || double.TryParse(row.Cells[columnName].Value?.ToString(), NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            ? value
            : 0;
    }

    private static IEnumerable<string> ReadGridColumnValues(DataGridView grid, string columnName)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (!row.IsNewRow)
            {
                yield return ReadCellString(row, columnName);
            }
        }
    }

    private static bool TryGetEditableGridRow(DataGridView grid, int rowIndex, out DataGridViewRow row)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Rows[rowIndex].IsNewRow)
        {
            row = null!;
            return false;
        }

        grid.EndEdit();
        row = grid.Rows[rowIndex];
        return true;
    }

    private static object? ParseObjectValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (bool.TryParse(text, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out doubleValue))
        {
            return doubleValue;
        }

        return text;
    }

    private static string FormatDoubleMap(Dictionary<string, double> values)
    {
        return string.Join(Environment.NewLine, values.OrderBy(v => v.Key).Select(v => $"{v.Key} = {v.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static Dictionary<string, double> ParseDoubleMap(string text)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in ReadMeaningfulLines(text))
        {
            var parts = SplitKeyValue(line);
            if (parts is null)
            {
                throw new FormatException("属性格式应为 key = number。");
            }

            if (!double.TryParse(parts.Value.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && !double.TryParse(parts.Value.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                throw new FormatException($"属性“{parts.Value.Key}”的数值无效。");
            }

            result[parts.Value.Key] = value;
        }

        return result;
    }

    private static string FormatStringMap(Dictionary<string, string> values)
    {
        return string.Join(Environment.NewLine, values.OrderBy(v => v.Key).Select(v => $"{v.Key} = {v.Value}"));
    }

    private static Dictionary<string, string> ParseStringMap(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in ReadMeaningfulLines(text))
        {
            var parts = SplitKeyValue(line);
            if (parts is null)
            {
                throw new FormatException("参数格式应为 key = value。");
            }

            result[parts.Value.Key] = parts.Value.Value;
        }

        return result;
    }

    private static string FormatObjectMap(Dictionary<string, object?> values)
    {
        return string.Join(Environment.NewLine, values.OrderBy(v => v.Key).Select(v => $"{v.Key} = {ObjectToText(v.Value)}"));
    }

    private static Dictionary<string, object?> ParseObjectMap(string text)
    {
        return ParseStringMap(text).ToDictionary(v => v.Key, v => (object?)v.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string ObjectToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            JsonElement json => json.ValueKind == JsonValueKind.String ? json.GetString() ?? string.Empty : json.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatComponents(IEnumerable<ComponentConfig> components)
    {
        return string.Join(Environment.NewLine, components.Select(component =>
        {
            var parameters = component.Parameters.Select(v => $"{v.Key}={ObjectToText(v.Value)}");
            return string.Join(" | ", new[] { component.Type }.Concat(parameters));
        }));
    }

    private static List<ComponentConfig> ParseComponents(string text)
    {
        var result = new List<ComponentConfig>();
        foreach (var line in ReadMeaningfulLines(text))
        {
            var segments = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
            {
                continue;
            }

            var component = new ComponentConfig { Type = segments[0] };
            foreach (var segment in segments.Skip(1))
            {
                var parts = SplitKeyValue(segment);
                if (parts is null)
                {
                    throw new FormatException($"组件“{component.Type}”的参数格式应为 key=value。");
                }

                component.Parameters[parts.Value.Key] = parts.Value.Value;
            }

            result.Add(component);
        }

        return result;
    }

    private static string FormatItemValues(IEnumerable<ItemFieldValue> values)
    {
        return string.Join(Environment.NewLine, values.Select(v => $"{v.Key} = {v.Value}"));
    }

    private static List<ItemFieldValue> ParseItemValues(string text)
    {
        return ParseStringMap(text).Select(v => new ItemFieldValue { Key = v.Key, Value = v.Value }).ToList();
    }

    private Dictionary<string, string> BuildItemCustomValueMap(IEnumerable<ItemFieldValue> values)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value.Key))
            {
                continue;
            }

            result[value.Key] = value.Value;
        }

        return result;
    }

    private List<ItemFieldValue> BuildItemCustomValues(Dictionary<string, string> values, string itemTypeId)
    {
        var options = ItemFieldOptions(itemTypeId);
        return OrderedOptionKeys(values.Keys, options)
            .Where(values.ContainsKey)
            .Select(key => new ItemFieldValue { Key = key, Value = values[key] })
            .ToList();
    }

    private static List<GameplayEffectReference> ReadGameplayEffectReferenceGrid(Control control)
    {
        var grid = FindEditorGrid(control) ?? throw new InvalidOperationException("无法找到效果引用表格控件。");
        var references = new List<GameplayEffectReference>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            if (row.Tag is GameplayEffectReference reference)
            {
                references.Add(CloneGameplayEffectReference(reference));
                continue;
            }

            var effectId = ReadCellString(row, "effectId");
            if (!string.IsNullOrWhiteSpace(effectId))
            {
                references.Add(new GameplayEffectReference { EffectId = effectId });
            }
        }

        return references;
    }

    private static string FormatLootEntries(IEnumerable<LootEntryDefinition> entries)
    {
        return string.Join(Environment.NewLine, entries.Select(v => $"{v.ItemId} | min={v.MinQuantity} | max={v.MaxQuantity} | chance={v.Chance.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static List<LootEntryDefinition> ParseLootEntries(string text)
    {
        var entries = new List<LootEntryDefinition>();
        foreach (var line in ReadMeaningfulLines(text))
        {
            var segments = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            var entry = new LootEntryDefinition { ItemId = segments[0] };
            foreach (var segment in segments.Skip(1))
            {
                var pair = SplitKeyValue(segment);
                if (pair is null)
                {
                    throw new FormatException("掉落条目格式应为 item.id | min=1 | max=1 | chance=1。");
                }

                switch (pair.Value.Key)
                {
                    case "min":
                        entry.MinQuantity = int.Parse(pair.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "max":
                        entry.MaxQuantity = int.Parse(pair.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "chance":
                        entry.Chance = double.Parse(pair.Value.Value, CultureInfo.InvariantCulture);
                        break;
                }
            }

            entries.Add(entry);
        }

        return entries;
    }

    private string FormatItemFields(IEnumerable<ItemFieldDefinition> fields)
    {
        return string.Join(Environment.NewLine, fields.OrderBy(v => v.Order).Select(v =>
        {
            var displayName = string.IsNullOrWhiteSpace(v.DisplayName)
                ? T(v.DisplayNameKey, v.Key)
                : v.DisplayName;
            return $"{v.Key} | name={displayName} | nameKey={v.DisplayNameKey} | type={v.ValueType} | default={v.DefaultValue} | required={v.Required.ToString().ToLowerInvariant()} | readonly={v.ReadOnly.ToString().ToLowerInvariant()} | category={v.CategoryKey} | order={v.Order} | options={string.Join(",", v.Options)}";
        }));
    }

    private static List<ItemFieldDefinition> ParseItemFields(string text)
    {
        var fields = new List<ItemFieldDefinition>();
        foreach (var line in ReadMeaningfulLines(text))
        {
            var segments = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            var field = new ItemFieldDefinition { Key = segments[0] };
            foreach (var segment in segments.Skip(1))
            {
                var pair = SplitKeyValue(segment);
                if (pair is null)
                {
                    throw new FormatException("字段格式应为 key | name=显示名 | type=Text。");
                }

                switch (pair.Value.Key)
                {
                    case "name":
                        field.DisplayName = pair.Value.Value;
                        break;
                    case "nameKey":
                        field.DisplayNameKey = pair.Value.Value;
                        break;
                    case "type":
                        if (Enum.TryParse<ItemFieldValueType>(pair.Value.Value, ignoreCase: true, out var valueType))
                        {
                            field.ValueType = valueType;
                        }
                        break;
                    case "default":
                        field.DefaultValue = pair.Value.Value;
                        break;
                    case "required":
                        field.Required = bool.TryParse(pair.Value.Value, out var required) && required;
                        break;
                    case "readonly":
                        field.ReadOnly = bool.TryParse(pair.Value.Value, out var readOnly) && readOnly;
                        break;
                    case "category":
                        field.CategoryKey = pair.Value.Value;
                        field.Category = pair.Value.Value;
                        break;
                    case "order":
                        field.Order = int.TryParse(pair.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order) ? order : 0;
                        break;
                    case "options":
                        field.Options = SplitValues(pair.Value.Value);
                        break;
                }
            }

            fields.Add(field);
        }

        return fields;
    }

    private static IEnumerable<string> ReadMeaningfulLines(string text)
    {
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v));
    }

    private static (string Key, string Value)? SplitKeyValue(string text)
    {
        var index = text.IndexOf('=');
        if (index < 0)
        {
            return null;
        }

        var key = text[..index].Trim();
        var value = text[(index + 1)..].Trim();
        return string.IsNullOrWhiteSpace(key) ? null : (key, value);
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static string UniqueId(string prefix, IEnumerable<string> existingIds)
    {
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(prefix))
        {
            return prefix;
        }

        for (var i = 1; i < 10_000; i++)
        {
            var candidate = $"{prefix}.{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{prefix}.{Guid.NewGuid():N}";
    }

    private static string UniqueSimpleKey(string prefix, IEnumerable<string> existingIds)
    {
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(prefix))
        {
            return prefix;
        }

        for (var i = 1; i < 10_000; i++)
        {
            var candidate = $"{prefix}{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{prefix}{Guid.NewGuid():N}";
    }

    private FieldSpec TextField(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, string> read, Action<object, string> write, bool readOnly = false)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Text, read, write) { ReadOnly = readOnly };
    }

    private FieldSpec Multiline(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, string> read, Action<object, string> write, int height, string hintKey = "", string hintFallback = "")
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.MultilineText, read, write) { Height = height, HintKey = hintKey, HintFallback = hintFallback };
    }

    private FieldSpec Choice(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, string> read, Action<object, string> write, Func<IReadOnlyList<OptionItem>> options, bool readOnly = false)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Choice, read, write) { GetOptions = options, ReadOnly = readOnly };
    }

    private FieldSpec MultiChoice(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, string> read, Action<object, string> write, Func<IReadOnlyList<OptionItem>> options, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.MultiChoice, read, write) { GetOptions = options, Height = height };
    }

    private FieldSpec DoubleMap(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, Dictionary<string, double>> read, Action<object, Dictionary<string, double>> write, Func<IReadOnlyList<OptionItem>> options, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.DoubleMap, _ => string.Empty, null)
        {
            ReadDoubleMap = read,
            WriteDoubleMap = write,
            GetOptions = options,
            Height = height
        };
    }

    private FieldSpec StringMap(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, Dictionary<string, string>> read, Action<object, Dictionary<string, string>> write, Func<IReadOnlyList<OptionItem>> options, int height, bool allowCustomKeys = false)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.StringMap, _ => string.Empty, null)
        {
            ReadStringMap = read,
            WriteStringMap = write,
            GetOptions = options,
            Height = height,
            AllowCustomKeys = allowCustomKeys
        };
    }

    private FieldSpec Components(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<ComponentConfig>> read, Action<object, List<ComponentConfig>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Components, _ => string.Empty, null)
        {
            ReadComponents = read,
            WriteComponents = write,
            Height = height
        };
    }

    private FieldSpec EffectRefs(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<GameplayEffectReference>> read, Action<object, List<GameplayEffectReference>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.EffectReferences, _ => string.Empty, null)
        {
            ReadEffectReferences = read,
            WriteEffectReferences = write,
            Height = height
        };
    }

    private FieldSpec EffectParameters(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<EffectParameterDefinition>> read, Action<object, List<EffectParameterDefinition>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.EffectParameters, _ => string.Empty, null)
        {
            ReadEffectParameters = read,
            WriteEffectParameters = write,
            Height = height
        };
    }

    private FieldSpec TowerDefenseWaypoints(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<TowerDefenseWaypointDefinition>> read, Action<object, List<TowerDefenseWaypointDefinition>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.TowerDefenseWaypoints, _ => string.Empty, null)
        {
            ReadTowerDefenseWaypoints = read,
            WriteTowerDefenseWaypoints = write,
            Height = height
        };
    }

    private FieldSpec TowerDefenseSpawnGroups(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<TowerDefenseSpawnGroupDefinition>> read, Action<object, List<TowerDefenseSpawnGroupDefinition>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.TowerDefenseSpawnGroups, _ => string.Empty, null)
        {
            ReadTowerDefenseSpawnGroups = read,
            WriteTowerDefenseSpawnGroups = write,
            Height = height
        };
    }

    private FieldSpec TowerDefenseWaveRefs(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<string>> read, Action<object, List<string>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.TowerDefenseWaveRefs, _ => string.Empty, null)
        {
            ReadTowerDefenseWaveRefs = read,
            WriteTowerDefenseWaveRefs = write,
            Height = height
        };
    }

    private FieldSpec TowerDefenseTowerLevels(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, List<TowerDefenseTowerLevelDefinition>> read, Action<object, List<TowerDefenseTowerLevelDefinition>> write, int height)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.TowerDefenseTowerLevels, _ => string.Empty, null)
        {
            ReadTowerDefenseTowerLevels = read,
            WriteTowerDefenseTowerLevels = write,
            Height = height
        };
    }

    private FieldSpec Number(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, double> read, Action<object, double> write, double min = -999999, double max = 999999, int decimals = 2)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Number, _ => string.Empty, null)
        {
            ReadNumber = read,
            WriteNumber = write,
            NumberMin = (decimal)min,
            NumberMax = (decimal)max,
            DecimalPlaces = decimals
        };
    }

    private FieldSpec Integer(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, int> read, Action<object, int> write, int min = -999999, int max = 999999)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Integer, _ => string.Empty, null)
        {
            ReadInteger = read,
            WriteInteger = write,
            IntegerMin = min,
            IntegerMax = max
        };
    }

    private FieldSpec Bool(string sectionKey, string sectionFallback, string labelKey, string labelFallback, Func<object, bool> read, Action<object, bool> write, bool readOnly = false)
    {
        return new FieldSpec(sectionKey, sectionFallback, labelKey, labelFallback, FieldKind.Boolean, _ => string.Empty, null)
        {
            ReadBool = read,
            WriteBool = write,
            ReadOnly = readOnly
        };
    }

    private sealed class FieldRowState
    {
        public FieldRowState(FieldSpec field, object record)
        {
            Field = field;
            Record = record;
            TextValue = field.ReadText(record);
            NumberValue = field.ReadNumber(record);
            IntegerValue = field.ReadInteger(record);
            BoolValue = field.ReadBool(record);
            DoubleMapValue = new Dictionary<string, double>(field.ReadDoubleMap(record), StringComparer.OrdinalIgnoreCase);
            StringMapValue = new Dictionary<string, string>(field.ReadStringMap(record), StringComparer.OrdinalIgnoreCase);
            ComponentsValue = CloneComponents(field.ReadComponents(record));
            EffectReferencesValue = field.ReadEffectReferences(record).Select(CloneGameplayEffectReference).ToList();
            EffectParametersValue = field.ReadEffectParameters(record).Select(CloneEffectParameterDefinition).ToList();
            TowerDefenseWaypointsValue = CloneTowerDefenseWaypoints(field.ReadTowerDefenseWaypoints(record));
            TowerDefenseSpawnGroupsValue = CloneTowerDefenseSpawnGroups(field.ReadTowerDefenseSpawnGroups(record));
            TowerDefenseWaveRefsValue = [.. field.ReadTowerDefenseWaveRefs(record)];
            TowerDefenseTowerLevelsValue = CloneTowerDefenseTowerLevels(field.ReadTowerDefenseTowerLevels(record));
        }

        public FieldSpec Field { get; }
        public object Record { get; }
        public string TextValue { get; set; }
        public double NumberValue { get; set; }
        public int IntegerValue { get; set; }
        public bool BoolValue { get; set; }
        public Dictionary<string, double> DoubleMapValue { get; set; }
        public Dictionary<string, string> StringMapValue { get; set; }
        public List<ComponentConfig> ComponentsValue { get; set; }
        public List<GameplayEffectReference> EffectReferencesValue { get; set; }
        public List<EffectParameterDefinition> EffectParametersValue { get; set; }
        public List<TowerDefenseWaypointDefinition> TowerDefenseWaypointsValue { get; set; }
        public List<TowerDefenseSpawnGroupDefinition> TowerDefenseSpawnGroupsValue { get; set; }
        public List<string> TowerDefenseWaveRefsValue { get; set; }
        public List<TowerDefenseTowerLevelDefinition> TowerDefenseTowerLevelsValue { get; set; }
        private IReadOnlyDictionary<string, FieldRowState>? _siblings;

        public void AttachSiblings(IReadOnlyDictionary<string, FieldRowState> siblings)
        {
            _siblings = siblings;
        }

        public FieldRowState? FindSibling(string labelKey)
        {
            return _siblings is not null && _siblings.TryGetValue(labelKey, out var sibling) ? sibling : null;
        }

        public void Apply()
        {
            switch (Field.Kind)
            {
                case FieldKind.Text:
                case FieldKind.MultilineText:
            case FieldKind.Choice:
            case FieldKind.MultiChoice:
                    Field.WriteText?.Invoke(Record, TextValue);
                    break;
                case FieldKind.Number:
                    Field.WriteNumber?.Invoke(Record, IsIntegerNumericField(this) ? RoundToInteger(NumberValue) : NumberValue);
                    break;
                case FieldKind.Integer:
                    Field.WriteInteger?.Invoke(Record, IntegerValue);
                    break;
                case FieldKind.Boolean:
                    Field.WriteBool?.Invoke(Record, BoolValue);
                    break;
                case FieldKind.DoubleMap:
                    Field.WriteDoubleMap?.Invoke(Record, DoubleMapValue);
                    break;
                case FieldKind.StringMap:
                    Field.WriteStringMap?.Invoke(Record, StringMapValue);
                    break;
                case FieldKind.Components:
                    Field.WriteComponents?.Invoke(Record, ComponentsValue);
                    break;
                case FieldKind.EffectReferences:
                    Field.WriteEffectReferences?.Invoke(Record, EffectReferencesValue);
                    break;
                case FieldKind.EffectParameters:
                    Field.WriteEffectParameters?.Invoke(Record, EffectParametersValue);
                    break;
                case FieldKind.TowerDefenseWaypoints:
                    Field.WriteTowerDefenseWaypoints?.Invoke(Record, TowerDefenseWaypointsValue);
                    break;
                case FieldKind.TowerDefenseSpawnGroups:
                    Field.WriteTowerDefenseSpawnGroups?.Invoke(Record, TowerDefenseSpawnGroupsValue);
                    break;
                case FieldKind.TowerDefenseWaveRefs:
                    Field.WriteTowerDefenseWaveRefs?.Invoke(Record, TowerDefenseWaveRefsValue);
                    break;
                case FieldKind.TowerDefenseTowerLevels:
                    Field.WriteTowerDefenseTowerLevels?.Invoke(Record, TowerDefenseTowerLevelsValue);
                    break;
            }
        }
    }

    private sealed class FieldEditor
    {
        private readonly FieldSpec? _field;
        private readonly object? _record;
        private readonly Control? _control;
        private readonly IReadOnlyList<FieldRowState>? _rows;

        public FieldEditor(FieldSpec field, object record, Control control)
        {
            _field = field;
            _record = record;
            _control = control;
        }

        public FieldEditor(IReadOnlyList<FieldRowState> rows)
        {
            _rows = rows;
        }

        public bool Apply(out string error)
        {
            error = string.Empty;
            try
            {
                if (_rows is not null)
                {
                    foreach (var row in _rows)
                    {
                        row.Apply();
                    }

                    return true;
                }

                if (_field is null || _record is null || _control is null)
                {
                    return true;
                }

                switch (_field.Kind)
                {
                    case FieldKind.Text:
                    case FieldKind.MultilineText:
                        _field.WriteText?.Invoke(_record, ((TextBox)_control).Text);
                        break;
                    case FieldKind.Number:
                        var numeric = (NumericUpDown)_control;
                        _field.WriteNumber?.Invoke(_record, _field.UseIntegerNumericControl(_record) ? RoundToInteger((double)numeric.Value) : (double)numeric.Value);
                        break;
                    case FieldKind.Integer:
                        _field.WriteInteger?.Invoke(_record, (int)((NumericUpDown)_control).Value);
                        break;
                    case FieldKind.Boolean:
                        _field.WriteBool?.Invoke(_record, ((CheckBox)_control).Checked);
                        break;
                    case FieldKind.Choice:
                        _field.WriteText?.Invoke(_record, ((ComboBox)_control).SelectedItem is OptionItem item ? item.Value : string.Empty);
                        break;
                    case FieldKind.MultiChoice:
                        _field.WriteText?.Invoke(_record, string.Join(Environment.NewLine, ReadMultiChoiceValues(_control)));
                        break;
                    case FieldKind.DoubleMap:
                        _field.WriteDoubleMap?.Invoke(_record, ReadDoubleMapGrid(_control));
                        break;
                    case FieldKind.StringMap:
                        _field.WriteStringMap?.Invoke(_record, ReadStringMapGrid(_control));
                        break;
                    case FieldKind.Components:
                        _field.WriteComponents?.Invoke(_record, ReadComponentsGrid(_control));
                        break;
                    case FieldKind.EffectReferences:
                        _field.WriteEffectReferences?.Invoke(_record, ReadGameplayEffectReferenceGrid(_control));
                        break;
                    case FieldKind.EffectParameters:
                        _field.WriteEffectParameters?.Invoke(_record, ReadEffectParameterDefinitionGrid(_control));
                        break;
                    case FieldKind.TowerDefenseWaypoints:
                        _field.WriteTowerDefenseWaypoints?.Invoke(_record, ReadTowerDefenseWaypointGrid(_control));
                        break;
                    case FieldKind.TowerDefenseSpawnGroups:
                        _field.WriteTowerDefenseSpawnGroups?.Invoke(_record, ReadTowerDefenseSpawnGroupGrid(_control));
                        break;
                    case FieldKind.TowerDefenseWaveRefs:
                        _field.WriteTowerDefenseWaveRefs?.Invoke(_record, ReadTowerDefenseWaveRefGrid(_control));
                        break;
                    case FieldKind.TowerDefenseTowerLevels:
                        _field.WriteTowerDefenseTowerLevels?.Invoke(_record, ReadTowerDefenseTowerLevelGrid(_control));
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    private sealed class FieldSpec
    {
        public FieldSpec(string sectionKey, string sectionFallback, string labelKey, string labelFallback, FieldKind kind, Func<object, string> readText, Action<object, string>? writeText)
        {
            SectionKey = sectionKey;
            SectionFallback = sectionFallback;
            LabelKey = labelKey;
            LabelFallback = labelFallback;
            Kind = kind;
            ReadText = readText;
            WriteText = writeText;
        }

        public string SectionKey { get; }
        public string SectionFallback { get; }
        public string LabelKey { get; }
        public string LabelFallback { get; }
        public FieldKind Kind { get; }
        public Func<object, string> ReadText { get; }
        public Action<object, string>? WriteText { get; }
        public Func<object, bool> UseIntegerNumericControl { get; set; } = _ => false;
        public Func<object, string>? ReadDisplayText { get; set; }
        public Func<object, double> ReadNumber { get; init; } = _ => 0;
        public Action<object, double>? WriteNumber { get; init; }
        public Func<object, int> ReadInteger { get; init; } = _ => 0;
        public Action<object, int>? WriteInteger { get; init; }
        public Func<object, bool> ReadBool { get; init; } = _ => false;
        public Action<object, bool>? WriteBool { get; init; }
        public Func<IReadOnlyList<OptionItem>> GetOptions { get; init; } = () => [];
        public Func<object, Dictionary<string, double>> ReadDoubleMap { get; init; } = _ => [];
        public Action<object, Dictionary<string, double>>? WriteDoubleMap { get; init; }
        public Func<object, Dictionary<string, string>> ReadStringMap { get; init; } = _ => [];
        public Action<object, Dictionary<string, string>>? WriteStringMap { get; init; }
        public Func<object, List<ComponentConfig>> ReadComponents { get; init; } = _ => [];
        public Action<object, List<ComponentConfig>>? WriteComponents { get; init; }
        public Func<object, List<GameplayEffectReference>> ReadEffectReferences { get; init; } = _ => [];
        public Action<object, List<GameplayEffectReference>>? WriteEffectReferences { get; init; }
        public Func<object, List<EffectParameterDefinition>> ReadEffectParameters { get; init; } = _ => [];
        public Action<object, List<EffectParameterDefinition>>? WriteEffectParameters { get; init; }
        public Func<object, List<TowerDefenseWaypointDefinition>> ReadTowerDefenseWaypoints { get; init; } = _ => [];
        public Action<object, List<TowerDefenseWaypointDefinition>>? WriteTowerDefenseWaypoints { get; init; }
        public Func<object, List<TowerDefenseSpawnGroupDefinition>> ReadTowerDefenseSpawnGroups { get; init; } = _ => [];
        public Action<object, List<TowerDefenseSpawnGroupDefinition>>? WriteTowerDefenseSpawnGroups { get; init; }
        public Func<object, List<string>> ReadTowerDefenseWaveRefs { get; init; } = _ => [];
        public Action<object, List<string>>? WriteTowerDefenseWaveRefs { get; init; }
        public Func<object, List<TowerDefenseTowerLevelDefinition>> ReadTowerDefenseTowerLevels { get; init; } = _ => [];
        public Action<object, List<TowerDefenseTowerLevelDefinition>>? WriteTowerDefenseTowerLevels { get; init; }
        public bool AllowCustomKeys { get; init; }
        public int Height { get; init; } = 34;
        public bool ReadOnly { get; init; }
        public decimal NumberMin { get; init; } = -999999M;
        public decimal NumberMax { get; init; } = 999999M;
        public int DecimalPlaces { get; init; } = 2;
        public int IntegerMin { get; init; } = -999999;
        public int IntegerMax { get; init; } = 999999;
        public string HintKey { get; init; } = "";
        public string HintFallback { get; init; } = "";
    }

    private sealed record CategoryDescriptor(
        string Key,
        string DomainKey,
        string DisplayKey,
        string FallbackName,
        string HelpKey,
        string FallbackHelp,
        Func<AssetLibrary, IList> GetItems,
        Func<object> CreateNew,
        Func<object, string> GetId,
        Func<object, string> GetName,
        Func<object, string> GetTypeText,
        Func<object, string> GetSummary);

    private sealed record NavigationGroupDescriptor(string Key, string DisplayKey, string FallbackName, IReadOnlyList<string> CategoryKeys);

    private sealed record AssetSearchResult(CategoryDescriptor Category, object Record);

    private sealed record OptionItem(string Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            DoubleBuffered = true;
        }
    }

    private sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public BufferedFlowLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    private sealed class FormulaExpressionEvaluator
    {
        private readonly string _expression;
        private readonly IReadOnlyDictionary<string, double> _variables;
        private int _position;

        private FormulaExpressionEvaluator(string expression, IReadOnlyDictionary<string, double> variables)
        {
            _expression = expression;
            _variables = variables;
        }

        public static double Evaluate(string expression, IReadOnlyDictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new FormatException("表达式不能为空。");
            }

            var evaluator = new FormulaExpressionEvaluator(expression, variables);
            var value = evaluator.ParseExpression();
            evaluator.SkipWhitespace();
            if (!evaluator.IsAtEnd)
            {
                throw new FormatException("表达式末尾存在无法识别的内容。");
            }

            return value;
        }

        private bool IsAtEnd => _position >= _expression.Length;

        private char Current => IsAtEnd ? '\0' : _expression[_position];

        private double ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (TryConsume('+'))
                {
                    value += ParseTerm();
                }
                else if (TryConsume('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseTerm()
        {
            var value = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (TryConsume('*'))
                {
                    value *= ParseFactor();
                }
                else if (TryConsume('/'))
                {
                    var divisor = ParseFactor();
                    if (Math.Abs(divisor) < double.Epsilon)
                    {
                        throw new DivideByZeroException("除数不能为 0。");
                    }

                    value /= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();
            if (TryConsume('+'))
            {
                return ParseFactor();
            }

            if (TryConsume('-'))
            {
                return -ParseFactor();
            }

            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWhitespace();
            if (TryConsume('('))
            {
                var value = ParseExpression();
                Expect(')');
                return value;
            }

            if (char.IsDigit(Current) || Current == '.')
            {
                return ParseNumber();
            }

            if (IsIdentifierStart(Current))
            {
                var identifier = ParseIdentifier();
                SkipWhitespace();
                if (TryConsume('('))
                {
                    return ParseFunction(identifier);
                }

                if (_variables.TryGetValue(identifier, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException($"变量 {identifier} 没有测试值。");
            }

            throw new FormatException("表达式中存在无法识别的字符。");
        }

        private double ParseFunction(string name)
        {
            var args = new List<double>();
            SkipWhitespace();
            if (!TryConsume(')'))
            {
                while (true)
                {
                    args.Add(ParseExpression());
                    SkipWhitespace();
                    if (TryConsume(')'))
                    {
                        break;
                    }

                    Expect(',');
                }
            }

            return name.ToLowerInvariant() switch
            {
                "max" when args.Count >= 2 => args.Max(),
                "min" when args.Count >= 2 => args.Min(),
                "clamp" when args.Count == 3 => Math.Min(Math.Max(args[0], args[1]), args[2]),
                "abs" when args.Count == 1 => Math.Abs(args[0]),
                "round" when args.Count == 1 => Math.Round(args[0]),
                "floor" when args.Count == 1 => Math.Floor(args[0]),
                "ceil" or "ceiling" when args.Count == 1 => Math.Ceiling(args[0]),
                _ => throw new FormatException($"函数 {name} 的参数数量不正确或暂不支持。")
            };
        }

        private double ParseNumber()
        {
            var start = _position;
            while (char.IsDigit(Current) || Current == '.')
            {
                _position++;
            }

            var text = _expression[start.._position];
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            throw new FormatException($"数字 {text} 无法解析。");
        }

        private string ParseIdentifier()
        {
            var start = _position;
            while (IsIdentifierPart(Current) || Current == '.')
            {
                _position++;
            }

            return _expression[start.._position];
        }

        private void Expect(char expected)
        {
            SkipWhitespace();
            if (!TryConsume(expected))
            {
                throw new FormatException($"缺少字符 {expected}。");
            }
        }

        private bool TryConsume(char value)
        {
            if (Current != value)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private static bool IsIdentifierStart(char value)
        {
            return char.IsLetter(value) || value == '_';
        }

        private static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }

    private enum FieldKind
    {
        Text,
        MultilineText,
        Number,
        Integer,
        Boolean,
        Choice,
        MultiChoice,
        DoubleMap,
        StringMap,
        Components,
        EffectReferences,
        EffectParameters,
        TowerDefenseWaypoints,
        TowerDefenseSpawnGroups,
        TowerDefenseWaveRefs,
        TowerDefenseTowerLevels
    }

    private enum TagContext
    {
        Unit,
        Target,
        Skill,
        Effect,
        Status,
        Item,
        Decoration,
        Terrain,
        Area,
        Resource
    }
}
