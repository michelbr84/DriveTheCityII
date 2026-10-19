using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Persisted id sequences. Ids are deterministic within a save line.</summary>
    internal sealed class CareerSequences
    {
        public long Mission;
        public long Vehicle;
        public long Rental;
        public long Operation;
        public long Offer;
        public long Challenge;
        public long Company;
        public long Transfer;
        public long Correction;
        public long Employee;
        public long Contract;
    }

    /// <summary>
    /// The whole career profile of one city save: the only mutable career state, written only by
    /// <see cref="CareerService"/> (SPEC-MODULES section 6, single writer).
    /// </summary>
    internal sealed class CareerState
    {
        public string ProfileId;
        public bool TestSeeded;

        /// <summary>Career-active clock checkpoint (last time seen), persisted so time is never skipped or repeated.</summary>
        public double Clock;

        public long CityRevision;
        public DeterministicRandom Rng = new DeterministicRandom(1);
        public CareerSequences Seq = new CareerSequences();
        public Ledger Wallet = new Ledger();
        public long DeferredCents;
        public Progression Progression = new Progression();
        public List<CareerVehicle> Vehicles = new List<CareerVehicle>();
        public Mission Mission;
        public List<MissionHistoryEntry> History = new List<MissionHistoryEntry>();
        public HomeState Home = new HomeState();
        public Company Company;
        public ChallengeBook Challenges = new ChallengeBook();
        public bool TaxiDuty;
        public SortedSet<string> TutorialHooks = new SortedSet<string>(StringComparer.Ordinal);

        public CareerVehicle FindVehicle(string id)
        {
            if (id == null)
            {
                return null;
            }
            foreach (CareerVehicle v in Vehicles)
            {
                if (v.Id == id)
                {
                    return v;
                }
            }
            return null;
        }

        public CareerVehicle ActiveBasicRental()
        {
            foreach (CareerVehicle v in Vehicles)
            {
                if (v.Kind == VehicleKind.BasicRental && !v.IsRemoved)
                {
                    return v;
                }
            }
            return null;
        }

        public MissionHistoryEntry FindHistory(string missionId)
        {
            foreach (MissionHistoryEntry h in History)
            {
                if (h.MissionId == missionId)
                {
                    return h;
                }
            }
            return null;
        }
    }
}
