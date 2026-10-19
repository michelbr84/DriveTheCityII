using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;

namespace DriveTheCityII.Core.Tests
{
    /// <summary>
    /// In-memory world used by the unit tests: a terrain height function, flat "decks" (roads, bridges, tunnels,
    /// sidewalks) over rectangles, water rectangles, and solid obstacles. It stands in for the CityBridge probe.
    /// </summary>
    public sealed class FakeWorld : IWorldProbe
    {
        public Func<float, float, float> Terrain = (x, z) => 0f;
        public readonly List<(Obb2 area, float height, SurfaceKind kind, long id)> Decks = new List<(Obb2, float, SurfaceKind, long)>();
        public readonly List<(Obb2 area, float level)> Water = new List<(Obb2, float)>();
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public float MapHalfSize = 5000f;

        public FakeWorld AddDeck(float cx, float cz, float halfW, float halfL, float height, SurfaceKind kind, long id, float yaw = 0f)
        {
            Decks.Add((Obb2.FromYaw(new Vec2(cx, cz), yaw, halfW, halfL), height, kind, id));
            return this;
        }

        public FakeWorld AddBuilding(float cx, float cz, float halfW, float halfL, float baseY, float height, long id, float yaw = 0f)
        {
            Obstacles.Add(new Obstacle
            {
                Footprint = Obb2.FromYaw(new Vec2(cx, cz), yaw, halfW, halfL),
                YMin = baseY,
                YMax = baseY + height,
                Kind = ObstacleKind.Building,
                SourceId = id,
            });
            return this;
        }

        public void SampleSurfaces(float x, float z, float minY, float maxY, List<SurfaceSample> results)
        {
            var p = new Vec2(x, z);
            results.Add(new SurfaceSample(Terrain(x, z), SurfaceKind.Terrain, 0));
            foreach (var d in Decks)
            {
                if (d.area.Contains(p) && d.height >= minY && d.height <= maxY)
                {
                    results.Add(new SurfaceSample(d.height, d.kind, d.id));
                }
            }
            foreach (var w in Water)
            {
                if (w.area.Contains(p))
                {
                    results.Add(new SurfaceSample(w.level, SurfaceKind.Water, -1));
                }
            }
        }

        public void QueryObstacles(Vec2 center, float radius, List<Obstacle> results)
        {
            foreach (var o in Obstacles)
            {
                if (Vec2.Distance(o.Footprint.Center, center) <= radius + o.Footprint.BoundingRadius)
                {
                    results.Add(o);
                }
            }
        }

        public bool IsInsideMap(float x, float z) => Math.Abs(x) <= MapHalfSize && Math.Abs(z) <= MapHalfSize;
    }
}
