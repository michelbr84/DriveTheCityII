using System;

namespace DriveTheCityII.Core.Geometry
{
    /// <summary>
    /// Minimal 2D vector on the horizontal plane (X, Z of the game world). Kept engine-free so that
    /// motion and contact logic can be unit-tested without the game.
    /// </summary>
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Z;

        public Vec2(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public float LengthSq => X * X + Z * Z;

        public Vec2 Normalized
        {
            get
            {
                float len = Length;
                return len > 1e-6f ? new Vec2(X / len, Z / len) : Zero;
            }
        }

        /// <summary>Perpendicular pointing to the right of this direction (clockwise seen from above).</summary>
        public Vec2 RightPerp => new Vec2(Z, -X);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Z / s);

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Z - a.Z * b.X;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

        /// <summary>Unit direction for a yaw angle in radians (0 = +Z, positive = clockwise from above, like Unity).</summary>
        public static Vec2 FromYaw(float yaw) => new Vec2((float)Math.Sin(yaw), (float)Math.Cos(yaw));

        /// <summary>Yaw angle in radians of a direction (inverse of <see cref="FromYaw"/>).</summary>
        public float ToYaw() => (float)Math.Atan2(X, Z);

        public bool Equals(Vec2 other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Z.GetHashCode();
        public override string ToString() => $"({X:0.###}, {Z:0.###})";
    }

    /// <summary>Minimal 3D vector (game world: Y up, left-handed, metres).</summary>
    public struct Vec3 : IEquatable<Vec3>
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3(Vec2 xz, float y)
        {
            X = xz.X;
            Y = y;
            Z = xz.Z;
        }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);

        public Vec2 XZ => new Vec2(X, Z);
        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized
        {
            get
            {
                float len = Length;
                return len > 1e-6f ? new Vec3(X / len, Y / len, Z / len) : Zero;
            }
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(float s, Vec3 a) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        public static float Distance(Vec3 a, Vec3 b) => (a - b).Length;

        public bool IsFinite => !(float.IsNaN(X) || float.IsNaN(Y) || float.IsNaN(Z) ||
                                  float.IsInfinity(X) || float.IsInfinity(Y) || float.IsInfinity(Z));

        public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec3 v && Equals(v);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ (Y.GetHashCode() * 31) ^ Z.GetHashCode();
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }

    public static class MathX
    {
        public const float Pi = (float)Math.PI;
        public const float TwoPi = (float)(Math.PI * 2.0);
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>Moves <paramref name="current"/> towards <paramref name="target"/> by at most <paramref name="maxDelta"/>.</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float d = target - current;
            if (Math.Abs(d) <= maxDelta)
            {
                return target;
            }
            return current + Math.Sign(d) * maxDelta;
        }

        /// <summary>Wraps an angle to (-pi, pi].</summary>
        public static float WrapAngle(float a)
        {
            a %= TwoPi;
            if (a <= -Pi)
            {
                a += TwoPi;
            }
            else if (a > Pi)
            {
                a -= TwoPi;
            }
            return a;
        }

        /// <summary>Exponential smoothing factor for a time constant (frame-rate independent).</summary>
        public static float SmoothFactor(float sharpness, float dt) => 1f - (float)Math.Exp(-sharpness * dt);

        public static bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));
    }
}
