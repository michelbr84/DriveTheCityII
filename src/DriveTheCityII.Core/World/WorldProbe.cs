using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;

namespace DriveTheCityII.Core.World
{
    /// <summary>Kind of surface under a point (SPEC-CONTACT surface classes).</summary>
    public enum SurfaceKind
    {
        Terrain = 0,
        Road = 1,
        Pedestrian = 2,
        Elevated = 3,
        Tunnel = 4,
        Structure = 5,
        Water = 6,
    }

    /// <summary>One candidate surface at a horizontal position. Several can exist (bridges, tunnels, stacked roads).</summary>
    public struct SurfaceSample
    {
        public float Height;
        public SurfaceKind Kind;

        /// <summary>Opaque identifier of the source (for logs and level tracking); 0 = terrain.</summary>
        public long SourceId;

        public SurfaceSample(float height, SurfaceKind kind, long sourceId = 0)
        {
            Height = height;
            Kind = kind;
            SourceId = sourceId;
        }

        public override string ToString() => $"{Kind}@{Height:0.##}#{SourceId}";
    }

    public enum ObstacleKind
    {
        Building = 0,
        StaticObject = 1,
        Vehicle = 2,
        Creature = 3,
        Proxy = 4,
        MapEdge = 5,

        /// <summary>
        /// Street furniture with a small footprint: poles, posts, signs, hydrants, bins. Solid for people and cars, but
        /// passed by long vehicles (see <see cref="Motion.VehicleSpec.PassesSmallProps"/>; ISSUE-040).
        /// </summary>
        SmallProp = 6,
    }

    /// <summary>A solid volume: an oriented footprint and a vertical range.</summary>
    public struct Obstacle
    {
        public Obb2 Footprint;
        public float YMin;
        public float YMax;
        public ObstacleKind Kind;
        public long SourceId;

        /// <summary>Horizontal velocity of the obstacle (moving vehicles), metres per second.</summary>
        public Vec2 Velocity;

        public override string ToString() => $"{Kind}#{SourceId} c={Footprint.Center} y=[{YMin:0.#},{YMax:0.#}]";
    }

    /// <summary>
    /// Read-only sampling port implemented by CityBridge from the game's data (terrain, nets, objects, water).
    /// All methods must be cheap enough to call several times per frame.
    /// </summary>
    public interface IWorldProbe
    {
        /// <summary>
        /// Appends every surface found at (x, z) whose height lies within [minY, maxY]. Terrain is always reported
        /// (with its true height, which may be above maxY when inside a tunnel) so that walls of terrain can be
        /// detected. Water is reported as <see cref="SurfaceKind.Water"/> with the water surface height.
        /// </summary>
        void SampleSurfaces(float x, float z, float minY, float maxY, List<SurfaceSample> results);

        /// <summary>Appends solid obstacles whose footprint may touch the circle (center, radius).</summary>
        void QueryObstacles(Vec2 center, float radius, List<Obstacle> results);

        /// <summary>True when (x, z) lies inside the playable map area.</summary>
        bool IsInsideMap(float x, float z);
    }
}
