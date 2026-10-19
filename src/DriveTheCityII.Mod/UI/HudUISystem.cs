using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Core.Persistence;
using DriveTheCityII.Core.Text;
using DriveTheCityII.Diagnostics;
using DriveTheCityII.Player;
using Game;
using Game.SceneFlow;
using Game.UI;
using UnityEngine;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.UI
{
    /// <summary>One row of a panel list (generic so that career modules can fill panels without JS changes).</summary>
    public sealed class PanelRow
    {
        public string Id;
        public string Title;
        public string Subtitle;
        public string Right;
        public bool Highlight;
        public readonly List<(string id, string label, bool enabled)> Actions = new List<(string, string, bool)>();

        public PanelRow Action(string id, string label, bool enabled = true)
        {
            Actions.Add((id, label, enabled));
            return this;
        }
    }

    /// <summary>A tab of the career panel, supplied by a module (Career, Garage, Company, Challenges, Help).</summary>
    public interface IPanelTab
    {
        string Id { get; }
        string Label { get; }
        int Order { get; }
        void Build(List<PanelRow> rows, out string header, out string footer);

        /// <summary>Handles an action id that starts with this tab's id + ":"; returns true if handled.</summary>
        bool HandleAction(string actionId);
    }

    /// <summary>Supplies extra HUD lines (wallet, objective, vehicle status) from other modules.</summary>
    public interface IHudContributor
    {
        void Contribute(JsonValue hud);
    }

    /// <summary>
    /// The HUD (SPEC-UI): publishes one compact JSON view model to the UI module (DriveTheCityII.mjs) and routes the
    /// actions the UI sends back. The UI never writes state itself; every action goes to the owning module (§4.8).
    /// </summary>
    public partial class HudUISystem : UISystemBase
    {
        public const string Group = "driveTheCityII";

        public static HudUISystem Instance { get; private set; }

        private ValueBinding<string> m_Hud;
        private float m_NextPublish;
        private string m_LastLocale;
        private readonly List<IPanelTab> m_Tabs = new List<IPanelTab>();
        private readonly List<IHudContributor> m_Contributors = new List<IHudContributor>();
        private readonly List<PanelRow> m_Rows = new List<PanelRow>();

        public bool PanelOpen { get; private set; }
        public string ActiveTab { get; private set; } = "help";

        public void RegisterTab(IPanelTab tab)
        {
            m_Tabs.RemoveAll(t => t.Id == tab.Id);
            m_Tabs.Add(tab);
            m_Tabs.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void RegisterContributor(IHudContributor c)
        {
            if (!m_Contributors.Contains(c))
            {
                m_Contributors.Add(c);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            AddBinding(m_Hud = new ValueBinding<string>(Group, "hud", "{\"mode\":\"Building\"}"));
            AddBinding(new TriggerBinding<string>(Group, "action", OnAction));
            RegisterTab(new HelpTab());
        }

        public void SetPanel(bool open)
        {
            PanelOpen = open;
            m_NextPublish = 0f;
        }

        public void TogglePanel() => SetPanel(!PanelOpen);

        protected override void OnUpdate()
        {
            string locale = GameManager.instance?.localizationManager?.activeLocaleId;
            if (locale != m_LastLocale)
            {
                m_LastLocale = locale;
                Loc.SetLocaleFromGame(locale);
                m_NextPublish = 0f;
            }
            float now = UTime.realtimeSinceStartup;
            if (now < m_NextPublish)
            {
                return;
            }
            m_NextPublish = now + 0.1f; // 10 Hz is enough for a HUD and keeps UI traffic low
            try
            {
                m_Hud.Update(BuildHud().ToJson(false));
            }
            catch (Exception ex)
            {
                DtcLog.Throttled("hud", now, 10f, "HUD build failed: " + ex.Message, warn: true);
            }
        }

        private JsonValue BuildHud()
        {
            PlayerSystem p = PlayerSystem.Instance;
            JsonValue hud = JsonValue.Object();
            bool city = HostFlow.IsCityReady;
            PlayerState state = p?.State ?? PlayerState.Building;
            hud.Set("city", city);
            hud.Set("mode", state.ToString());
            hud.Set("conflict", Runtime.ConflictDetected);
            string toggleKey = KeyName(p, DtcAction.ToggleCharacterMode);
            hud.Set("toggleLabel", Loc.T("hud.toggle", toggleKey));
            if (state == PlayerState.Building || p == null)
            {
                hud.Set("panelOpen", false);
                if (PanelOpen)
                {
                    PanelOpen = false;
                }
                return hud;
            }

            hud.Set("modeLabel", Loc.T(ModeKey(state)));
            hud.Set("paused", (p.StateMachine.Overlays & PlayerOverlay.Paused) != 0);
            hud.Set("pausedLabel", Loc.T("hud.paused"));
            ControlledVehicle v = p.Vehicle;
            bool driving = state == PlayerState.Driving && v != null;
            if (driving)
            {
                int kmh = (int)Math.Round(Math.Abs(v.State.Speed) * 3.6);
                hud.Set("speed", Loc.T("hud.speed", kmh));
                hud.Set("gear", v.State.ParkingBrake ? "P" : v.State.Gear == Gear.Reverse ? "R" : "D");
                hud.Set("sandbox", v.Kind == ActorKind.BorrowedNativeVehicle);
                hud.Set("sandboxLabel", Loc.T("hud.sandbox"));
                hud.Set("vehicleName", v.Prefab?.Name ?? string.Empty);
            }

            // Prompts for the current context (REQ-UI-003: blocked actions explain the next step via messages).
            JsonValue prompts = JsonValue.Array();
            string interact = KeyName(p, DtcAction.Interact);
            if (state == PlayerState.OnFoot && p.NearestEnterableDistance() < 4f)
            {
                prompts.Add(JsonValue.From(Loc.T("prompt.enter", interact)));
            }
            if (driving)
            {
                prompts.Add(JsonValue.From(Loc.T("prompt.exit", interact)));
            }
            if (state == PlayerState.OnFoot || driving)
            {
                prompts.Add(JsonValue.From(Loc.T("prompt.view", KeyName(p, DtcAction.CameraView))));
                prompts.Add(JsonValue.From(Loc.T("prompt.building", toggleKey)));
            }
            hud.Set("prompts", prompts);
            float msgAge = UTime.realtimeSinceStartup - p.LastMessageTime;
            if (!string.IsNullOrEmpty(p.LastMessage) && msgAge < 6f)
            {
                hud.Set("message", p.LastMessage);
            }
            hud.Set("panelHint", Loc.T("hud.panelHint", KeyName(p, DtcAction.CareerPanel)));

            foreach (IHudContributor c in m_Contributors)
            {
                try
                {
                    c.Contribute(hud);
                }
                catch (Exception ex)
                {
                    DtcLog.Throttled("hudc", UTime.realtimeSinceStartup, 10f, "HUD contributor failed: " + ex.Message, warn: true);
                }
            }

            hud.Set("panelOpen", PanelOpen);
            if (PanelOpen)
            {
                hud.Set("panel", BuildPanel());
            }
            return hud;
        }

        private JsonValue BuildPanel()
        {
            JsonValue panel = JsonValue.Object();
            JsonValue tabs = JsonValue.Array();
            IPanelTab active = null;
            foreach (IPanelTab t in m_Tabs)
            {
                if (t.Id == ActiveTab)
                {
                    active = t;
                }
            }
            active ??= m_Tabs.Count > 0 ? m_Tabs[0] : null;
            foreach (IPanelTab t in m_Tabs)
            {
                tabs.Add(JsonValue.Object().Set("id", t.Id).Set("label", t.Label).Set("active", t == active));
            }
            panel.Set("tabs", tabs);
            m_Rows.Clear();
            string header = string.Empty;
            string footer = string.Empty;
            active?.Build(m_Rows, out header, out footer);
            panel.Set("header", header ?? string.Empty);
            panel.Set("footer", footer ?? string.Empty);
            JsonValue rows = JsonValue.Array();
            foreach (PanelRow r in m_Rows)
            {
                JsonValue row = JsonValue.Object()
                    .Set("id", r.Id ?? string.Empty)
                    .Set("title", r.Title ?? string.Empty)
                    .Set("subtitle", r.Subtitle ?? string.Empty)
                    .Set("right", r.Right ?? string.Empty)
                    .Set("highlight", r.Highlight);
                JsonValue acts = JsonValue.Array();
                foreach ((string id, string label, bool enabled) in r.Actions)
                {
                    acts.Add(JsonValue.Object().Set("id", id).Set("label", label).Set("enabled", enabled));
                }
                row.Set("actions", acts);
                rows.Add(row);
            }
            panel.Set("rows", rows);
            return panel;
        }

        private static string ModeKey(PlayerState s)
        {
            switch (s)
            {
                case PlayerState.OnFoot: return "mode.onfoot";
                case PlayerState.EnteringVehicle: return "mode.entering";
                case PlayerState.Driving: return "mode.driving";
                case PlayerState.ExitingVehicle: return "mode.exiting";
                case PlayerState.Recovery: return "mode.recovery";
                default: return "mode.building";
            }
        }

        public static string KeyName(PlayerSystem p, DtcAction a)
        {
            if (p != null && p.Input.Keys.TryGetValue(a, out UnityEngine.InputSystem.Key k))
            {
                switch (k)
                {
                    case UnityEngine.InputSystem.Key.LeftShift: return "Shift";
                    case UnityEngine.InputSystem.Key.Space: return Loc.Locale == Loc.Portuguese ? "Espaço" : "Space";
                    case UnityEngine.InputSystem.Key.Backspace: return "Backspace";
                    case UnityEngine.InputSystem.Key.LeftAlt: return "Alt";
                    default: return k.ToString();
                }
            }
            return "?";
        }

        /// <summary>Actions sent by the UI module: "toggle", "panel", "tab:&lt;id&gt;", or "&lt;tabId&gt;:&lt;action&gt;".</summary>
        private void OnAction(string id)
        {
            try
            {
                DtcLog.Info("UI action: " + id);
                PlayerSystem p = PlayerSystem.Instance;
                if (id == "toggle")
                {
                    if (p == null)
                    {
                        return;
                    }
                    if (p.State == PlayerState.Building)
                    {
                        p.EnterCharacterMode();
                    }
                    else
                    {
                        p.RequestReturnToBuilding();
                    }
                    return;
                }
                if (id == "panel")
                {
                    TogglePanel();
                    return;
                }
                if (id.StartsWith("tab:", StringComparison.Ordinal))
                {
                    ActiveTab = id.Substring(4);
                    m_NextPublish = 0f;
                    return;
                }
                foreach (IPanelTab t in m_Tabs)
                {
                    if (id.StartsWith(t.Id + ":", StringComparison.Ordinal) && t.HandleAction(id))
                    {
                        m_NextPublish = 0f;
                        return;
                    }
                }
                DtcLog.Warn("UI action not handled: " + id);
            }
            catch (Exception ex)
            {
                DtcLog.Error("UI action failed: " + id, ex);
            }
        }

        public string DebugJson() => BuildHud().ToJson(false);

        /// <summary>Lets the test bridge exercise the same action path the UI uses.</summary>
        public void InvokeAction(string id) => OnAction(id);
    }

    /// <summary>Controls and help (always available).</summary>
    public sealed class HelpTab : IPanelTab
    {
        public string Id => "help";
        public string Label => Loc.Locale == Loc.Portuguese ? "Ajuda" : "Help";
        public int Order => 100;

        public void Build(List<PanelRow> rows, out string header, out string footer)
        {
            PlayerSystem p = PlayerSystem.Instance;
            bool pt = Loc.Locale == Loc.Portuguese;
            header = pt ? "Controles" : "Controls";
            footer = pt ? "Os controles podem ser alterados nas opções do mod." : "Controls can be changed in the mod options.";
            void Row(DtcAction a, string en, string ptText) =>
                rows.Add(new PanelRow { Id = a.ToString(), Title = pt ? ptText : en, Right = HudUISystem.KeyName(p, a) });
            Row(DtcAction.ToggleCharacterMode, "Enter / leave character mode", "Entrar / sair do modo personagem");
            Row(DtcAction.Forward, "Walk / accelerate", "Andar / acelerar");
            Row(DtcAction.Back, "Back / brake and reverse", "Voltar / frear e ré");
            Row(DtcAction.Left, "Left / steer left", "Esquerda / virar à esquerda");
            Row(DtcAction.Right, "Right / steer right", "Direita / virar à direita");
            Row(DtcAction.Run, "Run", "Correr");
            Row(DtcAction.Interact, "Enter or leave a vehicle, use a place", "Entrar ou sair do veículo, usar um local");
            Row(DtcAction.Handbrake, "Handbrake", "Freio de mão");
            Row(DtcAction.CameraView, "Change camera", "Mudar câmera");
            Row(DtcAction.Lights, "Headlights", "Faróis");
            Row(DtcAction.Recover, "Move to a safe spot", "Ir para um local seguro");
            Row(DtcAction.CareerPanel, "This panel", "Este painel");
            Row(DtcAction.FreeCursor, "Hold to free the mouse cursor", "Segure para liberar o cursor");
            rows.Add(new PanelRow { Id = "pad", Title = pt ? "Controle: analógico esquerdo, gatilhos, A entrar/sair, B freio de mão, Y câmera, X painel" : "Gamepad: left stick, triggers, A enter/exit, B handbrake, Y camera, X panel" });
        }

        public bool HandleAction(string actionId) => false;
    }
}
