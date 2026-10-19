using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Localization;
using Colossal.IO.AssetDatabase;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Diagnostics;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DriveTheCityII.Settings
{
    public enum SpeedUnits
    {
        Kmh = 0,
        Mph = 1,
    }

    /// <summary>
    /// Player options in the game's Options menu (SPEC-MODULES 4.9 settings; REQ-INPUT-001 remappable controls). Key
    /// bindings use the game's native rebinding UI; the chosen keys are then read directly from the keyboard by the
    /// InputRouter. Run (Shift) and free-cursor (Alt) are modifiers and stay fixed.
    /// </summary>
    [FileLocation("ModsSettings/DriveTheCityII/DriveTheCityII")]
    [SettingsUITabOrder(kGeneralTab, kControlsTab)]
    [SettingsUIGroupOrder(kGameplayGroup, kCameraGroup, kDisplayGroup, kKeysGroup, kSafetyGroup)]
    [SettingsUIShowGroupName(kGameplayGroup, kCameraGroup, kDisplayGroup, kKeysGroup, kSafetyGroup)]
    [SettingsUIKeyboardAction(nameof(DtcAction.ToggleCharacterMode), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Forward), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Back), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Left), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Right), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Interact), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.CameraView), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Handbrake), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Recover), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.CareerPanel), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Horn), ActionType.Button)]
    [SettingsUIKeyboardAction(nameof(DtcAction.Lights), ActionType.Button)]
    public sealed class DriveTheCityIISettings : ModSetting
    {
        public const string kGeneralTab = "General";
        public const string kControlsTab = "Controls";
        public const string kGameplayGroup = "Gameplay";
        public const string kCameraGroup = "Camera";
        public const string kDisplayGroup = "Display";
        public const string kKeysGroup = "Keys";
        public const string kSafetyGroup = "Safety";

        public static DriveTheCityIISettings Instance { get; private set; }

        public DriveTheCityIISettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kGeneralTab, kGameplayGroup)]
        public bool SandboxBorrowing { get; set; }

        [SettingsUISection(kGeneralTab, kGameplayGroup)]
        public bool RouteGuidance { get; set; }

        [SettingsUISection(kGeneralTab, kGameplayGroup)]
        public bool PauseCareerWhenPaused { get; set; }

        [SettingsUISlider(min = 2, max = 50, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kGeneralTab, kCameraGroup)]
        public int MouseSensitivity { get; set; }

        [SettingsUISection(kGeneralTab, kCameraGroup)]
        public bool InvertY { get; set; }

        [SettingsUISlider(min = 50, max = 90, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kGeneralTab, kCameraGroup)]
        public int FieldOfView { get; set; }

        [SettingsUISection(kGeneralTab, kDisplayGroup)]
        public SpeedUnits Units { get; set; }

        [SettingsUISection(kGeneralTab, kSafetyGroup)]
        public bool SafeDisable { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.F8, nameof(DtcAction.ToggleCharacterMode))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding ToggleKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.W, nameof(DtcAction.Forward))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding ForwardKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.S, nameof(DtcAction.Back))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding BackKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.A, nameof(DtcAction.Left))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding LeftKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.D, nameof(DtcAction.Right))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding RightKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.E, nameof(DtcAction.Interact))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding InteractKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.V, nameof(DtcAction.CameraView))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding CameraKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Space, nameof(DtcAction.Handbrake))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding HandbrakeKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Backspace, nameof(DtcAction.Recover))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding RecoverKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Tab, nameof(DtcAction.CareerPanel))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding PanelKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.H, nameof(DtcAction.Horn))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding HornKey { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.L, nameof(DtcAction.Lights))]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public ProxyBinding LightsKey { get; set; }

        [SettingsUIButton]
        [SettingsUISection(kControlsTab, kKeysGroup)]
        public bool ResetBindings
        {
            set
            {
                ResetKeyBindings();
                ApplyKeysToRouter();
            }
        }

        public override void SetDefaults()
        {
            SandboxBorrowing = true;
            RouteGuidance = true;
            PauseCareerWhenPaused = true;
            MouseSensitivity = 12;
            InvertY = false;
            FieldOfView = 62;
            Units = SpeedUnits.Kmh;
            SafeDisable = false;
        }

        public override void Apply()
        {
            base.Apply();
            ApplyKeysToRouter();
        }

        /// <summary>Creates, registers (options UI + key bindings) and loads the settings. Call once from OnLoad.</summary>
        public static DriveTheCityIISettings Create(IMod mod)
        {
            var s = new DriveTheCityIISettings(mod);
            Instance = s;
            try
            {
                SettingsLocale.Register(s);
                s.RegisterInOptionsUI();
                s.RegisterKeyBindings();
                AssetDatabase.global.LoadSettings(nameof(DriveTheCityIISettings), s, new DriveTheCityIISettings(mod));
                s.ApplyKeysToRouter();
                DtcLog.Info("Settings loaded");
            }
            catch (Exception ex)
            {
                DtcLog.Error("Settings could not be registered; defaults are used", ex);
            }
            return s;
        }

        private static readonly (DtcAction action, Func<DriveTheCityIISettings, ProxyBinding> get)[] s_Map =
        {
            (DtcAction.ToggleCharacterMode, s => s.ToggleKey),
            (DtcAction.Forward, s => s.ForwardKey),
            (DtcAction.Back, s => s.BackKey),
            (DtcAction.Left, s => s.LeftKey),
            (DtcAction.Right, s => s.RightKey),
            (DtcAction.Interact, s => s.InteractKey),
            (DtcAction.CameraView, s => s.CameraKey),
            (DtcAction.Handbrake, s => s.HandbrakeKey),
            (DtcAction.Recover, s => s.RecoverKey),
            (DtcAction.CareerPanel, s => s.PanelKey),
            (DtcAction.Horn, s => s.HornKey),
            (DtcAction.Lights, s => s.LightsKey),
        };

        /// <summary>Resolves each binding's control path to a physical key for the InputRouter.</summary>
        public void ApplyKeysToRouter()
        {
            InputRouter router = Player.PlayerSystem.Instance?.Input;
            if (router == null)
            {
                return;
            }
            router.MouseSensitivity = Math.Max(2, MouseSensitivity) / 100f;
            router.InvertY = InvertY;
            foreach ((DtcAction action, Func<DriveTheCityIISettings, ProxyBinding> get) in s_Map)
            {
                try
                {
                    string path = get(this).path;
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    if (InputSystem.FindControl(path) is KeyControl kc)
                    {
                        router.Keys[action] = kc.keyCode;
                    }
                }
                catch (Exception ex)
                {
                    DtcLog.Warn($"Binding for {action} could not be resolved: {ex.Message}");
                }
            }
        }
    }

    /// <summary>Option labels in English and Brazilian Portuguese for the game's Options screen.</summary>
    public static class SettingsLocale
    {
        public static void Register(DriveTheCityIISettings s)
        {
            var lm = Game.SceneFlow.GameManager.instance?.localizationManager;
            if (lm == null)
            {
                return;
            }
            lm.AddSource("en-US", new MemorySource(Build(s, pt: false)));
            lm.AddSource("pt-BR", new MemorySource(Build(s, pt: true)));
        }

        private static Dictionary<string, string> Build(DriveTheCityIISettings s, bool pt)
        {
            var d = new Dictionary<string, string>
            {
                { s.GetSettingsLocaleID(), "Drive the City II" },
                { s.GetOptionTabLocaleID(DriveTheCityIISettings.kGeneralTab), pt ? "Geral" : "General" },
                { s.GetOptionTabLocaleID(DriveTheCityIISettings.kControlsTab), pt ? "Controles" : "Controls" },
                { s.GetOptionGroupLocaleID(DriveTheCityIISettings.kGameplayGroup), pt ? "Jogabilidade" : "Gameplay" },
                { s.GetOptionGroupLocaleID(DriveTheCityIISettings.kCameraGroup), pt ? "Câmera" : "Camera" },
                { s.GetOptionGroupLocaleID(DriveTheCityIISettings.kDisplayGroup), pt ? "Exibição" : "Display" },
                { s.GetOptionGroupLocaleID(DriveTheCityIISettings.kKeysGroup), pt ? "Teclas" : "Keys" },
                { s.GetOptionGroupLocaleID(DriveTheCityIISettings.kSafetyGroup), pt ? "Segurança" : "Safety" },
                { s.GetBindingMapLocaleID(), "Drive the City II" },
            };
            void Opt(string prop, string en, string ptText, string enDesc, string ptDesc)
            {
                d[s.GetOptionLabelLocaleID(prop)] = pt ? ptText : en;
                d[s.GetOptionDescLocaleID(prop)] = pt ? ptDesc : enDesc;
            }
            Opt(nameof(DriveTheCityIISettings.SandboxBorrowing), "Sandbox: borrow traffic cars", "Sandbox: pegar carros do trânsito",
                "Lets you enter private cars from city traffic for free driving. They return to traffic when you leave. Career jobs always use your own vehicles.",
                "Permite entrar em carros particulares do trânsito para dirigir livremente. Eles voltam ao trânsito quando você sai. Trabalhos da carreira usam sempre seus veículos.");
            Opt(nameof(DriveTheCityIISettings.RouteGuidance), "Route guidance line", "Linha de navegação",
                "Draws the route to your current objective on the road.", "Desenha na via a rota até o objetivo atual.");
            Opt(nameof(DriveTheCityIISettings.PauseCareerWhenPaused), "Pause career timers with the game", "Pausar cronômetros da carreira com o jogo",
                "Mission deadlines and company time stop while the game is paused.", "Prazos e tempo da empresa param enquanto o jogo está pausado.");
            Opt(nameof(DriveTheCityIISettings.MouseSensitivity), "Mouse look sensitivity", "Sensibilidade do mouse",
                "How fast the camera turns with the mouse in character mode.", "Velocidade com que a câmera gira com o mouse no modo personagem.");
            Opt(nameof(DriveTheCityIISettings.InvertY), "Invert vertical look", "Inverter olhar vertical", "Inverts mouse and stick up/down look.", "Inverte o olhar para cima/baixo.");
            Opt(nameof(DriveTheCityIISettings.FieldOfView), "Field of view", "Campo de visão", "Camera field of view in degrees.", "Campo de visão da câmera em graus.");
            Opt(nameof(DriveTheCityIISettings.Units), "Speed units", "Unidade de velocidade", "Speed shown in km/h or mph.", "Velocidade em km/h ou mph.");
            Opt(nameof(DriveTheCityIISettings.SafeDisable), "Safe-disable (stop the mod for this session)", "Desativação segura (parar o mod nesta sessão)",
                "Returns to city building, keeps your career data, and stops all Drive the City II gameplay until turned off.",
                "Volta à construção da cidade, mantém os dados da carreira e para toda a jogabilidade do Drive the City II até ser desligado.");
            Opt(nameof(DriveTheCityIISettings.ResetBindings), "Reset key bindings", "Restaurar teclas", "Restores the default keys.", "Restaura as teclas padrão.");
            d[s.GetEnumValueLocaleID(SpeedUnits.Kmh)] = "km/h";
            d[s.GetEnumValueLocaleID(SpeedUnits.Mph)] = "mph";
            (string prop, DtcAction a, string en, string ptText)[] keys =
            {
                (nameof(DriveTheCityIISettings.ToggleKey), DtcAction.ToggleCharacterMode, "Enter / leave character mode", "Entrar / sair do modo personagem"),
                (nameof(DriveTheCityIISettings.ForwardKey), DtcAction.Forward, "Walk forward / accelerate", "Andar / acelerar"),
                (nameof(DriveTheCityIISettings.BackKey), DtcAction.Back, "Walk back / brake and reverse", "Voltar / frear e ré"),
                (nameof(DriveTheCityIISettings.LeftKey), DtcAction.Left, "Left / steer left", "Esquerda / virar à esquerda"),
                (nameof(DriveTheCityIISettings.RightKey), DtcAction.Right, "Right / steer right", "Direita / virar à direita"),
                (nameof(DriveTheCityIISettings.InteractKey), DtcAction.Interact, "Use vehicle / place", "Usar veículo / local"),
                (nameof(DriveTheCityIISettings.CameraKey), DtcAction.CameraView, "Change camera", "Mudar câmera"),
                (nameof(DriveTheCityIISettings.HandbrakeKey), DtcAction.Handbrake, "Handbrake", "Freio de mão"),
                (nameof(DriveTheCityIISettings.RecoverKey), DtcAction.Recover, "Move to a safe spot", "Ir para um local seguro"),
                (nameof(DriveTheCityIISettings.PanelKey), DtcAction.CareerPanel, "Jobs, garage and company panel", "Painel de trabalhos, garagem e empresa"),
                (nameof(DriveTheCityIISettings.HornKey), DtcAction.Horn, "Horn", "Buzina"),
                (nameof(DriveTheCityIISettings.LightsKey), DtcAction.Lights, "Headlights", "Faróis"),
            };
            foreach ((string prop, DtcAction a, string en, string ptText) in keys)
            {
                d[s.GetOptionLabelLocaleID(prop)] = pt ? ptText : en;
                d[s.GetOptionDescLocaleID(prop)] = pt ? ptText : en;
                d[s.GetBindingKeyLocaleID(a.ToString())] = pt ? ptText : en;
                d[s.GetBindingKeyHintLocaleID(a.ToString())] = pt ? ptText : en;
            }
            return d;
        }
    }
}
