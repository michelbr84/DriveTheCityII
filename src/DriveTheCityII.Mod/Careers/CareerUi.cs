using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Persistence;
using DriveTheCityII.Core.Text;
using DriveTheCityII.Diagnostics;
using DriveTheCityII.Player;
using DriveTheCityII.UI;
using Unity.Mathematics;

namespace DriveTheCityII.Careers
{
    /// <summary>Career text: PT-BR translation of a career key when available, otherwise the career's English text.</summary>
    public static class CareerText
    {
        public static string Of(string key, string english, string[] args = null)
        {
            if (Loc.Locale == Loc.Portuguese && !string.IsNullOrEmpty(key) && Loc.PortugueseTable.ContainsKey(key))
            {
                return Loc.T(key, args ?? Array.Empty<object>());
            }
            return string.IsNullOrEmpty(english) ? (key ?? string.Empty) : english;
        }

        public static string Result(CareerResult r) => r == null ? string.Empty : Of(r.Key, r.Text);

        public static string CareerName(CareerKind k)
        {
            switch (k)
            {
                case CareerKind.Taxi: return Loc.T("career.taxi");
                case CareerKind.Delivery: return Loc.T("career.delivery");
                case CareerKind.Freight: return Loc.T("career.freight");
                default: return Loc.T("career.bus");
            }
        }

        public static string ClassName(VehicleClass c) => Loc.T("class." + c.ToString().ToLowerInvariant());

        public static string LegName(LegKind k) => Loc.T("leg." + k.ToString().ToLowerInvariant());
    }

    /// <summary>Registers the career tabs and the HUD contributor.</summary>
    public static class CareerUi
    {
        public static void Register(CareerSystem c)
        {
            HudUISystem hud = HudUISystem.Instance;
            hud.RegisterTab(new JobsTab(c));
            hud.RegisterTab(new GarageTab(c));
            hud.RegisterTab(new CompanyTab(c));
            hud.RegisterTab(new ChallengesTab(c));
            hud.RegisterTab(new ProfileTab(c));
            hud.RegisterContributor(new CareerHud(c));
            Bridge.TestBridge.Handlers["career"] = CareerBridge.Handle;
        }

        internal static float3 PlayerPos()
        {
            PlayerSystem p = PlayerSystem.Instance;
            return p.State == PlayerState.Driving && p.Vehicle != null ? p.Vehicle.State.Position.ToF3() : p.Avatar.Position.ToF3();
        }

        /// <summary>
        /// The vehicle a job is accepted with: the career vehicle being driven, unless the job provides its own vehicle
        /// (shipper's truck, operator's bus) and the driven one is not of an allowed class. Then the job's vehicle is used.
        /// </summary>
        internal static string VehicleForAccept(CareerService s, MissionOffer offer)
        {
            PlayerSystem p = PlayerSystem.Instance;
            if (p == null || p.State != PlayerState.Driving || p.Vehicle == null || p.Vehicle.Kind != ActorKind.CareerVehicle)
            {
                return null;
            }
            string vid = p.Vehicle.CareerVehicleId;
            if (offer != null && offer.ProvidesServiceVehicle)
            {
                VehicleView v = s.GetVehicle(vid);
                if (v == null || !offer.AllowedClasses.Contains(v.Class))
                {
                    return null;
                }
            }
            return vid;
        }

        internal static void Report(CareerResult r)
        {
            PlayerSystem.Instance?.Say(CareerText.Result(r));
            DtcLog.Info($"Career action result: {r?.Code} {r?.Key} {r?.Text}");
        }
    }

    internal sealed class CareerHud : IHudContributor
    {
        private readonly CareerSystem m_C;

        public CareerHud(CareerSystem c)
        {
            m_C = c;
        }

