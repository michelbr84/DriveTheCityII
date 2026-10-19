using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Read-only city port implemented by the mod's CityBridge adapters (SPEC-MODULES sections 4.2 and 5). Career never
    /// writes the city through it (REQ-CITY-001). Exceptions thrown by an implementation are contained by Career and
    /// treated as <see cref="RouteStatus.Unknown"/> answers.
    /// </summary>
    public interface ICareerWorld
    {
        /// <summary>Places of the given kinds near a point. Career sorts the result itself, so order does not matter.</summary>
        IReadOnlyList<PlaceInfo> QueryPlaces(PlaceQuery query);

        /// <summary>Current data of a place, or null when it no longer exists (demolished or replaced).</summary>
        PlaceInfo Describe(string placeId);

        /// <summary>Road route estimate from a point to a place's access point for a vehicle class (REQ-CAREER-002).</summary>
        RouteEstimate Estimate(MapPoint from, string placeId, VehicleClass vehicleClass);

        /// <summary>City bus lines with their stops in driving order (OPEN-020).</summary>
        IReadOnlyList<TransitLineInfo> BusLines();

        /// <summary>Changes whenever the city was edited; a change triggers revalidation (REQ-CITY-002).</summary>
        long CityRevision { get; }
    }
}
