using UnityEngine;
namespace cowsins
{
    public class PlayerDefaultState : PlayerBaseState
    {
        private PlayerMovement player;
        private IPlayerStatsProvider statsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private IPlayerControlProvider playerControlProvider; // IPlayerControlProvider is implemented in PlayerControl.cs

        public PlayerDefaultState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) 
        {
            player = _ctx.PlayerMovement;
            statsProvider = _ctx.PlayerStatsProvider;
            playerControlProvider = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            InputManager.onJump += player.SwitchToJumpState;
            InputManager.onDash += player.SwitchToDashState;
            InputManager.onStartGrapple += player.StartGrapple;
            InputManager.onStopGrapple += player.StopGrapple;
        }

        public sealed override void UpdateState()
        {
            if (!playerControlProvider.IsControllable) return;
            HandleMovement();
            CheckSwitchState();
            CheckUnCrouch();
        }

        public sealed override void FixedUpdateState() 
        { 
            player.Movement();
        }

        public sealed override void ExitState() 
        {
            InputManager.onJump -= player.SwitchToJumpState;
            InputManager.onDash -= player.SwitchToDashState;
            InputManager.onStartGrapple -= player.StartGrapple;
            InputManager.onStopGrapple -= player.StopGrapple;
        }

        public sealed override void CheckSwitchState()
        {
            // Check climbing
            if (player.DetectLadders()) SwitchState(_factory.Climb());

            // Check Death
            if (statsProvider.Health <= 0) SwitchState(_factory.Die());
 
            // Check Crouch
            if (InputManager.crouching && !player.WallRunning && player.allowCrouch)
            {
                if (player.Grounded)
                    SwitchState(_factory.Crouch());
                else
                {
                    if (player.allowCrouchWhileJumping) SwitchState(_factory.Crouch());
                }

            }

            // Check Grapple
            if (player.allowGrapple)
            {
                player.HandleGrapple();
                player.UpdateGrappleRenderer();
            }
        }

        private void HandleMovement()
        {
            player.Look();
            player.FootSteps();
            player.HandleVelocities();
            player.HandleCoyoteJump();
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