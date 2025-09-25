using UnityEngine;

namespace cowsins
{
    public class PlayerDebugger : MonoBehaviour
    {
        private const float TopMargin = 130f;
        private const float BoxWidth = 320f;
        private const float LabelHeight = 20f;

        // Player Components
        private PlayerDependencies playerDependencies;
        private IPlayerStatsProvider playerStatsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private IPlayerControlProvider playerControlProvider; // IPlayerControlProvider is implemented in PlayerControl.cs
        private IPlayerMovementStateProvider playerMovementProvider; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private IWeaponReferenceProvider weaponController; // IWeaponReferenceProvider is implemented in WeaponController.cs
        private IWeaponBehaviourProvider weaponBehaviour; // IWeaponBehaviourProvider is implemented in WeaponController.cs
        private IInteractManagerProvider interactManager; // IInteractManagerProvider is implemented in InteractManager.cs
        private PlayerStates playerStates;
        private WeaponStates weaponStates;
        private Rigidbody rb;

        // Get references
        private void Start()
        {
            playerDependencies = GetComponent<PlayerDependencies>();
            playerStates = GetComponent<PlayerStates>();
            weaponStates = GetComponent<WeaponStates>();
            rb = GetComponent<Rigidbody>();

            playerStatsProvider = playerDependencies.PlayerStats;
            playerControlProvider = playerDependencies.PlayerControl;
            playerMovementProvider = playerDependencies.PlayerMovementState;
            weaponController = playerDependencies.WeaponReference;
            weaponBehaviour = playerDependencies.WeaponBehaviour;
            interactManager = playerDependencies.InteractManager;
        }

        private void DrawLabel(Rect position, string label)
        {
            GUI.Label(position, label);
        }

        private void DrawBox(Rect position, string title, System.Action drawContent)
        {
            GUI.Box(position, title);
            drawContent();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            // Player Information Box
            DrawBox(new Rect(10, TopMargin, BoxWidth, 130), "Player Information", () =>
            {
                DrawLabel(new Rect(20, TopMargin + 20, BoxWidth - 20, LabelHeight), $"Player Velocity: {Mathf.Round(rb.linearVelocity.magnitude)}");
                DrawLabel(new Rect(20, TopMargin + 40, BoxWidth - 20, LabelHeight), $"Player Position: {transform.position}");
                DrawLabel(new Rect(20, TopMargin + 60, BoxWidth - 20, LabelHeight), $"Player Orientation: {playerMovementProvider.Orientation.Forward}");
                DrawLabel(new Rect(20, TopMargin + 80, BoxWidth - 20, LabelHeight), $"Player State: {playerStates?.CurrentState}");
                DrawLabel(new Rect(20, TopMargin + 100, BoxWidth - 20, LabelHeight), $"Grounded: {playerMovementProvider.Grounded}");
            });

            // Weapon Information Box
            DrawBox(new Rect(10, TopMargin + 140, BoxWidth, 170), "Weapon Information", () =>
            {
                DrawLabel(new Rect(20, TopMargin + 160, BoxWidth - 20, LabelHeight), $"Weapon_SO: {weaponController.Weapon}");
                DrawLabel(new Rect(20, TopMargin + 180, BoxWidth - 20, LabelHeight), $"Weapon Object: {weaponController.Id}");
                DrawLabel(new Rect(20, TopMargin + 200, BoxWidth - 20, LabelHeight), $"Weapon Total Bullets: {weaponController.Id?.totalBullets}");
                DrawLabel(new Rect(20, TopMargin + 220, BoxWidth - 20, LabelHeight), $"Weapon Current Bullets: {weaponController.Id?.bulletsLeftInMagazine}");
                DrawLabel(new Rect(20, TopMargin + 240, BoxWidth - 20, LabelHeight), $"Reloading: {weaponBehaviour.Reloading}");
                DrawLabel(new Rect(20, TopMargin + 260, BoxWidth - 20, LabelHeight), $"Weapon State: {weaponStates?.CurrentState}");
                DrawLabel(new Rect(20, TopMargin + 280, BoxWidth - 20, LabelHeight), $"Weapon Aiming: {weaponBehaviour.IsAiming}");
            });

            // Player Stats Information Box
            DrawBox(new Rect(10, TopMargin + 320, BoxWidth, 70), "Player Stats Information", () =>
            {
                DrawLabel(new Rect(20, TopMargin + 340, BoxWidth - 20, LabelHeight), $"Health: {Mathf.Round(playerStatsProvider.Health)} / {playerStatsProvider.MaxHealth}");
                DrawLabel(new Rect(20, TopMargin + 360, BoxWidth - 20, LabelHeight), $"Shield: {Mathf.Round(playerStatsProvider.Shield)} / {playerStatsProvider.MaxShield}");
                DrawLabel(new Rect(20, TopMargin + 380, BoxWidth - 20, LabelHeight), $"Controllable: {playerControlProvider.IsControllable}");
            });

            // Interact Manager Information Box
            DrawBox(new Rect(10, TopMargin + 420, BoxWidth, 70), "Interact Manager Information", () =>
            {
                DrawLabel(new Rect(20, TopMargin + 440, BoxWidth - 20, LabelHeight), $"Highlighted Interactable: {interactManager.HighlightedInteractable?.name}");
                DrawLabel(new Rect(20, TopMargin + 460, BoxWidth - 20, LabelHeight), $"Interact Progress: {interactManager.ProgressElapsed:F1}");
            });

            // Input Manager Box
            DrawBox(new Rect(10, TopMargin + 500, BoxWidth, 300), "Input Manager", () =>
            {
                DrawLabel(new Rect(20, TopMargin + 520, BoxWidth - 20, LabelHeight), $"Movement: ({InputManager.x:F1},{InputManager.y:F1})");
                DrawLabel(new Rect(20, TopMargin + 540, BoxWidth - 20, LabelHeight), $"Look: ({InputManager.mousex:F1},{InputManager.mousey:F1})");
                DrawLabel(new Rect(20, TopMargin + 560, BoxWidth - 20, LabelHeight), $"Gamepad Look: ({InputManager.controllerx:F1},{InputManager.controllery:F1})");
                DrawLabel(new Rect(20, TopMargin + 580, BoxWidth - 20, LabelHeight), $"Shooting: {InputManager.shooting}");
                DrawLabel(new Rect(20, TopMargin + 600, BoxWidth - 20, LabelHeight), $"Reloading: {InputManager.reloading}");
                DrawLabel(new Rect(20, TopMargin + 620, BoxWidth - 20, LabelHeight), $"Aiming: {InputManager.aiming}");
                DrawLabel(new Rect(20, TopMargin + 640, BoxWidth - 20, LabelHeight), $"Sprinting: {InputManager.sprinting}");
                DrawLabel(new Rect(20, TopMargin + 660, BoxWidth - 20, LabelHeight), $"Crouching: {InputManager.crouching}");
                DrawLabel(new Rect(20, TopMargin + 680, BoxWidth - 20, LabelHeight), $"Interacting: {InputManager.interacting}");
            });
        }
#endif
    }
}
