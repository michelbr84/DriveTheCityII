using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Read-only views for the UI. Views are copies and never change career state.</summary>
    public sealed partial class CareerService
    {
        private static LedgerEntryView ToView(LedgerEntry e)
        {
            return new LedgerEntryView
            {
                TransactionId = e.TransactionId,
                Type = e.Type,
                AmountCents = e.AmountCents,
                BalanceAfterCents = e.BalanceAfterCents,
                Time = e.Time,
                SourceRef = e.SourceRef,
                Key = e.DescriptionKey,
                Text = e.Text,
                Lines = new List<LedgerLine>(e.Lines),
            };
        }

        public long WalletBalanceCents => m_State.Wallet.Balance;

        public long DeferredChargesCents => m_State.DeferredCents;

        public WalletView GetWallet(int recentCount = 20)
        {
            var v = new WalletView { BalanceCents = m_State.Wallet.Balance, DeferredCents = m_State.DeferredCents, EntryCount = m_State.Wallet.Count };
            IReadOnlyList<LedgerEntry> entries = m_State.Wallet.Entries;
            for (int i = entries.Count - 1; i >= 0 && v.Recent.Count < recentCount; i--)
            {
                v.Recent.Add(ToView(entries[i]));
            }
            return v;
        }

        /// <summary>The full wallet ledger in append order (transaction history, REQ-ECON-002).</summary>
        public IReadOnlyList<LedgerEntryView> GetLedger()
        {
            var list = new List<LedgerEntryView>();
            foreach (LedgerEntry e in m_State.Wallet.Entries)
            {
                list.Add(ToView(e));
            }
            return list;
        }

        public ProgressView GetProgress()
        {
            Progression p = m_State.Progression;
            var v = new ProgressView { Reputation = p.Reputation, TotalLevel = p.TotalLevel, TestSeeded = m_State.TestSeeded };
            foreach (CareerKind k in CareerSerializer.CareerKinds())
            {
                long xp = p.Xp(k);
                long start = Progression.CurrentThreshold(xp);
                long next = Progression.NextThreshold(xp);
                v.Careers.Add(new CareerProgressView
                {
                    Career = k,
                    Xp = xp,
                    Level = Progression.LevelFor(xp),
                    LevelStartXp = start,
                    NextLevelXp = next,
                    Progress01 = next < 0 ? 1 : CareerMath.Clamp((double)(xp - start) / Math.Max(1, next - start), 0, 1),
                });
            }
            foreach (Unlock u in Progression.AllUnlocks())
            {
                (p.IsUnlocked(u, m_State.Company != null) ? v.Unlocked : v.Locked).Add(u);
            }
            return v;
        }

        public IReadOnlyList<MissionHistoryEntry> GetMissionHistory() => m_State.History.AsReadOnly();

        /// <summary>Summary of the Accepted or Active mission, or null.</summary>
        public ActiveMissionView GetActiveMission()
        {
            Mission m = m_State.Mission;
            if (m == null)
            {
                return null;
            }
            double now = m_State.Clock;
            var v = new ActiveMissionView
            {
                MissionId = m.Id,
                Career = m.Career,
                State = m.State,
                VehicleId = m.VehicleId,
                AllowedClasses = new List<VehicleClass>(m.AllowedClasses),
                PlannedMeters = m.PlannedMeters,
                ContractValueCents = m.ContractValueCents,
                SettlementPending = m.SettlementPending,
                StartWindowRemaining = m.State == MissionState.Accepted ? Math.Max(0, m.StartWindowEnd - now) : -1,
            };
            foreach (MissionLeg l in m.Legs)
            {
                v.Legs.Add(new MissionLegView { Index = l.Index, Group = l.Group, Kind = l.Kind, State = l.State, Outcome = l.Outcome, PlaceId = l.Place.Id, PlaceName = l.Place.Name, Access = l.Place.Access, Radius = l.Radius, Count = l.Count, TargetTime = l.TargetTime });
            }
            MissionLeg cur = m.CurrentLeg;
            if (cur != null)
            {
                v.CurrentLegIndex = cur.Index;
                v.CurrentLegKind = cur.Kind;
                v.CurrentLegState = cur.State;
                v.TargetPlaceId = cur.Place.Id;
                v.TargetPlaceName = cur.Place.Name;
                v.TargetAccess = cur.Place.Access;
                MapPoint? from = m.HasVehiclePosition ? m.LastVehiclePosition : (m_HasSnapshot ? m_LastSnapshot.Position : (MapPoint?)null);
                if (from.HasValue)
                {
                    v.DistanceHintMeters = MapPoint.Distance(from.Value, cur.Place.Access);
                }
                if (cur.ServicingStartedAt >= 0 && cur.RequiredSeconds > 0)
                {
                    v.ServicingProgress01 = CareerMath.Clamp((now - cur.ServicingStartedAt) / cur.RequiredSeconds, 0, 1);
                }
            }
            if (m.Taxi != null)
            {
                v.PassengerState = m.Taxi.PassengerState;
                v.Passengers = m.Taxi.Passengers;
                v.RideQuality = m.Taxi.Quality;
                v.Evaluation = m.Taxi.Quality;
                if (m.Taxi.PassengerState == PassengerState.Waiting && m.Taxi.PatienceDeadline >= 0 && m.Legs[0].ArrivedAt < 0)
                {
                    v.PatienceRemaining = Math.Max(0, m.Taxi.PatienceDeadline - now);
                }
            }
            if (m.Delivery != null)
            {
                DeliveryTerms t = m.Delivery;
                v.ParcelsTotal = t.Parcels.Count;
                foreach (Parcel p in t.Parcels)
                {
                    if (p.State == ParcelState.Loaded)
                    {
                        v.ParcelsLoaded++;
                        v.WorstParcelIntegrity = v.WorstParcelIntegrity < 0 ? p.Integrity : Math.Min(v.WorstParcelIntegrity, p.Integrity);
                    }
                    else if (p.State == ParcelState.Delivered)
                    {
                        v.ParcelsDelivered++;
                    }
                }
                SetDeadlines(v, t.SoftDeadline, t.HardDeadline, now);
                v.Evaluation = DeliveryRules.Evaluation(m);
            }
            if (m.Freight != null)
            {
                v.LoadIntegrity = m.Freight.Integrity;
                v.PayloadTonnes = m.Freight.LoadedTonnes;
                SetDeadlines(v, m.Freight.SoftDeadline, m.Freight.HardDeadline, now);
                v.Evaluation = FreightRules.Evaluation(m);
            }
            if (m.Bus != null)
            {
                v.LineName = m.Bus.LineName;
                v.BusOnboard = m.Bus.OnboardCount;
                int waiting = 0;
                foreach (PassengerGroup g in m.Bus.Waiting)
                {
                    waiting += g.Count;
                }
                v.BusWaitingAtNextStop = waiting;
                v.StopsServed = m.Bus.StopsServed;
                v.StopsMissed = m.Bus.StopsMissed;
                v.Evaluation = BusRules.Evaluation(m);
            }
            return v;
        }

        private static void SetDeadlines(ActiveMissionView v, double soft, double hard, double now)
        {
            if (soft >= 0)
            {
                v.SoftDeadlineRemaining = soft - now;
                v.HardDeadlineRemaining = hard - now;
            }
        }

        public IReadOnlyList<VehicleView> GetVehicles(bool includeRemoved = false)
        {
            var list = new List<VehicleView>();
            Mission m = m_State.Mission;
            foreach (CareerVehicle c in m_State.Vehicles)
            {
                if (c.IsRemoved && !includeRemoved)
                {
                    continue;
                }
                VehicleModel model = c.Model;
                list.Add(new VehicleView
                {
                    Id = c.Id,
                    ModelId = c.ModelId,
                    Name = model?.Name ?? c.ModelId,
                    PrefabCategory = model?.PrefabCategory,
                    Class = c.Class,
                    Kind = c.Kind,
                    Holder = c.Holder,
                    State = c.State,
                    Immobilized = c.Immobilized,
                    FuelPercent = c.FuelFraction * 100.0,
                    ConditionPercent = c.Condition,
                    OdometerKm = c.OdometerKm,
                    Upgrades = new List<UpgradeKind>(c.Upgrades),
                    Performance = c.GetPerformance(),
                    Assignment = c.Assignment,
                    ManagedBadge = c.Assignment == FleetAssignment.Managed ? "On managed contract (simulated)" : null,
                    InCurrentMission = m != null && m.VehicleId == c.Id,
                    SaleValueCents = c.Kind == VehicleKind.Owned ? c.SaleValueCents() : 0,
                    StoredAtPlaceId = c.StoredAtPlaceId,
                    LastPosition = c.LastPosition,
                    HasPosition = c.HasPosition,
                });
            }
            return list;
        }

        public VehicleView GetVehicle(string vehicleId)
        {
            foreach (VehicleView v in GetVehicles(true))
            {
                if (v.Id == vehicleId)
                {
                    return v;
                }
            }
            return null;
        }

        public CompanyView GetCompany(int recentCount = 20)
        {
            Company c = m_State.Company;
            if (c == null)
            {
                return new CompanyView { Exists = false };
            }
            var v = new CompanyView
            {
                Exists = true,
                Id = c.Id,
                Name = c.Name,
                CashCents = c.Account.Balance,
                DebtCents = c.DebtCents,
                InArrears = c.InArrears,
                ArrearsPeriods = c.ArrearsPeriods,
                LastSettledPeriod = c.LastSettledPeriod,
                SettledUntil = c.SettledUntil,
                NextSettlementAt = c.SettledUntil + CareerBalance.CompanyPeriodSeconds,
                SimulatedJobsTotal = c.SimulatedJobsTotal,
                AutoMaintenance = c.AutoMaintenance,
                HeadquartersPlaceId = c.Headquarters?.Id,
                HeadquartersMissing = c.HeadquartersMissing,
            };
            foreach (Employee e in c.Employees)
            {
                v.Employees.Add(new EmployeeView { Id = e.Id, Name = e.Name, Skill = e.Skill, Reliability = e.Reliability, SalaryCents = e.SalaryCents, VehicleId = e.VehicleId, ContractId = e.ContractId, UnpaidPeriods = e.UnpaidPeriods });
            }
            foreach (CompanyContract k in c.Contracts)
            {
                v.Contracts.Add(new CompanyContractView { Id = k.Id, Career = k.Career, RequiredClass = k.RequiredClass, QuotaPerPeriod = k.QuotaPerPeriod, PayPerJobCents = k.PayPerJobCents, RemainingPeriods = k.RemainingPeriods, MinReliability = k.MinReliability, PenaltyPerMissingJobCents = k.PenaltyPerMissingJobCents, SimulatedJobsDone = k.JobsDone, JobsExpected = k.JobsExpected });
            }
            foreach (CareerVehicle cv in m_State.Vehicles)
            {
                if (cv.Holder == VehicleHolder.Company && !cv.IsRemoved)
                {
                    v.FleetVehicleIds.Add(cv.Id);
                }
            }
            IReadOnlyList<LedgerEntry> entries = c.Account.Entries;
            for (int i = entries.Count - 1; i >= 0 && v.RecentLedger.Count < recentCount; i--)
            {
                v.RecentLedger.Add(ToView(entries[i]));
            }
            return v;
        }

        /// <summary>The full company ledger in append order.</summary>
        public IReadOnlyList<LedgerEntryView> GetCompanyLedger()
        {
            var list = new List<LedgerEntryView>();
            if (m_State.Company != null)
            {
                foreach (LedgerEntry e in m_State.Company.Account.Entries)
                {
                    list.Add(ToView(e));
                }
            }
            return list;
        }

        public IReadOnlyList<ChallengeView> GetChallenges()
        {
            var list = new List<ChallengeView>();
            foreach (ChallengeDefinition d in m_State.Challenges.Definitions)
            {
                var v = new ChallengeView
                {
                    Id = d.Id,
                    Name = d.Name,
                    Type = d.Type,
                    Class = d.Class,
                    Available = d.Available,
                    UnavailableKey = d.UnavailableKey,
                    RouteVersion = d.RouteVersion,
                    ReferenceSeconds = d.ReferenceSeconds,
                    Running = m_Run != null && m_Run.ChallengeId == d.Id,
                    NextPoint = m_Run != null && m_Run.ChallengeId == d.Id ? m_Run.NextPoint : 0,
                };
                foreach (PlaceRef p in d.Points)
                {
                    v.Points.Add(p.Clone());
                }
                foreach (ChallengeRecord r in m_State.Challenges.Records)
                {
                    if (r.ChallengeId != d.Id)
                    {
                        continue;
                    }
                    if (r.RouteVersion == d.RouteVersion)
                    {
                        v.BestSeconds = r.BestSeconds;
                        v.Medal = r.Medal;
                        v.Attempts = r.Attempts;
                    }
                    else
                    {
                        v.HasOlderRecords = true;
                    }
                }
                list.Add(v);
            }
            return list;
        }

        public HomeView GetHome()
        {
            HomeState h = m_State.Home;
            var v = new HomeView { TemporaryBase = h.TemporaryBase, GarageCapacity = h.GarageCapacity };
            if (h.Home != null)
            {
                v.HasHome = true;
                v.HomePlaceId = h.Home.Id;
                v.HomeName = h.Home.Name;
                v.HomeAccess = h.Home.Access;
            }
            if (h.Garage != null)
            {
                v.HasGarage = true;
                v.GaragePlaceId = h.Garage.Id;
                v.GarageName = h.Garage.Name;
                v.GarageAccess = h.Garage.Access;
            }
            foreach (CareerVehicle c in m_State.Vehicles)
            {
                if (!c.IsRemoved && c.Holder == VehicleHolder.Player && c.State == VehicleLocationState.Stored)
                {
                    v.StoredVehicles++;
                }
            }
            return v;
        }
    }
}
