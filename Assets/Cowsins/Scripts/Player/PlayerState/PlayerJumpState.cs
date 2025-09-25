using UnityEngine;
namespace cowsins
{
    public class PlayerJumpState : PlayerBaseState
    {
        private PlayerMovement player;
        private IPlayerStatsProvider statsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs

        public PlayerJumpState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
            player = _ctx.PlayerMovement;
            statsProvider = _ctx.PlayerStatsProvider;
            playerControl = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            player.userEvents.OnJump.Invoke();
            player.Jump();

            InputManager.onStartGrapple += player.StartGrapple;
            InputManager.onStopGrapple += player.StopGrapple;
        }

        public sealed override void UpdateState()
        {
            CheckSwitchState();
            HandleMovement();
            CheckUnCrouch();
        }

        public sealed override void FixedUpdateState() { }

        public sealed override void ExitState() 
        {
            InputManager.onStartGrapple -= player.StartGrapple;
            InputManager.onStopGrapple -= player.StopGrapple;
        }

        public sealed override void CheckSwitchState()
        {
            if (player.DetectLadders())
            {
                SwitchState(_factory.Climb());
                return;
            }

            bool canJump = player.ReadyToJump && InputManager.jumping &&
                           (player.EnoughStaminaToJump && player.Grounded ||
                            player.WallRunning ||
                            player.jumpCount > 0 && player.maxJumps > 1 && player.EnoughStaminaToJump);

            if (canJump)
            {
                SwitchState(_factory.Jump());
                return;
            }

            if (statsProvider.Health <= 0)
            {
                SwitchState(_factory.Die());
                return;
            }

            if (player.Grounded || player.WallRunning)
            {
                SwitchState(_factory.Default());
                return;
            }

            bool canDash = player.canDash && InputManager.dashing &&
                           (player.infiniteDashes ||
                            player.currentDashes > 0 && !player.infiniteDashes);

            if (canDash)
            {
                SwitchState(_factory.Dash());
                return;
            }

            bool canCrouch = InputManager.crouching && !player.WallRunning &&
                             player.allowCrouch && player.allowCrouchWhileJumping;

            if (canCrouch)
            {
                SwitchState(_factory.Crouch());
                return;
            }

            // Check Grapple
            if (player.allowGrapple)
            {
                player.HandleGrapple();
                player.UpdateGrappleRenderer();
            }
        }

        void HandleMovement()
        {
            if(!playerControl.IsControllable) return;
            player.Movement();
            player.Look();
        }

        private bool canUnCrouch = false;

        private void CheckUnCrouch()
        {
            if (!InputManager.crouching)
            {
                // Check if there is a roof above the player to prevent uncrouching
                RaycastHit hit;
                bool isObstacleAbove = Physics.Raycast(_ctx.transform.position, _ctx.transform.up, out hit, player.RoofCheckDistance, player.WhatIsGround);

                canUnCrouch = !isObstacleAbove;
            }

            if (canUnCrouch)
            {
                // Invoke event and stop crouching when it is safe to do so
                player.StopCrouch();
            }
        }

    }
}