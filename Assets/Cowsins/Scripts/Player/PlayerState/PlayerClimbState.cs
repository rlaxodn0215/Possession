using UnityEngine;
namespace cowsins
{
    public class PlayerClimbState : PlayerBaseState
    {
        private Rigidbody rb;
        private PlayerMovement playerMovement;
        private IPlayerControlProvider playerControlProvider;

        public PlayerClimbState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) 
        {
            rb = _ctx.Rigidbody;
            playerMovement = _ctx.PlayerMovement;
            playerControlProvider = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            rb.useGravity = false;
            playerMovement.IsClimbing = true;
            rb.linearVelocity = Vector3.zero;
            playerMovement.userEvents.OnSpawn.Invoke();
        }

        public sealed override void UpdateState()
        {
            if (!playerControlProvider.IsControllable) return;
            playerMovement.HandleClimbMovement();
            playerMovement.VerticalLook();
            // Prevents speedlines from showing up
            playerMovement.HandleSpeedLines();
            CheckSwitchState();
        }

        public sealed override void FixedUpdateState() { }

        public sealed override void ExitState()
        {
            playerMovement.userEvents.OnEndClimb.Invoke();
            rb.useGravity = true;
            playerMovement.IsClimbing = false;
            playerMovement.HandleLadderFinishMotion();
        }

        public sealed override void CheckSwitchState()
        {
            if (InputManager.jumping || playerMovement.Grounded && InputManager.y < 0) SwitchState(_factory.Default());
            if (playerMovement.DetectTopLadder())
            {
                playerMovement.IsClimbing = false;
                rb.useGravity = true;
                playerMovement.ReachTopLadder();
                SwitchState(_factory.Default());
            }

        }
    }
}