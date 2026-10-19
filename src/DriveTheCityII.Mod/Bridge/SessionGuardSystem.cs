using Colossal.Serialization.Entities;
using DriveTheCityII.Diagnostics;
using Game;

namespace DriveTheCityII.Bridge
{
    /// <summary>
    /// Protects the player's data during automated test sessions: while the test bridge is enabled, the game's
    /// autosave is disabled for the session (autosave pruning deletes old autosaves of every city; refcache note 01 §8).
    /// The persisted autosave setting is never changed.
    /// </summary>
    public partial class SessionGuardSystem : GameSystemBase
    {
        private AutoSaveSystem m_AutoSave;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AutoSave = World.GetOrCreateSystemManaged<AutoSaveSystem>();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            Runtime.CheckConflicts();
            EnforceTestGuards();
        }

        protected override void OnUpdate()
        {
            EnforceTestGuards();
        }

        private void EnforceTestGuards()
        {
            if (!Runtime.TestMode || m_AutoSave == null)
            {
                return;
            }
            if (m_AutoSave.Enabled)
            {
                m_AutoSave.Enabled = false;
                TestBridge.Log("guard: game autosave disabled for this test session (setting unchanged)");
            }
        }
    }
}
