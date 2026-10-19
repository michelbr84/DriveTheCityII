using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using DriveTheCityII.Diagnostics;
using Game;
using Game.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DriveTheCityII.CityBridge
{
    public enum MarkerStyle
    {
        Pickup,
        Dropoff,
        ServicePoint,
        Checkpoint,
        Home,
        Vehicle,
    }

    public struct WorldMarker
    {
        public float3 Position;
        public MarkerStyle Style;
        public float Radius;
    }

    /// <summary>
    /// What the mod wants shown in the world (mission targets, service points, route guidance). Modules replace the
    /// content; <see cref="WorldMarkerSystem"/> draws it every frame while character mode is active.
    /// </summary>
    public static class MarkerBoard
    {
        public static readonly List<WorldMarker> Markers = new List<WorldMarker>();
        public static readonly List<float3> Route = new List<float3>();
        public static bool Visible;

        public static void Clear()
        {
            Markers.Clear();
            Route.Clear();
        }
    }

    /// <summary>Draws markers and the guidance line with the game's overlay renderer (refcache note 05 §B).</summary>
    public partial class WorldMarkerSystem : GameSystemBase
    {
        private OverlayRenderSystem m_Overlay;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Overlay = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
        }

        protected override void OnUpdate()
        {
            if (!MarkerBoard.Visible || (MarkerBoard.Markers.Count == 0 && MarkerBoard.Route.Count < 2))
            {
                return;
            }
            try
            {
                OverlayRenderSystem.Buffer buffer = m_Overlay.GetBuffer(out JobHandle deps);
                deps.Complete();
                float pulse = 0.75f + 0.25f * Mathf.Sin(UnityEngine.Time.realtimeSinceStartup * 3f);
                foreach (WorldMarker m in MarkerBoard.Markers)
                {
                    Color c = ColorFor(m.Style);
                    Color fill = new Color(c.r, c.g, c.b, 0.18f);
                    float d = Math.Max(2f, m.Radius * 2f);
                    buffer.DrawCircle(c, fill, 0.35f, OverlayRenderSystem.StyleFlags.Projected, new float2(0f, 1f), m.Position, d);
                    buffer.DrawCustomMesh(new Color(c.r, c.g, c.b, 0.55f * pulse), m.Position, 14f, 0.9f, OverlayRenderSystem.CustomMeshType.Cylinder);
                }
                bool guidance = Settings.DriveTheCityIISettings.Instance == null || Settings.DriveTheCityIISettings.Instance.RouteGuidance;
                int n = guidance ? Math.Min(MarkerBoard.Route.Count, 400) : 0;
                var routeColor = new Color(0.30f, 0.76f, 1f, 0.85f);
                for (int i = 1; i < n; i++)
                {
                    float3 a = MarkerBoard.Route[i - 1] + new float3(0f, 0.25f, 0f);
                    float3 b = MarkerBoard.Route[i] + new float3(0f, 0.25f, 0f);
                    if (math.distancesq(a, b) < 0.01f)
                    {
                        continue;
                    }
                    buffer.DrawLine(routeColor, new Color(routeColor.r, routeColor.g, routeColor.b, 0.55f), 0.15f,
                        OverlayRenderSystem.StyleFlags.Projected, new Line3.Segment(a, b), 1.4f, new float2(1f, 1f));
                }
                m_Overlay.AddBufferWriter(default(JobHandle));
            }
            catch (Exception ex)
            {
                DtcLog.Throttled("markers", UnityEngine.Time.realtimeSinceStartup, 10f, "Marker drawing failed: " + ex.Message, warn: true);
            }
        }

        private static Color ColorFor(MarkerStyle s)
        {
            switch (s)
            {
                case MarkerStyle.Pickup: return new Color(0.30f, 0.86f, 0.45f, 1f);
                case MarkerStyle.Dropoff: return new Color(1f, 0.70f, 0.28f, 1f);
                case MarkerStyle.ServicePoint: return new Color(0.30f, 0.76f, 1f, 1f);
                case MarkerStyle.Checkpoint: return new Color(0.95f, 0.35f, 0.85f, 1f);
                case MarkerStyle.Home: return new Color(0.95f, 0.95f, 0.40f, 1f);
                default: return new Color(1f, 1f, 1f, 1f);
            }
        }
    }
}