        public void Contribute(JsonValue hud)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                if (m_C.Disabled)
                {
                    hud.Set("money", Loc.T("career.disabledShort"));
                }
                return;
            }
            WalletView w = s.GetWallet(0);
            hud.Set("money", Loc.Money(w.BalanceCents));
            ProgressView pr = s.GetProgress();
            hud.Set("level", Loc.T("hud.level", pr.TotalLevel));
            PlayerSystem p = PlayerSystem.Instance;
            if (p.Vehicle != null && p.Vehicle.Kind == ActorKind.CareerVehicle && !string.IsNullOrEmpty(p.Vehicle.CareerVehicleId))
            {
                VehicleView v = s.GetVehicle(p.Vehicle.CareerVehicleId);
                if (v != null)
                {
                    hud.Set("fuel", v.FuelPercent / 100.0);
                    hud.Set("condition", v.ConditionPercent / 100.0);
                    hud.Set("fuelLabel", Loc.T("hud.fuel"));
                    hud.Set("conditionLabel", Loc.T("hud.condition"));
                    hud.Set("vehicleName", v.Name);
                }
            }
            ActiveMissionView m = s.GetActiveMission();
            if (m != null && !string.IsNullOrEmpty(m.MissionId))
            {
                float3 target = CareerWorldAdapter.ToF3(m.TargetAccess);
                float dist = math.distance(CareerUi.PlayerPos().xz, target.xz);
                string detail;
                switch (m.Career)
                {
                    case CareerKind.Taxi:
                        detail = Loc.T("obj.taxiDetail", m.Passengers, Loc.T("pax." + m.PassengerState.ToString().ToLowerInvariant()));
                        break;
                    case CareerKind.Delivery:
                        detail = Loc.T("obj.deliveryDetail", m.ParcelsDelivered, m.ParcelsTotal);
                        break;
                    case CareerKind.Freight:
                        detail = Loc.T("obj.freightDetail", m.PayloadTonnes.ToString("0.0", CultureInfo.InvariantCulture));
                        break;
                    default:
                        detail = Loc.T("obj.busDetail", m.LineName ?? string.Empty, m.StopsServed, m.BusOnboard);
                        break;
                }
                if (m.CurrentLegState == LegState.Servicing)
                {
                    detail += " · " + Loc.T("obj.servicing", (int)(m.ServicingProgress01 * 100));
                }
                if (m.HardDeadlineRemaining > 0)
                {
                    detail += " · " + Loc.T("obj.deadline", (int)m.HardDeadlineRemaining);
                }
                hud.Set("objective", JsonValue.Object()
                    .Set("label", CareerText.CareerName(m.Career))
                    .Set("title", CareerText.LegName(m.CurrentLegKind) + ": " + (m.TargetPlaceName ?? string.Empty))
                    .Set("detail", detail)
                    .Set("distance", Loc.Distance(dist))
                    .Set("reward", m.ContractValueCents > 0 ? Loc.Money(m.ContractValueCents) : string.Empty));
            }
            else
            {
                ChallengeView ch = m_C.RunningChallenge();
                if (ch != null && ch.Points.Count > 0)
                {
                    int next = Math.Min(Math.Max(0, ch.NextPoint), ch.Points.Count - 1);
                    float dist = math.distance(CareerUi.PlayerPos().xz, CareerWorldAdapter.ToF3(ch.Points[next].Access).xz);
                    string time = ch.RunStarted
                        ? Loc.T("challenges.time", (ch.RunElapsedSeconds + ch.RunPenaltySeconds).ToString("0.0", CultureInfo.InvariantCulture))
                        : Loc.T("challenges.toStart");
                    hud.Set("objective", JsonValue.Object()
                        .Set("label", Loc.T("challenge." + ch.Type.ToString().ToLowerInvariant()))
                        .Set("title", ch.Name)
                        .Set("detail", Loc.T("challenges.checkpoint", next + 1, ch.Points.Count) + " · " + time)
                        .Set("distance", Loc.Distance(dist))
                        .Set("reward", ch.BestSeconds > 0 ? Loc.T("challenges.best", ch.BestSeconds.ToString("0.0", CultureInfo.InvariantCulture), Loc.T("medal." + ch.Medal.ToString().ToLowerInvariant())) : string.Empty));
                }
            }
            CityPlace sp = m_C.NearestServicePoint(30f);
            if (sp != null)
            {
                JsonValue prompts = hud["prompts"];
                if (prompts.Kind == JsonKind.Array)
                {
                    prompts.Add(JsonValue.From(Loc.T("prompt.servicePoint", HudUISystem.KeyName(p, CityBridge.DtcAction.CareerPanel), sp.Name)));
                }
            }
        }
    }

    // ===================================================================== Jobs

    internal sealed class JobsTab : IPanelTab
    {
        private readonly CareerSystem m_C;
        private CareerKind m_Career = CareerKind.Taxi;

        // The panel is rebuilt at the HUD rate (10 Hz). Offers are fetched at most every 2 s (immediately after a career
        // switch, refresh or accept), so an empty board never regenerates offers on every rebuild.
        private IReadOnlyList<MissionOffer> m_Offers = Array.Empty<MissionOffer>();
        private float m_NextFetch;

        public JobsTab(CareerSystem c)
        {
            m_C = c;
        }

        public string Id => "jobs";
        public string Label => Loc.T("tab.jobs");
        public int Order => 10;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            header = Loc.T("tab.jobs");
            footer = string.Empty;
            CareerService s = m_C.Service;
            if (s == null)
            {
                rows.Add(new PanelRow { Id = "off", Title = Loc.T("career.disabled"), Subtitle = m_C.DisabledReason });
                return;
            }
            var careers = new PanelRow { Id = "careers", Title = Loc.T("jobs.chooseCareer") };
            foreach (CareerKind k in new[] { CareerKind.Taxi, CareerKind.Delivery, CareerKind.Freight, CareerKind.Bus })
            {
                careers.Action("jobs:career:" + k, (k == m_Career ? "● " : string.Empty) + CareerText.CareerName(k));
            }
            rows.Add(careers);

            ActiveMissionView m = s.GetActiveMission();
            if (m != null && !string.IsNullOrEmpty(m.MissionId))
            {
                rows.Add(new PanelRow
                {
                    Id = "active",
                    Highlight = true,
                    Title = Loc.T("jobs.active", CareerText.CareerName(m.Career)),
                    Subtitle = CareerText.LegName(m.CurrentLegKind) + ": " + m.TargetPlaceName,
                    Right = m.ContractValueCents > 0 ? Loc.Money(m.ContractValueCents) : string.Empty,
                }.Action("jobs:cancel", Loc.T("jobs.cancel")));
                footer = Loc.T("jobs.oneAtATime");
                return;
            }
            if (m_Career == CareerKind.Taxi)
            {
                rows.Add(new PanelRow { Id = "duty", Title = Loc.T("jobs.taxiDuty"), Subtitle = Loc.T("jobs.taxiDutyHint") }
                    .Action("jobs:duty:on", Loc.T("common.on")).Action("jobs:duty:off", Loc.T("common.off")));
            }
            float nowReal = UnityEngine.Time.realtimeSinceStartup;
            if (nowReal >= m_NextFetch)
            {
                m_NextFetch = nowReal + 2f;
                try
                {
                    m_Offers = s.GetOffers(m_Career, CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), m_C.Clock);
                }
                catch (Exception ex)
                {
                    DtcLog.Error("GetOffers failed", ex);
                    m_Offers = Array.Empty<MissionOffer>();
                }
            }
            IReadOnlyList<MissionOffer> offers = m_Offers;
            if (offers.Count == 0)
            {
                rows.Add(new PanelRow { Id = "none", Title = Loc.T("jobs.none"), Subtitle = Loc.T("jobs.noneHint") });
            }
            foreach (MissionOffer o in offers.Take(8))
            {
                string sub = Loc.Distance(o.PlannedMeters) + " · ~" + ((int)(o.PlannedSeconds / 60) + 1).ToString(CultureInfo.InvariantCulture) + " min";
                if (o.Passengers > 0)
                {
                    sub += " · " + Loc.T("jobs.passengers", o.Passengers);
                }
                if (o.ParcelCount > 0)
                {
                    sub += " · " + Loc.T("jobs.parcels", o.ParcelCount);
                }
                if (o.MassTonnes > 0)
                {
                    sub += " · " + o.MassTonnes.ToString("0.0", CultureInfo.InvariantCulture) + " t";
                }
                if (!string.IsNullOrEmpty(o.LineName))
                {
                    sub += " · " + o.LineName + " (" + o.StopCount.ToString(CultureInfo.InvariantCulture) + ")";
                }
                if (o.ProvidesServiceVehicle)
                {
                    sub += " · " + Loc.T("jobs.vehicleProvided");
                }
                if (!o.Eligible)
                {
                    sub += " · " + CareerText.Of(o.IneligibleKey, o.IneligibleText);
                }
                string pay = o.PayMinCents == o.PayMaxCents ? Loc.Money(o.PayMinCents) : Loc.Money(o.PayMinCents) + "–" + Loc.Money(o.PayMaxCents);
                rows.Add(new PanelRow { Id = o.Id, Title = CareerText.Of(o.TitleKey, o.Title), Subtitle = sub, Right = pay + " · +" + o.XpPreview + " XP" }
                    .Action("jobs:accept:" + o.Id, Loc.T("jobs.accept"), o.Eligible));
            }
            rows.Add(new PanelRow { Id = "refresh", Title = Loc.T("jobs.refreshHint") }.Action("jobs:refresh", Loc.T("jobs.refresh")));
        }

        public bool HandleAction(string id)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                return true;
            }
            string[] a = id.Split(':');
            PlayerSystem p = PlayerSystem.Instance;
            switch (a[1])
            {
                case "career":
                    m_Career = (CareerKind)Enum.Parse(typeof(CareerKind), a[2]);
                    m_NextFetch = 0f;
                    return true;
                case "refresh":
                    m_Offers = s.RefreshOffers(m_Career, CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), m_C.Clock);
                    m_NextFetch = UnityEngine.Time.realtimeSinceStartup + 2f;
                    return true;
                case "duty":
                    s.SetTaxiDuty(a[2] == "on");
                    return true;
                case "cancel":
                    CareerUi.Report(s.Cancel("player", m_C.Clock));
                    return true;
                case "accept":
                {
                    string offerId = string.Join(":", a.Skip(2));
                    MissionOffer offer = m_Offers.FirstOrDefault(o => o.Id == offerId);
                    string vehicleId = CareerUi.VehicleForAccept(s, offer);
                    CareerResult r = s.Accept(offerId, vehicleId, m_C.Clock);
                    CareerUi.Report(r);
                    m_NextFetch = 0f;
                    if (r.IsOk)
                    {
                        HudUISystem.Instance.SetPanel(false);
                    }
                    return true;
                }
            }
            return false;
        }
    }

    // ===================================================================== Garage

    internal sealed class GarageTab : IPanelTab
    {
        private readonly CareerSystem m_C;

        public GarageTab(CareerSystem c)
        {
            m_C = c;
        }

        public string Id => "garage";
        public string Label => Loc.T("tab.garage");
        public int Order => 20;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            CareerService s = m_C.Service;
            CityPlace sp = m_C.NearestServicePoint(30f);
            header = sp != null ? Loc.T("garage.at", sp.Name) : Loc.T("tab.garage");
            footer = sp == null ? Loc.T("garage.goToServicePoint") : Loc.T("garage.noFuelStations");
            if (s == null)
            {
                return;
            }
            float3 me = CareerUi.PlayerPos();
            foreach (VehicleView v in s.GetVehicles())
            {
                if (v.Holder != VehicleHolder.Player || v.State == VehicleLocationState.Removed)
                {
                    continue;
                }
                ControlledVehicle world = m_C.FindWorldVehicle(v.Id);
                bool nearVehicle = world != null && math.distance(world.State.Position.ToF3(), me) < 40f;
                var row = new PanelRow
                {
                    Id = v.Id,
                    Title = v.Name + " · " + CareerText.ClassName(v.Class),
                    Subtitle = Loc.T("garage.status", (int)v.FuelPercent, (int)v.ConditionPercent, v.OdometerKm.ToString("0", CultureInfo.InvariantCulture)) +
                               (v.Immobilized != ImmobilizedReason.None ? " · " + Loc.T("garage.immobilized." + v.Immobilized.ToString().ToLowerInvariant()) : string.Empty),
                    Right = Loc.T("vkind." + v.Kind.ToString().ToLowerInvariant()),
                    Highlight = PlayerSystem.Instance.Vehicle != null && PlayerSystem.Instance.Vehicle.CareerVehicleId == v.Id,
                };
                if (world == null)
                {
                    row.Action("garage:call:" + v.Id, Loc.T("garage.call"));
                }
                row.Action("garage:refuel:" + v.Id, Loc.T("garage.refuel"), sp != null && nearVehicle);
                row.Action("garage:repair:" + v.Id, Loc.T("garage.repair"), sp != null && nearVehicle);
                if (v.Kind == VehicleKind.BasicRental || v.Kind == VehicleKind.PremiumRental)
                {
                    row.Action("garage:return:" + v.Id, Loc.T("garage.return"), sp != null);
                }
                if (v.Kind == VehicleKind.Owned)
                {
                    row.Action("garage:sell:" + v.Id, Loc.T("garage.sell") + " " + Loc.Money(v.SaleValueCents), sp != null);
                }
                rows.Add(row);
            }
            rows.Add(new PanelRow { Id = "basic", Title = Loc.T("garage.basicRental"), Subtitle = Loc.T("garage.basicRentalHint") }
                .Action("garage:rentbasic", Loc.T("garage.rent")));
            if (sp != null)
            {
                foreach (VehicleModel model in CareerCatalog.Models)
                {
                    if (model.Acquisition == VehicleAcquisition.PremiumRental)
                    {
                        rows.Add(new PanelRow { Id = model.Id, Title = model.Name, Subtitle = Loc.T("garage.rentDetail", Loc.Money(model.RentalShiftFeeCents), Loc.Money(model.RentalDepositCents)), Right = CareerText.ClassName(model.Class) }
                            .Action("garage:rent:" + model.Id, Loc.T("garage.rent")));
                    }
                    else if (model.Acquisition == VehicleAcquisition.Purchase)
                    {
                        rows.Add(new PanelRow { Id = model.Id, Title = model.Name, Subtitle = Loc.T("garage.buyDetail", Loc.Money(model.PriceCents)), Right = CareerText.ClassName(model.Class) }
                            .Action("garage:buy:" + model.Id, Loc.T("garage.buy")));
                    }
                }
            }
            RecoveryView rec = s.GetRecoveryOptions(m_C.Clock);
            if (rec != null)
            {
                foreach (RecoveryOption o in rec.Options)
                {
                    rows.Add(new PanelRow { Id = "rec:" + o.Kind, Title = CareerText.Of(o.Key, o.Text), Right = o.CostCents > 0 ? Loc.Money(o.CostCents) : Loc.T("common.free") }
                        .Action("garage:recover:" + o.Kind + ":" + (o.VehicleId ?? string.Empty), Loc.T("common.choose")));
                }
            }
        }

        public bool HandleAction(string id)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                return true;
            }
            string[] a = id.Split(':');
            string arg = a.Length > 2 ? a[2] : null;
            CityPlace sp = m_C.NearestServicePoint(30f);
            double now = m_C.Clock;
            CareerResult r = null;
            switch (a[1])
            {
                case "call":
                {
                    VehicleView v = s.GetVehicle(arg);
                    if (v != null)
                    {
                        if (v.HasPosition && math.distance(CareerWorldAdapter.ToF3(v.LastPosition), CareerUi.PlayerPos()) > 500f)
                        {
                            r = s.RequestRoadsideService(arg, RoadsideService.Tow, UiOps.IdFor(s, id), now, math.distance(CareerWorldAdapter.ToF3(v.LastPosition), CareerUi.PlayerPos()));
                            if (!r.IsOk)
                            {
                                break;
                            }
                        }
                        if (!m_C.SpawnCareerVehicleNear(v, CareerUi.PlayerPos(), out string err))
                        {
                            PlayerSystem.Instance.Say(err);
                            return true;
                        }
                        PlayerSystem.Instance.Say(Loc.T("garage.called", v.Name));
                        return true;
                    }
                    break;
                }
                case "refuel":
                    r = s.Refuel(arg, sp?.Id, 1.0, UiOps.IdFor(s, id), now);
                    break;
                case "repair":
                    r = s.Repair(arg, sp?.Id, 100.0, UiOps.IdFor(s, id), now);
                    break;
                case "return":
                    m_C.RemoveWorldVehicle(arg);
                    r = s.ReturnRental(arg, sp != null, UiOps.IdFor(s, id), now);
                    break;
                case "sell":
                    m_C.RemoveWorldVehicle(arg);
                    r = s.SellVehicle(arg, now);
                    break;
                case "rentbasic":
                    r = s.RentBasic(now);
                    SpawnIfOk(r);
                    break;
                case "rent":
                    r = s.RentVehicle(arg, UiOps.IdFor(s, id), now);
                    SpawnIfOk(r);
                    break;
                case "buy":
                    r = s.BuyVehicle(arg, UiOps.IdFor(s, id), now);
                    SpawnIfOk(r);
                    break;
                case "recover":
                {
                    var kind = (RecoveryOptionKind)Enum.Parse(typeof(RecoveryOptionKind), arg);
                    string vid = a.Length > 3 && a[3].Length > 0 ? a[3] : null;
                    r = Recover(s, kind, vid, now, id);
                    break;
                }
            }
            if (r != null)
            {
                CareerUi.Report(r);
            }
            return true;
        }

        private static CareerResult Recover(CareerService s, RecoveryOptionKind kind, string vid, double now, string opKey)
        {
            switch (kind)
            {
                case RecoveryOptionKind.TakeBasicRental:
                {
                    CareerResult r = s.RentBasic(now);
                    SpawnIfOk(r);
                    return r;
                }
                case RecoveryOptionKind.RoadsideFuel: return s.RequestRoadsideService(vid, RoadsideService.Fuel, UiOps.IdFor(s, opKey), now);
                case RecoveryOptionKind.RoadsideQuickFix: return s.RequestRoadsideService(vid, RoadsideService.QuickFix, UiOps.IdFor(s, opKey), now);
                case RecoveryOptionKind.Tow: return s.RequestRoadsideService(vid, RoadsideService.Tow, UiOps.IdFor(s, opKey), now, 1000);
                case RecoveryOptionKind.MobileRefuel: return s.RequestRoadsideService(vid, RoadsideService.MobileRefuel, UiOps.IdFor(s, opKey), now);
                case RecoveryOptionKind.MobileRepair: return s.RequestRoadsideService(vid, RoadsideService.MobileRepair, UiOps.IdFor(s, opKey), now);
                case RecoveryOptionKind.SellVehicle: return s.SellVehicle(vid, now);
                case RecoveryOptionKind.ReturnRental: return s.ReturnRental(vid, false, UiOps.IdFor(s, opKey), now);
                default: return CareerResult.Fail(CareerResultCode.InvalidArgument, "DTC.Recovery.Unsupported", "That option is not available here.");
            }
        }

        private static void SpawnIfOk(CareerResult r)
        {
            if (r == null || !r.IsOk || string.IsNullOrEmpty(r.Id))
            {
                return;
            }
            CareerSystem c = CareerSystem.Instance;
            VehicleView v = c.Service.GetVehicle(r.Id);
            if (v == null)
            {
                return;
            }
            CityPlace sp = c.NearestServicePoint(60f);
            float3 near = sp != null ? sp.RoadAccess : CareerUi.PlayerPos();
            if (!c.SpawnCareerVehicleNear(v, near, out string err))
            {
                PlayerSystem.Instance.Say(err);
            }
        }
    }

    // ===================================================================== Company

    internal sealed class CompanyTab : IPanelTab
    {
        private readonly CareerSystem m_C;

        public CompanyTab(CareerSystem c)
        {
            m_C = c;
        }

        public string Id => "company";
        public string Label => Loc.T("tab.company");
        public int Order => 30;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            header = Loc.T("tab.company");
            footer = Loc.T("company.managedNote");
            CareerService s = m_C.Service;
            if (s == null)
            {
                return;
            }
            CompanyView c = s.GetCompany(5);
            if (!c.Exists)
            {
                rows.Add(new PanelRow { Id = "found", Title = Loc.T("company.foundTitle"), Subtitle = CareerText.Of(c.ExplanationKey, c.Explanation) }
                    .Action("company:found", Loc.T("company.found")));
                return;
            }
            header = c.Name;
            rows.Add(new PanelRow { Id = "cash", Title = Loc.T("company.cash", Loc.Money(c.CashCents)), Subtitle = c.DebtCents > 0 ? Loc.T("company.debt", Loc.Money(c.DebtCents)) : CareerText.Of(c.HeadingKey, c.Heading) }
                .Action("company:deposit:100000", Loc.T("company.deposit", Loc.Money(100000)))
                .Action("company:withdraw:100000", Loc.T("company.withdraw", Loc.Money(100000))));
            foreach (EmployeeView e in c.Employees)
            {
                rows.Add(new PanelRow { Id = e.Id, Title = e.Name, Subtitle = Loc.T("company.employee", e.Skill, (int)(e.Reliability * 100), Loc.Money(e.SalaryCents)), Right = e.ContractId != null ? Loc.T("company.assigned") : Loc.T("company.idle") }
                    .Action("company:fire:" + e.Id, Loc.T("company.fire")));
            }
            foreach (EmployeeCandidate cand in s.GetCandidates(m_C.Clock).Take(3))
            {
                rows.Add(new PanelRow { Id = cand.Id, Title = Loc.T("company.candidate", cand.Name), Subtitle = Loc.T("company.employee", cand.Skill, (int)(cand.Reliability * 100), Loc.Money(cand.SalaryCents)) }
                    .Action("company:hire:" + cand.Id, Loc.T("company.hire")));
            }
            foreach (CompanyContractView k in c.Contracts)
            {
                rows.Add(new PanelRow { Id = k.Id, Title = Loc.T("company.contract", CareerText.CareerName(k.Career), k.QuotaPerPeriod), Subtitle = Loc.T("company.contractDetail", Loc.Money(k.PayPerJobCents), k.RemainingPeriods, k.SimulatedJobsDone), Right = CareerText.ClassName(k.RequiredClass) }
                    .Action("company:cancel:" + k.Id, Loc.T("jobs.cancel")));
            }
            foreach (CompanyContractOffer o in s.GetCompanyContractOffers(m_C.Clock).Take(3))
            {
                rows.Add(new PanelRow { Id = o.Id, Title = Loc.T("company.offer", CareerText.CareerName(o.Career), o.QuotaPerPeriod), Subtitle = Loc.T("company.contractDetail", Loc.Money(o.PayPerJobCents), o.DurationPeriods, 0), Right = CareerText.ClassName(o.RequiredClass) }
                    .Action("company:accept:" + o.Id, Loc.T("jobs.accept")));
            }
            foreach (VehicleModel m in CareerCatalog.Models.Where(m => m.Acquisition == VehicleAcquisition.CompanyPurchase || m.Acquisition == VehicleAcquisition.Purchase))
            {
                rows.Add(new PanelRow { Id = "buy:" + m.Id, Title = Loc.T("company.buyFleet", m.Name), Subtitle = Loc.Money(m.PriceCents), Right = CareerText.ClassName(m.Class) }
                    .Action("company:buy:" + m.Id, Loc.T("garage.buy")));
            }
            rows.Add(new PanelRow { Id = "auto", Title = Loc.T("company.autoAssign") }.Action("company:autoassign", Loc.T("common.apply"))
                .Action("company:automaint:on", Loc.T("company.autoMaintOn")).Action("company:automaint:off", Loc.T("company.autoMaintOff")));
        }

        public bool HandleAction(string id)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                return true;
            }
            string[] a = id.Split(':');
            string arg = a.Length > 2 ? a[2] : null;
            double now = m_C.Clock;
            CareerResult r = null;
            switch (a[1])
            {
                case "found":
                {
                    string hq = NearestBuildingPlaceId(80f);
                    r = s.FoundCompany(Loc.T("company.defaultName"), hq, UiOps.IdFor(s, id), now);
                    break;
                }
                case "deposit": r = s.TransferToCompany(long.Parse(arg, CultureInfo.InvariantCulture), UiOps.IdFor(s, id), now); break;
                case "withdraw": r = s.TransferToWallet(long.Parse(arg, CultureInfo.InvariantCulture), UiOps.IdFor(s, id), now); break;
                case "hire": r = s.Hire(arg, now); break;
                case "fire": r = s.Fire(arg, now); break;
                case "accept": r = s.AcceptCompanyContract(arg, now); break;
                case "cancel": r = s.CancelCompanyContract(arg, now); break;
                case "buy": r = s.CompanyBuyVehicle(arg, UiOps.IdFor(s, id), now); break;
                case "automaint": r = s.SetAutoMaintenance(arg == "on", now); break;
                case "autoassign": r = AutoAssign(s, now); break;
            }
            if (r != null)
            {
                CareerUi.Report(r);
            }
            return true;
        }

        /// <summary>Assigns idle employees to free fleet vehicles of the class each open contract needs.</summary>
        private static CareerResult AutoAssign(CareerService s, double now)
        {
            CompanyView c = s.GetCompany(0);
            var usedVehicles = new HashSet<string>(c.Employees.Where(e => e.VehicleId != null).Select(e => e.VehicleId));
            int assigned = 0;
            CareerResult last = CareerResult.Success();
            foreach (CompanyContractView k in c.Contracts)
            {
                foreach (EmployeeView e in c.Employees.Where(e => e.ContractId == null))
                {
                    string vid = c.FleetVehicleIds.FirstOrDefault(id => !usedVehicles.Contains(id) && s.GetVehicle(id)?.Class == k.RequiredClass);
                    if (vid == null)
                    {
                        break;
                    }
                    last = s.AssignEmployee(e.Id, vid, k.Id, now);
                    if (last.IsOk)
                    {
                        usedVehicles.Add(vid);
                        assigned++;
                    }
                }
            }
            return assigned > 0 ? CareerResult.Success() : last;
        }

        internal static string NearestBuildingPlaceId(float radius)
        {
            float3 me = CareerUi.PlayerPos();
            CityPlace best = null;
            float bestD = radius;
            foreach (CityPlace p in CityServices.Places.Places)
            {
                if (p.Kind == CityPlaceKind.TransitStop)
                {
                    continue;
                }
                float d = math.distance(p.BuildingPos.xz, me.xz);
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best?.Id;
        }
    }

    // ===================================================================== Challenges

    internal sealed class ChallengesTab : IPanelTab
    {
        private readonly CareerSystem m_C;

        public ChallengesTab(CareerSystem c)
        {
            m_C = c;
        }

        public string Id => "challenges";
        public string Label => Loc.T("tab.challenges");
        public int Order => 40;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            header = Loc.T("tab.challenges");
            footer = Loc.T("challenges.hint");
            CareerService s = m_C.Service;
            if (s == null)
            {
                return;
            }
            IReadOnlyList<ChallengeView> list = s.GetChallenges();
            if (list.Count == 0)
            {
                list = s.GenerateChallenges(CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), m_C.Clock);
            }
            foreach (ChallengeView c in list)
            {
                string best = c.BestSeconds > 0 ? Loc.T("challenges.best", c.BestSeconds.ToString("0.0", CultureInfo.InvariantCulture), Loc.T("medal." + c.Medal.ToString().ToLowerInvariant())) : Loc.T("challenges.noRecord");
                var row = new PanelRow { Id = c.Id, Title = c.Name, Subtitle = Loc.T("challenge." + c.Type.ToString().ToLowerInvariant()) + " · " + best, Right = CareerText.ClassName(c.Class), Highlight = c.Running };
                if (c.Running)
                {
                    row.Action("challenges:abort", Loc.T("jobs.cancel"));
                }
                else
                {
                    row.Action("challenges:start:" + c.Id, Loc.T("challenges.start"), c.Available);
                }
                rows.Add(row);
            }
            rows.Add(new PanelRow { Id = "regen", Title = Loc.T("challenges.newNearby") }.Action("challenges:regen", Loc.T("jobs.refresh")));
        }

        public bool HandleAction(string id)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                return true;
            }
            string[] a = id.Split(':');
            PlayerSystem p = PlayerSystem.Instance;
            switch (a[1])
            {
                case "start":
                {
                    string vid = p.Vehicle != null && p.Vehicle.Kind == ActorKind.CareerVehicle ? p.Vehicle.CareerVehicleId : null;
                    CareerResult r = s.StartChallenge(a[2], vid, m_C.Clock);
                    CareerUi.Report(r);
                    if (r.IsOk)
                    {
                        HudUISystem.Instance.SetPanel(false);
                    }
                    return true;
                }
                case "abort":
                    CareerUi.Report(s.AbortChallenge(m_C.Clock));
                    return true;
                case "regen":
                    s.GenerateChallenges(CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), m_C.Clock);
                    return true;
            }
            return false;
        }
    }

    // ===================================================================== Profile

    internal sealed class ProfileTab : IPanelTab
    {
        private readonly CareerSystem m_C;

        public ProfileTab(CareerSystem c)
        {
            m_C = c;
        }

        public string Id => "profile";
        public string Label => Loc.T("tab.profile");
        public int Order => 50;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            header = Loc.T("tab.profile");
            footer = string.Empty;
            CareerService s = m_C.Service;
            if (s == null)
            {
                rows.Add(new PanelRow { Id = "off", Title = Loc.T("career.disabled"), Subtitle = m_C.DisabledReason });
                return;
            }
            WalletView w = s.GetWallet(8);
            ProgressView pr = s.GetProgress();
            rows.Add(new PanelRow { Id = "wallet", Title = Loc.T("profile.wallet", Loc.Money(w.BalanceCents)), Subtitle = w.DeferredCents > 0 ? Loc.T("profile.deferred", Loc.Money(w.DeferredCents)) : Loc.T("profile.reputation", pr.Reputation), Highlight = true });
            foreach (CareerProgressView c in pr.Careers)
            {
                rows.Add(new PanelRow { Id = "xp:" + c.Career, Title = CareerText.CareerName(c.Career) + " · " + Loc.T("hud.level", c.Level), Subtitle = Loc.T("profile.xp", c.Xp, c.NextLevelXp), Right = ((int)(c.Progress01 * 100)).ToString(CultureInfo.InvariantCulture) + "%" });
            }
            HomeView home = s.GetHome();
            var homeRow = new PanelRow { Id = "home", Title = home.HasHome ? Loc.T("profile.home", home.HomeName) : Loc.T("profile.noHome"), Subtitle = home.HasGarage ? Loc.T("profile.garage", home.GaragePlaceId) : string.Empty };
            homeRow.Action("profile:sethome", Loc.T("profile.setHome")).Action("profile:setgarage", Loc.T("profile.setGarage"));
            if (home.HasHome)
            {
                homeRow.Action("profile:gohome", Loc.T("profile.goHome"));
            }
            rows.Add(homeRow);
            foreach (LedgerEntryView e in w.Recent)
            {
                rows.Add(new PanelRow { Id = e.TransactionId, Title = CareerText.Of(e.Key, e.Text), Subtitle = e.Type.ToString(), Right = Loc.Money(e.AmountCents) });
            }
        }

        public bool HandleAction(string id)
        {
            CareerService s = m_C.Service;
            if (s == null)
            {
                return true;
            }
            string[] a = id.Split(':');
            double now = m_C.Clock;
            switch (a[1])
            {
                case "sethome":
                {
                    string place = NearestPlaceOfKind(CityPlaceKind.Residential, 80f);
                    CareerUi.Report(place == null ? CareerResult.Fail(CareerResultCode.NotFound, "DTC.Home.NoResidential", Loc.T("profile.noResidentialNearby")) : s.SetHome(place, now));
                    return true;
                }
                case "setgarage":
                {
                    CityPlace sp = m_C.NearestServicePoint(60f);
                    CareerUi.Report(sp == null ? CareerResult.Fail(CareerResultCode.NotAtServicePoint, "DTC.Home.NoServicePoint", Loc.T("garage.goToServicePoint")) : s.SetGarage(sp.Id, now));
                    return true;
                }
                case "gohome":
                {
                    CareerResult r = s.RequestReturnHome(now);
                    CareerUi.Report(r);
                    if (r.IsOk && r.HasPoint && PlayerSystem.Instance.State == PlayerState.OnFoot)
                    {
                        CityPlace hp = null;
                        if (!string.IsNullOrEmpty(r.PlaceId))
                        {
                            CityServices.Places.TryGet(r.PlaceId, out hp);
                        }
                        float3 to = hp != null ? hp.WalkAccess : CareerWorldAdapter.ToF3(r.Point);
                        PlayerSystem.Instance.TeleportAvatar(to);
                    }
                    return true;
                }
            }
            return false;
        }

        private static string NearestPlaceOfKind(CityPlaceKind kind, float radius)
        {
            float3 me = CareerUi.PlayerPos();
            CityPlace best = null;
            float bestD = radius;
            foreach (CityPlace p in CityServices.Places.Places)
            {
                if (p.Kind != kind)
                {
                    continue;
                }
                float d = math.distance(p.BuildingPos.xz, me.xz);
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best?.Id;
        }
    }
    /// <summary>
    /// Operation IDs for money actions from the panel. The same action (same row and button) repeated within
    /// <see cref="kWindowSeconds"/> reuses its ID, so a double click or a re-sent trigger is applied once
    /// (exactly-once charges, SPEC-ECON). A deliberate repeat after the window is a new operation.
    /// </summary>
    internal static class UiOps
    {
        private const float kWindowSeconds = 2f;
        private static readonly Dictionary<string, KeyValuePair<string, float>> s_Recent = new Dictionary<string, KeyValuePair<string, float>>();

        public static string IdFor(CareerService s, string actionKey)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (s_Recent.TryGetValue(actionKey, out KeyValuePair<string, float> e) && now - e.Value < kWindowSeconds)
            {
                s_Recent[actionKey] = new KeyValuePair<string, float>(e.Key, now);
                return e.Key;
            }
            if (s_Recent.Count > 64)
            {
                s_Recent.Clear();
            }
            string id = s.NewOperationId();
            s_Recent[actionKey] = new KeyValuePair<string, float>(id, now);
            return id;
        }
    }
}
