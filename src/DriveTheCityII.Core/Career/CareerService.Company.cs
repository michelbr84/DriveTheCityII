using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// The company is a MANAGERIAL SIMULATION (SPEC-COMPANY, REQ-COMPANY-002): employees and managed vehicles never
    /// exist in the city; their work is calculated per period against a settled-until checkpoint (REQ-COMPANY-003,
    /// REQ-TIME-004). Period p settles exactly once per save timeline with TransactionIds CO-&lt;CompanyId&gt;-&lt;p&gt;-&lt;Line&gt;.
    /// </summary>
    public sealed partial class CareerService
    {
        public const string CompanyHeadingKey = "DTC.Company.Heading";
        public const string CompanyHeadingText = "Managed operations (simulated)";
        public const string CompanyExplanationKey = "DTC.Company.Explanation";
        public const string CompanyExplanationText = "Your employees' work is calculated, not shown in the city.";

        private static readonly string[] s_FirstNames = { "Alex", "Bruna", "Carlos", "Dana", "Eli", "Fernanda", "Gus", "Helena", "Ivo", "Joana", "Kai", "Lia" };
        private static readonly string[] s_LastNames = { "Silva", "Moreau", "Kowalski", "Tanaka", "Okafor", "Lindqvist", "Rossi", "Haddad", "Novak", "Costa" };

        private static int BaseJobs(VehicleClass cls)
        {
            switch (cls)
            {
                case VehicleClass.Van:
                    return 5;
                case VehicleClass.RigidTruck:
                case VehicleClass.Bus:
                    return 2;
                default:
                    return 6;
            }
        }

        private static long FuelPerJob(VehicleClass cls) => cls == VehicleClass.Van ? 500 : (cls == VehicleClass.RigidTruck || cls == VehicleClass.Bus) ? 1500 : 300;

        private static double WearPerJob(VehicleClass cls) => cls == VehicleClass.Van ? 0.6 : (cls == VehicleClass.RigidTruck || cls == VehicleClass.Bus) ? 1.0 : 0.5;

        private static long BasePayPerJob(CareerKind k) => k == CareerKind.Delivery ? 4500 : k == CareerKind.Freight ? 13000 : k == CareerKind.Bus ? 12000 : 3500;

        private static VehicleClass ClassFor(CareerKind k) => k == CareerKind.Delivery ? VehicleClass.Van : k == CareerKind.Freight ? VehicleClass.RigidTruck : k == CareerKind.Bus ? VehicleClass.Bus : VehicleClass.Car;

        private static double SkillFactor(int skill) => 0.6 + 0.2 * CareerMath.Clamp(skill, 1, 5);

        private static double ConditionFactor(double condition) => condition <= 0 ? 0 : condition < 20 ? 0.5 : condition < 50 ? 0.75 : 1.0;

        private static bool ClassMatches(VehicleClass required, VehicleClass actual)
        {
            return required == actual || (required == VehicleClass.Car && actual == VehicleClass.Taxi);
        }

        private static long PeriodIndexAt(Company c, double now) => (long)Math.Floor(Math.Max(0, now - c.FoundedAt) / CareerBalance.CompanyPeriodSeconds);

        private CareerResult NoCompany() => Fail(CareerResultCode.NotFound, "DTC.Company.None", "You have no company.");

        private CareerResult InArrears() => Fail(CareerResultCode.CompanyInArrears, "DTC.Company.InArrears", "The company is in arrears: no new contracts, hires or purchases until its debt is paid.");

        // ------------------------------------------------------------------ founding and transfers

        /// <summary>Founds the company (SPEC-COMPANY 3.1): unlock, cost from the wallet (CF-&lt;CompanyId&gt;), name 3-32 chars.</summary>
        public CareerResult FoundCompany(string name, string headquartersPlaceId, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            if (m_State.Company != null)
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Company.Exists", "You already have a company.");
            }
            if (!ValidName(name))
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Company.InvalidName", "Company names need 3 to 32 displayable characters.");
            }
            if (!m_State.Progression.IsUnlocked(Unlock.CompanyFounding))
            {
                return Fail(CareerResultCode.Locked, "DTC.Company.Locked", Progression.RequirementText(Unlock.CompanyFounding));
            }
            if (m_State.DeferredCents > 0)
            {
                return Fail(CareerResultCode.DeferredChargesOpen, "DTC.Garage.DeferredOpen", "Repay your deferred charges first.");
            }
            if (CareerBalance.CompanyFoundingCents > m_State.Wallet.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Founding a company costs " + CareerMath.Money(CareerBalance.CompanyFoundingCents) + ".");
            }
            PlaceRef hq = null;
            if (!string.IsNullOrEmpty(headquartersPlaceId))
            {
                PlaceInfo p = DescribePlace(headquartersPlaceId, out bool unknown);
                if (p == null && !unknown)
                {
                    return Fail(CareerResultCode.NotFound, "DTC.Company.HqMissing", "That headquarters location does not exist.");
                }
                hq = p != null ? PlaceRef.From(p) : null;
            }
            hq = hq ?? m_State.Home.Garage?.Clone() ?? m_State.Home.Home?.Clone();
            string id = "C" + CareerMath.Inv(++m_State.Seq.Company);
            string tx = "CF-" + id;
            Spend(tx, LedgerEntryType.CompanyFounding, CareerBalance.CompanyFoundingCents, id, "DTC.Ledger.CompanyFounding", "Company founding: " + name.Trim());
            m_State.Company = new Company { Id = id, Name = name.Trim(), Headquarters = hq, FoundedAt = now };
            Hook("Company", "Your company has its own account. " + CompanyExplanationText);
            CareerResult r = CareerResult.Success(id);
            r.TransactionId = tx;
            r.AmountCents = -CareerBalance.CompanyFoundingCents;
            return Done(r);
        }

        private static bool ValidName(string name)
        {
            if (name == null)
            {
                return false;
            }
            string t = name.Trim();
            if (t.Length < 3 || t.Length > 32)
            {
                return false;
            }
            foreach (char ch in t)
            {
                if (char.IsControl(ch))
                {
                    return false;
                }
            }
            return true;
        }

        public CareerResult RenameCompany(string name, double nowSeconds)
        {
            Enter(nowSeconds);
            if (m_State.Company == null)
            {
                return NoCompany();
            }
            if (!ValidName(name))
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Company.InvalidName", "Company names need 3 to 32 displayable characters.");
            }
            m_State.Company.Name = name.Trim();
            return Done(CareerResult.Success(m_State.Company.Id));
        }

        /// <summary>Explicit wallet-to-company transfer: paired entries CT-&lt;TransferId&gt;-W and -C under one operation ID.</summary>
        public CareerResult TransferToCompany(long cents, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tid = "T-" + op;
            if (m_State.Wallet.Contains("CT-" + tid + "-W"))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This transfer was already made.");
            }
            if (cents <= 0)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Company.TransferAmount", "Enter a positive amount.");
            }
            if (cents > m_State.Wallet.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money in your wallet.");
            }
            m_State.Wallet.Append("CT-" + tid + "-W", LedgerEntryType.CompanyTransfer, -cents, now, c.Id, "DTC.Ledger.TransferToCompany", "Transfer to " + c.Name);
            c.Account.Append("CT-" + tid + "-C", LedgerEntryType.CompanyTransfer, cents, now, tid, "DTC.Ledger.TransferFromWallet", "Transfer from the owner's wallet");
            RepayCompanyDebt(c, "CO-" + c.Id + "-DEBT-" + tid, now);
            CareerResult r = CareerResult.Success(tid);
            r.AmountCents = cents;
            return Done(r);
        }

        /// <summary>Explicit company-to-wallet transfer (never automatic).</summary>
        public CareerResult TransferToWallet(long cents, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tid = "T-" + op;
            if (m_State.Wallet.Contains("CT-" + tid + "-W"))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This transfer was already made.");
            }
            if (cents <= 0)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Company.TransferAmount", "Enter a positive amount.");
            }
            if (cents > c.Account.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money in the company account.");
            }
            c.Account.Append("CT-" + tid + "-C", LedgerEntryType.CompanyTransfer, -cents, now, tid, "DTC.Ledger.TransferToWallet", "Transfer to the owner's wallet");
            m_State.Wallet.Append("CT-" + tid + "-W", LedgerEntryType.CompanyTransfer, cents, now, c.Id, "DTC.Ledger.TransferFromCompany", "Transfer from " + c.Name);
            CareerResult r = CareerResult.Success(tid);
            r.AmountCents = cents;
            return Done(r);
        }

        private void RepayCompanyDebt(Company c, string tx, double now)
        {
            if (c.DebtCents <= 0 || c.Account.Balance <= 0 || c.Account.Contains(tx))
            {
                return;
            }
            long repay = Math.Min(c.DebtCents, c.Account.Balance);
            if (c.Account.Append(tx, LedgerEntryType.CompanyCost, -repay, now, c.Id, "DTC.Ledger.CompanyDebt", "Debt repayment") == LedgerAppendStatus.Appended)
            {
                c.DebtCents -= repay;
                if (c.DebtCents == 0)
                {
                    c.ArrearsPeriods = 0;
                }
            }
        }

        // ------------------------------------------------------------------ fleet

        /// <summary>The company buys a vehicle from its own account (CO-&lt;CompanyId&gt;-VP-&lt;VehicleId&gt;).</summary>
        public CareerResult CompanyBuyVehicle(string modelId, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            VehicleModel model = CareerCatalog.Find(modelId);
            if (model == null || (model.Acquisition != VehicleAcquisition.Purchase && model.Acquisition != VehicleAcquisition.CompanyPurchase))
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Garage.NotForSale", "This model is not for sale.");
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string id = "V-" + op;
            if (m_State.FindVehicle(id) != null)
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This purchase was already made.");
            }
            if (c.InArrears)
            {
                return InArrears();
            }
            if (model.PriceCents > c.Account.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "The company account cannot pay " + CareerMath.Money(model.PriceCents) + ".");
            }
            string tx = "CO-" + c.Id + "-VP-" + id;
            c.Account.Append(tx, LedgerEntryType.VehiclePurchase, -model.PriceCents, now, id, "DTC.Ledger.VehiclePurchase", "Fleet purchase: " + model.Name);
            var v = new CareerVehicle
            {
                Id = id,
                ModelId = model.Id,
                Class = model.Class,
                Kind = VehicleKind.Owned,
                Holder = VehicleHolder.Company,
                State = VehicleLocationState.Stored,
                FuelLiters = model.TankLiters,
                Condition = model.Id == "car.used" ? 80 : 100,
                PurchasePriceCents = model.PriceCents,
                AcquiredAt = now,
            };
            m_State.Vehicles.Add(v);
            return VehicleOk(CareerResult.Success(), v, -model.PriceCents, tx);
        }

        /// <summary>Contributes a personal vehicle to the company as capital (no payment).</summary>
        public CareerResult ContributeVehicle(string vehicleId, double nowSeconds)
        {
            Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.IsRemoved || v.Kind != VehicleKind.Owned || v.Holder != VehicleHolder.Player)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Garage.NotOwned", "Only vehicles you own can be contributed.");
            }
            if (IsInOpenMission(v) || v.State == VehicleLocationState.InUse)
            {
                return Fail(CareerResultCode.VehicleInMission, "DTC.Garage.InUse", "Leave the vehicle and finish the job first.");
            }
            v.Holder = VehicleHolder.Company;
            v.Assignment = FleetAssignment.Unassigned;
            return Done(CareerResult.Success(v.Id));
        }

        // ------------------------------------------------------------------ hiring

        /// <summary>Up to 3 candidates per period, generated deterministically from (CompanyId, period, index).</summary>
        public IReadOnlyList<EmployeeCandidate> GetCandidates(double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return new List<EmployeeCandidate>();
            }
            SettleCompany(now, int.MaxValue);
            return CandidatesAt(c, now);
        }

        private List<EmployeeCandidate> CandidatesAt(Company c, double now)
        {
            var list = new List<EmployeeCandidate>();
            long period = PeriodIndexAt(c, now);
            for (int i = 0; i < CareerBalance.CompanyCandidatesPerPeriod; i++)
            {
                string id = "cand-" + CareerMath.Inv(period) + "-" + CareerMath.Inv(i);
                if (c.UsedOfferIds.Contains(id))
                {
                    continue;
                }
                DeterministicRandom rng = DeterministicRandom.FromParts(c.Id, CareerMath.Inv(period), "cand", CareerMath.Inv(i));
                int skill = rng.NextRange(1, 5);
                double reliability = Math.Round(rng.NextRange(0.70, 0.98), 2);
                long salary = CareerBalance.BaseSalaryCents + 1500 * (skill - 1) + 100 * rng.NextRange(0, 10);
                string name = s_FirstNames[rng.NextInt(s_FirstNames.Length)] + " " + s_LastNames[rng.NextInt(s_LastNames.Length)];
                list.Add(new EmployeeCandidate { Id = id, Name = name, Skill = skill, Reliability = reliability, SalaryCents = salary });
            }
            return list;
        }

        /// <summary>Hires a candidate for the hiring fee (company account). Employees are abstract records only.</summary>
        public CareerResult Hire(string candidateId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            if (c.UsedOfferIds.Contains(candidateId ?? string.Empty))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This candidate was already hired.");
            }
            SettleCompany(now, int.MaxValue);
            EmployeeCandidate cand = null;
            foreach (EmployeeCandidate k in CandidatesAt(c, now))
            {
                if (k.Id == candidateId)
                {
                    cand = k;
                }
            }
            if (cand == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.CandidateGone", "This candidate is no longer available.");
            }
            if (c.InArrears)
            {
                return InArrears();
            }
            if (CareerBalance.HiringFeeCents > c.Account.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "The company account cannot pay the hiring fee.");
            }
            string tx = "CO-" + c.Id + "-HIRE-" + cand.Id;
            c.Account.Append(tx, LedgerEntryType.CompanyCost, -CareerBalance.HiringFeeCents, now, cand.Id, "DTC.Ledger.HiringFee", "Hiring fee: " + cand.Name);
            var e = new Employee
            {
                Id = "E" + CareerMath.Inv(++m_State.Seq.Employee),
                Name = cand.Name,
                Skill = cand.Skill,
                Reliability = cand.Reliability,
                SalaryCents = cand.SalaryCents,
                HiredAt = now,
            };
            c.Employees.Add(e);
            c.UsedOfferIds.Add(cand.Id);
            CareerResult r = CareerResult.Success(e.Id);
            r.TransactionId = tx;
            return Done(r);
        }

        /// <summary>Releases an employee; severance is one period's salary (unpaid severance becomes company debt).</summary>
        public CareerResult Fire(string employeeId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            Employee e = c.FindEmployee(employeeId);
            if (e == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.EmployeeNotFound", "No such employee.");
            }
            long unpaid = CompanyPay(c, "CO-" + c.Id + "-FIRE-" + e.Id, e.SalaryCents, "DTC.Ledger.Severance", "Severance: " + e.Name, now);
            c.DebtCents += unpaid;
            ReleaseEmployee(c, e);
            return Done(CareerResult.Success(e.Id));
        }

        private void ReleaseEmployee(Company c, Employee e)
        {
            CareerVehicle v = m_State.FindVehicle(e.VehicleId);
            if (v != null)
            {
                v.Assignment = FleetAssignment.Unassigned;
                v.AssignedEmployeeId = null;
            }
            c.Employees.Remove(e);
        }

        // ------------------------------------------------------------------ contracts

        /// <summary>Up to 3 company contract offers per period; availability and pay scale with reputation.</summary>
        public IReadOnlyList<CompanyContractOffer> GetCompanyContractOffers(double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return new List<CompanyContractOffer>();
            }
            SettleCompany(now, int.MaxValue);
            return ContractOffersAt(c, now);
        }

        private List<CompanyContractOffer> ContractOffersAt(Company c, double now)
        {
            var list = new List<CompanyContractOffer>();
            long period = PeriodIndexAt(c, now);
            double repFactor = 0.8 + 0.4 * m_State.Progression.Reputation / 100.0;
            for (int i = 0; i < CareerBalance.CompanyContractOffersPerPeriod; i++)
            {
                string id = "coff-" + CareerMath.Inv(period) + "-" + CareerMath.Inv(i);
                if (c.UsedOfferIds.Contains(id))
                {
                    continue;
                }
                DeterministicRandom rng = DeterministicRandom.FromParts(c.Id, CareerMath.Inv(period), "contract", CareerMath.Inv(i));
                var career = (CareerKind)rng.NextInt(4);
                VehicleClass cls = ClassFor(career);
                long pay = CareerMath.RoundCents(BasePayPerJob(career) * repFactor);
                list.Add(new CompanyContractOffer
                {
                    Id = id,
                    Career = career,
                    RequiredClass = cls,
                    QuotaPerPeriod = BaseJobs(cls) * rng.NextRange(1, 3),
                    PayPerJobCents = pay,
                    DurationPeriods = rng.NextRange(3, 10),
                    MinReliability = 0.70 + 0.05 * rng.NextInt(4),
                    PenaltyPerMissingJobCents = pay / 2,
                });
            }
            return list;
        }

        public CareerResult AcceptCompanyContract(string offerId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            if (c.UsedOfferIds.Contains(offerId ?? string.Empty))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This contract was already taken.");
            }
            SettleCompany(now, int.MaxValue);
            CompanyContractOffer offer = null;
            foreach (CompanyContractOffer o in ContractOffersAt(c, now))
            {
                if (o.Id == offerId)
                {
                    offer = o;
                }
            }
            if (offer == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.ContractGone", "This contract is no longer offered.");
            }
            if (c.InArrears)
            {
                return InArrears();
            }
            var k = new CompanyContract
            {
                Id = "K" + CareerMath.Inv(++m_State.Seq.Contract),
                Career = offer.Career,
                RequiredClass = offer.RequiredClass,
                QuotaPerPeriod = offer.QuotaPerPeriod,
                PayPerJobCents = offer.PayPerJobCents,
                DurationPeriods = offer.DurationPeriods,
                RemainingPeriods = offer.DurationPeriods,
                MinReliability = offer.MinReliability,
                PenaltyPerMissingJobCents = offer.PenaltyPerMissingJobCents,
            };
            c.Contracts.Add(k);
            c.UsedOfferIds.Add(offer.Id);
            return Done(CareerResult.Success(k.Id));
        }

        /// <summary>Cancels a company contract: penalty of two periods' expected pay (unpaid part becomes company debt).</summary>
        public CareerResult CancelCompanyContract(string contractId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            CompanyContract k = c.FindContract(contractId);
            if (k == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.ContractNotFound", "No such contract.");
            }
            long penalty = 2L * k.QuotaPerPeriod * k.PayPerJobCents;
            c.DebtCents += CompanyPay(c, "CO-" + c.Id + "-CANCEL-" + k.Id, penalty, "DTC.Ledger.ContractCancelPenalty", "Contract cancellation penalty", now);
            EndContract(c, k);
            return Done(CareerResult.Success(k.Id));
        }

        private void EndContract(Company c, CompanyContract k)
        {
            foreach (Employee e in c.Employees)
            {
                if (e.ContractId == k.Id)
                {
                    e.ContractId = null;
                    CareerVehicle v = m_State.FindVehicle(e.VehicleId);
                    if (v != null)
                    {
                        v.Assignment = FleetAssignment.Unassigned;
                        v.AssignedEmployeeId = null;
                    }
                    e.VehicleId = null;
                }
            }
            c.Contracts.Remove(k);
        }

        /// <summary>
        /// Assigns an employee and a company vehicle to a contract. The vehicle becomes "On managed contract
        /// (simulated)": it has no world presence and cannot be driven until unassigned.
        /// </summary>
        public CareerResult AssignEmployee(string employeeId, string vehicleId, string contractId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            Employee e = c.FindEmployee(employeeId);
            CompanyContract k = c.FindContract(contractId);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (e == null || k == null || v == null || v.IsRemoved)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.AssignNotFound", "Employee, contract or vehicle not found.");
            }
            if (v.Holder != VehicleHolder.Company)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Company.NotFleet", "Only company vehicles can run managed contracts.");
            }
            if (!ClassMatches(k.RequiredClass, v.Class))
            {
                return Fail(CareerResultCode.WrongVehicleClass, "DTC.Company.WrongClass", "This contract needs a " + k.RequiredClass + ".");
            }
            if (e.Reliability < k.MinReliability)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Company.Reliability", "This employee's reliability is below the contract minimum.");
            }
            if (v.State == VehicleLocationState.InUse || IsInOpenMission(v))
            {
                return Fail(CareerResultCode.VehicleInMission, "DTC.Garage.InUse", "The vehicle is being driven.");
            }
            if (v.AssignedEmployeeId != null && v.AssignedEmployeeId != e.Id)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Company.VehicleTaken", "This vehicle is assigned to another employee.");
            }
            CareerVehicle previous = m_State.FindVehicle(e.VehicleId);
            if (previous != null && previous != v)
            {
                previous.Assignment = FleetAssignment.Unassigned;
                previous.AssignedEmployeeId = null;
            }
            e.VehicleId = v.Id;
            e.ContractId = k.Id;
            v.Assignment = FleetAssignment.Managed;
            v.AssignedEmployeeId = e.Id;
            v.State = VehicleLocationState.Stored;
            return Done(CareerResult.Success(e.Id));
        }

        public CareerResult UnassignEmployee(string employeeId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            Employee e = c.FindEmployee(employeeId);
            if (e == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Company.EmployeeNotFound", "No such employee.");
            }
            CareerVehicle v = m_State.FindVehicle(e.VehicleId);
            if (v != null)
            {
                v.Assignment = FleetAssignment.Unassigned;
                v.AssignedEmployeeId = null;
            }
            e.VehicleId = null;
            e.ContractId = null;
            return Done(CareerResult.Success(e.Id));
        }

        public CareerResult SetAutoMaintenance(bool on, double nowSeconds)
        {
            Enter(nowSeconds);
            if (m_State.Company == null)
            {
                return NoCompany();
            }
            m_State.Company.AutoMaintenance = on;
            return Done(CareerResult.Success(m_State.Company.Id));
        }

        /// <summary>
        /// Dissolution, always available (SPEC-COMPANY 5): contracts end without penalty, employees are released,
        /// vehicles move to personal ownership, remaining cash is transferred to the wallet, and debt is written off
        /// with -10 reputation and no wallet charge.
        /// </summary>
        public CareerResult DissolveCompany(double nowSeconds)
        {
            double now = Enter(nowSeconds);
            Company c = m_State.Company;
            if (c == null)
            {
                return NoCompany();
            }
            SettleCompany(now, int.MaxValue);
            c.Contracts.Clear();
            c.Employees.Clear();
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (v.Holder == VehicleHolder.Company && !v.IsRemoved)
                {
                    v.Holder = VehicleHolder.Player;
                    v.Assignment = FleetAssignment.Unassigned;
                    v.AssignedEmployeeId = null;
                }
            }
            long cash = c.Account.Balance;
            if (cash > 0)
            {
                string tid = "D-" + c.Id;
                c.Account.Append("CT-" + tid + "-C", LedgerEntryType.CompanyTransfer, -cash, now, tid, "DTC.Ledger.DissolutionTransfer", "Dissolution: cash to the owner");
                m_State.Wallet.Append("CT-" + tid + "-W", LedgerEntryType.CompanyTransfer, cash, now, c.Id, "DTC.Ledger.DissolutionTransfer", "Dissolution of " + c.Name);
            }
            if (c.DebtCents > 0)
            {
                m_State.Progression.AddReputation(CareerBalance.CompanyDissolveReputation);
            }
            m_State.Company = null;
            CareerResult r = CareerResult.Success(c.Id);
            r.AmountCents = cash;
            return Done(r);
        }

        // ------------------------------------------------------------------ period settlement

        /// <summary>Pays up to <paramref name="cost"/> from the company account; returns the unpaid remainder.</summary>
        private long CompanyPay(Company c, string tx, long cost, string key, string text, double now)
        {
            if (cost <= 0 || c.Account.Contains(tx))
            {
                return 0;
            }
            long paid = Math.Min(cost, c.Account.Balance);
            if (paid > 0)
            {
                c.Account.Append(tx, LedgerEntryType.CompanyCost, -paid, now, c.Id, key, text);
            }
            return cost - paid;
        }

        /// <summary>
        /// Settles every completed period after the checkpoint, in order and exactly once, at most
        /// <paramref name="maxPeriods"/> per call (catch-up is amortised, never dropped). Pure function of the persisted
        /// company state and seeded generators, so pauses, reloads and replays give the same result.
        /// </summary>
        private void SettleCompany(double now, int maxPeriods)
        {
            Company c = m_State.Company;
            if (c == null || now < c.SettledUntil)
            {
                return;
            }
            long startBalance = c.Account.Balance;
            long startDebt = c.DebtCents;
            long jobs = 0;
            int n = 0;
            while (n < maxPeriods)
            {
                long p = c.LastSettledPeriod + 1;
                double end = c.FoundedAt + (p + 1) * CareerBalance.CompanyPeriodSeconds;
                if (end > now + 1e-9)
                {
                    break;
                }
                jobs += SettlePeriod(c, p, end);
                c.LastSettledPeriod = p;
                n++;
                if (m_State.Company != c)
                {
                    return;
                }
            }
            if (n == 0)
            {
                return;
            }
            long net = (c.Account.Balance - startBalance) - (c.DebtCents - startDebt);
            m_Sink.Add(new CareerEvent
            {
                Kind = CareerEventKind.CompanySettled,
                Time = m_Now,
                AmountCents = net,
                Key = "DTC.Company.Report",
                Text = "Company report: " + n + " period(s) settled. Net " + CareerMath.Money(net) + ". Simulated jobs: " + jobs + ".",
                Args = new[] { CareerMath.Inv(n), CareerMath.Inv(net), CareerMath.Inv(jobs) },
                Level = NotificationLevel.Info,
            });
        }

        private long SettlePeriod(Company c, long p, double end)
        {
            string pre = "CO-" + c.Id + "-" + CareerMath.Inv(p) + "-";
            Ledger acct = c.Account;
            RepayCompanyDebt(c, pre + "DEBT", end);

            long jobsTotal = 0;
            var fuelCosts = new List<KeyValuePair<string, long>>();
            var penalties = new List<KeyValuePair<string, long>>();
            foreach (CompanyContract k in c.Contracts)
            {
                if (k.RemainingPeriods <= 0)
                {
                    continue;
                }
                int remaining = k.QuotaPerPeriod;
                int done = 0;
                foreach (Employee e in c.Employees)
                {
                    if (e.ContractId != k.Id || e.VehicleId == null)
                    {
                        continue;
                    }
                    CareerVehicle v = m_State.FindVehicle(e.VehicleId);
                    if (v == null || v.IsRemoved)
                    {
                        continue;
                    }
                    double capacity = BaseJobs(v.Class) * SkillFactor(e.Skill) * ConditionFactor(v.Condition);
                    int possible = Math.Min((int)Math.Floor(capacity + 1e-9), remaining);
                    DeterministicRandom rng = DeterministicRandom.FromParts(c.Id, CareerMath.Inv(p), e.Id);
                    int d = 0;
                    for (int j = 0; j < possible; j++)
                    {
                        if (rng.NextDouble() < e.Reliability)
                        {
                            d++;
                        }
                    }
                    remaining -= d;
                    done += d;
                    v.Condition = Math.Max(0, v.Condition - d * WearPerJob(v.Class));
                    if (d > 0)
                    {
                        fuelCosts.Add(new KeyValuePair<string, long>(v.Id, d * FuelPerJob(v.Class)));
                    }
                }
                long income = done * k.PayPerJobCents;
                if (income > 0)
                {
                    acct.Append(pre + "INC-" + k.Id, LedgerEntryType.CompanyIncome, income, end, k.Id, "DTC.Ledger.CompanyIncome", "Contract income (" + done + " simulated jobs)");
                }
                k.JobsDone += done;
                k.JobsExpected += k.QuotaPerPeriod;
                int missing = k.QuotaPerPeriod - done;
                if (missing > 0 && k.PenaltyPerMissingJobCents > 0)
                {
                    penalties.Add(new KeyValuePair<string, long>(k.Id, missing * k.PenaltyPerMissingJobCents));
                }
                jobsTotal += done;
            }

            // Costs in order: salaries, then maintenance, then other costs (SPEC-COMPANY section 5).
            bool unpaidAny = false;
            foreach (Employee e in c.Employees)
            {
                long unpaid = CompanyPay(c, pre + "SAL-" + e.Id, e.SalaryCents, "DTC.Ledger.Salary", "Salary: " + e.Name, end);
                if (unpaid > 0)
                {
                    c.DebtCents += unpaid;
                    e.UnpaidPeriods++;
                    e.Reliability = Math.Max(0.1, Math.Round(e.Reliability - CareerBalance.CompanyArrearsReliabilityLoss, 4));
                    unpaidAny = true;
                }
                else
                {
                    e.UnpaidPeriods = 0;
                }
            }
            if (c.AutoMaintenance)
            {
                foreach (CareerVehicle v in m_State.Vehicles)
                {
                    if (v.Holder != VehicleHolder.Company || v.IsRemoved || v.Condition >= 70)
                    {
                        continue;
                    }
                    long rate = v.Model != null ? v.Model.MaintenancePerPointCents : 0;
                    long cost = (long)Math.Ceiling((100 - v.Condition) * rate - 1e-9);
                    if (cost <= acct.Balance)
                    {
                        if (cost > 0)
                        {
                            acct.Append(pre + "MNT-" + v.Id, LedgerEntryType.CompanyCost, -cost, end, v.Id, "DTC.Ledger.CompanyMaintenance", "Automatic maintenance");
                        }
                        v.Condition = 100;
                    }
                }
            }
            foreach (KeyValuePair<string, long> f in fuelCosts)
            {
                long unpaid = CompanyPay(c, pre + "FUEL-" + f.Key, f.Value, "DTC.Ledger.CompanyFuel", "Fuel (calculated)", end);
                c.DebtCents += unpaid;
                unpaidAny |= unpaid > 0;
            }
            foreach (KeyValuePair<string, long> pen in penalties)
            {
                long unpaid = CompanyPay(c, pre + "PEN-" + pen.Key, pen.Value, "DTC.Ledger.CompanyPenalty", "Shortfall penalty", end);
                c.DebtCents += unpaid;
                unpaidAny |= unpaid > 0;
            }
            c.ArrearsPeriods = unpaidAny ? c.ArrearsPeriods + 1 : (c.DebtCents > 0 ? c.ArrearsPeriods : 0);

            foreach (Employee e in c.Employees.ToArray())
            {
                if (e.UnpaidPeriods >= CareerBalance.CompanyArrearsResignPeriods)
                {
                    Notify(NotificationLevel.Warning, "DTC.Company.Resigned", e.Name + " resigned after " + e.UnpaidPeriods + " unpaid periods.", e.Name);
                    ReleaseEmployee(c, e);
                }
            }
            foreach (CompanyContract k in c.Contracts.ToArray())
            {
                if (k.RemainingPeriods <= 0)
                {
                    continue;
                }
                k.RemainingPeriods--;
                if (k.RemainingPeriods == 0)
                {
                    double fulfilment = k.JobsExpected > 0 ? (double)k.JobsDone / k.JobsExpected : 1;
                    if (fulfilment < CareerBalance.CompanyShortfallThreshold)
                    {
                        m_State.Progression.AddReputation(CareerBalance.CompanyShortfallReputation);
                    }
                    EndContract(c, k);
                }
            }
            c.SimulatedJobsTotal += jobsTotal;
            return jobsTotal;
        }

        private void RevalidateCompanyHeadquarters()
        {
            Company c = m_State.Company;
            if (c?.Headquarters == null)
            {
                return;
            }
            PlaceInfo p = DescribePlace(c.Headquarters.Id, out bool unknown);
            if (unknown)
            {
                return;
            }
            if (p == null)
            {
                if (!c.HeadquartersMissing)
                {
                    c.HeadquartersMissing = true;
                    Notify(NotificationLevel.Warning, "DTC.Company.HqMissing", "Your company headquarters no longer exists. Choose a new location; managed operations continue.");
                }
            }
            else
            {
                c.HeadquartersMissing = false;
                c.Headquarters.Access = p.Access;
            }
        }
    }
}
