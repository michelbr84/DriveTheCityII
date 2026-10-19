using System;
using System.Collections.Generic;
using System.Globalization;

namespace DriveTheCityII.Core.Career
{
    /// <summary>A point in game-world metres (Y up). Plain data, never an entity handle (REQ-SAVE-003).</summary>
    public struct MapPoint
    {
        public double X;
        public double Y;
        public double Z;

        public MapPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool IsFinite => CareerMath.IsFinite(X) && CareerMath.IsFinite(Y) && CareerMath.IsFinite(Z);

        public static double Distance(MapPoint a, MapPoint b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Horizontal distance (X/Z plane), used for headings and parking tolerances.</summary>
        public static double DistanceXZ(MapPoint a, MapPoint b)
        {
            double dx = a.X - b.X;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", X, Y, Z);
    }

    /// <summary>Category of a real city place as reported by the world port.</summary>
    public enum PlaceKind
    {
        Residential = 0,
        Commercial = 1,
        Office = 2,
        Industrial = 3,
        Warehouse = 4,
        Service = 5,
        Leisure = 6,
        TransitStop = 7,
        ServicePoint = 8,
        Home = 9,
        Depot = 10,
        Other = 11,
    }

    /// <summary>
    /// A place answered by <see cref="ICareerWorld"/>. <see cref="Id"/> is a stable string produced by the mod (for
    /// example "bld:&lt;prefab&gt;:&lt;x&gt;:&lt;z&gt;" or "stop:&lt;line&gt;:&lt;index&gt;"). <see cref="Access"/> is the
    /// validated access point (curb, stop, bay), never a building center (REQ-CAREER-003).
    /// </summary>
    public sealed class PlaceInfo
    {
        public string Id;
        public PlaceKind Kind;
        public string Name;
        public MapPoint Access;

        public PlaceInfo()
        {
        }

        public PlaceInfo(string id, PlaceKind kind, string name, MapPoint access)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Access = access;
        }

        public override string ToString() => $"{Id} ({Kind}) {Name}";
    }

    /// <summary>Career vehicle classes. <see cref="Taxi"/> is a car-class vehicle with taxi presentation.</summary>
    public enum VehicleClass
    {
        Car = 0,
        Taxi = 1,
        Van = 2,
        RigidTruck = 3,
        Bus = 4,
    }

    public enum RouteStatus
    {
        /// <summary>The port could not decide. Never treated as reachable (SPEC-MODULES section 5).</summary>
        Unknown = 0,
        Reachable = 1,
        Unreachable = 2,
    }

    /// <summary>Answer of the route planner. The default value is <see cref="RouteStatus.Unknown"/>.</summary>
    public struct RouteEstimate
    {
        public RouteStatus Status;
        public double DistanceMeters;
        public double TimeSeconds;

        public bool IsReachable => Status == RouteStatus.Reachable && CareerMath.IsFinite(DistanceMeters) && DistanceMeters >= 0 && CareerMath.IsFinite(TimeSeconds) && TimeSeconds >= 0;

        public static RouteEstimate Reachable(double distanceMeters, double timeSeconds) => new RouteEstimate { Status = RouteStatus.Reachable, DistanceMeters = distanceMeters, TimeSeconds = timeSeconds };

        public static RouteEstimate Unreachable() => new RouteEstimate { Status = RouteStatus.Unreachable };

        public static RouteEstimate Unknown() => new RouteEstimate { Status = RouteStatus.Unknown };
    }

    /// <summary>A real city bus line, with its stops in driving order (SPEC-BUS, OPEN-020).</summary>
    public sealed class TransitLineInfo
    {
        public string Id;
        public string Name;
        public int Number;
        public IReadOnlyList<PlaceInfo> Stops;

        public TransitLineInfo()
        {
        }

        public TransitLineInfo(string id, string name, int number, IReadOnlyList<PlaceInfo> stops)
        {
            Id = id;
            Name = name;
            Number = number;
            Stops = stops;
        }
    }

    /// <summary>Filter for <see cref="ICareerWorld.QueryPlaces"/>.</summary>
    public sealed class PlaceQuery
    {
        /// <summary>Accepted kinds. Null or empty means any kind.</summary>
        public IReadOnlyList<PlaceKind> Kinds;

        public MapPoint Near;
        public double RadiusMeters;
        public int MaxCount;

        public PlaceQuery()
        {
        }

