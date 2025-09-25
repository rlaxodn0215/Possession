using UnityEngine;
namespace cowsins
{
    public class PlayerCrouchState : PlayerBaseState
    {
        private IPlayerControlProvider playerControlProvider; // IPlayerControlProvider is implemented in PlayerControl.cs
        private IPlayerStatsProvider statsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private PlayerMovement player;

        public PlayerCrouchState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
            player = _ctx.PlayerMovement;
            statsProvider = _ctx.PlayerStatsProvider;
            playerControlProvider = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            player.userEvents.OnCrouch.Invoke();
            player.StartCrouch();
        }

        public sealed override void UpdateState()
        {
            if (!playerControlProvider.IsControllable) return;
            if (InputManager.crouching)
            {
                _ctx.transform.localScale = Vector3.MoveTowards(_ctx.transform.localScale, player.crouchScale, Time.deltaTime * player.crouchTransitionSpeed * 1.5f);
                if (player.crouchCancelMethod == PlayerMovement.CrouchCancelMethod.Smooth) player.SmoothCrouchCancel();
            }
            player.isCrouching = true;
            CheckSwitchState();
            player.Look();
        }
        public sealed override void FixedUpdateState()
        {
            if (!playerControlProvider.IsControllable) return;
            HandleMovement();
        }

        public sealed override void ExitState() 
        { 
            player.UncrouchEvents(); // Invoke your own method on the moment you are standing up NOT WHILE YOU ARE NOT CROUCHING
        }

        public sealed override void CheckSwitchState()
        {

            if (player.ReadyToJump && InputManager.jumping && player.canJumpWhileCrouching && (player.EnoughStaminaToJump && player.Grounded || player.WallRunning || player.jumpCount > 0 && player.maxJumps > 1 && player.EnoughStaminaToJump))
                SwitchState(_factory.Jump());

            if (statsProvider.Health <= 0) SwitchState(_factory.Die());

            if (player.canDash && InputManager.dashing && (player.infiniteDashes || player.currentDashes > 0 && !player.infiniteDashes)) SwitchState(_factory.Dash());


            CheckUnCrouch();

        }

        private bool canUnCrouch = false;

        private void HandleMovement()
        {
            player.Movement();
            player.FootSteps();
            player.HandleSpeedLines();
        }

        private void CheckUnCrouch()
        {
            if (!InputManager.crouching) // Prevent from uncrouching when there´s a roof and we can get hit with it
            {
                RaycastHit hit;
                bool isObstacleAbove = Physics.Raycast(_ctx.transform.position, _ctx.transform.up, out hit, player.RoofCheckDistance, player.WhatIsGround);

                canUnCrouch = !isObstacleAbove;

                if (canUnCrouch)
                {
                    _ctx.PlayerMovement.StopCrouch();
                    if (_ctx.transform.localScale == player.PlayerScale)
                        SwitchState(_factory.Default());
                }
            }
        }
    }
}