using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DriveTheCityII.Core.Text;
using Xunit;

namespace DriveTheCityII.Core.Tests
{
    /// <summary>
    /// PT-BR career messages (LocCareerMessages). Reads the Loc tables directly and never changes Loc.Locale, so it
    /// cannot interfere with other tests.
    /// </summary>
    public class LocCareerMessagesTests
    {
        private static readonly Regex s_Placeholder = new Regex(@"\{(\d+)\}", RegexOptions.CultureInvariant);

        /// <summary>Keys whose call sites pass CareerEvent.Args, with the number of Args passed.</summary>
        private static readonly Dictionary<string, int> s_ArgCounts = new Dictionary<string, int>
        {
            { "DTC.Mission.Leg.Servicing", 1 },
            { "DTC.Mission.Leg.Done", 1 },
            { "DTC.Mission.Leg.Interrupted", 1 },
            { "DTC.Taxi.Boarded", 1 },
            { "DTC.Taxi.Substituted", 2 },
            { "DTC.Delivery.Skipped", 1 },
            { "DTC.Freight.Skipped", 1 },
            { "DTC.Freight.Substituted", 2 },
            { "DTC.Bus.StopMissed", 1 },
            { "DTC.Bus.StopSkipped", 1 },
            { "DTC.Bus.Invalidated.LineRemoved", 1 },
            { "DTC.Bus.Invalidated.LineChanged", 1 },
            { "DTC.Bus.LineModified", 3 },
            { "DTC.Home.Demolished", 1 },
            { "DTC.Home.DemolishedNoBase", 1 },
            { "DTC.Challenge.RouteChanged", 1 },
            { "DTC.Challenge.Armed", 1 },
            { "DTC.Challenge.Started", 1 },
            { "DTC.Challenge.Failed", 1 },
            { "DTC.Challenge.Aborted", 1 },
            { "DTC.Challenge.Void.RouteChanged", 1 },
            { "DTC.Challenge.Void.Building", 1 },
            { "DTC.Challenge.Void.Exit", 1 },
            { "DTC.Challenge.Void.Recovery", 1 },
        };

        /// <summary>Keys whose English text embeds values that are not (or not faithfully) passed as Args.</summary>
        private static readonly string[] s_EnglishOnly =
        {
            "DTC.Taxi.Offer.Title", "DTC.Bus.Offer.Title", "DTC.Delivery.Offer.Courier", "DTC.Freight.Offer.General",
            "DTC.Mission.Settled", "DTC.Company.Report", "DTC.Challenge.Completed", "DTC.Company.Resigned",
            "DTC.Challenge.Unavailable", "DTC.Result.InsufficientFunds", "DTC.Mission.Locked", "DTC.Garage.Locked",
            "DTC.Mission.VehicleIneligible", "DTC.Career.InternalError", "DTC.Settle.Taxi.Tip", "DTC.Ledger.VehicleSale",
            "DTC.Ledger.RecoveryService", "DTC.Ledger.MissionSettlement",
        };

        private static IReadOnlyList<string> Keys()
        {
            Assert.NotNull(Loc.EnglishTable); // Runs Loc's static constructor, which registers the career tables.
            LocCareerMessages.Register(); // Idempotent.
            return LocCareerMessages.Keys;
        }

