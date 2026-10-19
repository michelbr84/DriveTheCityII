using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// An abstract employee record of the MANAGERIAL SIMULATION (SPEC-COMPANY section 3.4): no avatar, no vehicle in
    /// the world, never shown on the map (REQ-COMPANY-002).
    /// </summary>
    public sealed class Employee
    {
        public string Id;
        public string Name;
        public int Skill;
        public double Reliability;
        public long SalaryCents;
        public string VehicleId;
        public string ContractId;
        public double HiredAt;
        public int UnpaidPeriods;

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("id", Id)
                .Set("name", Name)
                .Set("skill", Skill)
                .Set("reliability", Reliability)
                .Set("salary", SalaryCents)
                .Set("vehicle", VehicleId)
                .Set("contract", ContractId)
                .Set("hiredAt", HiredAt)
                .Set("unpaid", UnpaidPeriods);
        }

        internal static Employee FromJson(JsonValue o, string path, JsonRead r)
        {
            return new Employee
            {
                Id = r.Str(o, "id", path),
                Name = r.Str(o, "name", path),
                Skill = r.Int(o, "skill", path, 1, 5),
                Reliability = r.Num(o, "reliability", path, 0, 1),
                SalaryCents = r.Long(o, "salary", path, 0),
                VehicleId = r.Str(o, "vehicle", path, false),
                ContractId = r.Str(o, "contract", path, false),
                HiredAt = r.Num(o, "hiredAt", path, 0),
                UnpaidPeriods = r.Int(o, "unpaid", path, 0, 1000),
            };
        }
    }

    /// <summary>A company contract: a job quota per period fulfilled only by the managerial simulation (SPEC-COMPANY 3.5).</summary>
    public sealed class CompanyContract
    {
        public string Id;
        public CareerKind Career;
        public VehicleClass RequiredClass;
        public int QuotaPerPeriod;
        public long PayPerJobCents;
        public int DurationPeriods;
        public int RemainingPeriods;
        public double MinReliability;
        public long PenaltyPerMissingJobCents;
        public long JobsDone;
        public long JobsExpected;

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("id", Id)
                .Set("career", Career.ToString())
                .Set("class", RequiredClass.ToString())
                .Set("quota", QuotaPerPeriod)
                .Set("payPerJob", PayPerJobCents)
                .Set("duration", DurationPeriods)
                .Set("remaining", RemainingPeriods)
                .Set("minReliability", MinReliability)
                .Set("penalty", PenaltyPerMissingJobCents)
                .Set("jobsDone", JobsDone)
                .Set("jobsExpected", JobsExpected);
        }

        internal static CompanyContract FromJson(JsonValue o, string path, JsonRead r)
        {
            return new CompanyContract
            {
                Id = r.Str(o, "id", path),
                Career = r.Enum<CareerKind>(o, "career", path),
                RequiredClass = r.Enum<VehicleClass>(o, "class", path),
                QuotaPerPeriod = r.Int(o, "quota", path, 0, 10000),
                PayPerJobCents = r.Long(o, "payPerJob", path, 0),
                DurationPeriods = r.Int(o, "duration", path, 0, 100000),
                RemainingPeriods = r.Int(o, "remaining", path, 0, 100000),
                MinReliability = r.Num(o, "minReliability", path, 0, 1),
                PenaltyPerMissingJobCents = r.Long(o, "penalty", path, 0),
                JobsDone = r.Long(o, "jobsDone", path, 0),
                JobsExpected = r.Long(o, "jobsExpected", path, 0),
            };
        }
    }

    /// <summary>A hiring candidate, regenerated deterministically per period (not persisted).</summary>
    public sealed class EmployeeCandidate
    {
        public string Id;
        public string Name;
        public int Skill;
        public double Reliability;
        public long SalaryCents;
    }

    /// <summary>A company contract offer, regenerated deterministically per period (not persisted).</summary>
    public sealed class CompanyContractOffer
    {
        public string Id;
        public CareerKind Career;
        public VehicleClass RequiredClass;
        public int QuotaPerPeriod;
        public long PayPerJobCents;
        public int DurationPeriods;
        public double MinReliability;
        public long PenaltyPerMissingJobCents;
    }

    /// <summary>
    /// The player's company: a separate account with its own append-only ledger (SPEC-COMPANY section 3.2), employees,
    /// contracts and the settled-until checkpoint <see cref="LastSettledPeriod"/> (REQ-COMPANY-003, REQ-TIME-004).
    /// Everything here is a MANAGERIAL SIMULATION: results are calculated, never shown in the city.
    /// </summary>
    public sealed class Company
    {
        public string Id;
        public string Name;
        public PlaceRef Headquarters;
        public bool HeadquartersMissing;
        public double FoundedAt;

        /// <summary>Index of the last settled period (-1 before the first). Period p covers [FoundedAt + p L, FoundedAt + (p+1) L).</summary>
        public long LastSettledPeriod = -1;

        public Ledger Account = new Ledger();
        public long DebtCents;
        public int ArrearsPeriods;
        public bool AutoMaintenance = true;
        public long SimulatedJobsTotal;
        public List<Employee> Employees = new List<Employee>();
        public List<CompanyContract> Contracts = new List<CompanyContract>();

        /// <summary>Candidate and contract-offer ids already taken, so they are not offered again.</summary>
        public SortedSet<string> UsedOfferIds = new SortedSet<string>(System.StringComparer.Ordinal);

        public bool InArrears => DebtCents > 0;

        public double SettledUntil => FoundedAt + (LastSettledPeriod + 1) * CareerBalance.CompanyPeriodSeconds;

        public Employee FindEmployee(string id)
        {
            foreach (Employee e in Employees)
            {
                if (e.Id == id)
                {
                    return e;
                }
            }
            return null;
        }

        public CompanyContract FindContract(string id)
        {
            foreach (CompanyContract c in Contracts)
            {
                if (c.Id == id)
                {
                    return c;
                }
            }
            return null;
        }

        internal JsonValue ToJson()
        {
            JsonValue employees = JsonValue.Array();
            foreach (Employee e in Employees)
            {
                employees.Add(e.ToJson());
            }
            JsonValue contracts = JsonValue.Array();
            foreach (CompanyContract c in Contracts)
            {
                contracts.Add(c.ToJson());
            }
            return JsonValue.Object()
                .Set("id", Id)
                .Set("name", Name)
                .Set("hq", Headquarters != null ? Headquarters.ToJson() : JsonValue.Null)
                .Set("hqMissing", HeadquartersMissing)
                .Set("foundedAt", FoundedAt)
                .Set("lastSettledPeriod", LastSettledPeriod)
                .Set("balance", Account.Balance)
                .Set("ledger", Account.ToJson())
                .Set("debt", DebtCents)
                .Set("arrears", ArrearsPeriods)
                .Set("autoMaintenance", AutoMaintenance)
                .Set("simulatedJobs", SimulatedJobsTotal)
                .Set("employees", employees)
                .Set("contracts", contracts)
                .Set("usedOffers", JsonRead.WriteStrList(UsedOfferIds));
        }

        internal static Company FromJson(JsonValue o, string path, JsonRead r)
        {
            var c = new Company
            {
                Id = r.Str(o, "id", path),
                Name = r.Str(o, "name", path),
                Headquarters = PlaceRef.FromJson(r.Obj(o, "hq", path, false), path + ".hq", r),
                HeadquartersMissing = r.Bool(o, "hqMissing", path),
                FoundedAt = r.Num(o, "foundedAt", path, 0),
                LastSettledPeriod = r.Long(o, "lastSettledPeriod", path, -1, 1000000000),
                DebtCents = r.Long(o, "debt", path, 0),
                ArrearsPeriods = r.Int(o, "arrears", path, 0, 1000000),
                AutoMaintenance = r.Bool(o, "autoMaintenance", path),
                SimulatedJobsTotal = r.Long(o, "simulatedJobs", path, 0),
            };
            long balance = r.Long(o, "balance", path, 0);
            c.Account = Ledger.Restore(r.Arr(o, "ledger", path), balance, path + ".ledger", r);
            int i = 0;
            foreach (JsonValue e in r.Arr(o, "employees", path).Items)
            {
                c.Employees.Add(Employee.FromJson(e, path + ".employees[" + i++ + "]", r));
            }
            i = 0;
            foreach (JsonValue k in r.Arr(o, "contracts", path).Items)
            {
                c.Contracts.Add(CompanyContract.FromJson(k, path + ".contracts[" + i++ + "]", r));
            }
            foreach (string s in r.StrList(o, "usedOffers", path))
            {
                c.UsedOfferIds.Add(s);
            }
            return c;
        }
    }
}
