using System;

namespace DriveTheCityII.Core.Geometry
{
    /// <summary>
    /// Oriented rectangle on the horizontal plane: a center, a unit forward axis, and half extents
    /// (HalfWidth along the right axis, HalfLength along the forward axis).
    /// </summary>
    public struct Obb2
    {
        public Vec2 Center;
        public Vec2 Forward;
        public float HalfWidth;
        public float HalfLength;

        public Obb2(Vec2 center, Vec2 forward, float halfWidth, float halfLength)
        {
            Center = center;
            Forward = forward.LengthSq > 1e-12f ? forward.Normalized : new Vec2(0f, 1f);
            HalfWidth = Math.Max(0f, halfWidth);
            HalfLength = Math.Max(0f, halfLength);
        }

        public static Obb2 FromYaw(Vec2 center, float yaw, float halfWidth, float halfLength) =>
            new Obb2(center, Vec2.FromYaw(yaw), halfWidth, halfLength);

        public Vec2 Right => Forward.RightPerp;

        /// <summary>Radius of the circle that encloses the rectangle.</summary>
        public float BoundingRadius => (float)Math.Sqrt(HalfWidth * HalfWidth + HalfLength * HalfLength);

        public Vec2 Corner(int i)
        {
            float sx = (i == 0 || i == 3) ? -1f : 1f;
            float sz = (i < 2) ? -1f : 1f;
            return Center + Right * (HalfWidth * sx) + Forward * (HalfLength * sz);
        }

        /// <summary>Point expressed in this box's local frame (x = right, z = forward).</summary>
        public Vec2 ToLocal(Vec2 p)
        {
            Vec2 d = p - Center;
            return new Vec2(Vec2.Dot(d, Right), Vec2.Dot(d, Forward));
        }

        public Vec2 ToWorld(Vec2 local) => Center + Right * local.X + Forward * local.Z;

        public bool Contains(Vec2 p)
        {
            Vec2 l = ToLocal(p);
            return Math.Abs(l.X) <= HalfWidth && Math.Abs(l.Z) <= HalfLength;
        }

        /// <summary>Closest point of the rectangle (including its interior) to <paramref name="p"/>.</summary>
        public Vec2 ClosestPoint(Vec2 p)
        {
            Vec2 l = ToLocal(p);
            l.X = MathX.Clamp(l.X, -HalfWidth, HalfWidth);
            l.Z = MathX.Clamp(l.Z, -HalfLength, HalfLength);
            return ToWorld(l);
        }
    }

    /// <summary>Result of an overlap test: the translation that separates the first shape from the second.</summary>
    public struct Penetration
    {
        public bool Overlaps;

        /// <summary>Unit normal pointing from the obstacle towards the moving shape.</summary>
        public Vec2 Normal;

        /// <summary>Distance to move along <see cref="Normal"/> to separate.</summary>
        public float Depth;

        public static readonly Penetration None = new Penetration { Overlaps = false };
    }

    public static class Overlap
    {
        /// <summary>Circle (moving) against an oriented rectangle (obstacle).</summary>
        public static Penetration CircleVsBox(Vec2 c, float radius, Obb2 box)
        {
            Vec2 local = box.ToLocal(c);
            bool inside = Math.Abs(local.X) <= box.HalfWidth && Math.Abs(local.Z) <= box.HalfLength;
            if (!inside)
            {
                Vec2 closest = box.ClosestPoint(c);
                Vec2 d = c - closest;
                float distSq = d.LengthSq;
                if (distSq >= radius * radius)
                {
                    return Penetration.None;
                }
                float dist = (float)Math.Sqrt(distSq);
                Vec2 n = dist > 1e-6f ? d / dist : (c - box.Center).Normalized;
                return new Penetration { Overlaps = true, Normal = n, Depth = radius - dist };
            }

            // Center inside the box: push out through the nearest face.
            float dx = box.HalfWidth - Math.Abs(local.X);
            float dz = box.HalfLength - Math.Abs(local.Z);
            if (dx < dz)
            {
                Vec2 n = box.Right * (local.X >= 0f ? 1f : -1f);
                return new Penetration { Overlaps = true, Normal = n, Depth = dx + radius };
            }
            else
            {
                Vec2 n = box.Forward * (local.Z >= 0f ? 1f : -1f);
                return new Penetration { Overlaps = true, Normal = n, Depth = dz + radius };
            }
        }

        /// <summary>Oriented rectangle (moving) against oriented rectangle (obstacle), separating axis test.</summary>
        public static Penetration BoxVsBox(Obb2 a, Obb2 b)
        {
            Vec2[] axes = { a.Right, a.Forward, b.Right, b.Forward };
            float minDepth = float.MaxValue;
            Vec2 bestAxis = Vec2.Zero;
            Vec2 centerDelta = a.Center - b.Center;
            for (int i = 0; i < axes.Length; i++)
            {
                Vec2 axis = axes[i];
                float ra = ProjectRadius(a, axis);
                float rb = ProjectRadius(b, axis);
                float dist = Vec2.Dot(centerDelta, axis);
                float overlap = ra + rb - Math.Abs(dist);
                if (overlap <= 0f)
                {
                    return Penetration.None;
                }
                if (overlap < minDepth)
                {
                    minDepth = overlap;
                    bestAxis = dist >= 0f ? axis : -axis;
                }
            }
            return new Penetration { Overlaps = true, Normal = bestAxis, Depth = minDepth };
        }

        private static float ProjectRadius(Obb2 box, Vec2 axis) =>
            box.HalfWidth * Math.Abs(Vec2.Dot(box.Right, axis)) + box.HalfLength * Math.Abs(Vec2.Dot(box.Forward, axis));

        /// <summary>True when the vertical ranges [aMin, aMax] and [bMin, bMax] overlap.</summary>
        public static bool RangesOverlap(float aMin, float aMax, float bMin, float bMax) => aMin < bMax && bMin < aMax;
    }
}
