using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.World
{
    public enum GroundStatus
    {
        /// <summary>A supporting surface lies within [refY - stepDown, refY + stepUp].</summary>
        Supported = 0,

        /// <summary>The nearest supporting surface is further below than stepDown: the body falls towards it.</summary>
        Drop = 1,

        /// <summary>A wall: terrain or a surface rises above refY + stepUp and no lower drivable level exists here.</summary>
        Blocked = 2,

        /// <summary>Water deeper than the allowed depth covers the supporting surface.</summary>
        DeepWater = 3,

        /// <summary>Outside the playable map, or no surface could be sampled.</summary>
        Invalid = 4,
    }

    public struct GroundResult
    {
        public GroundStatus Status;

        /// <summary>Height of the chosen supporting surface (valid for Supported and Drop).</summary>
        public float Height;

        public SurfaceKind Kind;
        public long SourceId;

        /// <summary>Water surface height above the support, when water is present (otherwise 0).</summary>
        public float WaterDepth;

        public bool IsSupported => Status == GroundStatus.Supported;

        public override string ToString() => $"{Status} {Kind}@{Height:0.##} water={WaterDepth:0.##}";
    }

    /// <summary>
    /// Chooses the surface that supports a body at a horizontal position, honouring levels: the highest surface not
    /// above the body's reference height plus its step allowance. Bridges above are ignored while underneath them,
    /// tunnels are kept while inside them, and a rising terrain face counts as a wall (SPEC-CONTACT).
    /// </summary>
    public sealed class GroundSolver
    {
        private readonly List<SurfaceSample> m_Scratch = new List<SurfaceSample>(16);

        /// <summary>How far below the reference height surfaces are searched (metres).</summary>
        public float MaxScanDepth = 250f;

        public GroundResult Solve(IWorldProbe probe, float x, float z, float refY, float stepUp, float stepDown, float maxWaterDepth)
        {
            if (!probe.IsInsideMap(x, z))
            {
                return new GroundResult { Status = GroundStatus.Invalid };
            }

            m_Scratch.Clear();
            probe.SampleSurfaces(x, z, refY - MaxScanDepth, refY + stepUp, m_Scratch);

            float ceiling = refY + stepUp;
            bool haveBest = false;
            SurfaceSample best = default;
            bool haveTerrain = false;
            SurfaceSample terrain = default;
            bool haveWater = false;
            float waterHeight = float.MinValue;

            for (int i = 0; i < m_Scratch.Count; i++)
            {
                SurfaceSample s = m_Scratch[i];
                if (!MathXFinite(s.Height))
                {
                    continue;
                }
                if (s.Kind == SurfaceKind.Water)
                {
                    haveWater = true;
                    waterHeight = Math.Max(waterHeight, s.Height);
                    continue;
                }
                if (s.Kind == SurfaceKind.Terrain)
                {
                    haveTerrain = true;
                    terrain = s;
                }
                if (s.Height <= ceiling && (!haveBest || s.Height > best.Height))
                {
                    haveBest = true;
                    best = s;
                }
            }

            if (!haveBest)
            {
                if (haveTerrain && terrain.Height > ceiling)
                {
                    return new GroundResult { Status = GroundStatus.Blocked, Height = terrain.Height, Kind = SurfaceKind.Terrain };
                }
                return new GroundResult { Status = GroundStatus.Invalid };
            }

            // Terrain above the step allowance is a wall unless we stand on a non-terrain level (tunnel, road, deck).
            if (haveTerrain && terrain.Height > ceiling && best.Kind == SurfaceKind.Terrain)
            {
                return new GroundResult { Status = GroundStatus.Blocked, Height = terrain.Height, Kind = SurfaceKind.Terrain };
            }

            float waterDepth = haveWater && waterHeight > best.Height ? waterHeight - best.Height : 0f;
            if (waterDepth > maxWaterDepth && best.Kind == SurfaceKind.Terrain)
            {
                return new GroundResult
                {
                    Status = GroundStatus.DeepWater,
                    Height = best.Height,
                    Kind = best.Kind,
                    SourceId = best.SourceId,
                    WaterDepth = waterDepth,
                };
            }

            GroundStatus status = best.Height < refY - stepDown ? GroundStatus.Drop : GroundStatus.Supported;
            return new GroundResult
            {
                Status = status,
                Height = best.Height,
                Kind = best.Kind,
                SourceId = best.SourceId,
                WaterDepth = waterDepth,
            };
        }

        private static bool MathXFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));
    }
}
