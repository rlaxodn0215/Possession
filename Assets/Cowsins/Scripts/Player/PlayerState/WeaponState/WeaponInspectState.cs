namespace cowsins
{
    using UnityEngine;
    public class WeaponInspectState : WeaponBaseState
    {
        private PlayerDependencies playerDependencies;
        private WeaponController controller;
        private WeaponAnimator animator;
        private InteractManager interact;
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs
        private IPlayerMovementStateProvider player; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs

        private float timer;

        public WeaponInspectState(WeaponStates currentContext, WeaponStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
            playerDependencies = _ctx.PlayerDependencies;
            player = _ctx.PlayerMovement;
            controller = _ctx.WeaponController;
            animator = _ctx.WeaponAnimator;
            interact = _ctx.InteractManager;
            playerControl = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            timer = 0;

            animator.InitializeInspection();
            interact.ToggleInspectionState(true);

            if (!interact.RealtimeAttachmentCustomization) return;

            UIEvents.onStartInspection?.Invoke();
            UIEvents.onGenerateInspectionUI?.Invoke(playerDependencies);

            UIController.instance.UnlockMouse();

            InputManager.onInspect += SwitchToDefault;
        }


        public sealed override void UpdateState()
        {
            if (interact.RealtimeAttachmentCustomization) playerControl.LoseControl();

            if (timer <= 1) timer += Time.deltaTime;

            controller.StopAim();

            CheckSwitchState();
        }

        public sealed override void FixedUpdateState() { }

        public sealed override void ExitState()
        {
            interact.ToggleInspectionState(false);
            animator.DisableInspection();
            playerControl.CheckIfCanGrantControl();

            UIController.instance.LockMouse();

            UIEvents.onEnableAttachmentUI?.Invoke(null);
            UIEvents.onStopInspection?.Invoke();

            InputManager.onInspect -= SwitchToDefault;
        }
        public sealed override void CheckSwitchState()
        {
            if (timer < 1) return;
            if (InputManager.shooting && !interact.RealtimeAttachmentCustomization || player.CurrentSpeed == player.RunSpeed) SwitchState(_factory.Default());
        }

        private void SwitchToDefault()
        {
            if (timer < 1) return;
            SwitchState(_factory.Default());
        }
    }
}