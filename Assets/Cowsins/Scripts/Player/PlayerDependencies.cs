using UnityEngine;

namespace cowsins
{
    /// <summary>
    /// Stores references to all required dependencies for the Player to function properly.
    /// This class must not be removed and all the interfaces must be implemented.
    /// </summary>
    public class PlayerDependencies : MonoBehaviour
    {
        // PlayerMovement
        public IPlayerMovementActionsProvider PlayerMovementActions { get; private set; }
        public IPlayerMovementEventsProvider PlayerMovementEvents { get; private set; }
        public IPlayerMovementStateProvider PlayerMovementState { get; private set; }

        // WeaponController
        public IWeaponReferenceProvider WeaponReference { get; private set; }
        public IWeaponBehaviourProvider WeaponBehaviour { get; private set; }
        public IWeaponRecoilProvider WeaponRecoil { get; private set; }

        // PlayerStats
        public IDamageable Damageable { get; private set; }
        public IPlayerStatsProvider PlayerStats { get; private set; }
        public IFallHeightProvider FallHeight { get; private set; }

        // InteractManager
        public IInteractManagerProvider InteractManager { get; private set; }

        // PlayerControl
        public IPlayerControlProvider PlayerControl { get; private set; }


        private void OnEnable()
        {
            PlayerMovementActions = GetRequiredComponent<IPlayerMovementActionsProvider>();
            PlayerMovementEvents = GetRequiredComponent<IPlayerMovementEventsProvider>();
            PlayerMovementState = GetRequiredComponent<IPlayerMovementStateProvider>();

            WeaponReference = GetRequiredComponent<IWeaponReferenceProvider>();
            WeaponBehaviour = GetRequiredComponent<IWeaponBehaviourProvider>();
            WeaponRecoil = GetRequiredComponent<IWeaponRecoilProvider>();

            Damageable = GetRequiredComponent<IDamageable>();
            PlayerStats = GetRequiredComponent<IPlayerStatsProvider>();
            FallHeight = GetComponent<IFallHeightProvider>();

            InteractManager = GetRequiredComponent<IInteractManagerProvider>();

            PlayerControl = GetRequiredComponent<IPlayerControlProvider>();
        }

        private void Start()
        {
            InputManager.Instance.SetPlayer(this);
        }

        private T GetRequiredComponent<T>() where T : class
        {
            var component = GetComponent<T>();
            if (component == null) Debug.LogError($"<color=red>[COWSINS]</color> Missing required component of type {typeof(T).Name} on {gameObject.name}");
            return component;
        }
    }
}