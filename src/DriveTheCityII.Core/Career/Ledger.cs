using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Ledger entry types of SPEC-ECON section 4 (company lines use the company ledger).</summary>
    public enum LedgerEntryType
    {
        InitialBalance = 0,
        MissionSettlement = 1,
        RentalCharge = 2,
        RentalDeposit = 3,
        DepositRefund = 4,
        VehiclePurchase = 5,
        VehicleSale = 6,
        Fuel = 7,
        Maintenance = 8,
        Upgrade = 9,
        RecoveryService = 10,
        DeferredChargeRepayment = 11,
        GarageExpansion = 12,
        CompanyFounding = 13,
        CompanyTransfer = 14,
        ChallengeReward = 15,
        Correction = 16,
        CompanyIncome = 17,
        CompanyCost = 18,
    }

    public enum LedgerAppendStatus
    {
        Appended = 0,

        /// <summary>Duplicate TransactionId: rejected, nothing changed (SPEC-ECON section 4 invariant 1).</summary>
        AlreadyApplied = 1,

        /// <summary>A second settlement for the same MissionId (SPEC-MISSION section 8 invariant b).</summary>
        AlreadySettled = 2,

        /// <summary>The entry would make the balance negative.</summary>
        InsufficientFunds = 3,

        Invalid = 4,
    }

    /// <summary>One itemized line of an entry's breakdown.</summary>
    public sealed class LedgerLine
    {
        public string Key;
        public string Text;
        public long AmountCents;

        public LedgerLine()
        {
        }

        public LedgerLine(string key, string text, long amountCents)
        {
            Key = key;
            Text = text;
            AmountCents = amountCents;
        }
    }

    /// <summary>An immutable-by-convention ledger entry. Entries are never edited or deleted.</summary>
    public sealed class LedgerEntry
    {
        public string TransactionId;
        public LedgerEntryType Type;
        public long AmountCents;
        public long BalanceAfterCents;

        /// <summary>Career-active time of the entry.</summary>
        public double Time;

        /// <summary>Source reference: MissionId, VehicleRecordId, RentalId, CompanyId plus period, ...</summary>
        public string SourceRef;

        public string DescriptionKey;
        public string Text;
        public List<LedgerLine> Lines = new List<LedgerLine>();

        internal JsonValue ToJson()
        {
            JsonValue o = JsonValue.Object()
                .Set("tx", TransactionId)
                .Set("type", Type.ToString())
                .Set("amount", AmountCents)
                .Set("balanceAfter", BalanceAfterCents)
                .Set("time", Time)
                .Set("source", SourceRef ?? string.Empty)
                .Set("key", DescriptionKey ?? string.Empty)
                .Set("text", Text ?? string.Empty);
            JsonValue lines = JsonValue.Array();
            foreach (LedgerLine l in Lines)
            {
                lines.Add(JsonValue.Object().Set("key", l.Key ?? string.Empty).Set("text", l.Text ?? string.Empty).Set("amount", l.AmountCents));
            }
            o.Set("lines", lines);
            return o;
        }

        internal static LedgerEntry FromJson(JsonValue o, string path, JsonRead r)
        {
            var e = new LedgerEntry
            {
                TransactionId = r.Str(o, "tx", path),
                Type = r.Enum<LedgerEntryType>(o, "type", path),
                AmountCents = r.Long(o, "amount", path),
                BalanceAfterCents = r.Long(o, "balanceAfter", path),
                Time = r.Num(o, "time", path, 0),
                SourceRef = r.Str(o, "source", path, false) ?? string.Empty,
                DescriptionKey = r.Str(o, "key", path, false) ?? string.Empty,
                Text = r.Str(o, "text", path, false) ?? string.Empty,
            };
            int i = 0;
            foreach (JsonValue l in r.Arr(o, "lines", path).Items)
            {
                string lp = path + ".lines[" + i++ + "]";
                if (l.Kind != JsonKind.Object)
                {
                    r.Error(lp, "expected object");
                    continue;
                }
                e.Lines.Add(new LedgerLine(r.Str(l, "key", lp, false) ?? string.Empty, r.Str(l, "text", lp, false) ?? string.Empty, r.Long(l, "amount", lp)));
            }
            return e;
        }
    }

    /// <summary>
    /// Append-only ledger (REQ-ECON-002). The balance is a derived cache of the entries; a duplicate TransactionId is a
    /// no-op; at most one <see cref="LedgerEntryType.MissionSettlement"/> exists per MissionId (stored in
    /// <see cref="LedgerEntry.SourceRef"/>); the balance never goes negative. Used for both the wallet and the company
    /// account (SPEC-COMPANY section 3.2).
    /// </summary>
    public sealed class Ledger
    {
        private readonly List<LedgerEntry> m_Entries = new List<LedgerEntry>();
        private readonly HashSet<string> m_Ids = new HashSet<string>();
        private readonly HashSet<string> m_SettledMissions = new HashSet<string>();

        public long Balance { get; private set; }

        public IReadOnlyList<LedgerEntry> Entries => m_Entries;

        public int Count => m_Entries.Count;

        public bool Contains(string transactionId) => transactionId != null && m_Ids.Contains(transactionId);

        public bool HasSettlement(string missionId) => missionId != null && m_SettledMissions.Contains(missionId);

        public LedgerEntry Find(string transactionId)
        {
            if (!Contains(transactionId))
            {
                return null;
            }
            for (int i = m_Entries.Count - 1; i >= 0; i--)
            {
                if (m_Entries[i].TransactionId == transactionId)
                {
                    return m_Entries[i];
                }
            }
            return null;
        }

        /// <summary>Appends an entry. On success <see cref="LedgerEntry.BalanceAfterCents"/> is set.</summary>
        public LedgerAppendStatus Append(LedgerEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.TransactionId))
            {
                return LedgerAppendStatus.Invalid;
            }
            if (m_Ids.Contains(e.TransactionId))
            {
                return LedgerAppendStatus.AlreadyApplied;
            }
            if (e.Type == LedgerEntryType.MissionSettlement && m_SettledMissions.Contains(e.SourceRef ?? string.Empty))
            {
                return LedgerAppendStatus.AlreadySettled;
            }
            if (Balance + e.AmountCents < 0)
            {
                return LedgerAppendStatus.InsufficientFunds;
            }
            Balance += e.AmountCents;
            e.BalanceAfterCents = Balance;
            m_Entries.Add(e);
            m_Ids.Add(e.TransactionId);
            if (e.Type == LedgerEntryType.MissionSettlement)
            {
                m_SettledMissions.Add(e.SourceRef ?? string.Empty);
            }
            return LedgerAppendStatus.Appended;
        }

        /// <summary>Convenience: builds and appends an entry.</summary>
        public LedgerAppendStatus Append(string txId, LedgerEntryType type, long amountCents, double time, string sourceRef, string key, string text, List<LedgerLine> lines = null)
        {
            return Append(new LedgerEntry
            {
                TransactionId = txId,
                Type = type,
                AmountCents = amountCents,
                Time = time,
                SourceRef = sourceRef ?? string.Empty,
                DescriptionKey = key ?? string.Empty,
                Text = text ?? string.Empty,
                Lines = lines ?? new List<LedgerLine>(),
            });
        }

        internal JsonValue ToJson()
        {
            JsonValue a = JsonValue.Array();
            foreach (LedgerEntry e in m_Entries)
            {
                a.Add(e.ToJson());
            }
            return a;
        }

        /// <summary>
        /// Rebuilds a ledger from persisted entries and validates every invariant: unique TransactionIds, one settlement
        /// per mission, a consistent balance-after chain, no negative running balance, and the cached balance.
        /// </summary>
        internal static Ledger Restore(JsonValue entries, long cachedBalance, string path, JsonRead r)
        {
            var ledger = new Ledger();
            int i = 0;
            foreach (JsonValue item in entries.Items)
            {
                string p = path + "[" + i++ + "]";
                if (item.Kind != JsonKind.Object)
                {
                    r.Error(p, "expected object");
                    continue;
                }
                LedgerEntry e = LedgerEntry.FromJson(item, p, r);
                if (string.IsNullOrEmpty(e.TransactionId))
                {
                    continue;
                }
                long expectedAfter = e.BalanceAfterCents;
                LedgerAppendStatus s = ledger.Append(e);
                switch (s)
                {
                    case LedgerAppendStatus.Appended:
                        if (e.BalanceAfterCents != expectedAfter)
                        {
                            r.Error(p, "balanceAfter chain mismatch");
                        }
                        break;
                    case LedgerAppendStatus.AlreadyApplied:
                        r.Error(p, "duplicate TransactionId '" + e.TransactionId + "'");
                        break;
                    case LedgerAppendStatus.AlreadySettled:
                        r.Error(p, "second settlement for mission '" + e.SourceRef + "'");
                        break;
                    case LedgerAppendStatus.InsufficientFunds:
                        r.Error(p, "running balance would be negative");
                        break;
                    default:
                        r.Error(p, "invalid entry");
                        break;
                }
            }
            if (ledger.Balance != cachedBalance)
            {
                r.Error(path, "cached balance " + cachedBalance + " does not equal the ledger sum " + ledger.Balance);
            }
            return ledger;
        }
    }
}
