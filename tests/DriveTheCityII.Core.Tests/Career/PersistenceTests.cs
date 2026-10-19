using System.Linq;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Persistence;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class PersistenceTests
    {
        private static CareerSim NewTaxiSim()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.Walk(1);
            sim.RentBasicAndBoard();
            return sim;
        }

        [Fact]
        public void SaveLoadAtEveryTaxiStageResumesAndPaysExactlyOnceLikeAnUninterruptedRide()
        {
            CareerSim reference = NewTaxiSim();
            string refId = TaxiTests.RunTaxi(reference);
            long expected = reference.Svc.WalletBalanceCents;

            CareerSim sim = NewTaxiSim();
            sim.AfterStep = s => s.Reload();
            string id = TaxiTests.RunTaxi(sim);
            sim.AfterStep = null;

            // Reloaded after: accept (Accepted), arrival at pickup (servicing), boarded, arrival at drop-off, settled.
            Assert.Equal(5, sim.Reloads);
            Assert.Equal(refId, id);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Equal(1, sim.CountSettlements(id));
            Assert.Equal(expected, sim.Svc.WalletBalanceCents);
            Assert.Equal(sim.LedgerSum(), sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void ReloadDuringBoardingRestartsTheServicingStep()
        {
            CareerSim sim = NewTaxiSim();
            var offer = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now)[0];
            sim.Svc.Accept(offer.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(3);
            Assert.Equal(LegState.Servicing, sim.Svc.GetActiveMission().CurrentLegState);
            sim.Reload();
            // Servicing progress is transient (SPEC-MISSION 9): the persisted leg is Approaching or restarts.
            sim.Stop(1);
            Assert.Equal(PassengerState.Waiting, sim.Svc.GetActiveMission().PassengerState);
            sim.Stop(5);
            Assert.Equal(PassengerState.Aboard, sim.Svc.GetActiveMission().PassengerState);
        }

        [Fact]
        public void RepeatedTicksSettleCallsAndOperationIdsNeverPayTwice()
        {
            CareerSim sim = NewTaxiSim();
            string id = TaxiTests.RunTaxi(sim);
            long balance = sim.Svc.WalletBalanceCents;
            int count = sim.Svc.GetLedger().Count;

            for (int i = 0; i < 20; i++)
            {
                sim.Stop(1); // replayed arrival at the drop-off
            }
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.Settle(id, sim.Now).Code);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.Settle(id, sim.Now).Code);
            sim.Reload();
            sim.Stop(3);
            Assert.Equal(balance, sim.Svc.WalletBalanceCents);
            Assert.Equal(count, sim.Svc.GetLedger().Count);
            Assert.Equal(1, sim.CountSettlements(id));
        }

        [Fact]
        public void RestoringAnOlderSaveRestoresItsBalanceExactlyAndSettlesInItsOwnTimeline()
        {
            CareerSim sim = NewTaxiSim();
            var offer = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now)[0];
            string id = sim.Svc.Accept(offer.Id, sim.VehicleId, sim.Now).Id;
            sim.DriveTo("res-a");
            sim.Stop(5);
            string saveA = sim.Svc.ToJsonText();
            long balanceA = sim.Svc.WalletBalanceCents;
            double clockA = sim.Svc.ClockSeconds;

            sim.DriveTo("off-b");
            sim.Stop(4);
            long balanceB = sim.Svc.WalletBalanceCents;
            Assert.True(balanceB > balanceA);

            CareerService restored = CareerService.FromJsonText(saveA, sim.World, out CareerLoadResult result);
            Assert.True(result.IsOk, result.ToString());
            Assert.Equal(balanceA, restored.WalletBalanceCents);
            Assert.Equal(clockA, restored.ClockSeconds);
            Assert.Equal(id, restored.GetActiveMission().MissionId);
            Assert.Equal(PassengerState.Aboard, restored.GetActiveMission().PassengerState);
            Assert.Equal(0, restored.GetLedger().Count(e => e.Type == LedgerEntryType.MissionSettlement));

            // Continue in A's timeline: the same mission settles once there.
            sim.Svc = restored;
            sim.Now = clockA;
            sim.Stop(1);
            sim.Stop(4);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Equal(1, sim.CountSettlements(id));
            Assert.Equal(balanceB, sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void DestinationDemolishedWhileSavedResolvesSafelyAfterLoadWithNoPayout()
        {
            CareerSim sim = NewTaxiSim();
            var offer = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now)[0];
            string id = sim.Svc.Accept(offer.Id, sim.VehicleId, sim.Now).Id;
            sim.DriveTo(new MapPoint(100, 0, 0));
            string save = sim.Svc.ToJsonText();

            sim.World.Remove("off-b");
            CareerService loaded = CareerService.FromJsonText(save, sim.World, out CareerLoadResult result);
            Assert.True(result.IsOk, result.ToString());
            Assert.NotNull(loaded.GetActiveMission());
            sim.Svc = loaded;
            sim.Building(1);
            sim.Drive(0.5, 0, 0);

            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal("MS-" + id + "-CANCELLED", settle.TransactionId);
            Assert.Equal(0, settle.AmountCents);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
        }

        // ------------------------------------------------------------------ validation (quarantine)

        private static JsonValue RichSave()
        {
            CareerSim sim = NewTaxiSim();
            var offer = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now)[0];
            sim.Svc.Accept(offer.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            return sim.Svc.ToJson();
        }

        private static CareerLoadResult LoadMutated(System.Action<JsonValue> mutate)
        {
            JsonValue json = RichSave();
            mutate(json);
            string text = json.ToJson(false);
            CareerService svc = CareerService.FromJsonText(text, TestCities.Taxi(), out CareerLoadResult result);
            if (!result.IsOk)
            {
                Assert.Null(svc);
            }
            return result;
        }

        [Fact]
        public void ValidSaveLoads()
        {
            CareerLoadResult r = LoadMutated(j => { });
            Assert.Equal(CareerLoadStatus.Loaded, r.Status);
        }

        [Fact]
        public void WalletNotEqualToLedgerSumIsQuarantined()
        {
            CareerLoadResult r = LoadMutated(j => j["wallet"].Set("balance", j["wallet"]["balance"].AsLong() + 100));
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
            Assert.Contains(r.Reasons, s => s.Contains("ledger sum"));
        }

        [Fact]
        public void DuplicateTransactionIdIsQuarantined()
        {
            CareerLoadResult r = LoadMutated(j =>
            {
                JsonValue ledger = j["wallet"]["ledger"];
                JsonValue copy = JsonValue.Parse(ledger[0].ToJson(false));
                long amount = copy["amount"].AsLong();
                long balance = j["wallet"]["balance"].AsLong();
                copy.Set("balanceAfter", balance + amount);
                ledger.Add(copy);
                j["wallet"].Set("balance", balance + amount);
            });
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
            Assert.Contains(r.Reasons, s => s.Contains("duplicate TransactionId"));
        }

        [Fact]
        public void ActiveMissionWithoutLegsIsQuarantined()
        {
            CareerLoadResult r = LoadMutated(j => j["mission"].Set("legs", JsonValue.Array()));
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
        }

        [Fact]
        public void ActiveMissionWithMissingVehicleIsQuarantined()
        {
            CareerLoadResult r = LoadMutated(j => j["mission"].Set("vehicle", "V-nope"));
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
        }

        [Fact]
        public void NonFiniteOrOutOfRangeNumbersAreQuarantined()
        {
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j.Set("clock", JsonValue.Null)).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["vehicles"][0].Set("fuel", 1e300)).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["progression"].Set("reputation", 400)).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["mission"]["legs"][0]["place"]["access"].Set("x", "far")).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["wallet"]["ledger"][0].Set("amount", 0.5)).Status);
        }

        [Fact]
        public void UnknownEnumValuesAndBadGeneratorStateAreQuarantined()
        {
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["mission"].Set("state", "Teleported")).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j["mission"].Set("state", "5")).Status);
            Assert.Equal(CareerLoadStatus.Quarantined, LoadMutated(j => j.Set("rng", "zz")).Status);
        }

        [Fact]
        public void SettledButStillOpenMissionIsQuarantined()
        {
            CareerSim sim = NewTaxiSim();
            string id = TaxiTests.RunTaxi(sim);
            JsonValue done = sim.Svc.ToJson();
            JsonValue open = RichSave();
            // Graft an open mission whose id is already settled in this ledger.
            JsonValue mission = open["mission"];
            mission.Set("id", id);
            mission.Set("vehicle", done["vehicles"][0]["id"].AsString());
            done.Set("mission", mission);
            CareerService.FromJson(done, out CareerLoadResult r);
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
            Assert.Contains(r.Reasons, s => s.Contains("already settled"));
        }

        [Fact]
        public void NewerSchemaIsRefusedAndGarbageIsQuarantined()
        {
            JsonValue json = RichSave();
            json.Set("schemaVersion", CareerService.SchemaVersion + 1);
            Assert.Null(CareerService.FromJson(json, out CareerLoadResult r));
            Assert.Equal(CareerLoadStatus.NewerSchemaRefused, r.Status);

            Assert.Null(CareerService.FromJsonText("{\"schemaVersion\":1,", null, out r));
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);

            Assert.Null(CareerService.FromJson(JsonValue.Null, out r));
            Assert.Equal(CareerLoadStatus.NoCareerData, r.Status);

            json.Set("schemaVersion", 0);
            Assert.Null(CareerService.FromJson(json, out r));
            Assert.Equal(CareerLoadStatus.Quarantined, r.Status);
        }

        [Fact]
        public void MigrationHookRejectsMissingStepsWithoutTouchingTheInput()
        {
            JsonValue json = JsonValue.Object().Set("schemaVersion", 1);
            Assert.True(CareerMigrations.TryMigrate(json, 1, 1, out JsonValue same, out _));
            Assert.Same(json, same);
            Assert.False(CareerMigrations.TryMigrate(json, 1, 2, out _, out string error));
            Assert.Contains("no migration", error);
        }

        // ------------------------------------------------------------------ determinism

        [Fact]
        public void JsonRoundTripIsByteIdenticalAndDeterministicAcrossRuns()
        {
            string a = BuildRichCareer().Svc.ToJsonText();
            string b = BuildRichCareer().Svc.ToJsonText();
            Assert.Equal(a, b);

            CareerService parsed = CareerService.FromJsonText(a, TestCities.Taxi(), out CareerLoadResult r);
            Assert.True(r.IsOk, r.ToString());
            Assert.Equal(a, parsed.ToJsonText());

            string indented = parsed.ToJsonText(true);
            CareerService fromIndented = CareerService.FromJsonText(indented, TestCities.Taxi(), out r);
            Assert.True(r.IsOk, r.ToString());
            Assert.Equal(a, fromIndented.ToJsonText());
        }

        private static CareerSim BuildRichCareer()
        {
            CareerSim sim = NewTaxiSim();
            TaxiTests.RunTaxi(sim);
            sim.Svc.DebugGrant(400000, sim.Now);
            sim.Svc.DebugSeedXp(CareerKind.Taxi, 2000);
            sim.Svc.DebugSeedXp(CareerKind.Delivery, 2000);
            sim.Svc.DebugSeedXp(CareerKind.Freight, 2000);
            sim.Svc.DebugSetReputation(70);
            Assert.True(sim.Svc.BuyVehicle("car.used", "op-car", sim.Now).IsOk);
            Assert.True(sim.Svc.FoundCompany("Round Trip Ltd", null, "op-co", sim.Now).IsOk);
            Assert.True(sim.Svc.TransferToCompany(50000, "op-t", sim.Now).IsOk);
            Assert.True(sim.Svc.SetHome("res-a", sim.Now).IsOk);
            Assert.True(sim.Svc.DefineChallenge(ChallengeType.TimeTrial, "TT", new[] { "res-a", "off-b" }, VehicleClass.Car, 0, sim.Now).IsOk);
            var offer = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now)[0];
            sim.Svc.Accept(offer.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.Stop(CareerBalance.CompanyPeriodSeconds * 2, 30);
            return sim;
        }
    }
}
