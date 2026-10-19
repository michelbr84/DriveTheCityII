using System;
using System.Collections.Generic;
using System.Globalization;

namespace DriveTheCityII.Core.Text
{
    /// <summary>
    /// Player-facing text in English and Brazilian Portuguese (REQ-L10N-001). Keys are stable; every UI string goes
    /// through <see cref="T(string, object[])"/>. Missing PT-BR entries fall back to English, missing keys to the key.
    /// </summary>
    public static class Loc
    {
        public const string English = "en-US";
        public const string Portuguese = "pt-BR";

        public static string Locale { get; set; } = English;

        private static readonly Dictionary<string, string> s_En = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_Pt = new Dictionary<string, string>(StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, string> EnglishTable => s_En;
        public static IReadOnlyDictionary<string, string> PortugueseTable => s_Pt;

        public static void SetLocaleFromGame(string gameLocaleId)
        {
            Locale = gameLocaleId != null && gameLocaleId.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? Portuguese : English;
        }

        public static void Add(string key, string en, string pt)
        {
            s_En[key] = en;
            if (pt != null)
            {
                s_Pt[key] = pt;
            }
        }

        public static string T(string key, params object[] args)
        {
            string fmt;
            if (Locale == Portuguese && s_Pt.TryGetValue(key, out string pt))
            {
                fmt = pt;
            }
            else if (!s_En.TryGetValue(key, out fmt))
            {
                fmt = key;
            }
            if (args == null || args.Length == 0)
            {
                return fmt;
            }
            try
            {
                return string.Format(Locale == Portuguese ? PtCulture : CultureInfo.InvariantCulture, fmt, args);
            }
            catch (FormatException)
            {
                return fmt;
            }
        }

        private static readonly CultureInfo PtCulture = CreatePt();

        private static CultureInfo CreatePt()
        {
            try
            {
                return CultureInfo.GetCultureInfo("pt-BR");
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        /// <summary>Formats integer cents as money in the active locale (in-game currency symbol "¢" is avoided: "$").</summary>
        public static string Money(long cents)
        {
            double v = cents / 100.0;
            return Locale == Portuguese
                ? "$ " + v.ToString("N2", PtCulture)
                : (v < 0 ? "-$" : "$") + Math.Abs(v).ToString("N2", CultureInfo.InvariantCulture);
        }

        public static string Distance(double meters)
        {
            if (meters < 1000)
            {
                return ((int)Math.Round(meters)).ToString(CultureInfo.InvariantCulture) + " m";
            }
            return (meters / 1000.0).ToString("0.0", Locale == Portuguese ? PtCulture : CultureInfo.InvariantCulture) + " km";
        }

        static Loc()
        {
            // Modes and HUD
            Add("mode.building", "City building", "Construção da cidade");
            Add("mode.onfoot", "On foot", "A pé");
            Add("mode.entering", "Entering vehicle", "Entrando no veículo");
            Add("mode.driving", "Driving", "Dirigindo");
            Add("mode.exiting", "Leaving vehicle", "Saindo do veículo");
            Add("mode.recovery", "Recovering", "Recuperando");
            Add("hud.sandbox", "SANDBOX", "SANDBOX");
            Add("hud.speed", "{0} km/h", "{0} km/h");
            Add("hud.speedMph", "{0} mph", "{0} mph");
            Add("msg.safeDisabled", "Drive the City II is safe-disabled in the mod options. Turn it off there to play again.",
                "Drive the City II está com a desativação segura ligada nas opções do mod. Desligue-a lá para jogar de novo.");
            Add("hud.fuel", "Fuel", "Combustível");
            Add("hud.condition", "Condition", "Estado");
            Add("hud.level", "Level {0}", "Nível {0}");
            Add("hud.paused", "Paused", "Pausado");
            Add("hud.toggle", "Drive the City ({0})", "Drive the City ({0})");
            Add("hud.panelHint", "{0}: jobs, garage and company", "{0}: trabalhos, garagem e empresa");
            Add("hud.devVisual", "Placeholder visuals", "Visual provisório");

            // Prompts
            Add("prompt.enter", "{0}: enter vehicle", "{0}: entrar no veículo");
            Add("prompt.exit", "{0}: get out (stop first)", "{0}: sair (pare antes)");
            Add("prompt.recover", "{0}: move to a safe spot", "{0}: ir para um local seguro");
            Add("prompt.building", "{0}: return to city building", "{0}: voltar à construção da cidade");
            Add("prompt.view", "{0}: change camera", "{0}: mudar câmera");
            Add("prompt.servicePoint", "{0}: use {1}", "{0}: usar {1}");

            // Player messages
            Add("msg.enterMode", "Character mode. WASD walk, Shift run, E use vehicle, V camera, F8 return to city building.",
                "Modo personagem. WASD andar, Shift correr, E usar veículo, V câmera, F8 voltar à construção.");
            Add("msg.conflict", "Another vehicle-control mod is active; Drive the City II stays inactive to avoid conflicts.",
                "Outro mod de controle de veículos está ativo; Drive the City II fica inativo para evitar conflitos.");
            Add("msg.notReady", "The city is not ready yet. Try again in a moment.", "A cidade ainda não está pronta. Tente novamente em instantes.");
            Add("msg.noCharacter", "No character model is available in this city.", "Nenhum modelo de personagem disponível nesta cidade.");
            Add("msg.cameraUnavailable", "Camera not available.", "Câmera indisponível.");
            Add("msg.noWalkable", "No walkable place near the camera. Move the camera over a street and try again.",
                "Nenhum lugar caminhável perto da câmera. Mova a câmera para uma rua e tente de novo.");
            Add("msg.spawnBlocked", "The spawn point is blocked. Move the camera and try again.", "O ponto de início está bloqueado. Mova a câmera e tente de novo.");
            Add("msg.cannotEnterMode", "Cannot enter character mode right now (menu or loading).", "Não é possível entrar no modo personagem agora (menu ou carregamento).");
            Add("msg.stopFirst", "Stop the vehicle before returning to city building.", "Pare o veículo antes de voltar à construção da cidade.");
            Add("msg.waitAction", "Wait until the current action finishes.", "Aguarde a ação atual terminar.");
            Add("msg.noVehicleSandbox", "No vehicle close enough. Walk up to a car door and press E.", "Nenhum veículo perto. Chegue à porta de um carro e aperte E.");
            Add("msg.noVehicleCareer", "No vehicle of yours nearby. Rent one at a service point.", "Nenhum veículo seu por perto. Alugue um num ponto de serviço.");
            Add("msg.vehicleMoving", "That vehicle is moving.", "Esse veículo está em movimento.");
            Add("msg.cannotEnterVehicle", "Cannot enter a vehicle now.", "Não é possível entrar num veículo agora.");
            Add("msg.entering", "Entering the vehicle...", "Entrando no veículo...");
            Add("msg.drivingSandbox", "Driving (sandbox). W/S throttle and brake/reverse, A/D steer, Space handbrake, E exit.",
                "Dirigindo (sandbox). W/S acelerar e frear/ré, A/D direção, Espaço freio de mão, E sair.");
            Add("msg.driving", "Driving. W/S throttle and brake/reverse, A/D steer, Space handbrake, E exit.",
                "Dirigindo. W/S acelerar e frear/ré, A/D direção, Espaço freio de mão, E sair.");
            Add("msg.stopToExit", "Stop the vehicle to get out.", "Pare o veículo para sair.");
            Add("msg.exitBlocked", "Exit blocked on every side. Move the vehicle a little, or press Backspace to recover.",
                "Saída bloqueada em todos os lados. Mova um pouco o veículo ou aperte Backspace para recuperar.");
            Add("msg.leftBorrowed", "You left the car; it returns to city traffic.", "Você deixou o carro; ele volta ao trânsito da cidade.");
            Add("msg.parked", "Vehicle parked.", "Veículo estacionado.");
            Add("msg.recoverFailed", "Could not find a safe place; returned to city building.", "Não foi possível achar um lugar seguro; voltou à construção da cidade.");
            Add("msg.recoveryLoop", "Repeated problems placing you safely; returned to city building.", "Problemas repetidos ao posicionar você; voltou à construção da cidade.");
            Add("msg.recovering", "Recovering: {0}", "Recuperando: {0}");
            Add("msg.vehicleToRoad", "Vehicle moved to the nearest road.", "Veículo movido para a via mais próxima.");
            Add("msg.toSidewalk", "Moved to the nearest sidewalk.", "Movido para a calçada mais próxima.");
            Add("msg.noSafePoint", "No safe place found nearby; returned to city building.", "Nenhum lugar seguro por perto; voltou à construção da cidade.");
            Add("msg.vehicleGone", "The vehicle disappeared. Finding a safe place for you.", "O veículo sumiu. Procurando um lugar seguro para você.");
            Add("msg.stuck", "You seem stuck. Press Backspace to move to a safe spot.", "Parece que você está preso. Aperte Backspace para ir a um local seguro.");
            Add("msg.noModel", "No vehicle model of type {0} is available.", "Nenhum modelo de veículo do tipo {0} disponível.");
            Add("msg.savedSafely", "Game saved. Your character and vehicles were not written into the city save.",
                "Jogo salvo. Seu personagem e veículos não foram gravados no save da cidade.");
            Add("msg.panelClosed", "Panel closed.", "Painel fechado.");
        }
    }
}
