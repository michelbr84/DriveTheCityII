using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Roadside and inaccessible-service recovery (SPEC-GARAGE section 9).</summary>
    public enum RoadsideService
    {
        /// <summary>Roadside fuel: fee + 25% of a tank at cost.</summary>
        Fuel = 0,

        /// <summary>Roadside quick fix to condition 20: fee + points at the class rate.</summary>
        QuickFix = 1,

        /// <summary>Tow to the nearest service point: base + per km towed. The mod relocates the vehicle.</summary>
        Tow = 2,

        /// <summary>No fuel point reachable: a full tank at the normal price plus a call fee.</summary>
        MobileRefuel = 3,

        /// <summary>No repair point reachable: a full repair at the class rate plus a call fee.</summary>
        MobileRepair = 4,
    }

    public sealed partial class CareerService
    {
        private CareerResult VehicleOk(CareerResult r, CareerVehicle v, long amount, string tx)
        {
            r.Id = v.Id;
            r.AmountCents = amount;
            r.TransactionId = tx;
            return Done(r);
        }

        private bool IsInOpenMission(CareerVehicle v)
        {
            Mission m = m_State.Mission;
            return m != null && m.IsOpen && m.VehicleId == v.Id;
        }

        // ------------------------------------------------------------------ rentals (REQ-GARAGE-001)

        /// <summary>
        /// Rents a catalog model: "car.basic" (always available, free, revenue share) or a premium rental (per-shift fee
        /// plus deposit, unlock required). Idempotent per operation ID.
        /// </summary>
        public CareerResult RentVehicle(string modelId, string operationId, double nowSeconds)
        {
            VehicleModel model = CareerCatalog.Find(modelId);
            if (model != null && model.Acquisition == VehicleAcquisition.BasicRental)
            {
                return RentBasic(nowSeconds);
            }
            double now = Enter(nowSeconds);
            if (model == null || model.Acquisition != VehicleAcquisition.PremiumRental)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Garage.NotRentable", "This model cannot be rented.");
            }
            if (!m_State.Progression.IsUnlocked(model.RequiredUnlock))
            {
                return Fail(CareerResultCode.Locked, "DTC.Garage.Locked", Progression.RequirementText(model.RequiredUnlock));
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string rentalId = "R-" + op;
            foreach (CareerVehicle existing in m_State.Vehicles)
            {
                if (existing.RentalId == rentalId)
                {
                    CareerResult again = CareerResult.Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This rental was already made.");
                    again.Id = existing.Id;
                    return Done(again);
                }
            }
            long cost = model.RentalShiftFeeCents + model.RentalDepositCents;
            if (cost > m_State.Wallet.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money: the shift fee and deposit cost " + CareerMath.Money(cost) + ".");
            }
            Spend("RN-" + rentalId + "-1", LedgerEntryType.RentalCharge, model.RentalShiftFeeCents, rentalId, "DTC.Ledger.RentalFee", "Rental shift fee (" + model.Name + ")");
            Spend("RD-" + rentalId, LedgerEntryType.RentalDeposit, model.RentalDepositCents, rentalId, "DTC.Ledger.RentalDeposit", "Rental deposit (" + model.Name + ")");
            var v = new CareerVehicle
            {
                Id = "V-" + op,
                ModelId = model.Id,
                Class = model.Class,
                Kind = VehicleKind.PremiumRental,
                Holder = VehicleHolder.Player,
                State = VehicleLocationState.Parked,
                FuelLiters = model.TankLiters,
                Condition = 100,
                AcquiredAt = now,
                RentalId = rentalId,
                RentalStartedAt = now,
                RentalFeeCents = model.RentalShiftFeeCents,
                DepositCents = model.RentalDepositCents,
                FuelAtStart = model.TankLiters,
                ConditionAtStart = 100,
            };
            m_State.Vehicles.Add(v);
            return VehicleOk(CareerResult.Success(), v, -cost, "RN-" + rentalId + "-1");
        }

        /// <summary>
        /// The basic rental: always available with no money, with deferred charges open and with every desk demolished
        /// (delivered to the player). One at a time; an immobilized basic rental is replaced for free (REQ-ECON-004,
        /// REQ-GARAGE-006). Repeated calls return the active rental.
        /// </summary>
        public CareerResult RentBasic(double nowSeconds)
        {
            double now = Enter(nowSeconds);
            CareerVehicle current = m_State.ActiveBasicRental();
            if (current != null && !current.IsImmobilized)
            {
                CareerResult same = CareerResult.Success(current.Id);
                same.Text = "Your basic rental is ready.";
                return Done(same);
            }
            VehicleModel model = CareerCatalog.Find(CareerCatalog.BasicRentalModelId);
            string rentalId = "R" + CareerMath.Inv(++m_State.Seq.Rental);
            var v = new CareerVehicle
            {
                Id = "V-" + rentalId,
                ModelId = model.Id,
                Class = model.Class,
                Kind = VehicleKind.BasicRental,
                Holder = VehicleHolder.Player,
                State = VehicleLocationState.Parked,
                FuelLiters = model.TankLiters,
                Condition = 100,
                AcquiredAt = now,
                RentalId = rentalId,
                RentalStartedAt = now,
                FuelAtStart = model.TankLiters,
                ConditionAtStart = 100,
            };
            if (current != null)
            {
                current.State = VehicleLocationState.Removed;
                if (current.HasPosition)
                {
                    v.LastPosition = current.LastPosition;
                    v.HasPosition = true;
                }
                m_Sink.Add(new CareerEvent { Kind = CareerEventKind.VehicleReleased, Time = now, VehicleId = current.Id, Key = "DTC.Garage.BasicReplaced", Text = "The disabled basic rental was collected; a replacement was delivered." });
                Mission m = m_State.Mission;
                if (m != null && m.IsOpen && m.VehicleId == current.Id)
                {
                    m.VehicleId = v.Id;
                }
            }
            m_State.Vehicles.Add(v);
            Hook("BasicRental", "The basic rental needs no money and includes fuel; it takes 25% of each job's pay.");
            return VehicleOk(CareerResult.Success(), v, 0, null);
        }

        /// <summary>
        /// Returns a rental. A basic rental is always free to return. A premium rental refunds the deposit minus extra
        /// hours (25% of the fee per started hour beyond 2 h), fuel used and condition lost; remote return adds a
        /// towing fee. Any excess is a mandatory charge (deferred if needed).
        /// </summary>
        public CareerResult ReturnRental(string vehicleId, bool atRentalDesk, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || !v.IsRental)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Garage.NotARental", "This is not one of your rentals.");
            }
            if (v.IsRemoved)
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This rental was already returned.");
            }
            if (IsInOpenMission(v))
            {
                return Fail(CareerResultCode.VehicleInMission, "DTC.Garage.InMission", "Finish or cancel the job first.");
            }
            long net = 0;
            string tx = null;
            if (v.Kind == VehicleKind.PremiumRental)
            {
                VehicleModel model = v.Model;
                double elapsed = Math.Max(0, now - v.RentalStartedAt);
                int extraHours = (int)Math.Ceiling(Math.Max(0, elapsed - CareerBalance.PremiumShiftSeconds) / 3600.0 - 1e-9);
                long extra = CareerMath.RoundCents(v.RentalFeeCents * CareerBalance.PremiumExtraHourShare * Math.Max(0, extraHours));
                long fuel = CareerMath.RoundCents(Math.Max(0, v.FuelAtStart - v.FuelLiters) * (model != null ? model.FuelPricePerLiterCents : 0));
                long cond = CareerMath.RoundCents(Math.Max(0, v.ConditionAtStart - v.Condition) * (model != null ? model.MaintenancePerPointCents : 0));
                long tow = atRentalDesk ? 0 : CareerBalance.TowBaseCents;
                long refund = v.DepositCents - extra - fuel - cond - tow;
                if (refund > 0)
                {
                    tx = "RR-" + v.RentalId;
                    m_State.Wallet.Append(tx, LedgerEntryType.DepositRefund, refund, now, v.RentalId, "DTC.Ledger.DepositRefund", "Deposit refund", new List<LedgerLine>
                    {
                        new LedgerLine("DTC.Ledger.Deposit", "Deposit", v.DepositCents),
                        new LedgerLine("DTC.Ledger.ExtraHours", "Extra hours", -extra),
                        new LedgerLine("DTC.Ledger.FuelUsed", "Fuel used", -fuel),
                        new LedgerLine("DTC.Ledger.ConditionLost", "Condition lost", -cond),
                        new LedgerLine("DTC.Ledger.Towing", "Remote return towing", -tow),
                    });
                    net = refund;
                }
                else if (refund < 0)
                {
                    tx = "RN-" + v.RentalId + "-RETURN";
                    ChargeMandatory(tx, LedgerEntryType.RentalCharge, -refund, v.RentalId, "DTC.Ledger.RentalReturnCharge", "Rental return charges beyond the deposit", out long paid, out _);
                    net = -paid;
                }
            }
            v.State = VehicleLocationState.Removed;
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.VehicleReleased, Time = now, VehicleId = v.Id, Key = "DTC.Garage.RentalReturned", Text = "Rental returned." });
            return VehicleOk(CareerResult.Success(), v, net, tx);
        }

        // ------------------------------------------------------------------ purchase and sale (REQ-GARAGE-002)

        /// <summary>Buys a model for personal use (unlock, funds, no open deferred charge). Charged once as VP-&lt;VehicleId&gt;.</summary>
        public CareerResult BuyVehicle(string modelId, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            VehicleModel model = CareerCatalog.Find(modelId);
            if (model == null || (model.Acquisition != VehicleAcquisition.Purchase && model.Acquisition != VehicleAcquisition.CompanyPurchase))
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Garage.NotForSale", "This model is not for sale.");
            }
            if (model.Acquisition == VehicleAcquisition.CompanyPurchase)
            {
                return Fail(CareerResultCode.Locked, "DTC.Garage.CompanyOnly", Progression.RequirementText(Unlock.CompanyOnly));
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string id = "V-" + op;
            CareerVehicle existing = m_State.FindVehicle(id);
            if (existing != null)
            {
                CareerResult again = CareerResult.Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This purchase was already made.");
                again.Id = id;
                return Done(again);
            }
            if (!m_State.Progression.IsUnlocked(model.RequiredUnlock))
            {
                return Fail(CareerResultCode.Locked, "DTC.Garage.Locked", Progression.RequirementText(model.RequiredUnlock));
            }
            if (m_State.DeferredCents > 0)
            {
                return Fail(CareerResultCode.DeferredChargesOpen, "DTC.Garage.DeferredOpen", "Repay your deferred charges before buying vehicles.");
            }
            if (model.PriceCents > m_State.Wallet.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money: this vehicle costs " + CareerMath.Money(model.PriceCents) + ".");
            }
            string tx = "VP-" + id;
            Spend(tx, LedgerEntryType.VehiclePurchase, model.PriceCents, id, "DTC.Ledger.VehiclePurchase", "Purchase: " + model.Name);
            // Delivered to storage; beyond capacity it stays Parked at the delivery point (SPEC-GARAGE section 5).
            bool room = StoredCount(VehicleHolder.Player) < m_State.Home.GarageCapacity;
            var v = new CareerVehicle
            {
                Id = id,
                ModelId = model.Id,
                Class = model.Class,
                Kind = VehicleKind.Owned,
                Holder = VehicleHolder.Player,
                State = room ? VehicleLocationState.Stored : VehicleLocationState.Parked,
                StoredAtPlaceId = m_State.Home.Garage?.Id,
                FuelLiters = model.TankLiters,
                Condition = model.Id == "car.used" ? 80 : 100,
                PurchasePriceCents = model.PriceCents,
                AcquiredAt = now,
            };
            m_State.Vehicles.Add(v);
            Hook("FirstPurchase", "You own this vehicle now: no revenue share, but you pay its fuel and repairs.");
            return VehicleOk(CareerResult.Success(), v, -model.PriceCents, tx);
        }

        /// <summary>Sells a personal vehicle (Stored, Parked or Immobilized; not in a job). Paid once as VS-&lt;VehicleId&gt;.</summary>
        public CareerResult SellVehicle(string vehicleId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.Kind != VehicleKind.Owned)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Garage.NotOwned", "You do not own this vehicle.");
            }
            string tx = "VS-" + v.Id;
            if (v.IsRemoved || m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This vehicle was already sold.");
            }
            if (v.Holder != VehicleHolder.Player)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Garage.CompanyVehicle", "This vehicle belongs to your company.");
            }
            if (IsInOpenMission(v))
            {
                return Fail(CareerResultCode.VehicleInMission, "DTC.Garage.InMission", "Finish or cancel the job first.");
            }
            if (v.State == VehicleLocationState.InUse)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.InUse", "Leave the vehicle before selling it.");
            }
            long value = v.SaleValueCents();
            m_State.Wallet.Append(tx, LedgerEntryType.VehicleSale, value, now, v.Id, "DTC.Ledger.VehicleSale", "Sale: " + (v.Model?.Name ?? v.ModelId));
            v.State = VehicleLocationState.Removed;
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.VehicleReleased, Time = now, VehicleId = v.Id, Key = "DTC.Garage.Sold", Text = "Vehicle sold." });
            return VehicleOk(CareerResult.Success(), v, value, tx);
        }

        // ------------------------------------------------------------------ fuel, repair, upgrades

        private CareerResult CheckServiceable(CareerVehicle v, string servicePointId, out bool ok)
        {
            ok = false;
            if (v == null || v.IsRemoved)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Garage.VehicleNotFound", "That vehicle is not available.");
            }
            if (v.Assignment == FleetAssignment.Managed)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.Managed", "This vehicle is on a managed contract (simulated).");
            }
            if (string.IsNullOrEmpty(servicePointId))
            {
                return Fail(CareerResultCode.NotAtServicePoint, "DTC.Garage.NotAtServicePoint", "Stop at a service point first, or call mobile service.");
            }
            if (m_World != null)
            {
                PlaceInfo p = DescribePlace(servicePointId, out bool unknown);
                if (!unknown && p == null)
                {
                    return Fail(CareerResultCode.NotAtServicePoint, "DTC.Garage.ServicePointGone", "This service point no longer exists. Use mobile service or another point.");
                }
            }
            ok = true;
            return null;
        }

        /// <summary>
        /// Refuels at a fuel service point (the caller checked the vehicle is stopped within 8 m of it) up to
        /// <paramref name="targetFraction"/> of the tank. Buys what the wallet can afford. Basic rental and service
        /// vehicles refuel free (fuel included). Charged once as FU-&lt;OperationId&gt;.
        /// </summary>
        public CareerResult Refuel(string vehicleId, string servicePointId, double targetFraction, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            CareerResult bad = CheckServiceable(v, servicePointId, out bool ok);
            if (!ok)
            {
                return bad;
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tx = "FU-" + op;
            if (m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This refuel was already paid.");
            }
            double target = CareerMath.IsFinite(targetFraction) ? CareerMath.Clamp(targetFraction, 0, 1) : 1;
            double liters = Math.Max(0, target * v.TankLiters - v.FuelLiters);
            if (liters <= 1e-6)
            {
                return VehicleOk(CareerResult.Success(), v, 0, null);
            }
            if (v.FuelIncluded)
            {
                v.AddFuel(liters);
                return VehicleOk(CareerResult.Success(), v, 0, null);
            }
            long price = v.Model.FuelPricePerLiterCents;
            long cost = (long)Math.Ceiling(liters * price - 1e-9);
            if (cost > m_State.Wallet.Balance)
            {
                liters = Math.Floor((double)m_State.Wallet.Balance / price * 100) / 100;
                cost = (long)Math.Ceiling(liters * price - 1e-9);
                if (liters <= 0 || cost > m_State.Wallet.Balance)
                {
                    return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money for fuel. Roadside services can be deferred; the basic rental is always free.");
                }
            }
            Spend(tx, LedgerEntryType.Fuel, cost, v.Id, "DTC.Ledger.Fuel", "Fuel " + CareerMath.Inv(liters, "0.0") + " L");
            v.AddFuel(liters);
            return VehicleOk(CareerResult.Success(), v, -cost, tx);
        }

        /// <summary>
        /// Buys condition points at a repair service point (exterior access point, no parts; REQ-GARAGE-004). Partial
        /// repair when funds are short. Basic rental and service vehicles are repaired free. MR-&lt;OperationId&gt;.
        /// </summary>
        public CareerResult Repair(string vehicleId, string servicePointId, double targetCondition, string operationId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            CareerResult bad = CheckServiceable(v, servicePointId, out bool ok);
            if (!ok)
            {
                return bad;
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tx = "MR-" + op;
            if (m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This repair was already paid.");
            }
            double target = CareerMath.IsFinite(targetCondition) ? CareerMath.Clamp(targetCondition, 0, 100) : 100;
            double points = Math.Max(0, target - v.Condition);
            if (points <= 1e-6)
            {
                return VehicleOk(CareerResult.Success(), v, 0, null);
            }
            if (v.FuelIncluded)
            {
                v.AddCondition(points);
                return VehicleOk(CareerResult.Success(), v, 0, null);
            }
            long rate = v.Model.MaintenancePerPointCents;
            long cost = (long)Math.Ceiling(points * rate - 1e-9);
            if (cost > m_State.Wallet.Balance)
            {
                points = Math.Floor((double)m_State.Wallet.Balance / rate * 10) / 10;
                cost = (long)Math.Ceiling(points * rate - 1e-9);
                if (points <= 0 || cost > m_State.Wallet.Balance)
                {
                    return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money for repairs. Roadside services can be deferred; the basic rental is always free.");
                }
            }
            Spend(tx, LedgerEntryType.Maintenance, cost, v.Id, "DTC.Ledger.Maintenance", "Repair " + CareerMath.Inv(points, "0.0") + " points");
            v.AddCondition(points);
            return VehicleOk(CareerResult.Success(), v, -cost, tx);
        }

        /// <summary>Installs an upgrade on an owned vehicle (one per slot; never required by any content). UP-&lt;OperationId&gt;.</summary>
        public CareerResult Upgrade(string vehicleId, UpgradeKind upgrade, string servicePointId, string operationId, double nowSeconds)
        {
            Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            CareerResult bad = CheckServiceable(v, servicePointId, out bool ok);
            if (!ok)
            {
                return bad;
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tx = "UP-" + op;
            if (m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This upgrade was already paid.");
            }
            if (v.Kind != VehicleKind.Owned || v.Holder != VehicleHolder.Player)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Garage.UpgradeOwnedOnly", "Only vehicles you own can be upgraded.");
            }
            UpgradeInfo info = CareerCatalog.FindUpgrade(upgrade);
            if (info == null || Array.IndexOf(info.Classes, v.Class) < 0)
            {
                return Fail(CareerResultCode.WrongVehicleClass, "DTC.Garage.UpgradeClass", "This upgrade does not fit this vehicle.");
            }
            foreach (UpgradeKind u in v.Upgrades)
            {
                UpgradeInfo other = CareerCatalog.FindUpgrade(u);
                if (other != null && other.Slot == info.Slot)
                {
                    return Fail(CareerResultCode.InvalidState, "DTC.Garage.SlotUsed", "This slot already has an upgrade.");
                }
            }
            if (m_State.DeferredCents > 0)
            {
                return Fail(CareerResultCode.DeferredChargesOpen, "DTC.Garage.DeferredOpen", "Repay your deferred charges before buying upgrades.");
            }
            if (info.PriceCents > m_State.Wallet.Balance)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money: this upgrade costs " + CareerMath.Money(info.PriceCents) + ".");
            }
            Spend(tx, LedgerEntryType.Upgrade, info.PriceCents, v.Id, "DTC.Ledger.Upgrade", "Upgrade: " + info.Name);
            v.Upgrades.Add(upgrade);
            return VehicleOk(CareerResult.Success(), v, -info.PriceCents, tx);
        }

        /// <summary>
        /// Roadside recovery for an immobilized or stranded vehicle (SPEC-GARAGE section 9). Never refused for lack of
        /// money: unpaid amounts become deferred charges. Free for basic rental and service vehicles. RS-&lt;OperationId&gt;.
        /// </summary>
        public CareerResult RequestRoadsideService(string vehicleId, RoadsideService service, string operationId, double nowSeconds, double towDistanceMeters = 0)
        {
            double now = Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.IsRemoved)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Garage.VehicleNotFound", "That vehicle is not available.");
            }
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tx = "RS-" + op;
            if (m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This service was already provided.");
            }
            VehicleModel model = v.Model;
            long fuelPrice = model != null ? model.FuelPricePerLiterCents : 0;
            long rate = model != null ? model.MaintenancePerPointCents : 0;
            long cost;
            string text;
            switch (service)
            {
                case RoadsideService.Fuel:
                {
                    double liters = Math.Max(0, CareerBalance.RoadsideFuelFraction * v.TankLiters - v.FuelLiters);
                    cost = CareerBalance.RoadsideFuelFeeCents + CareerMath.RoundCents(liters * fuelPrice);
                    text = "Roadside fuel";
                    v.AddFuel(liters);
                    break;
                }
                case RoadsideService.QuickFix:
                {
                    double points = Math.Max(0, CareerBalance.RoadsideQuickFixCondition - v.Condition);
                    cost = CareerBalance.RoadsideQuickFixFeeCents + CareerMath.RoundCents(points * rate);
                    text = "Roadside quick fix";
                    v.AddCondition(points);
                    break;
                }
                case RoadsideService.Tow:
                {
                    double km = CareerMath.IsFinite(towDistanceMeters) ? Math.Max(0, towDistanceMeters) / 1000.0 : 0;
                    cost = CareerBalance.TowBaseCents + CareerMath.RoundCents(CareerBalance.TowPerKmCents * km);
                    text = "Towing";
                    if (v.IsImmobilized)
                    {
                        // Towed to a service point: it stays immobilized there until refuelled or repaired.
                        v.HasPosition = false;
                    }
                    break;
                }
                case RoadsideService.MobileRefuel:
                {
                    double liters = Math.Max(0, v.TankLiters - v.FuelLiters);
                    cost = CareerBalance.MobileServiceFeeCents + CareerMath.RoundCents(liters * fuelPrice);
                    text = "Mobile fuel service";
                    v.AddFuel(liters);
                    break;
                }
                default:
                {
                    double points = Math.Max(0, 100 - v.Condition);
                    cost = CareerBalance.MobileServiceFeeCents + CareerMath.RoundCents(points * rate);
                    text = "Mobile repair service";
                    v.AddCondition(points);
                    break;
                }
            }
            if (v.FuelIncluded)
            {
                cost = 0;
            }
            ChargeMandatory(tx, LedgerEntryType.RecoveryService, cost, v.Id, "DTC.Ledger.RecoveryService", text, out long paid, out long deferred);
            CareerResult r = CareerResult.Success(v.Id);
            r.AmountCents = -paid;
            r.DeferredCents = deferred;
            r.TransactionId = tx;
            return Done(r);
        }

        // ------------------------------------------------------------------ storage

        /// <summary>Garage capacity expansion 2 -> 4 -> 6 (SPEC-HOME 3.3). GE-&lt;OperationId&gt;.</summary>
        public CareerResult ExpandGarage(string operationId, double nowSeconds)
        {
            Enter(nowSeconds);
            string op = string.IsNullOrEmpty(operationId) ? NewOperationIdInternal() : operationId;
            string tx = "GE-" + op;
            if (m_State.Wallet.Contains(tx))
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Result.AlreadyApplied", "This expansion was already paid.");
            }
            int level = m_State.Home.GarageLevel;
            if (level >= CareerBalance.GarageExpansionCents.Length)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.MaxCapacity", "The garage is already at maximum capacity.");
            }
            if (m_State.DeferredCents > 0)
            {
                return Fail(CareerResultCode.DeferredChargesOpen, "DTC.Garage.DeferredOpen", "Repay your deferred charges first.");
            }
            long price = CareerBalance.GarageExpansionCents[level];
            if (Spend(tx, LedgerEntryType.GarageExpansion, price, "garage", "DTC.Ledger.GarageExpansion", "Garage expansion") != LedgerAppendStatus.Appended)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "Not enough money: the expansion costs " + CareerMath.Money(price) + ".");
            }
            m_State.Home.GarageLevel++;
            CareerResult r = CareerResult.Success();
            r.AmountCents = -price;
            r.TransactionId = tx;
            return Done(r);
        }

        /// <summary>Stores a personal vehicle (no world entity while stored), within garage capacity.</summary>
        public CareerResult StoreVehicle(string vehicleId, double nowSeconds)
        {
            Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.IsRemoved || v.Kind != VehicleKind.Owned)
            {
                return Fail(CareerResultCode.NotOwned, "DTC.Garage.NotOwned", "Only owned vehicles can be stored.");
            }
            if (v.State == VehicleLocationState.Stored)
            {
                return Done(CareerResult.Success(v.Id));
            }
            if (IsInOpenMission(v) || v.State == VehicleLocationState.InUse)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.InUse", "Leave the vehicle and finish the job first.");
            }
            if (v.Holder == VehicleHolder.Player && StoredCount(VehicleHolder.Player) >= m_State.Home.GarageCapacity)
            {
                return Fail(CareerResultCode.CapacityExceeded, "DTC.Garage.Full", "The garage is full. Expand it or keep the vehicle parked.");
            }
            v.State = VehicleLocationState.Stored;
            v.StoredAtPlaceId = m_State.Home.Garage?.Id;
            return Done(CareerResult.Success(v.Id));
        }

        private int StoredCount(VehicleHolder holder)
        {
            int stored = 0;
            foreach (CareerVehicle o in m_State.Vehicles)
            {
                if (!o.IsRemoved && o.Kind == VehicleKind.Owned && o.Holder == holder && o.State == VehicleLocationState.Stored)
                {
                    stored++;
                }
            }
            return stored;
        }

        /// <summary>Retrieves a stored vehicle: it becomes Parked at the garage (or default storage) access point.</summary>
        public CareerResult RetrieveVehicle(string vehicleId, double nowSeconds)
        {
            Enter(nowSeconds);
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            if (v == null || v.IsRemoved || v.State != VehicleLocationState.Stored)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.NotStored", "This vehicle is not in storage.");
            }
            if (v.Assignment == FleetAssignment.Managed)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Garage.Managed", "This vehicle is on a managed contract (simulated).");
            }
            v.State = VehicleLocationState.Parked;
            CareerResult r = CareerResult.Success(v.Id);
            r.PlaceId = v.StoredAtPlaceId;
            if (m_State.Home.Garage != null)
            {
                r.Point = m_State.Home.Garage.Access;
                r.HasPoint = true;
            }
            return Done(r);
        }

        // ------------------------------------------------------------------ queries

        /// <summary>Performance effects for the mod's vehicle model (condition, fuel, upgrades).</summary>
        public VehiclePerformance GetPerformance(string vehicleId)
        {
            CareerVehicle v = m_State.FindVehicle(vehicleId);
            return v != null ? v.GetPerformance() : VehiclePerformance.Nominal;
        }

        /// <summary>
        /// The "Get back to work" panel (SPEC-ECON section 7): the basic rental and a starter job are always listed at
        /// zero cost, so a zero-credit path to paid work always exists (REQ-ECON-004, REQ-GARAGE-006).
        /// </summary>
        public RecoveryView GetRecoveryOptions(double nowSeconds)
        {
            var view = new RecoveryView { BalanceCents = m_State.Wallet.Balance, DeferredCents = m_State.DeferredCents };
            long cheapestNeeded = long.MaxValue;
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (v.IsRemoved || v.Kind == VehicleKind.Service || v.Assignment == FleetAssignment.Managed || v.Holder != VehicleHolder.Player)
                {
                    continue;
                }
                if (!v.IsImmobilized)
                {
                    view.HasUsableVehicle = true;
                    continue;
                }
                if (v.Kind == VehicleKind.BasicRental)
                {
                    continue;
                }
                VehicleModel model = v.Model;
                if (v.Immobilized == ImmobilizedReason.OutOfFuel)
                {
                    long c = CareerBalance.RoadsideFuelFeeCents + CareerMath.RoundCents(CareerBalance.RoadsideFuelFraction * v.TankLiters * (model?.FuelPricePerLiterCents ?? 0));
                    view.Options.Add(new RecoveryOption { Kind = RecoveryOptionKind.RoadsideFuel, VehicleId = v.Id, CostCents = c, CanDefer = true, Key = "DTC.Recovery.RoadsideFuel", Text = "Roadside fuel" });
                    cheapestNeeded = Math.Min(cheapestNeeded, c);
                }
                else
                {
                    long c = CareerBalance.RoadsideQuickFixFeeCents + CareerMath.RoundCents(CareerBalance.RoadsideQuickFixCondition * (model?.MaintenancePerPointCents ?? 0));
                    view.Options.Add(new RecoveryOption { Kind = RecoveryOptionKind.RoadsideQuickFix, VehicleId = v.Id, CostCents = c, CanDefer = true, Key = "DTC.Recovery.QuickFix", Text = "Roadside quick fix" });
                    cheapestNeeded = Math.Min(cheapestNeeded, c);
                }
                view.Options.Add(new RecoveryOption { Kind = RecoveryOptionKind.Tow, VehicleId = v.Id, CostCents = CareerBalance.TowBaseCents, CanDefer = true, Key = "DTC.Recovery.Tow", Text = "Tow to the nearest service point" });
                if (v.Kind == VehicleKind.Owned)
                {
                    view.Options.Add(new RecoveryOption { Kind = RecoveryOptionKind.SellVehicle, VehicleId = v.Id, CostCents = -v.SaleValueCents(), Key = "DTC.Recovery.Sell", Text = "Sell the vehicle (optional)" });
                }
                else if (v.Kind == VehicleKind.PremiumRental)
                {
                    view.Options.Add(new RecoveryOption { Kind = RecoveryOptionKind.ReturnRental, VehicleId = v.Id, CostCents = CareerBalance.TowBaseCents, CanDefer = true, Key = "DTC.Recovery.ReturnRental", Text = "Return the rental remotely" });
                }
            }
            view.Insolvent = (!view.HasUsableVehicle && m_State.Wallet.Balance <= 0) || (!view.HasUsableVehicle && cheapestNeeded != long.MaxValue && cheapestNeeded > m_State.Wallet.Balance);
            view.Options.Insert(0, new RecoveryOption { Kind = RecoveryOptionKind.TakeBasicRental, CostCents = 0, Key = "DTC.Recovery.BasicRental", Text = "Take the basic rental (free; 25% revenue share)" });
            view.Options.Insert(1, new RecoveryOption { Kind = RecoveryOptionKind.StarterJob, CostCents = 0, Key = "DTC.Recovery.StarterJob", Text = "Accept a starter taxi or courier job" });
            return view;
        }
    }
}
