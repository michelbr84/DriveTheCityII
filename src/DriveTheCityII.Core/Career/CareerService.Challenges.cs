using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Challenges (SPEC-CHALLENGE section 3; REQ-CHAL-001, REQ-CHAL-002): checkpoint routes, time trials and parking
    /// challenges on the player's own city. Results are personal records per route version; cash and XP rewards are
    /// paid once per tier and route version (CH-&lt;ChallengeId&gt;-&lt;RouteVersion&gt;-&lt;Tier&gt;).
    /// </summary>
    public sealed partial class CareerService
    {
        private static readonly PlaceKind[] s_ChallengeKinds = { PlaceKind.Commercial, PlaceKind.Residential, PlaceKind.Office, PlaceKind.Leisure, PlaceKind.ServicePoint, PlaceKind.Industrial };

        private static bool ChallengeClassMatches(VehicleClass required, VehicleClass actual)
        {
            return required == actual || (required == VehicleClass.Car && actual == VehicleClass.Taxi) || (required == VehicleClass.Taxi && actual == VehicleClass.Car);
        }

        /// <summary>Defines a player-authored or mod-designated challenge after validating places and connectivity.</summary>
        public CareerResult DefineChallenge(ChallengeType type, string name, IList<string> placeIds, VehicleClass vehicleClass, double targetHeadingDegrees, double nowSeconds)
        {
            Enter(nowSeconds);
            if (m_World == null)
            {
                return Fail(CareerResultCode.NoWorld, "DTC.Career.NoWorld", "City data is not available.");
            }
            int min = type == ChallengeType.Parking ? 1 : 2;
            int max = type == ChallengeType.Parking ? 1 : type == ChallengeType.TimeTrial ? 2 : 14;
            if (placeIds == null || placeIds.Count < min || placeIds.Count > max)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Challenge.PointCount", "Wrong number of points for this challenge type.");
            }
            var points = new List<PlaceRef>();
            foreach (string id in placeIds)
            {
                PlaceInfo p = DescribePlace(id, out bool unknown);
                if (p == null)
                {
                    return Fail(CareerResultCode.NotFound, "DTC.Challenge.PointMissing", unknown ? "City data could not confirm a point." : "A point no longer exists.");
                }
                points.Add(PlaceRef.From(p));
            }
            var def = new ChallengeDefinition
            {
                Id = "CH" + CareerMath.Inv(++m_State.Seq.Challenge),
                Name = string.IsNullOrEmpty(name) ? type.ToString() : name,
                Type = type,
                Class = vehicleClass,
                Points = points,
                TargetHeadingDegrees = CareerMath.IsFinite(targetHeadingDegrees) ? targetHeadingDegrees : 0,
                PlayerAuthored = true,
            };
            if (!SignChallenge(def, out double reference, out string signature))
            {
                return Fail(CareerResultCode.OfferWithdrawn, "DTC.Challenge.NotConnected", "The route is not connected by road.");
            }
            def.ReferenceSeconds = reference;
            def.Signature = signature;
            m_State.Challenges.Definitions.Add(def);
            return Done(CareerResult.Success(def.Id));
        }

        /// <summary>
        /// Deterministic generated set near a point (seeded only by city data): a time trial, a checkpoint route and a
        /// parking challenge for cars, when the city allows them. Existing generated challenges are kept.
        /// </summary>
        public IReadOnlyList<ChallengeView> GenerateChallenges(MapPoint near, double nowSeconds)
        {
            Enter(nowSeconds);
            if (m_World == null)
            {
                return GetChallenges();
            }
            List<PlaceInfo> places = QueryPlacesSorted(s_ChallengeKinds, near, 3000, 64);
            // Time trial: the first pair 800-3000 m apart.
            bool done = false;
            for (int i = 0; i < places.Count && !done; i++)
            {
                for (int j = i + 1; j < places.Count && !done; j++)
                {
                    double d = MapPoint.DistanceXZ(places[i].Access, places[j].Access);
                    if (d >= 800 && d <= 3000)
                    {
                        done = TryAddGenerated("gen-tt-" + StableHash.Hex(StableHash.Fnv1a64(places[i].Id + "|" + places[j].Id)).Substring(0, 10), "Time trial", ChallengeType.TimeTrial, new List<PlaceInfo> { places[i], places[j] }, 0);
                    }
                }
            }
            // Checkpoint route: a greedy chain of 4 points, consecutive points 200-2000 m apart.
            var chain = new List<PlaceInfo>();
            foreach (PlaceInfo p in places)
            {
                if (chain.Count == 0)
                {
                    chain.Add(p);
                    continue;
                }
                double d = MapPoint.DistanceXZ(chain[chain.Count - 1].Access, p.Access);
                if (d >= 200 && d <= 2000)
                {
                    chain.Add(p);
                }
                if (chain.Count == 4)
                {
                    break;
                }
            }
            if (chain.Count == 4)
            {
                var sb = new StringBuilder();
                foreach (PlaceInfo p in chain)
                {
                    sb.Append(p.Id).Append('|');
                }
                TryAddGenerated("gen-cp-" + StableHash.Hex(StableHash.Fnv1a64(sb.ToString())).Substring(0, 10), "Checkpoint route", ChallengeType.CheckpointRoute, chain, 0);
            }
            List<PlaceInfo> lots = QueryPlacesByDistance(new[] { PlaceKind.ServicePoint, PlaceKind.Commercial }, near, 3000, 1);
            if (lots.Count > 0)
            {
                TryAddGenerated("gen-pk-" + StableHash.Hex(StableHash.Fnv1a64(lots[0].Id)).Substring(0, 10), "Parking challenge", ChallengeType.Parking, lots, 0);
            }
            return GetChallenges();
        }

        private bool TryAddGenerated(string id, string name, ChallengeType type, List<PlaceInfo> places, double heading)
        {
            if (m_State.Challenges.Find(id) != null)
            {
                return true;
            }
            var def = new ChallengeDefinition { Id = id, Name = name, Type = type, Class = VehicleClass.Car, TargetHeadingDegrees = heading };
            foreach (PlaceInfo p in places)
            {
                def.Points.Add(PlaceRef.From(p));
            }
            if (!SignChallenge(def, out double reference, out string signature))
            {
                return false;
            }
            def.ReferenceSeconds = reference;
            def.Signature = signature;
            m_State.Challenges.Definitions.Add(def);
            return true;
        }

        /// <summary>
        /// Route signature (ordered references plus a fingerprint of the route) and reference time. Returns false when
        /// a leg is not reachable (Unknown counts as not reachable).
        /// </summary>
        private bool SignChallenge(ChallengeDefinition def, out double reference, out string signature)
        {
            reference = 0;
            var sb = new StringBuilder();
            sb.Append(def.Type).Append('|');
            for (int i = 0; i < def.Points.Count; i++)
            {
                PlaceRef p = def.Points[i];
                sb.Append(p.Id).Append('@').Append(Math.Round(p.Access.X).ToString(CultureInfo.InvariantCulture)).Append(',').Append(Math.Round(p.Access.Z).ToString(CultureInfo.InvariantCulture)).Append(';');
                if (i > 0)
                {
                    RouteEstimate e = EstimateRoute(def.Points[i - 1].Access, p.Id, def.Class);
                    if (!e.IsReachable)
                    {
                        signature = null;
                        return false;
                    }
                    reference += e.TimeSeconds;
                    sb.Append(Math.Round(e.DistanceMeters / 10).ToString(CultureInfo.InvariantCulture)).Append(';');
                }
            }
            if (def.Type == ChallengeType.Parking)
            {
                reference = CareerBalance.ParkingReferenceSeconds;
                sb.Append(Math.Round(def.TargetHeadingDegrees).ToString(CultureInfo.InvariantCulture));
            }
            signature = StableHash.Hex(StableHash.Fnv1a64(sb.ToString()));
            return reference > 0;
        }

        /// <summary>Re-signs challenges after a city edit: changed routes get a new version; broken ones become unavailable.</summary>
        private void RevalidateChallenges()
        {
            foreach (ChallengeDefinition def in m_State.Challenges.Definitions)
            {
                bool unknown = false;
                bool missing = false;
                var updated = new List<PlaceRef>();
                foreach (PlaceRef p in def.Points)
                {
                    PlaceInfo info = DescribePlace(p.Id, out bool u);
                    unknown |= u;
                    if (info == null && !u)
                    {
                        missing = true;
                    }
                    PlaceRef copy = p.Clone();
                    if (info != null)
                    {
                        copy.Access = info.Access;
                    }
                    updated.Add(copy);
                }
                if (unknown)
                {
                    continue;
                }
                if (missing)
                {
                    SetUnavailable(def);
                    continue;
                }
                var probe = new ChallengeDefinition { Type = def.Type, Class = def.Class, Points = updated, TargetHeadingDegrees = def.TargetHeadingDegrees };
                if (!SignChallenge(probe, out double reference, out string signature))
                {
                    SetUnavailable(def);
                    continue;
                }
                def.Available = true;
                def.UnavailableKey = null;
                if (signature != def.Signature)
                {
                    def.Points = updated;
                    def.Signature = signature;
                    def.ReferenceSeconds = reference;
                    def.RouteVersion++;
                    Notify(NotificationLevel.Info, "DTC.Challenge.RouteChanged", "The route of '" + def.Name + "' changed. Earlier records are kept as 'route changed'.", def.Name);
                }
            }
        }

        private void SetUnavailable(ChallengeDefinition def)
        {
            if (def.Available)
            {
                Notify(NotificationLevel.Warning, "DTC.Challenge.Unavailable", "'" + def.Name + "' is unavailable: route no longer connected.", def.Name);
            }
            def.Available = false;
            def.UnavailableKey = "DTC.Challenge.Unavailable";
        }

        /// <summary>Arms a challenge run with a career vehicle (Accepted; Active from the start line).</summary>
        public CareerResult StartChallenge(string challengeId, string vehicleId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            ChallengeDefinition def = m_State.Challenges.Find(challengeId);
            if (def == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Challenge.NotFound", "No such challenge.");
            }
            if (!def.Available)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Challenge.Unavailable", "This challenge is unavailable: route no longer connected.");
            }
            if (m_State.Mission != null)
            {
                return Fail(CareerResultCode.MissionAlreadyActive, "DTC.Mission.AlreadyActive", "Finish or cancel your current job first.");
            }
            if (m_Run != null)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Challenge.RunActive", "A challenge is already running.");
            }
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.IsRemoved || v.Kind == VehicleKind.Service || v.Assignment == FleetAssignment.Managed)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Garage.VehicleNotFound", "Challenges need one of your career vehicles.");
            }
            if (!ChallengeClassMatches(def.Class, v.Class))
            {
                return Fail(CareerResultCode.WrongVehicleClass, "DTC.Challenge.WrongClass", "This challenge needs a " + def.Class + ".");
            }
            if (v.IsImmobilized)
            {
                return Fail(CareerResultCode.VehicleImmobilized, "DTC.Garage.Immobilized", "This vehicle cannot move.");
            }
            m_Run = new ChallengeRun { ChallengeId = def.Id, VehicleId = v.Id, State = MissionState.Accepted, ArmedAt = now, RouteVersion = def.RouteVersion };
            if (def.Type == ChallengeType.Parking)
            {
                m_Run.State = MissionState.Active;
                m_Run.StartedAt = now;
            }
            EmitRun(def, m_Run.State, "DTC.Challenge.Armed", "Challenge armed: " + def.Name + ".");
            return Done(CareerResult.Success(def.Id));
        }

        /// <summary>Aborts the current run (Cancelled, no result, no penalty).</summary>
        public CareerResult AbortChallenge(double nowSeconds)
        {
            Enter(nowSeconds);
            if (m_Run == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Challenge.NoRun", "No challenge is running.");
            }
            VoidRun("DTC.Challenge.Aborted");
            return Done(CareerResult.Success());
        }

        private void EmitRun(ChallengeDefinition def, MissionState state, string key, string text)
        {
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.ChallengeStateChanged, Time = m_Now, MissionId = def.Id, ToState = state, Key = key, Text = text, Args = new[] { def.Name } });
        }

        private void VoidRun(string key)
        {
            if (m_Run == null)
            {
                return;
            }
            ChallengeDefinition def = m_State.Challenges.Find(m_Run.ChallengeId);
            m_Run = null;
            if (def != null)
            {
                EmitRun(def, MissionState.Cancelled, key, "Challenge run void: no result, no penalty.");
            }
        }

        private void TickChallenge(PlayerSnapshot s, ContactSeverity contact, double now)
        {
            ChallengeRun run = m_Run;
            if (run == null)
            {
                return;
            }
            ChallengeDefinition def = m_State.Challenges.Find(run.ChallengeId);
            if (def == null || !def.Available || def.RouteVersion != run.RouteVersion)
            {
                VoidRun("DTC.Challenge.Void.RouteChanged");
                return;
            }
            if (s.Mode != PlayerMode.Driving || s.VehicleId != run.VehicleId)
            {
                VoidRun(s.Mode == PlayerMode.Building ? "DTC.Challenge.Void.Building" : "DTC.Challenge.Void.Exit");
                return;
            }
            if (run.State == MissionState.Active && contact != ContactSeverity.None)
            {
                if (def.Type == ChallengeType.Parking && contact == ContactSeverity.Severe)
                {
                    FinishRun(def, run, false, 0);
                    return;
                }
                run.PenaltySeconds += CareerBalance.ChallengeContactPenaltySeconds[(int)contact - 1];
            }
            if (def.Type == ChallengeType.Parking)
            {
                if (now - run.StartedAt > CareerBalance.ParkingTimeLimitSeconds)
                {
                    FinishRun(def, run, false, 0);
                    return;
                }
                if ((s.Stationary || s.SpeedMps <= 0.1) && InBay(def, s))
                {
                    FinishRun(def, run, true, now - run.StartedAt + run.PenaltySeconds);
                }
                return;
            }
            PlaceRef next = def.Points[run.NextPoint];
            if (MapPoint.Distance(s.Position, next.Access) > CareerBalance.ChallengeCheckpointRadius)
            {
                return;
            }
            if (run.NextPoint == 0)
            {
                run.State = MissionState.Active;
                run.StartedAt = now;
                EmitRun(def, MissionState.Active, "DTC.Challenge.Started", "Go!");
            }
            run.NextPoint++;
            if (run.NextPoint >= def.Points.Count)
            {
                FinishRun(def, run, true, now - run.StartedAt + run.PenaltySeconds);
            }
        }

        /// <summary>Parking tolerances: lateral, longitudinal and heading error in the bay's frame (SPEC-CHALLENGE 3.1).</summary>
        private static bool InBay(ChallengeDefinition def, PlayerSnapshot s)
        {
            bool truck = def.Class == VehicleClass.RigidTruck || def.Class == VehicleClass.Bus;
            double tolLat = truck ? CareerBalance.TruckParkingLateralTolerance : CareerBalance.ParkingLateralTolerance;
            double tolLong = truck ? CareerBalance.TruckParkingLongitudinalTolerance : CareerBalance.ParkingLongitudinalTolerance;
            double tolHeading = truck ? CareerBalance.TruckParkingHeadingTolerance : CareerBalance.ParkingHeadingTolerance;
            MapPoint target = def.Points[0].Access;
            double h = def.TargetHeadingDegrees * Math.PI / 180.0;
            double fx = Math.Sin(h);
            double fz = Math.Cos(h);
            double ox = s.Position.X - target.X;
            double oz = s.Position.Z - target.Z;
            double longitudinal = ox * fx + oz * fz;
            double lateral = ox * fz - oz * fx;
            return Math.Abs(lateral) <= tolLat && Math.Abs(longitudinal) <= tolLong && CareerMath.HeadingError(s.HeadingDegrees, def.TargetHeadingDegrees) <= tolHeading;
        }

        public static MedalTier MedalFor(double seconds, double reference)
        {
            if (reference <= 0 || seconds < 0)
            {
                return MedalTier.None;
            }
            double r = seconds / reference;
            if (r <= CareerBalance.MedalGold)
            {
                return MedalTier.Gold;
            }
            if (r <= CareerBalance.MedalSilver)
            {
                return MedalTier.Silver;
            }
            return r <= CareerBalance.MedalBronze ? MedalTier.Bronze : MedalTier.None;
        }

        private void FinishRun(ChallengeDefinition def, ChallengeRun run, bool success, double seconds)
        {
            m_Run = null;
            ChallengeRecord rec = m_State.Challenges.FindRecord(def.Id, def.RouteVersion);
            if (rec == null)
            {
                rec = new ChallengeRecord { ChallengeId = def.Id, RouteVersion = def.RouteVersion };
                m_State.Challenges.Records.Add(rec);
            }
            rec.Attempts++;
            rec.LastAttemptAt = m_Now;
            if (!success)
            {
                EmitRun(def, MissionState.Failed, "DTC.Challenge.Failed", "Challenge failed.");
                return;
            }
            MedalTier medal = MedalFor(seconds, def.ReferenceSeconds);
            if (rec.BestSeconds < 0 || seconds < rec.BestSeconds)
            {
                rec.BestSeconds = seconds;
            }
            if (medal > rec.Medal)
            {
                rec.Medal = medal;
            }
            long cash = 0;
            for (int tier = 1; tier <= (int)medal; tier++)
            {
                string tx = "CH-" + def.Id + "-" + CareerMath.Inv(def.RouteVersion) + "-" + ((MedalTier)tier);
                if (m_State.Wallet.Contains(tx))
                {
                    continue;
                }
                long reward = CareerBalance.ChallengeRewardCents[tier - 1];
                if (m_State.Wallet.Append(tx, LedgerEntryType.ChallengeReward, reward, m_Now, def.Id, "DTC.Ledger.ChallengeReward", def.Name + ": " + (MedalTier)tier + " reward") == LedgerAppendStatus.Appended)
                {
                    m_State.Progression.AddXp(Progression.CareerForClass(def.Class), CareerBalance.ChallengeRewardXp[tier - 1]);
                    cash += reward;
                }
            }
            m_Sink.Add(new CareerEvent
            {
                Kind = CareerEventKind.ChallengeStateChanged,
                Time = m_Now,
                MissionId = def.Id,
                ToState = MissionState.Completed,
                AmountCents = cash,
                Key = "DTC.Challenge.Completed",
                Text = def.Name + ": " + seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s (" + medal + ")" + (cash > 0 ? ", reward " + CareerMath.Money(cash) : string.Empty) + ".",
                Args = new[] { def.Name, seconds.ToString("0.00", CultureInfo.InvariantCulture), medal.ToString() },
            });
        }
    }
}
