using UnityEngine;

namespace cowsins
{
    public class WeaponDefaultState : WeaponBaseState
    {
        private WeaponController weaponController;
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs
        private IPlayerMovementStateProvider movement; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private InteractManager interact;

        private bool holdingEmpty = false;

        public WeaponDefaultState(WeaponStates currentContext, WeaponStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) 
        {
            weaponController = _ctx.WeaponController;
            playerControl = _ctx.PlayerControlProvider;
            interact = _ctx.InteractManager;
            movement = _ctx.PlayerMovement;
        }

        public override void EnterState()
        {
            holdingEmpty = false;

            InputManager.onInspect += SwitchToInspect;
            InputManager.onMelee += SwitchToMelee;
            InputManager.onToggleFlashlight += weaponController.ToggleFlashLight;
        }

        public override void UpdateState()
        {
            if (playerControl == null || !playerControl.IsControllable)
            {
                weaponController.StopAim();
                return;
            }

            weaponController.HandleInventory();

            if (weaponController.Weapon == null || movement.IsClimbing) return;

            CheckSwitchState();
            CheckAim();
        }

        public override void FixedUpdateState() {}

        public override void ExitState()
        {
            InputManager.onInspect -= SwitchToInspect;
            InputManager.onMelee -= SwitchToMelee;
            InputManager.onToggleFlashlight -= weaponController.ToggleFlashLight;
        }
        public override void CheckSwitchState()
        {
            if (InputManager.shooting)
            {
                if (weaponController.Weapon.audioSFX.emptyMagShoot != null && weaponController.Id.bulletsLeftInMagazine <= 0 && !holdingEmpty)
                {
                    SoundManager.Instance.PlaySound(weaponController.Weapon.audioSFX.emptyMagShoot, 0, .15f, true);
                    holdingEmpty = true;
                }
                else if (CanSwitchToShoot(weaponController)) 
                    SwitchState(_factory.Shoot());
            }
            else holdingEmpty = false;

            if (weaponController.Weapon.infiniteBullets) return;

            if (weaponController.Weapon.reloadStyle == ReloadingStyle.defaultReload)
            {
                if (CanSwitchToReload(weaponController))
                    SwitchState(_factory.Reload());
            }
            else
            {
                if (weaponController.Id.heatRatio >= 1) SwitchState(_factory.Reload());
            }

        }

        private void CheckAim()
        {
            if (InputManager.aiming && weaponController.Weapon.allowAim) weaponController.Aim();
            CheckStopAim();
        }

        private void CheckStopAim() { if (!InputManager.aiming) weaponController.StopAim(); }

        private bool CanSwitchToShoot(WeaponController controller)
        {
            return !(_ctx.holding && controller.Weapon.shootMethod == ShootingMethod.Press) && controller.Id.bulletsLeftInMagazine > 0;
        }

        private bool CanSwitchToReload(WeaponController controller)
        {
            return InputManager.reloading && (int)controller.Weapon.shootStyle != 2 && controller.Id.bulletsLeftInMagazine < controller.Id.magazineSize && controller.Id.totalBullets > 0
                        || controller.Id.bulletsLeftInMagazine <= 0 && controller.AutoReload && (int)controller.Weapon.shootStyle != 2 && controller.Id.bulletsLeftInMagazine < controller.Id.magazineSize && controller.Id.totalBullets > 0;
        }

        private void SwitchToInspect()
        {
            if (playerControl.IsControllable && weaponController.Weapon && UIController.instance.GetInspectionUIAlpha() <= 0 && interact.CanInspect) SwitchState(_factory.Inspect());
        }

        private void SwitchToMelee()
        {
            if (playerControl.IsControllable && weaponController.CanMelee && weaponController.IsMeleeAvailable && !movement.IsClimbing) SwitchState(_factory.Melee());
        }
    }
}