        private static SortedSet<int> Placeholders(string text)
        {
            var set = new SortedSet<int>();
            foreach (Match m in s_Placeholder.Matches(text))
            {
                set.Add(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            return set;
        }

        [Fact]
        public void KeysAreRegisteredOnceAndAreCareerKeys()
        {
            IReadOnlyList<string> keys = Keys();
            Assert.True(keys.Count > 150, "Expected the career message keys to be registered, got " + keys.Count);
            Assert.Equal(keys.Count, keys.Distinct().Count());
            Assert.All(keys, k => Assert.StartsWith("DTC.", k));
        }

        [Fact]
        public void EveryKeyHasEnglishAndPortugueseText()
        {
            foreach (string k in Keys())
            {
                Assert.True(Loc.EnglishTable.TryGetValue(k, out string en) && !string.IsNullOrWhiteSpace(en), "Missing English: " + k);
                Assert.True(Loc.PortugueseTable.TryGetValue(k, out string pt) && !string.IsNullOrWhiteSpace(pt), "Missing Portuguese: " + k);
            }
        }

        [Fact]
        public void PlaceholdersMatchBetweenEnglishAndPortuguese()
        {
            foreach (string k in Keys())
            {
                Assert.True(Placeholders(Loc.EnglishTable[k]).SetEquals(Placeholders(Loc.PortugueseTable[k])), "Placeholder mismatch: " + k);
            }
        }

        [Fact]
        public void TemplatesHaveNoStrayBraces()
        {
            foreach (string k in Keys())
            {
                foreach (string text in new[] { Loc.EnglishTable[k], Loc.PortugueseTable[k] })
                {
                    string rest = s_Placeholder.Replace(text, string.Empty);
                    Assert.False(rest.Contains("{") || rest.Contains("}"), "Stray brace in " + k + ": " + text);
                }
            }
        }

        [Fact]
        public void KeysShownWithoutArgsHaveNoPlaceholders()
        {
            // Results, recovery options, ledger lines and Notify calls without Args are shown unformatted, so a
            // placeholder would reach the player as a literal "{0}".
            foreach (string k in Keys().Where(k => !s_ArgCounts.ContainsKey(k)))
            {
                Assert.True(Placeholders(Loc.PortugueseTable[k]).Count == 0, "Unexpected placeholder: " + k);
            }
        }

        [Fact]
        public void KeysUsedWithArgsExistAndFormatWithThoseArgs()
        {
            IReadOnlyList<string> keys = Keys();
            foreach (KeyValuePair<string, int> kv in s_ArgCounts)
            {
                Assert.Contains(kv.Key, keys);
                object[] args = Enumerable.Range(0, kv.Value).Select(i => (object)("ARG" + i)).ToArray();
                foreach (string template in new[] { Loc.EnglishTable[kv.Key], Loc.PortugueseTable[kv.Key] })
                {
                    SortedSet<int> used = Placeholders(template);
                    Assert.True(used.Count == 0 || used.Max < kv.Value, "Placeholder beyond Args in " + kv.Key + ": " + template);
                    string formatted = string.Format(CultureInfo.InvariantCulture, template, args);
                    Assert.DoesNotContain("{", formatted);
                    foreach (int i in used)
                    {
                        Assert.Contains("ARG" + i, formatted);
                    }
                }
            }
        }

        [Fact]
        public void EnglishTemplatesReproduceTheCoreTextForSampleArgs()
        {
            Keys();
            Assert.Equal("Line 12 was modified: 2 stop(s) removed, 1 added. Passengers were moved to the nearest remaining stops.",
                string.Format(CultureInfo.InvariantCulture, Loc.EnglishTable["DTC.Bus.LineModified"], "12", "2", "1"));
            Assert.Equal("The destination 'Old' no longer exists. The passenger asked to go to 'New' instead; the fare was recalculated.",
                string.Format(CultureInfo.InvariantCulture, Loc.EnglishTable["DTC.Taxi.Substituted"], "Old", "New"));
            Assert.Equal("Stop done: Main St.", string.Format(CultureInfo.InvariantCulture, Loc.EnglishTable["DTC.Mission.Leg.Done"], "Main St"));
            Assert.Equal("A linha 12 foi modificada: 2 parada(s) removida(s), 1 adicionada(s). Os passageiros foram levados às paradas restantes mais próximas.",
                string.Format(CultureInfo.InvariantCulture, Loc.PortugueseTable["DTC.Bus.LineModified"], "12", "2", "1"));
        }

        [Fact]
        public void KeysWithUnpassedValuesStayEnglish()
        {
            Keys();
            foreach (string k in s_EnglishOnly)
            {
                Assert.False(Loc.PortugueseTable.ContainsKey(k), "Should keep its English text: " + k);
            }
        }
    }
}
