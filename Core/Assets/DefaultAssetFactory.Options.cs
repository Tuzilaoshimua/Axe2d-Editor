namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    public static List<OptionSetDefinition> CreateDefaultOptionSets()
    {
        return
        [
            OptionSet("unitKind", "单位类型", ("player", "玩家"), ("npc", "NPC"), ("enemy", "敌人"), ("boss", "首领"), ("summon", "召唤物"), ("tower", "固定/可建造单位")),
            OptionSet("attitude", "阵营立场", ("friendly", "友方"), ("neutral", "中立"), ("hostile", "敌对")),
            OptionSet("valueType", "数值类型", ("Number", "数字"), ("Integer", "整数"), ("Boolean", "布尔")),
            OptionSet("formulaKind", "公式类型", ("expression", "表达式"), ("nodeGraph", "计算节点图")),
            OptionSet("behaviorType", "AI 行为类型", ("passive", "被动待机"), ("meleeChase", "近战追击"), ("patrol", "巡逻警戒"), ("rangedKite", "远程保持距离"), ("flee", "逃跑求援"), ("bossPhases", "Boss 阶段战")),
            OptionSet("movementMode", "移动模式", ("topDown", "俯视移动"), ("sideView", "横版移动"), ("isometric", "等轴测移动"), ("none", "不移动")),
            OptionSet("targetSelector", "目标选择", ("none", "无目标"), ("nearestHostile", "最近敌对目标"), ("playerFirst", "玩家优先"), ("lowestHealth", "低血量优先"), ("eventDriven", "事件指定")),
            OptionSet("patrolMode", "巡逻模式", ("none", "不巡逻"), ("route", "路线巡逻"), ("radius", "半径巡逻"), ("waypoints", "路点巡逻")),
            OptionSet("skillType", "技能类型", ("active", "主动"), ("passive", "被动"), ("toggle", "切换"), ("ultimate", "终极技")),
            OptionSet("targetingMode", "目标方式", ("selfForward", "自身前方"), ("aimedProjectile", "瞄准投射物"), ("selfOrAlly", "自身或友方"), ("enemyUnit", "敌方单位"), ("allyUnit", "友方单位"), ("anyUnit", "任意单位"), ("taggedUnit", "指定标签单位"), ("areaTaggedUnits", "范围内指定标签单位"), ("groundArea", "地面范围"), ("dashForward", "向前冲刺"), ("groundPoint", "地面点位"), ("self", "自身"), ("towerTarget", "自动防御目标")),
            OptionSet("effectKind", "玩法效果类型", ("damage", "伤害"), ("heal", "治疗"), ("restore", "恢复属性"), ("lifeSteal", "吸血"), ("applyStatus", "施加状态"), ("knockback", "击退"), ("reward", "奖励"), ("buff", "增益"), ("trigger", "触发器语义")),
            OptionSet("statusKind", "状态类型", ("buff", "增益"), ("debuff", "减益"), ("control", "控制")),
            OptionSet("visualEffectKind", "表现类型", ("spriteAnimation", "精灵动画"), ("soundOnly", "仅音效"), ("screenShake", "屏幕震动")),
            OptionSet("rarity", "稀有度", ("common", "普通"), ("uncommon", "优秀"), ("rare", "稀有"), ("epic", "史诗"), ("quest", "关键")),
            OptionSet("equipmentSlot", "装备槽", ("mainHand", "主手"), ("offHand", "副手"), ("body", "身体"), ("accessory", "饰品")),
            OptionSet("decorationKind", "装饰物类型", ("static", "静态"), ("foliage", "植被"), ("light", "光源"), ("breakable", "可破坏"), ("interactive", "可交互")),
            OptionSet("interactionKind", "交互类型", ("generic", "通用"), ("talk", "交谈"), ("rescue", "营救"), ("open", "打开"), ("teleport", "传送")),
            OptionSet("viewType", "地图视角", ("TopDown", "俯视角"), ("SideView", "横版"), ("Isometric", "等轴测")),
            OptionSet("statCategory", "属性分类", ("stat.category.general", "通用"), ("stat.category.combat", "战斗"), ("stat.category.movement", "移动"), ("stat.category.reward", "奖励"), ("stat.category.strategy", "资源")),
            OptionSet("traitCategory", "特性分类", ("trait.category.general", "通用"), ("trait.category.entity", "实体"), ("trait.category.combat", "战斗"), ("trait.category.world", "世界")),
            OptionSet("tag", "标签", ("unit", "单位"), ("player", "玩家"), ("human", "人类"), ("npc", "NPC"), ("enemy", "敌人"), ("boss", "首领"), ("summon", "召唤物"), ("tower", "固定单位"), ("buildable", "可建造"), ("path", "路线"), ("base", "目标点"), ("attackable", "可攻击"), ("interactable", "可交互"), ("rescuable", "可营救"), ("rescueTarget", "营救目标"), ("guard", "守卫"), ("starterEnemy", "入门敌人"), ("slime", "史莱姆"), ("ranged", "远程"), ("mage", "法师"), ("building", "建筑"), ("trap", "陷阱"), ("undead", "亡灵"), ("bloodless", "无血生物"), ("mechanical", "机械"), ("damageOverTime", "持续伤害"), ("control", "控制"), ("defense", "防御"), ("lifeSteal", "吸血"), ("nature", "自然"), ("light", "光源"), ("mineral", "矿物"), ("breakable", "可破坏"), ("chest", "宝箱"), ("attack", "攻击"), ("melee", "近战"), ("physical", "物理"), ("magic", "魔法"), ("projectile", "投射物"), ("area", "范围"), ("caster", "施法者"), ("fire", "火焰"), ("poison", "毒素"), ("slow", "减速"), ("heal", "治疗"), ("restore", "恢复"), ("support", "支援"), ("mobility", "机动"), ("utility", "功能"), ("reward", "奖励"), ("buff", "增益"), ("debuff", "减益"), ("potion", "药水"), ("mana", "法力"), ("weapon", "武器"), ("sword", "剑"), ("dagger", "匕首"), ("armor", "护甲"), ("key", "钥匙"), ("rescue", "营救"), ("quest", "任务"), ("material", "材料"), ("herb", "草药"), ("relic", "遗物"), ("plain", "平原"), ("forest", "森林"), ("mountain", "山地"), ("water", "水域"), ("flying", "飞行"), ("swimmer", "游泳"), ("shield", "盾牌"), ("sibling", "兄妹/羁绊"), ("capturePoint", "占领点"), ("tactics", "格子行动"), ("strategy", "规划"), ("economy", "经济"), ("currency", "货币"), ("supply", "补给"), ("research", "研究"), ("unlock", "解锁"), ("build", "建造"), ("territory", "区域"), ("village", "村庄")),
            OptionSet("animationKey", "动画键", ("idle", "待机"), ("idleDown", "向下待机"), ("idleLeft", "向左待机"), ("idleRight", "向右待机"), ("idleUp", "向上待机"), ("walkDown", "向下行走"), ("walkLeft", "向左行走"), ("walkRight", "向右行走"), ("walkUp", "向上行走"), ("attackDown", "向下攻击"), ("attackLeft", "向左攻击"), ("attackRight", "向右攻击"), ("attackUp", "向上攻击"), ("castDown", "向下施法"), ("castLeft", "向左施法"), ("castRight", "向右施法"), ("castUp", "向上施法"), ("alertDown", "向下警戒"), ("rageDown", "向下狂暴"), ("hitDown", "向下受击"), ("hitLeft", "向左受击"), ("hitRight", "向右受击"), ("hitUp", "向上受击"), ("dead", "死亡"), ("spawn", "生成"), ("despawn", "消失"), ("open", "打开"), ("closed", "关闭"), ("burn", "燃烧"), ("slash", "斩击"), ("fireball", "火球"), ("frostBolt", "寒霜弹"), ("poison", "毒素"), ("heal", "治疗"), ("loop", "循环"), ("dash", "冲刺"), ("summon", "召唤")),
            OptionSet("componentType", "组件类型", ("CustomComponent", "自定义组件"), ("PlayerInput", "玩家输入"), ("TopDownMovement", "俯视移动"), ("CameraFollow", "镜头跟随"), ("Health", "生命"), ("AIController", "AI 控制"), ("IdleBrain", "待机行为"), ("Interactable", "可交互"), ("DropLoot", "掉落"), ("HitboxAttack", "攻击判定"), ("Hurtbox", "受击区域"), ("ProjectileShooter", "投射物发射"), ("ChaseTargetAI", "追击 AI"), ("PatrolAI", "巡逻 AI"), ("DetectionRadius", "感知范围"), ("LineOfSightAI", "视线 AI"), ("BossPhaseController", "Boss 阶段"), ("Knockback", "击退"), ("DefenseTower", "自动防御攻击"), ("PathFollower", "路线跟随"), ("BaseLeakTarget", "基地漏怪目标"), ("TacticalUnit", "战术单位"), ("GridOccupant", "格子占位"), ("TurnActor", "回合行动者"), ("CapturePoint", "占领点"), ("BondEmitter", "羁绊源"), ("ResourceStorage", "资源存储"), ("ResourceProducer", "资源生产"), ("ProductionQueue", "生产队列"), ("ResearchProvider", "研究提供者"), ("TerritoryController", "领地控制器")),
            OptionSet("componentParameter", "组件参数", ("speedStat", "速度属性"), ("maxHpStat", "最大生命属性"), ("profileField", "AI 字段"), ("profileId", "AI 预设"), ("lootTableField", "掉落表字段"), ("trigger", "触发器"), ("skillField", "技能字段"), ("shape", "形状"), ("projectileField", "投射物字段"), ("interactionMode", "交互模式"), ("detectionRange", "发现范围"), ("loseRange", "丢失范围"), ("attackStat", "攻击属性"), ("range", "范围"), ("patrolRadius", "巡逻半径"), ("turnDelay", "转向延迟"), ("projectileId", "投射物"), ("cooldown", "冷却"), ("phaseCount", "阶段数量"), ("force", "力度"), ("AIProfileId", "AI 预设 ID"), ("LootTableId", "掉落表 ID"), ("ProjectileId", "投射物 ID"), ("SkillId", "技能 ID"), ("skillId", "技能"), ("targetPriority", "目标优先级"), ("pathId", "路线"), ("baseRuleId", "基地规则"), ("gridRuleId", "格子规则"), ("actionRuleId", "行动规则"), ("turnRuleId", "回合规则"), ("rangeId", "战术范围"), ("objectiveRuleId", "目标规则"), ("bondRuleId", "羁绊规则"), ("resourceRuleId", "资源规则"), ("productionRuleId", "生产规则"), ("techRuleId", "科技规则"), ("territoryRuleId", "领地规则"), ("movePoints", "移动力"), ("actionPoints", "行动点")),
            OptionSet("routeMode", "路线模式", ("waypoints", "路点"), ("navMesh", "导航网格"), ("eventDriven", "事件驱动")),
            OptionSet("waveStartMode", "波次启动方式", ("manual", "手动启动"), ("auto", "自动启动"), ("eventDriven", "事件驱动")),
            OptionSet("victoryCondition", "胜利条件", ("allWavesCleared", "清完所有波次"), ("surviveTimer", "坚持到计时结束"), ("eventDriven", "事件驱动")),
            OptionSet("defeatCondition", "失败条件", ("baseLifeZero", "基地生命归零"), ("eventDriven", "事件驱动")),
            OptionSet("buildableRole", "可建造单位定位", ("damage", "输出"), ("control", "控制"), ("support", "支援"), ("economy", "经济")),
            OptionSet("targetPriority", "目标优先级", ("first", "最靠前"), ("nearest", "最近"), ("lowestHealth", "最低生命"), ("highestHealth", "最高生命"), ("fastest", "最快"), ("boss", "首领优先")),
            OptionSet("gridType", "格子类型", ("square", "方格"), ("hex", "六边形"), ("free", "自由网格")),
            OptionSet("movementMetric", "移动计算方式", ("manhattan", "曼哈顿距离"), ("chebyshev", "八方向距离"), ("euclidean", "欧氏距离"), ("hex", "六边形距离")),
            OptionSet("turnMode", "回合模式", ("sideTurn", "阵营回合"), ("unitInitiative", "单位主动值"), ("speedOrder", "速度排序"), ("actionPoint", "行动点")),
            OptionSet("actionRefreshMode", "行动刷新方式", ("turnStart", "回合开始"), ("unitTurnStart", "单位行动开始"), ("roundStart", "轮次开始"), ("eventDriven", "事件驱动")),
            OptionSet("rangeShape", "范围形状", ("diamond", "菱形"), ("square", "方形"), ("line", "直线"), ("cone", "锥形"), ("circle", "圆形"), ("custom", "自定义")),
            OptionSet("areaShape", "影响区域形状", ("single", "单格"), ("diamond", "菱形"), ("square", "方形"), ("line", "直线"), ("cross", "十字"), ("circle", "圆形"), ("custom", "自定义")),
            OptionSet("objectiveType", "目标类型", ("defeatAll", "全灭"), ("surviveRounds", "坚持回合"), ("capturePoint", "占领点"), ("escort", "护送"), ("escape", "撤离"), ("protectUnit", "保护单位"), ("eventDriven", "事件驱动")),
            OptionSet("bondTriggerTiming", "羁绊触发时机", ("whileAdjacent", "相邻期间"), ("turnStart", "回合开始"), ("beforeAttack", "攻击前"), ("afterAttack", "攻击后"), ("onDamaged", "受击时"), ("eventDriven", "事件驱动")),
            OptionSet("bondDurationMode", "羁绊持续方式", ("whileConditionMet", "条件成立期间"), ("currentTurn", "当前回合"), ("combatOnly", "本次战斗"), ("fixedDuration", "固定持续"), ("eventDriven", "事件驱动")),
            OptionSet("stackingMode", "叠加方式", ("unique", "唯一"), ("refresh", "刷新"), ("stack", "叠加"), ("replace", "替换")),
            OptionSet("resourceKind", "资源类型", ("currency", "货币"), ("material", "材料"), ("supply", "补给"), ("research", "研究"), ("population", "人口"), ("custom", "自定义")),
            OptionSet("producedAssetKind", "产出类型", ("unit", "单位"), ("item", "物品"), ("resource", "资源"), ("skill", "技能"), ("event", "事件")),
            OptionSet("techKind", "科技类型", ("unlock", "解锁"), ("upgrade", "强化"), ("ruleChange", "规则变更"), ("eventUnlock", "事件解锁")),
            OptionSet("diplomaticState", "外交状态", ("allied", "同盟"), ("friendly", "友好"), ("neutral", "中立"), ("rival", "竞争"), ("war", "战争")),
            OptionSet("territoryControlMode", "领地控制方式", ("occupyPoint", "占据点位"), ("areaInfluence", "区域影响"), ("captureProgress", "占领进度"), ("eventDriven", "事件驱动"))
        ];
    }

    private static OptionSetDefinition OptionSet(string id, string displayName, params (string Value, string Name)[] values)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (value, name) in values)
        {
            if (string.IsNullOrWhiteSpace(value) || map.ContainsKey(value))
            {
                continue;
            }

            map[value] = name;
        }

        return new OptionSetDefinition
        {
            Id = id,
            DisplayName = displayName,
            DisplayNameKey = $"optionSet.{id}.name",
            Values = map
        };
    }
}
