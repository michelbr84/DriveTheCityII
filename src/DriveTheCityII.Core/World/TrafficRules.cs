using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;

namespace DriveTheCityII.Core.World
{
    /// <summary>
    /// Extra guidance-route costs, in seconds, for turning round, by vehicle length (ISSUE-026, ISSUE-034).
    /// </summary>
    public static class TurnPenalties
    {
        /// <summary>Longer than this: a bus or a coach, which cannot turn round in a street or at an ordinary junction.</summary>
        public const float BusLength = 10.5f;

        /// <summary>Longer than this: a truck, which can turn round only with room to spare.</summary>
        public const float LongLength = 7f;

        /// <summary>
        /// Starting on the lane that runs the other way (turning round in the street). Long vehicles prefer going round
        /// the block, but not at any price: with 600 s a truck facing away from its drop was sent on an 11 km loop
        /// (observed in game on 2026-10-19). A 12.7 m bus failed one turn-round after another on a two-lane street with
        /// 120 s (observed in game on 2026-10-19), so buses get 400 s.
        /// </summary>
        public static float WrongWayStart(float length) => length > BusLength ? 400f : length > LongLength ? 120f : 30f;

        /// <summary>
        /// A U-turn movement at a junction. Cars and trucks make them as the game's traffic does; a bus needs more room
        /// than an ordinary junction has, so it goes round the block unless that costs more than 400 s.
        /// </summary>
        public static float UTurnLane(float length) => length > BusLength ? 400f : 0f;
    }

    /// <summary>
    /// Which vehicle the player's vehicle is waiting behind (ISSUE-033). The game clears a traffic deadlock only when
    /// following the "blocked by" links from a stopped vehicle leads back round to it; a player's vehicle that never says
    /// what it waits for hides every such loop that passes through it, and the queues stand until it leaves.
    /// </summary>
    public static class WaitingBehind
    {
        /// <summary>Stopped at least this long before a vehicle is reported (brief mutual waits are not deadlocks).</summary>
        public const float WaitSeconds = 5f;

        /// <summary>Speeds below this count as stopped, in metres per second.</summary>
        public const float StoppedSpeed = 1f;

        /// <summary>Largest gap between our front and the vehicle ahead that still counts as waiting behind it, in metres.</summary>
        public const float MaxGap = 6f;

        /// <summary>
        /// The index in <paramref name="obstacles"/> of the vehicle we wait behind: the nearest moving-traffic vehicle
        /// across our path within <see cref="MaxGap"/> of our front, once we have been stopped for
        /// <see cref="WaitSeconds"/>. -1 when we are moving, have only just stopped, or nothing is there.
        /// </summary>
        public static int Pick(Obb2 self, float speed, float stoppedSeconds, IReadOnlyList<Obstacle> obstacles)
        {
            if (obstacles == null || stoppedSeconds < WaitSeconds || Math.Abs(speed) > StoppedSpeed)
            {
                return -1;
            }
            Vec2 fwd = self.Forward;
            Vec2 right = self.Right;
            int best = -1;
            float bestGap = float.MaxValue;
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle o = obstacles[i];
                if (o.Kind != ObstacleKind.Vehicle)
                {
                    continue;
                }
                Obb2 b = o.Footprint;
                Vec2 d = b.Center - self.Center;
                float along = Vec2.Dot(d, fwd);
                if (along <= 0f)
                {
                    continue;
                }
                // The other vehicle's half extents along our heading and across it.
                float extAlong = b.HalfWidth * Math.Abs(Vec2.Dot(b.Right, fwd)) + b.HalfLength * Math.Abs(Vec2.Dot(b.Forward, fwd));
                float extAcross = b.HalfWidth * Math.Abs(Vec2.Dot(b.Right, right)) + b.HalfLength * Math.Abs(Vec2.Dot(b.Forward, right));
                float gap = along - self.HalfLength - extAlong;
                // In our path: the bodies overlap across our heading by more than a few centimetres. A vehicle whose body
                // already overlaps ours (negative gap) is the one we are stuck against: observed in game on 2026-10-20, a
                // car stopped across the front half of a 12.7 m bus, 3 m from its centre, waiting for the bus.
                bool inPath = Math.Abs(Vec2.Dot(d, right)) < self.HalfWidth + extAcross - 0.3f;
                if (!inPath || gap > MaxGap)
                {
                    continue;
                }
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }
            return best;
        }
    }
}
