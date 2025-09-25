using UnityEngine;
namespace cowsins
{
    public class PlayerDeadState : PlayerBaseState
    {
        private Rigidbody rb;
        private IPlayerControlProvider playerControlProvider; // IPlayerControlProvider is implemented in PlayerControl.cs
        public PlayerDeadState(PlayerStates currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) 
        {
            rb = _ctx.Rigidbody;
            playerControlProvider = _ctx.PlayerControlProvider;
        }

        public sealed override void EnterState()
        {
            playerControlProvider.LoseControl();
            rb.isKinematic = true;
        }

        public sealed override void UpdateState()
        {
            CheckSwitchState();
        }

        public sealed override void FixedUpdateState() { }

        public sealed override void ExitState() { rb.isKinematic = false; }

        public sealed override void CheckSwitchState() { }


    }
}