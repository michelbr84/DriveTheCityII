using DriveTheCityII.Diagnostics;
using Game;
using Game.Rendering;
using Unity.Entities;
using UnityEngine;

namespace DriveTheCityII.CityBridge
{
    /// <summary>
    /// Camera ownership (SPEC-CONTROL camera owner; refcache note 02 §1). Takes the game's cinematic camera controller
    /// with its own input disabled, drives its pose every frame, and restores the previous controller exactly.
    /// The terrain clamp extension is disabled while owned so that the camera can follow into tunnels.
    /// </summary>
    public sealed class CameraRig
    {
        private CameraUpdateSystem m_Cus;
        private IGameCameraController m_Previous;
        private Vector3 m_SavedPivot;
        private Vector3 m_SavedRotation;
        private float m_SavedZoom;
        private float m_SavedFov;
        private float m_SavedNear;
        private bool m_SavedInput;
        private bool m_SavedCollisions;
        private Behaviour m_TerrainClamp;
        private bool m_SavedClamp;

        public bool Owned { get; private set; }

        public Vector3 TargetPosition;
        public Vector3 TargetEuler;
        public float TargetFov = 60f;
        public float NearClip = 0.15f;

        public bool TryGetGameplayView(out Vector3 pivot, out Vector3 rotation)
        {
            pivot = default;
            rotation = default;
            Ensure();
            if (m_Cus?.gamePlayController == null)
            {
                return false;
            }
            pivot = m_Cus.gamePlayController.pivot;
            rotation = m_Cus.gamePlayController.rotation;
            return true;
        }

        private void Ensure()
        {
            m_Cus ??= World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<CameraUpdateSystem>();
        }

        public bool Take()
        {
            if (Owned)
            {
                return true;
            }
            Ensure();
            if (m_Cus == null || m_Cus.cinematicCameraController == null || m_Cus.activeCameraController == null)
            {
                DtcLog.Warn("CameraRig: camera controllers not available");
                return false;
            }
            CinematicCameraController cc = m_Cus.cinematicCameraController;
            m_Previous = m_Cus.activeCameraController;
            if (m_Cus.gamePlayController != null)
            {
                m_SavedPivot = m_Cus.gamePlayController.pivot;
                m_SavedRotation = m_Cus.gamePlayController.rotation;
                m_SavedZoom = m_Cus.gamePlayController.zoom;
            }
            m_SavedFov = cc.fov;
            m_SavedNear = cc.lens.NearClipPlane;
            m_SavedInput = cc.inputEnabled;
            m_SavedCollisions = cc.collisionsEnabled;
            cc.inputEnabled = false;
            cc.collisionsEnabled = false;
            cc.TryMatchPosition(m_Previous);
            m_Cus.activeCameraController = cc;
            m_TerrainClamp = cc.GetComponent<CinemachineRestrictToTerrain>();
            if (m_TerrainClamp != null)
            {
                m_SavedClamp = m_TerrainClamp.enabled;
                m_TerrainClamp.enabled = false;
            }
            Owned = true;
            DtcLog.Info("CameraRig: camera taken (cinematic controller, input off, terrain clamp off)");
            return true;
        }

        /// <summary>Applies the target pose; call from a system that runs before CameraUpdateSystem in PreCulling.</summary>
        public void Apply()
        {
            if (!Owned || m_Cus == null)
            {
                return;
            }
            CinematicCameraController cc = m_Cus.cinematicCameraController;
            if (cc == null)
            {
                return;
            }
            // Re-assert ownership if something else switched controllers (per-frame owner check, SPEC-CONTROL §10).
            if (!ReferenceEquals(m_Cus.activeCameraController, cc))
            {
                DtcLog.Warn("CameraRig: another controller became active; re-acquiring");
                m_Cus.activeCameraController = cc;
            }
            cc.inputEnabled = false;
            cc.position = TargetPosition;
            cc.rotation = TargetEuler;
            cc.fov = TargetFov;
            cc.lens.NearClipPlane = NearClip;
            cc.SnapToTarget();
        }

        public void Release()
        {
            if (!Owned)
            {
                return;
            }
            Owned = false;
            if (m_Cus == null)
            {
                return;
            }
            CinematicCameraController cc = m_Cus.cinematicCameraController;
            if (cc != null)
            {
                cc.fov = m_SavedFov;
                cc.lens.NearClipPlane = m_SavedNear;
                cc.inputEnabled = m_SavedInput;
                cc.collisionsEnabled = m_SavedCollisions;
            }
            if (m_TerrainClamp != null)
            {
                m_TerrainClamp.enabled = m_SavedClamp;
            }
            IGameCameraController back = m_Previous ?? m_Cus.gamePlayController;
            if (m_Cus.gamePlayController != null && ReferenceEquals(back, m_Cus.gamePlayController))
            {
                m_Cus.gamePlayController.pivot = m_SavedPivot;
                m_Cus.gamePlayController.rotation = m_SavedRotation;
                m_Cus.gamePlayController.zoom = m_SavedZoom;
            }
            if (back != null)
            {
                m_Cus.activeCameraController = back;
            }
            DtcLog.Info("CameraRig: camera returned to the city-building controller");
        }
    }
}