        public PlaceQuery(IReadOnlyList<PlaceKind> kinds, MapPoint near, double radiusMeters, int maxCount)
        {
            Kinds = kinds;
            Near = near;
            RadiusMeters = radiusMeters;
            MaxCount = maxCount;
        }

        public bool Accepts(PlaceKind kind)
        {
            if (Kinds == null || Kinds.Count == 0)
            {
                return true;
            }
            for (int i = 0; i < Kinds.Count; i++)
            {
                if (Kinds[i] == kind)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Player mode as observed by the mod (a reduction of SPEC-CONTROL states).</summary>
    public enum PlayerMode
    {
        Building = 0,
        OnFoot = 1,
        Driving = 2,
        Transition = 3,
    }

    /// <summary>
    /// Per-frame observation supplied by the mod. Distances and impacts are accumulated since the previous snapshot.
    /// <see cref="HeadingDegrees"/>, <see cref="LongitudinalAccel"/> and <see cref="LateralAccel"/> are optional
    /// additions (leave 0 when unknown): heading feeds parking challenges, accelerations feed harsh-event penalties.
    /// </summary>
    public struct PlayerSnapshot
    {
        public PlayerMode Mode;
        public MapPoint Position;
        public double SpeedMps;

        /// <summary>Career vehicle id of the vehicle being driven, or null.</summary>
        public string VehicleId;

        public bool Stationary;

        /// <summary>Metres driven since the previous snapshot.</summary>
        public double DistanceDrivenMeters;

        /// <summary>Largest impact closing speed since the previous snapshot (m/s), 0 when none.</summary>
        public double ImpactSpeed;

        /// <summary>Vehicle yaw in degrees, same convention as challenge target headings.</summary>
        public double HeadingDegrees;

        /// <summary>Longitudinal acceleration in m/s^2 (signed; braking is negative).</summary>
        public double LongitudinalAccel;

        /// <summary>Lateral acceleration in m/s^2 (signed).</summary>
        public double LateralAccel;

        public static PlayerSnapshot Building() => new PlayerSnapshot { Mode = PlayerMode.Building };

        public static PlayerSnapshot OnFoot(MapPoint p) => new PlayerSnapshot { Mode = PlayerMode.OnFoot, Position = p, Stationary = true };

        public static PlayerSnapshot Driving(string vehicleId, MapPoint p, double speed, double driven) => new PlayerSnapshot
        {
            Mode = PlayerMode.Driving,
            VehicleId = vehicleId,
            Position = p,
            SpeedMps = speed,
            Stationary = speed < CareerBalance.ServiceSpeedMps,
            DistanceDrivenMeters = driven,
        };
    }

    /// <summary>The four player careers (REQ-ECON-003 keeps XP per career).</summary>
    public enum CareerKind
    {
        Taxi = 0,
        Delivery = 1,
        Freight = 2,
        Bus = 3,
    }

    /// <summary>Contact severity from the closing speed (SPEC-DRIVING section 3.6).</summary>
    public enum ContactSeverity
    {
        None = 0,
        Light = 1,
        Medium = 2,
        Severe = 3,
    }

    /// <summary>Small numeric helpers that netstandard2.0 lacks.</summary>
    public static class CareerMath
    {
        public static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        public static long Clamp(long v, long min, long max) => v < min ? min : (v > max ? max : v);

        /// <summary>Rounds a money value in cents (half away from zero).</summary>
        public static long RoundCents(double cents) => (long)Math.Round(cents, MidpointRounding.AwayFromZero);

        /// <summary>Smallest absolute difference between two headings, in degrees (0..180).</summary>
        public static double HeadingError(double a, double b)
        {
            double d = (a - b) % 360.0;
            if (d < 0)
            {
                d += 360.0;
            }
            return d > 180.0 ? 360.0 - d : d;
        }

        public static string Inv(long v) => v.ToString(CultureInfo.InvariantCulture);

        public static string Inv(int v) => v.ToString(CultureInfo.InvariantCulture);

        public static string Inv(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);

        /// <summary>English fallback money format ("12.50 cr"), culture-invariant.</summary>
        public static string Money(long cents)
        {
            string sign = cents < 0 ? "-" : string.Empty;
            long a = Math.Abs(cents);
            return sign + (a / 100).ToString(CultureInfo.InvariantCulture) + "." + (a % 100).ToString("00", CultureInfo.InvariantCulture) + " cr";
        }
    }
}
