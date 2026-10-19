using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Control
{
    /// <summary>Player states of SPEC-CONTROL section 4.</summary>
    public enum PlayerState
    {
        Building = 0,
        OnFoot = 1,
        EnteringVehicle = 2,
        Driving = 3,
        ExitingVehicle = 4,
        Recovery = 5,
    }

    /// <summary>Overlays of SPEC-CONTROL section 3. They wrap a state without replacing it.</summary>
    [Flags]
    public enum PlayerOverlay
    {
        None = 0,
        Paused = 1,
        Menu = 2,
        Loading = 4,
        Unloading = 8,
    }

    public enum TransitionResultCode
    {
        Ok = 0,
        Illegal = 1,
        BlockedByOverlay = 2,
        SameState = 3,
    }

    public struct TransitionRecord
    {
        public double Time;
        public PlayerState From;
        public PlayerState To;
        public string TransitionId;
        public string Reason;

        public override string ToString() => $"{Time:0.00}s {TransitionId} {From}->{To} ({Reason})";
    }

    /// <summary>
    /// The ControlAuthority state machine (SPEC-CONTROL). It validates transitions against the legal table, enforces
    /// timeouts on transitional states, breaks Recovery loops, and records a bounded history for diagnostics. It owns
    /// no game resources; the Player module performs the side effects and registers their cleanup in
    /// <see cref="CleanupStack"/> scopes.
    /// </summary>
    public sealed class ControlStateMachine
    {
        public const float TransitionTimeout = 5f;
        public const float RecoveryTimeout = 12f;
        public const int RecoveryLoopCount = 3;
        public const float RecoveryLoopWindow = 60f;
        private const int HistoryCapacity = 64;

        private static readonly Dictionary<(PlayerState, PlayerState), string> s_Legal = new Dictionary<(PlayerState, PlayerState), string>
        {
            { (PlayerState.Building, PlayerState.OnFoot), "TR-01" },
            { (PlayerState.OnFoot, PlayerState.Building), "TR-02" },
            { (PlayerState.OnFoot, PlayerState.EnteringVehicle), "TR-03" },
            { (PlayerState.EnteringVehicle, PlayerState.Driving), "TR-04" },
            { (PlayerState.EnteringVehicle, PlayerState.OnFoot), "TR-05" },
            { (PlayerState.Driving, PlayerState.ExitingVehicle), "TR-06" },
            { (PlayerState.ExitingVehicle, PlayerState.OnFoot), "TR-07" },
            { (PlayerState.ExitingVehicle, PlayerState.Driving), "TR-08" },
            { (PlayerState.Driving, PlayerState.Building), "TR-09" },
            { (PlayerState.OnFoot, PlayerState.Recovery), "TR-10" },
            { (PlayerState.Driving, PlayerState.Recovery), "TR-10" },
            { (PlayerState.EnteringVehicle, PlayerState.Recovery), "TR-10" },
            { (PlayerState.ExitingVehicle, PlayerState.Recovery), "TR-10" },
            { (PlayerState.Recovery, PlayerState.OnFoot), "TR-11" },
            { (PlayerState.Recovery, PlayerState.Driving), "TR-11" },
            { (PlayerState.Recovery, PlayerState.Building), "TR-12" },
        };

        private readonly List<TransitionRecord> m_History = new List<TransitionRecord>(HistoryCapacity);
        private readonly Queue<double> m_RecoveryEntries = new Queue<double>();
        private double m_Now;

        public PlayerState State { get; private set; } = PlayerState.Building;
        public PlayerOverlay Overlays { get; private set; } = PlayerOverlay.None;

        /// <summary>Seconds spent in the current state (unscaled time, excluding nothing).</summary>
        public float TimeInState { get; private set; }

        /// <summary>Raised after every state change, with the record.</summary>
        public event Action<TransitionRecord> StateChanged;

        public IReadOnlyList<TransitionRecord> History => m_History;

        public bool IsTransitional => State == PlayerState.EnteringVehicle || State == PlayerState.ExitingVehicle || State == PlayerState.Recovery;

        public bool IsCharacterMode => State != PlayerState.Building;

        public static bool IsLegal(PlayerState from, PlayerState to, out string transitionId) => s_Legal.TryGetValue((from, to), out transitionId);

        /// <summary>Requests a normal transition. Forced returns to Building use <see cref="ForceBuilding"/>.</summary>
        public TransitionResultCode Request(PlayerState to, string reason)
        {
            if (to == State)
            {
                return TransitionResultCode.SameState;
            }
            if (!IsLegal(State, to, out string id))
            {
                return TransitionResultCode.Illegal;
            }
            // Loading/Unloading allow only teardown to Building; Menu/Paused do not block teardown either.
            if ((Overlays & (PlayerOverlay.Loading | PlayerOverlay.Unloading)) != 0 && to != PlayerState.Building)
            {
                return TransitionResultCode.BlockedByOverlay;
            }
            // Starting a transition is not allowed under any overlay (TR-03, TR-06 guards).
            if (Overlays != PlayerOverlay.None && (to == PlayerState.EnteringVehicle || to == PlayerState.ExitingVehicle || (State == PlayerState.Building && to == PlayerState.OnFoot)))
            {
                return TransitionResultCode.BlockedByOverlay;
            }
            Apply(to, id, reason);
            return TransitionResultCode.Ok;
        }

        /// <summary>TR-13: forced return to Building from any state (fault, safe-disable, Loading, Unloading).</summary>
        public void ForceBuilding(string reason)
        {
            if (State == PlayerState.Building)
            {
                return;
            }
            Apply(PlayerState.Building, "TR-13", reason);
        }

        public void SetOverlay(PlayerOverlay overlay, bool on)
        {
            Overlays = on ? (Overlays | overlay) : (Overlays & ~overlay);
        }

        /// <summary>
        /// Advances timers. Returns true when the current transitional state exceeded its timeout; the caller must
        /// then abort (TR-05/TR-08) or enter Recovery/Building.
        /// </summary>
        public bool Tick(float dt)
        {
            m_Now += dt;
            if ((Overlays & PlayerOverlay.Paused) == 0)
            {
                TimeInState += dt;
            }
            switch (State)
            {
                case PlayerState.EnteringVehicle:
                case PlayerState.ExitingVehicle:
                    return TimeInState > TransitionTimeout;
                case PlayerState.Recovery:
                    return TimeInState > RecoveryTimeout;
                default:
                    return false;
            }
        }

        /// <summary>True when Recovery was entered too often recently (TR-12 loop breaker).</summary>
        public bool RecoveryLoopDetected
        {
            get
            {
                while (m_RecoveryEntries.Count > 0 && m_Now - m_RecoveryEntries.Peek() > RecoveryLoopWindow)
                {
                    m_RecoveryEntries.Dequeue();
                }
                return m_RecoveryEntries.Count >= RecoveryLoopCount;
            }
        }

        private void Apply(PlayerState to, string id, string reason)
        {
            var rec = new TransitionRecord { Time = m_Now, From = State, To = to, TransitionId = id, Reason = reason ?? string.Empty };
            State = to;
            TimeInState = 0f;
            if (to == PlayerState.Recovery)
            {
                m_RecoveryEntries.Enqueue(m_Now);
            }
            if (m_History.Count == HistoryCapacity)
            {
                m_History.RemoveAt(0);
            }
            m_History.Add(rec);
            StateChanged?.Invoke(rec);
        }
    }

    /// <summary>
    /// Records acquired resources with their release actions and releases them in reverse order on any exit path
    /// (SPEC-CONTROL section 7). A failing release is reported and skipped; the remaining releases still run.
    /// </summary>
    public sealed class CleanupStack
    {
        private readonly List<KeyValuePair<string, Action>> m_Items = new List<KeyValuePair<string, Action>>();

        public int Count => m_Items.Count;

        public void Push(string name, Action release)
        {
            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }
            m_Items.Add(new KeyValuePair<string, Action>(name, release));
        }

        public bool Contains(string name)
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].Key == name)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Releases (and removes) one named resource if present.</summary>
        public bool Release(string name, Action<string, Exception> onError = null)
        {
            for (int i = m_Items.Count - 1; i >= 0; i--)
            {
                if (m_Items[i].Key != name)
                {
                    continue;
                }
                Action a = m_Items[i].Value;
                m_Items.RemoveAt(i);
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(name, ex);
                }
                return true;
            }
            return false;
        }

        /// <summary>Removes a named entry without running it (the resource was handed over elsewhere).</summary>
        public bool Forget(string name)
        {
            for (int i = m_Items.Count - 1; i >= 0; i--)
            {
                if (m_Items[i].Key == name)
                {
                    m_Items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public int ReleaseAll(Action<string, Exception> onError = null)
        {
            int released = 0;
            while (m_Items.Count > 0)
            {
                int last = m_Items.Count - 1;
                KeyValuePair<string, Action> item = m_Items[last];
                m_Items.RemoveAt(last);
                try
                {
                    item.Value();
                    released++;
                }
                catch (Exception ex)
                {
                    onError?.Invoke(item.Key, ex);
                }
            }
            return released;
        }
    }
}
