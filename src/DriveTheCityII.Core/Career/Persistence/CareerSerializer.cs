using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Outcome of loading persisted career data (SPEC-MODULES 4.7, SPEC-SAVE section 8).</summary>
    public enum CareerLoadStatus
    {
        Loaded = 0,

        /// <summary>Loaded after an in-memory forward migration; written back only at the next save.</summary>
        Migrated = 1,

        NoCareerData = 2,

        /// <summary>The data has a newer schema than this build supports; nothing was loaded or modified.</summary>
        NewerSchemaRefused = 3,

        /// <summary>The data failed validation; nothing was loaded. The caller keeps the last valid copy.</summary>
        Quarantined = 4,
    }

    public sealed class CareerLoadResult
    {
        public CareerLoadStatus Status;
        public int SchemaVersionFound;
        public List<string> Reasons = new List<string>();

        public bool IsOk => Status == CareerLoadStatus.Loaded || Status == CareerLoadStatus.Migrated;

        public override string ToString() => Status + (Reasons.Count > 0 ? ": " + string.Join("; ", Reasons) : string.Empty);
    }

    /// <summary>
    /// Forward-only schema migrations (SPEC-SAVE section 10). Each step is a pure function from version N to N+1.
    /// Schema 1 is the first version, so no step exists yet; add steps to <see cref="s_Steps"/> keyed by source version.
    /// </summary>
    public static class CareerMigrations
    {
        private static readonly Dictionary<int, Func<JsonValue, JsonValue>> s_Steps = new Dictionary<int, Func<JsonValue, JsonValue>>();

        public static bool TryMigrate(JsonValue root, int fromVersion, int toVersion, out JsonValue migrated, out string error)
        {
            migrated = root;
            error = null;
            for (int v = fromVersion; v < toVersion; v++)
            {
                if (!s_Steps.TryGetValue(v, out Func<JsonValue, JsonValue> step))
                {
                    error = "no migration from schema " + v + " to " + (v + 1);
                    return false;
                }
                try
                {
                    migrated = step(migrated);
                    migrated.Set("schemaVersion", v + 1);
                }
                catch (Exception ex)
                {
                    error = "migration " + v + " failed: " + ex.Message;
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>Serialization, validation and migration of <see cref="CareerState"/>.</summary>
    internal static class CareerSerializer
    {
        public static JsonValue Write(CareerState s, int schemaVersion)
        {
            JsonValue xp = JsonValue.Object();
            foreach (CareerKind k in CareerKinds())
            {
                xp.Set(k.ToString(), s.Progression.Xp(k));
            }
            JsonValue vehicles = JsonValue.Array();
            foreach (CareerVehicle v in s.Vehicles)
            {
                vehicles.Add(v.ToJson());
            }
            JsonValue history = JsonValue.Array();
            foreach (MissionHistoryEntry h in s.History)
            {
                history.Add(h.ToJson());
            }
            JsonValue seq = JsonValue.Object()
                .Set("mission", s.Seq.Mission)
                .Set("vehicle", s.Seq.Vehicle)
                .Set("rental", s.Seq.Rental)
                .Set("operation", s.Seq.Operation)
                .Set("offer", s.Seq.Offer)
                .Set("challenge", s.Seq.Challenge)
                .Set("company", s.Seq.Company)
                .Set("transfer", s.Seq.Transfer)
                .Set("correction", s.Seq.Correction)
                .Set("employee", s.Seq.Employee)
                .Set("contract", s.Seq.Contract);

            return JsonValue.Object()
                .Set("schemaVersion", schemaVersion)
                .Set("profileId", s.ProfileId)
                .Set("testSeeded", s.TestSeeded)
                .Set("clock", s.Clock)
                .Set("cityRevision", s.CityRevision)
                .Set("rng", s.Rng.StateHex)
                .Set("seq", seq)
                .Set("wallet", JsonValue.Object()
                    .Set("balance", s.Wallet.Balance)
                    .Set("deferred", s.DeferredCents)
                    .Set("ledger", s.Wallet.ToJson()))
                .Set("progression", JsonValue.Object().Set("xp", xp).Set("reputation", s.Progression.Reputation))
                .Set("vehicles", vehicles)
                .Set("mission", s.Mission != null ? WriteMission(s.Mission) : JsonValue.Null)
                .Set("history", history)
                .Set("home", s.Home.ToJson())
                .Set("company", s.Company != null ? s.Company.ToJson() : JsonValue.Null)
                .Set("challenges", s.Challenges.ToJson())
                .Set("taxiDuty", s.TaxiDuty)
                .Set("tutorial", JsonRead.WriteStrList(s.TutorialHooks));
        }

        public static CareerState Read(JsonValue root, int currentSchema, out CareerLoadResult result)
        {
            result = new CareerLoadResult();
            if (root == null || root.Kind == JsonKind.Null)
            {
                result.Status = CareerLoadStatus.NoCareerData;
                return null;
            }
            if (root.Kind != JsonKind.Object)
            {
                return Quarantine(result, "root: expected object");
            }
            JsonValue sv = root["schemaVersion"];
            if (sv.Kind != JsonKind.Number || sv.NumberValue != Math.Floor(sv.NumberValue))
            {
                return Quarantine(result, "schemaVersion: missing or not an integer");
            }
            int version = (int)sv.NumberValue;
            result.SchemaVersionFound = version;
            if (version > currentSchema)
            {
                result.Status = CareerLoadStatus.NewerSchemaRefused;
                result.Reasons.Add("schema " + version + " is newer than supported schema " + currentSchema);
                return null;
            }
            if (version < 1)
            {
                return Quarantine(result, "schemaVersion " + version + " is invalid");
            }
            bool migrated = false;
            if (version < currentSchema)
            {
                // Migrate a parsed copy so the caller's DOM is never modified.
                JsonValue copy = JsonValue.Parse(root.ToJson(false));
                if (!CareerMigrations.TryMigrate(copy, version, currentSchema, out JsonValue m, out string error))
                {
                    return Quarantine(result, error);
                }
                root = m;
                migrated = true;
            }

            var r = new JsonRead();
            CareerState s = ReadState(root, r);
            if (s != null)
            {
                ValidateCrossReferences(s, r);
            }
            if (!r.Ok || s == null)
            {
                result.Status = CareerLoadStatus.Quarantined;
                result.Reasons.AddRange(r.Errors);
                return null;
            }
            result.Status = migrated ? CareerLoadStatus.Migrated : CareerLoadStatus.Loaded;
            return s;
        }

        private static CareerState Quarantine(CareerLoadResult result, string reason)
        {
            result.Status = CareerLoadStatus.Quarantined;
            result.Reasons.Add(reason);
            return null;
        }

        private static CareerState ReadState(JsonValue o, JsonRead r)
        {
            var s = new CareerState
            {
                ProfileId = r.Str(o, "profileId", "$"),
                TestSeeded = r.Bool(o, "testSeeded", "$"),
                Clock = r.Num(o, "clock", "$", 0),
                CityRevision = r.Long(o, "cityRevision", "$", long.MinValue / 2, long.MaxValue / 2),
                TaxiDuty = r.Bool(o, "taxiDuty", "$"),
            };
            string rng = r.Str(o, "rng", "$");
            if (rng != null && DeterministicRandom.TryParseState(rng, out ulong st))
            {
                s.Rng = new DeterministicRandom(st);
            }
            else
            {
                r.Error("$.rng", "invalid generator state");
            }

            JsonValue seq = r.Obj(o, "seq", "$");
            if (seq != null)
            {
                s.Seq.Mission = r.Long(seq, "mission", "$.seq", 0);
                s.Seq.Vehicle = r.Long(seq, "vehicle", "$.seq", 0);
                s.Seq.Rental = r.Long(seq, "rental", "$.seq", 0);
                s.Seq.Operation = r.Long(seq, "operation", "$.seq", 0);
                s.Seq.Offer = r.Long(seq, "offer", "$.seq", 0);
                s.Seq.Challenge = r.Long(seq, "challenge", "$.seq", 0);
                s.Seq.Company = r.Long(seq, "company", "$.seq", 0);
                s.Seq.Transfer = r.Long(seq, "transfer", "$.seq", 0);
                s.Seq.Correction = r.Long(seq, "correction", "$.seq", 0);
                s.Seq.Employee = r.Long(seq, "employee", "$.seq", 0);
                s.Seq.Contract = r.Long(seq, "contract", "$.seq", 0);
            }

            JsonValue wallet = r.Obj(o, "wallet", "$");
            if (wallet != null)
            {
                long balance = r.Long(wallet, "balance", "$.wallet", 0);
                s.DeferredCents = r.Long(wallet, "deferred", "$.wallet", 0, CareerBalance.DeferredChargeCapCents);
                s.Wallet = Ledger.Restore(r.Arr(wallet, "ledger", "$.wallet"), balance, "$.wallet.ledger", r);
            }

            JsonValue prog = r.Obj(o, "progression", "$");
            if (prog != null)
            {
                JsonValue xpObj = r.Obj(prog, "xp", "$.progression");
                var xp = new long[4];
                if (xpObj != null)
                {
                    foreach (CareerKind k in CareerKinds())
                    {
                        xp[(int)k] = r.Long(xpObj, k.ToString(), "$.progression.xp", 0, 1000000000);
                    }
                }
                int rep = r.Int(prog, "reputation", "$.progression", 0, 100);
                s.Progression.Restore(xp, rep);
            }

            int i = 0;
            foreach (JsonValue v in r.Arr(o, "vehicles", "$").Items)
            {
                string p = "$.vehicles[" + i++ + "]";
                if (v.Kind != JsonKind.Object)
                {
                    r.Error(p, "expected object");
                    continue;
                }
                s.Vehicles.Add(CareerVehicle.FromJson(v, p, r));
            }

            JsonValue mission = r.Obj(o, "mission", "$", false);
            if (mission != null)
            {
                s.Mission = ReadMission(mission, "$.mission", r);
            }

            i = 0;
            foreach (JsonValue h in r.Arr(o, "history", "$").Items)
            {
                s.History.Add(MissionHistoryEntry.FromJson(h, "$.history[" + i++ + "]", r));
            }

            JsonValue home = r.Obj(o, "home", "$");
            if (home != null)
            {
                s.Home = HomeState.FromJson(home, "$.home", r);
            }
            JsonValue company = r.Obj(o, "company", "$", false);
            if (company != null)
            {
                s.Company = Company.FromJson(company, "$.company", r);
            }
            JsonValue challenges = r.Obj(o, "challenges", "$");
            if (challenges != null)
            {
                s.Challenges = ChallengeBook.FromJson(challenges, "$.challenges", r);
            }
            foreach (string t in r.StrList(o, "tutorial", "$"))
            {
                s.TutorialHooks.Add(t);
            }
            return s;
        }

        /// <summary>Invariants across records (SPEC-MODULES section 7 item 5, SPEC-SAVE section 7 step 2).</summary>
        private static void ValidateCrossReferences(CareerState s, JsonRead r)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int basic = 0;
            foreach (CareerVehicle v in s.Vehicles)
            {
                if (v.Id != null && !ids.Add(v.Id))
                {
                    r.Error("$.vehicles", "duplicate vehicle id '" + v.Id + "'");
                }
                if (v.Kind == VehicleKind.BasicRental && !v.IsRemoved)
                {
                    basic++;
                }
                if (v.Holder == VehicleHolder.Company && s.Company == null && !v.IsRemoved)
                {
                    r.Error("$.vehicles", "company vehicle '" + v.Id + "' without a company");
                }
            }
            if (basic > 1)
            {
                r.Error("$.vehicles", "more than one active basic rental");
            }

            var history = new HashSet<string>(StringComparer.Ordinal);
            foreach (MissionHistoryEntry h in s.History)
            {
                if (h.MissionId != null && !history.Add(h.MissionId))
                {
                    r.Error("$.history", "duplicate mission '" + h.MissionId + "'");
                }
                if (h.TransactionId != null && !s.Wallet.Contains(h.TransactionId))
                {
                    r.Error("$.history", "settlement '" + h.TransactionId + "' missing from the ledger");
                }
            }
            if (s.History.Count > CareerBalance.MissionHistoryCap)
            {
                r.Error("$.history", "history exceeds its cap");
            }

            Mission m = s.Mission;
            if (m != null)
            {
                ValidateMission(s, m, r);
            }

            Company c = s.Company;
            if (c != null)
            {
                var employees = new HashSet<string>(StringComparer.Ordinal);
                foreach (Employee e in c.Employees)
                {
                    if (!employees.Add(e.Id ?? string.Empty))
                    {
                        r.Error("$.company.employees", "duplicate employee '" + e.Id + "'");
                    }
                    if (e.VehicleId != null)
                    {
                        CareerVehicle v = s.FindVehicle(e.VehicleId);
                        if (v == null || v.Holder != VehicleHolder.Company || v.IsRemoved)
                        {
                            r.Error("$.company.employees", "employee '" + e.Id + "' assigned to an invalid vehicle");
                        }
                    }
                    if (e.ContractId != null && c.FindContract(e.ContractId) == null)
                    {
                        r.Error("$.company.employees", "employee '" + e.Id + "' assigned to an unknown contract");
                    }
                }
            }
        }

        private static void ValidateMission(CareerState s, Mission m, JsonRead r)
        {
            if (!m.IsOpen)
            {
                r.Error("$.mission.state", "persisted mission must be Accepted or Active, found " + m.State);
            }
            if (m.Legs.Count == 0)
            {
                r.Error("$.mission.legs", "mission without legs");
            }
            for (int i = 0; i < m.Legs.Count; i++)
            {
                if (m.Legs[i].Index != i)
                {
                    r.Error("$.mission.legs[" + i + "]", "leg index mismatch");
                }
            }
            if (m.State == MissionState.Active)
            {
                if (m.AllRequiredResolved && !m.SettlementPending)
                {
                    r.Error("$.mission", "Active mission without a pending leg");
                }
                CareerVehicle v = s.FindVehicle(m.VehicleId);
                if (v == null || v.IsRemoved)
                {
                    r.Error("$.mission.vehicle", "Active mission without a valid vehicle");
                }
            }
            if (s.Wallet.HasSettlement(m.Id))
            {
                r.Error("$.mission", "open mission '" + m.Id + "' is already settled in the ledger");
            }
            int terms = (m.Taxi != null ? 1 : 0) + (m.Delivery != null ? 1 : 0) + (m.Freight != null ? 1 : 0) + (m.Bus != null ? 1 : 0);
            bool match = (m.Career == CareerKind.Taxi && m.Taxi != null) || (m.Career == CareerKind.Delivery && m.Delivery != null) || (m.Career == CareerKind.Freight && m.Freight != null) || (m.Career == CareerKind.Bus && m.Bus != null);
            if (terms != 1 || !match)
            {
                r.Error("$.mission", "career terms do not match the career");
            }
            if (m.Delivery != null)
            {
                foreach (Parcel p in m.Delivery.Parcels)
                {
                    if (p.LegIndex < 0 || p.LegIndex >= m.Legs.Count)
                    {
                        r.Error("$.mission.delivery", "parcel '" + p.Id + "' references an invalid leg");
                    }
                }
            }
            if (m.Freight != null)
            {
                foreach (FreightLoad l in m.Freight.Loads)
                {
                    if (l.LegIndex < 0 || l.LegIndex >= m.Legs.Count)
                    {
                        r.Error("$.mission.freight", "load references an invalid leg");
                    }
                }
            }
            if (m.AllowedClasses.Count == 0)
            {
                r.Error("$.mission.allowedClasses", "no allowed vehicle class");
            }
        }

        internal static JsonValue WriteMission(Mission m)
        {
            JsonValue legs = JsonValue.Array();
            foreach (MissionLeg l in m.Legs)
            {
                legs.Add(l.ToJson());
            }
            JsonValue classes = JsonValue.Array();
            foreach (VehicleClass c in m.AllowedClasses)
            {
                classes.Add(JsonValue.From(c.ToString()));
            }
            return JsonValue.Object()
                .Set("id", m.Id)
                .Set("career", m.Career.ToString())
                .Set("state", m.State.ToString())
                .Set("fault", m.Fault.ToString())
                .Set("endReason", m.EndReasonKey)
                .Set("vehicle", m.VehicleId)
                .Set("allowedClasses", classes)
                .Set("usesServiceVehicle", m.UsesServiceVehicle)
                .Set("serviceModel", m.ServiceModelId)
                .Set("serviceSpawn", m.ServiceSpawnPlaceId)
                .Set("acceptedAt", m.AcceptedAt)
                .Set("activatedAt", m.ActivatedAt)
                .Set("startWindowEnd", m.StartWindowEnd)
                .Set("endedAt", m.EndedAt)
                .Set("plannedMeters", m.PlannedMeters)
                .Set("plannedSeconds", m.PlannedSeconds)
                .Set("approachMeters", m.ApproachMeters)
                .Set("contractValue", m.ContractValueCents)
                .Set("legs", legs)
                .Set("cleanliness", m.Cleanliness)
                .Set("light", m.LightContacts)
                .Set("medium", m.MediumContacts)
                .Set("severe", m.SevereContacts)
                .Set("harsh", m.HarshEvents)
                .Set("driven", m.DrivenMeters)
                .Set("hasVehiclePosition", m.HasVehiclePosition)
                .Set("vehiclePosition", JsonRead.WritePoint(m.LastVehiclePosition))
                .Set("awaySince", m.AwaySince)
                .Set("immobileSince", m.ImmobileSince)
                .Set("settlementPending", m.SettlementPending)
                .Set("rng", m.Rng.StateHex)
                .Set("taxi", m.Taxi != null ? m.Taxi.ToJson() : JsonValue.Null)
                .Set("delivery", m.Delivery != null ? m.Delivery.ToJson() : JsonValue.Null)
                .Set("freight", m.Freight != null ? m.Freight.ToJson() : JsonValue.Null)
                .Set("bus", m.Bus != null ? m.Bus.ToJson() : JsonValue.Null);
        }

        internal static Mission ReadMission(JsonValue o, string path, JsonRead r)
        {
            var m = new Mission
            {
                Id = r.Str(o, "id", path),
                Career = r.Enum<CareerKind>(o, "career", path),
                State = r.Enum<MissionState>(o, "state", path),
                Fault = r.Enum<MissionFault>(o, "fault", path),
                EndReasonKey = r.Str(o, "endReason", path, false),
                VehicleId = r.Str(o, "vehicle", path, false),
                UsesServiceVehicle = r.Bool(o, "usesServiceVehicle", path),
                ServiceModelId = r.Str(o, "serviceModel", path, false),
                ServiceSpawnPlaceId = r.Str(o, "serviceSpawn", path, false),
                AcceptedAt = r.Num(o, "acceptedAt", path, 0),
                ActivatedAt = r.Num(o, "activatedAt", path, -1),
                StartWindowEnd = r.Num(o, "startWindowEnd", path, 0),
                EndedAt = r.Num(o, "endedAt", path, -1),
                PlannedMeters = r.Num(o, "plannedMeters", path, 0),
                PlannedSeconds = r.Num(o, "plannedSeconds", path, 0),
                ApproachMeters = r.Num(o, "approachMeters", path, 0),
                ContractValueCents = r.Long(o, "contractValue", path, 0),
                Cleanliness = r.Num(o, "cleanliness", path, 0, 100),
                LightContacts = r.Int(o, "light", path, 0),
                MediumContacts = r.Int(o, "medium", path, 0),
                SevereContacts = r.Int(o, "severe", path, 0),
                HarshEvents = r.Int(o, "harsh", path, 0),
                DrivenMeters = r.Num(o, "driven", path, 0),
                HasVehiclePosition = r.Bool(o, "hasVehiclePosition", path),
                LastVehiclePosition = r.Point(o, "vehiclePosition", path),
                AwaySince = r.Num(o, "awaySince", path, -1),
                ImmobileSince = r.Num(o, "immobileSince", path, -1),
                SettlementPending = r.Bool(o, "settlementPending", path),
            };
            foreach (string c in r.StrList(o, "allowedClasses", path))
            {
                if (Enum.TryParse(c, false, out VehicleClass vc) && Enum.IsDefined(typeof(VehicleClass), vc))
                {
                    m.AllowedClasses.Add(vc);
                }
                else
                {
                    r.Error(path + ".allowedClasses", "unknown class '" + c + "'");
                }
            }
            string rng = r.Str(o, "rng", path);
            if (rng != null && DeterministicRandom.TryParseState(rng, out ulong st))
            {
                m.Rng = new DeterministicRandom(st);
            }
            else
            {
                r.Error(path + ".rng", "invalid generator state");
            }
            int i = 0;
            foreach (JsonValue l in r.Arr(o, "legs", path).Items)
            {
                string lp = path + ".legs[" + i++ + "]";
                if (l.Kind != JsonKind.Object)
                {
                    r.Error(lp, "expected object");
                    continue;
                }
                m.Legs.Add(MissionLeg.FromJson(l, lp, r));
            }
            JsonValue t;
            if ((t = r.Obj(o, "taxi", path, false)) != null)
            {
                m.Taxi = TaxiTerms.FromJson(t, path + ".taxi", r);
            }
            if ((t = r.Obj(o, "delivery", path, false)) != null)
            {
                m.Delivery = DeliveryTerms.FromJson(t, path + ".delivery", r);
            }
            if ((t = r.Obj(o, "freight", path, false)) != null)
            {
                m.Freight = FreightTerms.FromJson(t, path + ".freight", r);
            }
            if ((t = r.Obj(o, "bus", path, false)) != null)
            {
                m.Bus = BusTerms.FromJson(t, path + ".bus", r);
            }
            return m;
        }

        internal static IEnumerable<CareerKind> CareerKinds()
        {
            yield return CareerKind.Taxi;
            yield return CareerKind.Delivery;
            yield return CareerKind.Freight;
            yield return CareerKind.Bus;
        }
    }
}
