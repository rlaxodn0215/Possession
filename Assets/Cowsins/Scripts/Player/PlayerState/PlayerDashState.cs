using UnityEngine;
using System;
namespace cowsins
{
    public class PlayerDashState : PlayerBaseState
    {
        private PlayerMovement player;
        private Rigidbody rb;

        private float dashTimer;
        private Vector2 input;

        private EventHandler onDashNoInfinite;

        public PlayerDashState(PlayerStates currentContext, PlayerStateFactory playerStateFactory, Vector2 inp)
            : base(currentContext, playerStateFactory)
        {
            player = _ctx.PlayerMovement;
            rb = _ctx.Rigidbody;
            input = inp;
        }

        public sealed override void EnterState()
        {
            onDashNoInfinite = player.RegainDash;

            dashTimer = player.dashDuration;
            player.dashing = true;
            rb.useGravity = true;

            player.userEvents.OnStartDash.Invoke();

            player.cameraFOVManager.ForceAddFOV(-player.fovToAddOnDash);

            if (!player.infiniteDashes)
            {
                player.currentDashes--;
                onDashNoInfinite?.Invoke(this, EventArgs.Empty);
            }

            //staminaLoss
            if (player.usesStamina) player.ReduceStamina(player.StaminaLossOnDash);

            // Gather Inputs on Enter State to avoid No reg issues
            input = InputManager.inputActions.GameControls.Movement.ReadValue<Vector2>();

        }

        public sealed override void UpdateState()
        {
            player.userEvents.OnDashing?.Invoke();
            Vector3 dir = GetProperDirection();
            player.HandleStairs(dir);
            rb.AddForce(dir * player.dashForce * Time.deltaTime, ForceMode.Impulse);

            // Remove not wanted Y velocity after slope
            Vector3 vel = rb.linearVelocity;
            vel.y = 0;
            rb.linearVelocity = vel;

            player.HandleSpeedLines(); 
            CheckSwitchState();
        }
        public sealed override void FixedUpdateState()
        {
        }

        public sealed override void ExitState()
        {
            player.userEvents.OnEndDash?.Invoke();
            player.dashing = false;
            rb.useGravity = true;
        }

        public sealed override void CheckSwitchState()
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0 || !player.Dashing) SwitchState(_factory.Default());

        }

        private Vector3 CameraBasedInput()
        {
            Vector3 forward = player.playerCam.transform.forward;
            Vector3 right = player.playerCam.transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 dir = forward * input.y + right * input.x;

            return dir;

        }

        private Vector3 GetProperDirection()
        {
            Vector3 direction = Vector3.zero;
            switch (player.dashMethod)
            {
                case PlayerMovement.DashMethod.ForwardAlways:
                    direction = player.Orientation.Forward;
                    break;
                case PlayerMovement.DashMethod.Free:
                    direction = player.playerCam.forward;
                    break;
                case PlayerMovement.DashMethod.InputBased:
                    direction = (input.x == 0 && input.y == 0) ? player.Orientation.Forward : CameraBasedInput();
                    break;
            }
            return direction;
        }
    }
}