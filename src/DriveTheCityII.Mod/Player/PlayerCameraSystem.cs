using Game;

namespace DriveTheCityII.Player
{
    /// <summary>Applies the Player camera pose in PreCulling, just before CameraUpdateSystem (refcache note 02 §1.3).</summary>
    public partial class PlayerCameraSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            PlayerSystem.Instance?.ApplyCamera();
        }
    }
}
