using System;
using System.Collections.Generic;
using DriveTheCityII.Diagnostics;
using Game.Input;
using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DriveTheCityII.CityBridge
{
    /// <summary>Logical actions of the character modes (SPEC-CONTROLS). Bindings are remappable in the settings.</summary>
    public enum DtcAction
    {
        Forward,
        Back,
        Left,
        Right,
        Run,
        Interact,
        ToggleCharacterMode,
        CameraView,
        Handbrake,
        Recover,
        CareerPanel,
        FreeCursor,
        Horn,
        Lights,
    }

    /// <summary>Snapshot of the player's input for one frame (already routed: empty when not delivered).</summary>
    public struct InputFrame
    {
        public float MoveX;
        public float MoveZ;
        public bool Run;
        public float Throttle;
        public float Brake;
        public float Steer;
        public bool Handbrake;
        public float LookX;
        public float LookY;
        public bool InteractPressed;
        public bool TogglePressed;
        public bool CameraViewPressed;
        public bool RecoverPressed;
        public bool CareerPanelPressed;
        public bool FreeCursorHeld;
        public bool HornHeld;
        public bool LightsPressed;
        public bool GamepadActive;
    }

    /// <summary>
    /// Input ownership (SPEC-CONTROL §5.1): blocks the city-builder Camera, Tool and Shortcuts maps while a character
    /// context is active (IR-01), reads our actions directly from the devices, clears held input on context switches
    /// (IR-04), and manages cursor lock for mouse look.
    /// </summary>
    public sealed class InputRouter : IDisposable
    {
        private InputBarrier m_Camera;
        private InputBarrier m_Tool;
        private InputBarrier m_Shortcuts;
        private bool m_Active;
        private bool m_CursorLocked;
        private int m_SkipFrames;

        public readonly Dictionary<DtcAction, Key> Keys = new Dictionary<DtcAction, Key>
        {
            { DtcAction.Forward, Key.W },
            { DtcAction.Back, Key.S },
            { DtcAction.Left, Key.A },
            { DtcAction.Right, Key.D },
            { DtcAction.Run, Key.LeftShift },
            { DtcAction.Interact, Key.E },
            { DtcAction.ToggleCharacterMode, Key.F8 },
            { DtcAction.CameraView, Key.V },
            { DtcAction.Handbrake, Key.Space },
            { DtcAction.Recover, Key.Backspace },
            { DtcAction.CareerPanel, Key.Tab },
            { DtcAction.FreeCursor, Key.LeftAlt },
            { DtcAction.Horn, Key.H },
            { DtcAction.Lights, Key.L },
        };

        public float MouseSensitivity = 0.12f;
        public float GamepadLookSpeed = 160f;
        public bool InvertY;

        /// <summary>Simulated input injected by the test bridge (labelled as simulated in reports). Null = none.</summary>
        public InputFrame? Injected;

        public bool Active => m_Active;

        public void Activate()
        {
            if (m_Active)
            {
                return;
            }
            InputManager im = InputManager.instance;
            m_Camera ??= im.CreateMapBarrier(InputManager.kCameraMap, "DriveTheCityII");
            m_Tool ??= im.CreateMapBarrier(InputManager.kToolMap, "DriveTheCityII");
            m_Shortcuts ??= im.CreateMapBarrier(InputManager.kShortcutsMap, "DriveTheCityII");
            m_Camera.blocked = true;
            m_Tool.blocked = true;
            m_Shortcuts.blocked = true;
            m_Active = true;
            m_SkipFrames = 2; // IR-04: the key that switched contexts must not leak into the new one
            DtcLog.Info("InputRouter: character context active (Camera, Tool, Shortcuts maps blocked)");
        }

        public void Deactivate()
        {
            if (!m_Active)
            {
                return;
            }
            if (m_Camera != null)
            {
                m_Camera.blocked = false;
            }
            if (m_Tool != null)
            {
                m_Tool.blocked = false;
            }
            if (m_Shortcuts != null)
            {
                m_Shortcuts.blocked = false;
            }
            SetCursorLock(false);
            m_Active = false;
            Injected = null;
            DtcLog.Info("InputRouter: host context restored");
        }

        public void Dispose()
        {
            Deactivate();
            m_Camera?.Dispose();
            m_Tool?.Dispose();
            m_Shortcuts?.Dispose();
            m_Camera = m_Tool = m_Shortcuts = null;
        }

        /// <summary>The toggle key is read in every context (it is how the player enters character mode).</summary>
        public bool ToggleWasPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || InputManager.instance.hasInputFieldFocus)
            {
                return false;
            }
            return Pressed(kb, DtcAction.ToggleCharacterMode);
        }

        public void SetCursorLock(bool locked)
        {
            InputManager im = InputManager.instance;
            if (locked)
            {
                im.hideCursor = true;
                im.cursorLockMode = CursorLockMode.Locked;
            }
            else if (m_CursorLocked)
            {
                im.hideCursor = false;
                im.cursorLockMode = SharedSettings.instance?.graphics != null
                    ? SharedSettings.instance.graphics.cursorMode.ToUnityCursorMode()
                    : CursorLockMode.None;
            }
            m_CursorLocked = locked;
        }

        public InputFrame Read(bool uiBlocking)
        {
            var f = new InputFrame();
            if (!m_Active)
            {
                return f;
            }
            if (Injected.HasValue)
            {
                return Injected.Value;
            }
            if (m_SkipFrames > 0)
            {
                m_SkipFrames--;
                return f;
            }
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Gamepad.current;
            if (uiBlocking || InputManager.instance.hasInputFieldFocus)
            {
                return f;
            }
            if (kb != null)
            {
                float fwd = (Held(kb, DtcAction.Forward) ? 1f : 0f) - (Held(kb, DtcAction.Back) ? 1f : 0f);
                float side = (Held(kb, DtcAction.Right) ? 1f : 0f) - (Held(kb, DtcAction.Left) ? 1f : 0f);
                f.MoveZ = fwd;
                f.MoveX = side;
                f.Throttle = Held(kb, DtcAction.Forward) || kb.upArrowKey.isPressed ? 1f : 0f;
                f.Brake = Held(kb, DtcAction.Back) || kb.downArrowKey.isPressed ? 1f : 0f;
                f.Steer = side + (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
                f.Run = Held(kb, DtcAction.Run);
                f.Handbrake = Held(kb, DtcAction.Handbrake);
                f.InteractPressed = Pressed(kb, DtcAction.Interact);
                f.TogglePressed = Pressed(kb, DtcAction.ToggleCharacterMode);
                f.CameraViewPressed = Pressed(kb, DtcAction.CameraView);
                f.RecoverPressed = Pressed(kb, DtcAction.Recover);
                f.CareerPanelPressed = Pressed(kb, DtcAction.CareerPanel);
                f.FreeCursorHeld = Held(kb, DtcAction.FreeCursor);
                f.HornHeld = Held(kb, DtcAction.Horn);
                f.LightsPressed = Pressed(kb, DtcAction.Lights);
            }
            if (mouse != null && m_CursorLocked && !f.FreeCursorHeld)
            {
                Vector2 d = mouse.delta.ReadValue();
                f.LookX += d.x * MouseSensitivity;
                f.LookY += d.y * MouseSensitivity * (InvertY ? -1f : 1f);
            }
            if (pad != null)
            {
                Vector2 ls = pad.leftStick.ReadValue();
                Vector2 rs = pad.rightStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f || rs.sqrMagnitude > 0.04f || pad.rightTrigger.ReadValue() > 0.05f || pad.leftTrigger.ReadValue() > 0.05f)
                {
                    f.GamepadActive = true;
                }
                f.MoveX = Math.Abs(ls.x) > 0.15f ? ls.x : f.MoveX;
                f.MoveZ = Math.Abs(ls.y) > 0.15f ? ls.y : f.MoveZ;
                f.Steer = Math.Abs(ls.x) > 0.12f ? ls.x : f.Steer;
                f.Throttle = Math.Max(f.Throttle, pad.rightTrigger.ReadValue());
                f.Brake = Math.Max(f.Brake, pad.leftTrigger.ReadValue());
                f.Run |= pad.leftStickButton.isPressed;
                f.Handbrake |= pad.buttonEast.isPressed;
                f.InteractPressed |= pad.buttonSouth.wasPressedThisFrame;
                f.CameraViewPressed |= pad.buttonNorth.wasPressedThisFrame;
                f.RecoverPressed |= pad.selectButton.wasPressedThisFrame;
                f.CareerPanelPressed |= pad.buttonWest.wasPressedThisFrame;
                float dt = Time.unscaledDeltaTime;
                f.LookX += Math.Abs(rs.x) > 0.15f ? rs.x * GamepadLookSpeed * dt : 0f;
                f.LookY += Math.Abs(rs.y) > 0.15f ? rs.y * GamepadLookSpeed * dt * (InvertY ? -1f : 1f) : 0f;
            }
            f.Steer = Mathf.Clamp(f.Steer, -1f, 1f);
            return f;
        }

        private bool Held(Keyboard kb, DtcAction a)
        {
            KeyControl k = Control(kb, a);
            return k != null && k.isPressed;
        }

        private bool Pressed(Keyboard kb, DtcAction a)
        {
            KeyControl k = Control(kb, a);
            return k != null && k.wasPressedThisFrame;
        }

        private KeyControl Control(Keyboard kb, DtcAction a)
        {
            if (!Keys.TryGetValue(a, out Key key) || key == Key.None)
            {
                return null;
            }
            try
            {
                return kb[key];
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
