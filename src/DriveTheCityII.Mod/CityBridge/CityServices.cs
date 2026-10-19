using System;
using System.Diagnostics;
using Game.UI;
using Unity.Entities;

namespace DriveTheCityII.CityBridge
{
    /// <summary>Shared CityBridge services for the current city: road router and place index (main thread).</summary>
    public static class CityServices
    {
        public static readonly RoadRouter Router = new RoadRouter();
        public static readonly PlaceIndex Places = new PlaceIndex();
        private static NameSystem s_Names;

        public static NameSystem Names => s_Names ??= World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<NameSystem>();

        /// <summary>Combined revision of roads, buildings and routes (career revalidation trigger).</summary>
        public static long CityRevision => ((long)Router.Revision << 32) ^ Places.Revision;

        public static void Refresh(EntityManager em, CityWorldProbe probe, float now)
        {
            Router.EnsureBuilt(em);
            Places.EnsureBuilt(em, probe, Names, now);
        }

        public static void Reset()
        {
            s_Names = null;
        }

        /// <summary>Times a call in milliseconds (diagnostics).</summary>
        public static T Timed<T>(Func<T> f, out long ms)
        {
            var sw = Stopwatch.StartNew();
            T r = f();
            ms = sw.ElapsedMilliseconds;
            return r;
        }
    }
}
