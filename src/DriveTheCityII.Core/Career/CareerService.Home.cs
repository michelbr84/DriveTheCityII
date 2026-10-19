namespace DriveTheCityII.Core.Career
{
    /// <summary>Home base and garage location (SPEC-HOME; REQ-HOME-001, REQ-HOME-003). Exterior access points only.</summary>
    public sealed partial class CareerService
    {
        private static readonly PlaceKind[] s_HomeKinds = { PlaceKind.Residential, PlaceKind.Home };

        /// <summary>"Make this my home": an eligible residential place; free, once per 10 career minutes.</summary>
        public CareerResult SetHome(string placeId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            PlaceInfo p = DescribePlace(placeId, out bool unknown);
            if (p == null)
            {
                return Fail(unknown ? CareerResultCode.NoWorld : CareerResultCode.NotFound, "DTC.Home.NotFound", "This place cannot be used.");
            }
            if (p.Kind != PlaceKind.Residential && p.Kind != PlaceKind.Home)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Home.NotResidential", "Only residential buildings can be your home.");
            }
            HomeState h = m_State.Home;
            if (!h.TemporaryBase && h.LastHomeChangeAt >= 0 && now - h.LastHomeChangeAt < CareerBalance.HomeChangeCooldownSeconds)
            {
                return Fail(CareerResultCode.Cooldown, "DTC.Home.Cooldown", "You can change your home again in a few minutes.");
            }
            h.Home = PlaceRef.From(p);
            h.TemporaryBase = false;
            h.LastHomeChangeAt = now;
            CareerResult r = CareerResult.Success(p.Id);
            r.Point = p.Access;
            r.HasPoint = true;
            return Done(r);
        }

        /// <summary>"Use as my garage": a place with a vehicle access point (storage, purchase delivery point).</summary>
        public CareerResult SetGarage(string placeId, double nowSeconds)
        {
            Enter(nowSeconds);
            PlaceInfo p = DescribePlace(placeId, out bool unknown);
            if (p == null)
            {
                return Fail(unknown ? CareerResultCode.NoWorld : CareerResultCode.NotFound, "DTC.Home.NotFound", "This place cannot be used.");
            }
            if (p.Kind == PlaceKind.TransitStop)
            {
                return Fail(CareerResultCode.InvalidArgument, "DTC.Home.GarageKind", "A transit stop cannot be a garage.");
            }
            m_State.Home.Garage = PlaceRef.From(p);
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (!v.IsRemoved && v.State == VehicleLocationState.Stored && v.Holder == VehicleHolder.Player && v.StoredAtPlaceId == null)
                {
                    v.StoredAtPlaceId = p.Id;
                }
            }
            return Done(CareerResult.Success(p.Id));
        }

        /// <summary>"Return home" while on foot with no open job: no cost, 60 s cooldown. Returns the home access point.</summary>
        public CareerResult RequestReturnHome(double nowSeconds)
        {
            double now = Enter(nowSeconds);
            HomeState h = m_State.Home;
            if (h.Home == null)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Home.None", "Choose a home first.");
            }
            if (m_State.Mission != null)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Home.MissionOpen", "Finish or cancel your job first.");
            }
            if (h.LastReturnHomeAt >= 0 && now - h.LastReturnHomeAt < CareerBalance.ReturnHomeCooldownSeconds)
            {
                return Fail(CareerResultCode.Cooldown, "DTC.Home.ReturnCooldown", "Return home is cooling down.");
            }
            h.LastReturnHomeAt = now;
            CareerResult r = CareerResult.Success(h.Home.Id);
            r.Point = h.Home.Access;
            r.HasPoint = true;
            r.PlaceId = h.Home.Id;
            return Done(r);
        }

        /// <summary>Respawn candidate: home (or temporary base), then garage (SPEC-HOME 3.2, SPEC-PLAYER spawn order).</summary>
        public bool TryGetRespawnPoint(out MapPoint point, out string placeId)
        {
            PlaceRef p = m_State.Home.Home ?? m_State.Home.Garage;
            point = p != null ? p.Access : default;
            placeId = p?.Id;
            return p != null;
        }

        /// <summary>Fallbacks for a demolished home or garage (SPEC-HOME section 4). Stored vehicles are never lost.</summary>
        private void RevalidateHome()
        {
            HomeState h = m_State.Home;
            if (h.Home != null)
            {
                PlaceInfo p = DescribePlace(h.Home.Id, out bool unknown);
                if (!unknown && p == null)
                {
                    string oldName = h.Home.Name;
                    var candidates = QueryPlacesByDistance(s_HomeKinds, h.Home.Access, 2000, 1);
                    if (candidates.Count > 0)
                    {
                        h.Home = PlaceRef.From(candidates[0]);
                        h.TemporaryBase = true;
                        Notify(NotificationLevel.Warning, "DTC.Home.Demolished", "Your home at " + oldName + " no longer exists. A temporary base was set nearby; choose a new home.", oldName);
                    }
                    else
                    {
                        h.Home = null;
                        h.TemporaryBase = false;
                        Notify(NotificationLevel.Warning, "DTC.Home.DemolishedNoBase", "Your home at " + oldName + " no longer exists. Choose a new home.", oldName);
                    }
                }
                else if (p != null)
                {
                    h.Home.Access = p.Access;
                    h.Home.Name = p.Name ?? h.Home.Name;
                }
            }
            if (h.Garage != null)
            {
                PlaceInfo g = DescribePlace(h.Garage.Id, out bool unknown);
                if (!unknown && g == null)
                {
                    string gid = h.Garage.Id;
                    h.Garage = null;
                    foreach (CareerVehicle v in m_State.Vehicles)
                    {
                        if (v.StoredAtPlaceId == gid)
                        {
                            v.StoredAtPlaceId = null;
                        }
                    }
                    Notify(NotificationLevel.Warning, "DTC.Home.GarageDemolished", "Your garage no longer exists. Stored vehicles were moved to the default storage.");
                }
                else if (g != null)
                {
                    h.Garage.Access = g.Access;
                }
            }
        }
    }
}
