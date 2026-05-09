using Axe2DEditor.Core.Components;

namespace Axe2DEditor.Core.Assets;

public static partial class DefaultAssetFactory
{
    private static List<ComponentConfig> CreatePlayerComponents()
    {
        return
        [
            CreateComponent("PlayerInput"),
            CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
            CreateComponent("CameraFollow"),
            CreateComponent("Health", ("maxHpStat", "maxHp"))
        ];
    }

    private static List<ComponentConfig> CreateEnemyComponents(string aiProfileId)
    {
        if (string.Equals(aiProfileId, "ai.patrolGuard", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                CreateComponent("PatrolAI", ("patrolRadius", 10), ("turnDelay", 1.5)),
                CreateComponent("Health", ("maxHpStat", "maxHp")),
                CreateComponent("DetectionRadius", ("range", 6))
            ];
        }

        if (string.Equals(aiProfileId, "ai.rangedKeepDistance", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
                CreateComponent("ProjectileShooter", ("projectileId", "projectile.template.basic"), ("cooldown", 1.2)),
                CreateComponent("Health", ("maxHpStat", "maxHp")),
                CreateComponent("LineOfSightAI", ("range", 8))
            ];
        }

        if (string.Equals(aiProfileId, "ai.bossPhases", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                CreateComponent("BossPhaseController", ("phaseCount", 3)),
                CreateComponent("ChaseTargetAI", ("detectionRange", 10), ("loseRange", 14)),
                CreateComponent("Health", ("maxHpStat", "maxHp")),
                CreateComponent("HitboxAttack", ("attackStat", "attack"), ("range", 2.2)),
                CreateComponent("Knockback", ("force", 18))
            ];
        }

        return
        [
            CreateComponent("TopDownMovement", ("speedStat", "moveSpeed")),
            CreateComponent("ChaseTargetAI", ("detectionRange", 8), ("loseRange", 12)),
            CreateComponent("Health", ("maxHpStat", "maxHp")),
            CreateComponent("HitboxAttack", ("attackStat", "attack"), ("range", 1.5))
        ];
    }

    private static List<ComponentConfig> CreateNpcComponents(string interactionProfileId)
    {
        var interactionMode = string.Equals(interactionProfileId, "interaction.rescue", StringComparison.OrdinalIgnoreCase)
            ? "rescue"
            : "dialogue";

        return
        [
            CreateComponent("IdleBrain"),
            CreateComponent("Health", ("maxHpStat", "maxHp")),
            CreateComponent("Interactable", ("interactionMode", interactionMode))
        ];
    }

    private static Dictionary<string, string> CreateFourDirectionAnimations(string prefix)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idleDown"] = $"{prefix}.idle_down",
            ["idleLeft"] = $"{prefix}.idle_left",
            ["idleRight"] = $"{prefix}.idle_right",
            ["idleUp"] = $"{prefix}.idle_up",
            ["walkDown"] = $"{prefix}.walk_down",
            ["walkLeft"] = $"{prefix}.walk_left",
            ["walkRight"] = $"{prefix}.walk_right",
            ["walkUp"] = $"{prefix}.walk_up",
            ["attackDown"] = $"{prefix}.attack_down",
            ["hitDown"] = $"{prefix}.hit_down",
            ["dead"] = $"{prefix}.dead"
        };
    }

    private static Dictionary<string, string> CreateSimpleCombatAnimations(string prefix)
    {
        var animations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idleDown"] = $"{prefix}.idle_down",
            ["walkDown"] = $"{prefix}.walk_down",
            ["attackDown"] = $"{prefix}.attack_down",
            ["hitDown"] = $"{prefix}.hit_down",
            ["dead"] = $"{prefix}.dead"
        };

        if (prefix.Contains("patrol", StringComparison.OrdinalIgnoreCase))
        {
            animations["alertDown"] = $"{prefix}.alert_down";
        }

        if (prefix.Contains("boss", StringComparison.OrdinalIgnoreCase))
        {
            animations["rageDown"] = $"{prefix}.rage_down";
        }

        return animations;
    }

    private static Dictionary<string, string> CreateCasterAnimations(string prefix)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idleDown"] = $"{prefix}.idle_down",
            ["walkDown"] = $"{prefix}.walk_down",
            ["castDown"] = $"{prefix}.cast_down",
            ["hitDown"] = $"{prefix}.hit_down",
            ["dead"] = $"{prefix}.dead"
        };
    }

    private static Dictionary<string, string> CreateIdleAnimations(string prefix)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idleDown"] = $"{prefix}.idle_down",
            ["idleLeft"] = $"{prefix}.idle_left",
            ["idleRight"] = $"{prefix}.idle_right",
            ["idleUp"] = $"{prefix}.idle_up"
        };
    }
}
