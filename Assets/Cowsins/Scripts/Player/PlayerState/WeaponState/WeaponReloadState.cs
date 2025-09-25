namespace cowsins
{
    public class WeaponReloadState : WeaponBaseState
    {
        private WeaponController controller;
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs

        public WeaponReloadState(WeaponStates currentContext, WeaponStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
            controller = _ctx.WeaponController;
            playerControl = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            controller.StartReload();
        }

        public sealed override void UpdateState()
        {
            CheckSwitchState();
            if (!playerControl.IsControllable) return;
            CheckStopAim();
        }

        public sealed override void FixedUpdateState()
        {
        }

        public sealed override void ExitState() { }

        public sealed override void CheckSwitchState()
        {
            if (!controller.Reloading) SwitchState(_factory.Default());
        }
        private void CheckStopAim()
        {
            if (!InputManager.aiming || !controller.Weapon.allowAimingIfReloading) controller.StopAim();
        }
    }
}