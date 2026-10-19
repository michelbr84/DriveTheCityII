using System.Collections.Generic;
using System.Globalization;

namespace DriveTheCityII.Core.Career
{
    public enum CareerEventKind
    {
        MissionStateChanged = 0,
        LegStateChanged = 1,
        Settled = 2,
        Notification = 3,

        /// <summary>A service vehicle (bus, shipper truck) was provided or replaced; the mod spawns it.</summary>
        ServiceVehicleProvided = 4,

        /// <summary>A vehicle record left play (returned, sold, collected by its operator); the mod despawns it.</summary>
        VehicleReleased = 5,

        ChallengeStateChanged = 6,
        CompanySettled = 7,

        /// <summary>A vehicle became immobilized (out of fuel or breakdown).</summary>
        VehicleImmobilized = 8,
    }

    public enum NotificationLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Tutorial = 3,
    }

    /// <summary>
    /// Domain event returned by <see cref="CareerService"/> calls (SPEC-MODULES 4.6). Player-facing text is always a
    /// localization <see cref="Key"/> with <see cref="Args"/>; <see cref="Text"/> is the English fallback (REQ-L10N-001).
    /// </summary>
    public sealed class CareerEvent
    {
        public CareerEventKind Kind;
        public double Time;
        public string MissionId;
        public CareerKind Career;
        public MissionState FromState;
        public MissionState ToState;
        public MissionFault Fault;
        public int LegIndex = -1;
        public LegState LegState;
        public string TransactionId;
        public long AmountCents;
        public string VehicleId;
        public string PlaceId;
        public string Key;
        public string[] Args;
        public string Text;
        public NotificationLevel Level;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0}s {1} {2} {3}", Time, Kind, Key ?? string.Empty, Text ?? string.Empty);
        }

        internal static CareerEvent Notify(double time, NotificationLevel level, string key, string text, params string[] args)
        {
            return new CareerEvent { Kind = CareerEventKind.Notification, Time = time, Level = level, Key = key, Text = text, Args = args };
        }
    }

    /// <summary>Collects events for one service call.</summary>
    internal sealed class EventSink
    {
        public readonly List<CareerEvent> Events = new List<CareerEvent>();

        public void Add(CareerEvent e)
        {
            Events.Add(e);
        }

        public void Notify(double time, NotificationLevel level, string key, string text, params string[] args)
        {
            Events.Add(CareerEvent.Notify(time, level, key, text, args));
        }
    }
}
