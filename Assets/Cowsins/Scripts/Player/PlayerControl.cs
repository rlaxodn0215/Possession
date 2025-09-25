using UnityEngine;

namespace cowsins
{
    // This class controls the controllable state of the player.
    // If non-controllable ( controllable == false ), the player won´t be able to move or perform any action.
    // GrantControl, LoseControl, ToggleControl and CheckIfCanGrantControl methods can be accessed to handle the player control.
    public class PlayerControl : MonoBehaviour, IPlayerControlProvider
    {
        public bool IsControllable => controllable;
        private bool controllable = true;

        // Reference to PlayerStats.cs ( IPlayerStatsProvider is implemented in PlayerStats.cs )
        private IPlayerStatsProvider playerStatusProvider; 

        private void Awake()
        {
            playerStatusProvider = GetComponent<IPlayerStatsProvider>();
            GrantControl();
        }

        /// <summary>
        /// Forces the player to be controlled. CheckIfCanGrantControl() method is recommended instead.
        /// </summary>
        public void GrantControl() => controllable = true;

        /// <summary>
        /// Disallows the player to be controlled.
        /// </summary>
        public void LoseControl() => controllable = false;

        /// <summary>
        /// Toggles the controllable state of the player.
        /// </summary>
        public void ToggleControl() => controllable = !controllable;

        /// <summary>
        /// Checks if the game is paused or the player is dead before allowing the player to be controlled.
        /// </summary>
        public void CheckIfCanGrantControl()
        {
            if (PauseMenu.isPaused || playerStatusProvider?.IsDead == true) return;
            GrantControl();
        }
    }
}