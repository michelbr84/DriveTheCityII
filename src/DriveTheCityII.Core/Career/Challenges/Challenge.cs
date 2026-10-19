using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    public enum ChallengeType
    {
        /// <summary>Pass ordered checkpoints; time plus contact penalties.</summary>
        CheckpointRoute = 0,

        /// <summary>Fastest completion between a start and a finish place.</summary>
        TimeTrial = 1,

        /// <summary>Stop inside a bay within position and heading tolerances.</summary>
        Parking = 2,
    }

    public enum MedalTier
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
    }

    /// <summary>
    /// A challenge definition (SPEC-CHALLENGE section 3). <see cref="RouteVersion"/> increases when the route signature
    /// changes after a city edit; old results stay visible as "route changed".
    /// </summary>
    public sealed class ChallengeDefinition
    {
        public string Id;
        public string Name;
        public ChallengeType Type;
        public VehicleClass Class;
        public List<PlaceRef> Points = new List<PlaceRef>();
        public double ReferenceSeconds;
        public double TargetHeadingDegrees;
        public int RouteVersion = 1;
        public string Signature;
        public bool Available = true;
        public string UnavailableKey;
        public bool PlayerAuthored;

        internal JsonValue ToJson()
        {
            JsonValue points = JsonValue.Array();
            foreach (PlaceRef p in Points)
            {
                points.Add(p.ToJson());
            }
            return JsonValue.Object()
                .Set("id", Id)
                .Set("name", Name)
                .Set("type", Type.ToString())
                .Set("class", Class.ToString())
                .Set("points", points)
                .Set("reference", ReferenceSeconds)
                .Set("heading", TargetHeadingDegrees)
                .Set("version", RouteVersion)
                .Set("signature", Signature)
                .Set("available", Available)
                .Set("unavailableKey", UnavailableKey)
                .Set("playerAuthored", PlayerAuthored);
        }

        internal static ChallengeDefinition FromJson(JsonValue o, string path, JsonRead r)
        {
            var d = new ChallengeDefinition
            {
                Id = r.Str(o, "id", path),
                Name = r.Str(o, "name", path),
                Type = r.Enum<ChallengeType>(o, "type", path),
                Class = r.Enum<VehicleClass>(o, "class", path),
                ReferenceSeconds = r.Num(o, "reference", path, 0),
                TargetHeadingDegrees = r.Num(o, "heading", path, -720, 720),
                RouteVersion = r.Int(o, "version", path, 1),
                Signature = r.Str(o, "signature", path),
                Available = r.Bool(o, "available", path),
                UnavailableKey = r.Str(o, "unavailableKey", path, false),
                PlayerAuthored = r.Bool(o, "playerAuthored", path),
            };
            int i = 0;
            foreach (JsonValue p in r.Arr(o, "points", path).Items)
            {
                d.Points.Add(PlaceRef.FromJson(p, path + ".points[" + i++ + "]", r));
            }
            int min = d.Type == ChallengeType.Parking ? 1 : 2;
            if (d.Points.Count < min)
            {
                r.Error(path + ".points", "too few points");
            }
            return d;
        }
    }

    /// <summary>Personal record per challenge and route version (REQ-CHAL-001).</summary>
    public sealed class ChallengeRecord
    {
        public string ChallengeId;
        public int RouteVersion;
        public double BestSeconds = -1;
        public MedalTier Medal;
        public int Attempts;
        public double LastAttemptAt = -1;

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("challenge", ChallengeId)
                .Set("version", RouteVersion)
                .Set("best", BestSeconds)
                .Set("medal", Medal.ToString())
                .Set("attempts", Attempts)
                .Set("lastAttemptAt", LastAttemptAt);
        }

        internal static ChallengeRecord FromJson(JsonValue o, string path, JsonRead r)
        {
            return new ChallengeRecord
            {
                ChallengeId = r.Str(o, "challenge", path),
                RouteVersion = r.Int(o, "version", path, 1),
                BestSeconds = r.Num(o, "best", path, -1),
                Medal = r.Enum<MedalTier>(o, "medal", path),
                Attempts = r.Int(o, "attempts", path, 0),
                LastAttemptAt = r.Num(o, "lastAttemptAt", path, -1),
            };
        }
    }

    /// <summary>A run in progress. Transient: a run is void on save/load, exit, Building or recovery (SPEC-CHALLENGE 3.3).</summary>
    internal sealed class ChallengeRun
    {
        public string ChallengeId;
        public string VehicleId;
        public MissionState State;
        public int NextPoint;
        public double ArmedAt;
        public double StartedAt = -1;
        public double PenaltySeconds;
        public int RouteVersion;
    }

    /// <summary>Persisted challenge data: definitions and records.</summary>
    public sealed class ChallengeBook
    {
        public List<ChallengeDefinition> Definitions = new List<ChallengeDefinition>();
        public List<ChallengeRecord> Records = new List<ChallengeRecord>();

        public ChallengeDefinition Find(string id)
        {
            foreach (ChallengeDefinition d in Definitions)
            {
                if (d.Id == id)
                {
                    return d;
                }
            }
            return null;
        }

        public ChallengeRecord FindRecord(string id, int version)
        {
            foreach (ChallengeRecord r in Records)
            {
                if (r.ChallengeId == id && r.RouteVersion == version)
                {
                    return r;
                }
            }
            return null;
        }

        internal JsonValue ToJson()
        {
            JsonValue defs = JsonValue.Array();
            foreach (ChallengeDefinition d in Definitions)
            {
                defs.Add(d.ToJson());
            }
            JsonValue recs = JsonValue.Array();
            foreach (ChallengeRecord r in Records)
            {
                recs.Add(r.ToJson());
            }
            return JsonValue.Object().Set("definitions", defs).Set("records", recs);
        }

        internal static ChallengeBook FromJson(JsonValue o, string path, JsonRead r)
        {
            var b = new ChallengeBook();
            int i = 0;
            foreach (JsonValue d in r.Arr(o, "definitions", path).Items)
            {
                b.Definitions.Add(ChallengeDefinition.FromJson(d, path + ".definitions[" + i++ + "]", r));
            }
            i = 0;
            foreach (JsonValue d in r.Arr(o, "records", path).Items)
            {
                b.Records.Add(ChallengeRecord.FromJson(d, path + ".records[" + i++ + "]", r));
            }
            return b;
        }
    }
}
