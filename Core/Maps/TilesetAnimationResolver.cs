using System.Drawing;

namespace Axe2DEditor.Core.Maps;

public static class TilesetAnimationResolver
{
    public static bool TryResolveFrame(
        MapTerrainDefinition? terrain,
        TilesetPlanDefinition? plan,
        int tileX,
        int tileY,
        int animationTimeMs,
        out Point frameTile)
    {
        frameTile = new Point(tileX, tileY);
        var frames = BuildFrames(terrain, plan, tileX, tileY);
        if (frames.Count <= 0)
        {
            return false;
        }

        if (frames.Count == 1)
        {
            frameTile = new Point(frames[0].TileX, frames[0].TileY);
            return true;
        }

        var frameIndex = ResolveFrameIndex(frames, animationTimeMs);
        frameTile = new Point(frames[frameIndex].TileX, frames[frameIndex].TileY);
        return true;
    }

    public static bool HasAnimation(MapTerrainDefinition? terrain, TilesetPlanDefinition? plan, int tileX, int tileY)
    {
        return BuildFrames(terrain, plan, tileX, tileY).Count > 1;
    }

    private static List<TilesetFrameDefinition> BuildFrames(MapTerrainDefinition? terrain, TilesetPlanDefinition? plan, int tileX, int tileY)
    {
        if (terrain is not null)
        {
            if (terrain.Frames is { Count: > 0 })
            {
                return NormalizeFrames(terrain.Frames, terrain.AnimationFrameDurationMs);
            }

            if (terrain.Animated && terrain.TileX >= 0 && terrain.TileY >= 0)
            {
                var count = Math.Max(1, terrain.AnimationFrames);
                return Enumerable.Range(0, count)
                    .Select(index => new TilesetFrameDefinition
                    {
                        TileX = terrain.TileX + index,
                        TileY = terrain.TileY,
                        DurationMs = terrain.AnimationFrameDurationMs
                    })
                    .ToList();
            }
        }

        if (plan?.Regions is null)
        {
            return [];
        }

        var region = plan.Regions.FirstOrDefault(candidate =>
            string.Equals(candidate.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)
            && candidate.Animated
            && candidate.X == tileX
            && candidate.Y == tileY);
        if (region is null)
        {
            return [];
        }

        if (region.AnimationFrames is { Count: > 0 })
        {
            return NormalizeFrames(region.AnimationFrames, region.AnimationFrameDurationMs);
        }

        return Enumerable.Range(0, region.Height)
            .SelectMany(y => Enumerable.Range(0, region.Width).Select(x => new TilesetFrameDefinition
            {
                TileX = region.X + x,
                TileY = region.Y + y,
                DurationMs = region.AnimationFrameDurationMs
            }))
            .ToList();
    }

    private static List<TilesetFrameDefinition> NormalizeFrames(IEnumerable<TilesetFrameDefinition> frames, int defaultDurationMs)
    {
        return frames
            .Where(frame => frame.TileX >= 0 && frame.TileY >= 0)
            .Select(frame => new TilesetFrameDefinition
            {
                TileX = frame.TileX,
                TileY = frame.TileY,
                DurationMs = Math.Clamp(frame.DurationMs <= 0 ? defaultDurationMs : frame.DurationMs, 16, 2000)
            })
            .ToList();
    }

    private static int ResolveFrameIndex(IReadOnlyList<TilesetFrameDefinition> frames, int animationTimeMs)
    {
        var total = frames.Sum(frame => Math.Max(16, frame.DurationMs));
        if (total <= 0)
        {
            return 0;
        }

        var elapsed = animationTimeMs % total;
        if (elapsed < 0)
        {
            elapsed += total;
        }

        var cursor = 0;
        for (var index = 0; index < frames.Count; index++)
        {
            cursor += Math.Max(16, frames[index].DurationMs);
            if (elapsed < cursor)
            {
                return index;
            }
        }

        return frames.Count - 1;
    }
}
