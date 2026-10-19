using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Residential base and garage location (SPEC-HOME sections 3.2 and 3.3). Both are exterior access points of real
    /// buildings, stored as stable references. Choosing them never changes native state (REQ-CITY-001).
    /// </summary>
    public sealed class HomeState
    {
        public PlaceRef Home;

        /// <summary>The home was demolished and a temporary base is used until the player picks a new one.</summary>
        public bool TemporaryBase;

        public double LastHomeChangeAt = -1;
        public PlaceRef Garage;

        /// <summary>Garage capacity level: 0, 1, 2 for 2, 4, 6 stored vehicles.</summary>
        public int GarageLevel;

        public double LastReturnHomeAt = -1;

        public int GarageCapacity => CareerBalance.GarageCapacityByLevel[CareerMath.Clamp(GarageLevel, 0, CareerBalance.GarageCapacityByLevel.Length - 1)];

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("home", Home != null ? Home.ToJson() : JsonValue.Null)
                .Set("temporaryBase", TemporaryBase)
                .Set("lastHomeChangeAt", LastHomeChangeAt)
                .Set("garage", Garage != null ? Garage.ToJson() : JsonValue.Null)
                .Set("garageLevel", GarageLevel)
                .Set("lastReturnHomeAt", LastReturnHomeAt);
        }

        internal static HomeState FromJson(JsonValue o, string path, JsonRead r)
        {
            var h = new HomeState
            {
                Home = PlaceRef.FromJson(r.Obj(o, "home", path, false), path + ".home", r),
                TemporaryBase = r.Bool(o, "temporaryBase", path),
                LastHomeChangeAt = r.Num(o, "lastHomeChangeAt", path, -1),
                Garage = PlaceRef.FromJson(r.Obj(o, "garage", path, false), path + ".garage", r),
                GarageLevel = r.Int(o, "garageLevel", path, 0, CareerBalance.GarageCapacityByLevel.Length - 1),
                LastReturnHomeAt = r.Num(o, "lastReturnHomeAt", path, -1),
            };
            return h;
        }
    }
}
