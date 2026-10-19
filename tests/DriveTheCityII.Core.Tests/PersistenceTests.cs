using System;
using System.Collections.Generic;
using System.Linq;
using DriveTheCityII.Core.Persistence;
using Xunit;

namespace DriveTheCityII.Core.Tests
{
    public class JsonTests
    {
        [Fact]
        public void RoundTripPreservesValuesAndOrder()
        {
            JsonValue o = JsonValue.Object()
                .Set("b", 1)
                .Set("a", "x\"y\\z\né")
                .Set("c", 1.5)
                .Set("d", true)
                .Set("e", JsonValue.Array().Add(JsonValue.From(-7L)).Add(JsonValue.Null))
                .Set("big", 9007199254740991L);
            string text = o.ToJson(false);
            JsonValue p = JsonValue.Parse(text);
            Assert.Equal(text, p.ToJson(false));
            Assert.Equal("x\"y\\z\né", p["a"].AsString());
            Assert.Equal(1.5, p["c"].AsDouble());
            Assert.True(p["d"].AsBool());
            Assert.Equal(-7, p["e"][0].AsInt());
            Assert.Equal(JsonKind.Null, p["e"][1].Kind);
            Assert.Equal(new[] { "b", "a", "c", "d", "e", "big" }, p.Properties.Select(kv => kv.Key).ToArray());
        }

        [Theory]
        [InlineData("{")]
        [InlineData("{\"a\":}")]
        [InlineData("[1,2")]
        [InlineData("\"abc")]
        [InlineData("{} x")]
        [InlineData("nul")]
        public void InvalidJsonIsRejected(string text)
        {
            Assert.False(JsonValue.TryParse(text, out _, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void NumbersUseInvariantCulture()
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
                string t = JsonValue.Object().Set("v", 1234.5).ToJson(false);
                Assert.Equal("{\"v\":1234.5}", t);
                Assert.Equal(1234.5, JsonValue.Parse(t)["v"].AsDouble());
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prev;
            }
        }
    }

    public class EnvelopeTests
    {
        private static SaveEnvelope Sample(long rev = 3) => new SaveEnvelope
        {
            CityId = "c0ffee",
            Revision = rev,
            SavedUtc = "2026-10-19T00:00:00Z",
            ModVersion = "0.1.0",
            Career = JsonValue.Object().Set("wallet", 12345L).Set("ledger", JsonValue.Array()),
        };

        [Fact]
        public void EnvelopeRoundTrip()
        {
            string text = Sample().ToJsonText();
            EnvelopeLoadResult r = SaveEnvelope.Parse(text);
            Assert.True(r.IsOk, r.Error);
            Assert.Equal("c0ffee", r.Envelope.CityId);
            Assert.Equal(3, r.Envelope.Revision);
            Assert.Equal(12345L, r.Envelope.Career["wallet"].AsLong());
        }

        [Fact]
        public void TamperedCareerFailsChecksum()
        {
            string text = Sample().ToJsonText().Replace("12345", "99999");
            Assert.Equal(EnvelopeStatus.ChecksumMismatch, SaveEnvelope.Parse(text).Status);
        }

        [Fact]
        public void NewerSchemaIsRefused()
        {
            string text = Sample().ToJsonText().Replace("\"schema\":1", "\"schema\":99");
            Assert.Equal(EnvelopeStatus.NewerSchemaRefused, SaveEnvelope.Parse(text).Status);
        }

        [Fact]
        public void EmptyMeansNoCareer()
        {
            Assert.Equal(EnvelopeStatus.NoCareerData, SaveEnvelope.Parse(null).Status);
            Assert.Equal(EnvelopeStatus.Corrupt, SaveEnvelope.Parse("garbage").Status);
        }
    }

    public class SidecarTests
    {
        private sealed class MemFiles : ISidecarFiles
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool Exists(string path) => Files.ContainsKey(path);
            public string ReadText(string path) => Files.TryGetValue(path, out string t) ? t : null;
            public void WriteTextAtomic(string path, string text) => Files[path] = text;
            public IEnumerable<string> ListFiles(string directory, string pattern) =>
                Files.Keys.Where(k => k.StartsWith(directory + "/", StringComparison.Ordinal) && k.Substring(directory.Length + 1).StartsWith("rev-", StringComparison.Ordinal)).ToList();
            public void Delete(string path) => Files.Remove(path);
            public void EnsureDirectory(string directory) { }
        }

        [Fact]
        public void SavesExactRevisionAndFallsBackToEarlier()
        {
            var files = new MemFiles();
            var store = new SidecarStore("root", files);
            for (long r = 1; r <= 3; r++)
            {
                store.SaveRevision(new SaveEnvelope { CityId = "city", Revision = r, Career = JsonValue.Object().Set("rev", r) });
            }
            Assert.True(store.TryLoad("city", 2, false, out SaveEnvelope e2, out long got2));
            Assert.Equal(2, got2);
            Assert.Equal(2, e2.Career["rev"].AsLong());
            // Revision 5 does not exist; the newest earlier valid one is returned only when allowed.
            Assert.False(store.TryLoad("city", 5, false, out _, out _));
            Assert.True(store.TryLoad("city", 5, true, out SaveEnvelope e3, out long got3));
            Assert.Equal(3, got3);
        }

        [Fact]
        public void CorruptRevisionIsSkippedForEarlierValidOne()
        {
            var files = new MemFiles();
            var store = new SidecarStore("root", files);
            store.SaveRevision(new SaveEnvelope { CityId = "city", Revision = 1, Career = JsonValue.Object().Set("ok", true) });
            store.SaveRevision(new SaveEnvelope { CityId = "city", Revision = 2, Career = JsonValue.Object().Set("ok", true) });
            files.Files[store.RevisionPath("city", 2)] = "{broken";
            Assert.True(store.TryLoad("city", 2, true, out _, out long got));
            Assert.Equal(1, got);
        }

        [Fact]
        public void PrunesToKeepLimit()
        {
            var files = new MemFiles();
            var store = new SidecarStore("root", files) { KeepRevisions = 3 };
            for (long r = 1; r <= 7; r++)
            {
                store.SaveRevision(new SaveEnvelope { CityId = "c", Revision = r });
            }
            Assert.Equal(3, files.ListFiles(store.CityDirectory("c"), "rev-*.json").Count());
            Assert.True(files.Exists(store.RevisionPath("c", 7)));
            Assert.False(files.Exists(store.RevisionPath("c", 4)));
        }

        [Fact]
        public void QuarantineKeepsDataAndReason()
        {
            var files = new MemFiles();
            var store = new SidecarStore("root", files);
            string path = store.Quarantine("c", "{bad", "checksum", "20261019T000000Z");
            Assert.Equal("{bad", files.ReadText(path));
            Assert.Contains(files.Files.Keys, k => k.EndsWith(".reason.txt", StringComparison.Ordinal));
        }
    }
}
