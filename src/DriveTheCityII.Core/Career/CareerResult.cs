using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    public enum CareerResultCode
    {
        Ok = 0,

        /// <summary>The operation (TransactionId/operation ID) was already applied; nothing changed.</summary>
        AlreadyApplied = 1,

        InsufficientFunds = 2,
        Locked = 3,
        NotFound = 4,
        InvalidState = 5,
        InvalidArgument = 6,
        NotAtServicePoint = 7,
        WrongVehicleClass = 8,
        MissionAlreadyActive = 9,
        OfferExpired = 10,
        OfferWithdrawn = 11,
        NoWorld = 12,
        DeferredChargesOpen = 13,
        CapacityExceeded = 14,
        Cooldown = 15,
        VehicleImmobilized = 16,
        VehicleInMission = 17,
        CompanyInArrears = 18,
        NotOwned = 19,
    }

    /// <summary>Result of a command. <see cref="Key"/>/<see cref="Text"/> explain refusals with a next step (REQ-UI-003).</summary>
    public sealed class CareerResult
    {
        public CareerResultCode Code;
        public string Key;
        public string Text;

        /// <summary>Created or affected id (mission, vehicle, company, ...).</summary>
        public string Id;

        public string TransactionId;
        public long AmountCents;

        /// <summary>Amount turned into a deferred charge, when a mandatory charge exceeded the wallet.</summary>
        public long DeferredCents;

        public string PlaceId;
        public MapPoint Point;
        public bool HasPoint;
        public List<CareerEvent> Events = new List<CareerEvent>();

        public bool IsOk => Code == CareerResultCode.Ok;

        public static CareerResult Success(string id = null) => new CareerResult { Code = CareerResultCode.Ok, Id = id, Key = "DTC.Result.Ok", Text = "Done." };

        public static CareerResult Fail(CareerResultCode code, string key, string text) => new CareerResult { Code = code, Key = key, Text = text };

        public override string ToString() => Code + ": " + Text;
    }
}
