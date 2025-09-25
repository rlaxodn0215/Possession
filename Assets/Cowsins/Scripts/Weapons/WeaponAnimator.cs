using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace cowsins
{
    public class WeaponAnimator : MonoBehaviour
    {
        [SerializeField] private ParentConstraint parentConstraint;

        [SerializeField] private Animator holsterMotionObject;

        public Animator HolsterMotionObject => holsterMotionObject;

        private PlayerDependencies playerDependencies;
        private WeaponStates weaponStates;
        private IPlayerMovementStateProvider player; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private IWeaponReferenceProvider weaponController; // IWeaponReferenceProvider is implemented in WeaponController.cs
        private IWeaponBehaviourProvider weaponBehaviour; // IWeaponBehaviourProvider is implemented in WeaponController.cs
        private IInteractManagerProvider interactManager; // IWeaponReferenceProvider is implemented in InteractManager.cs
        private Rigidbody rb;

        private void Start()
        {
            playerDependencies = GetComponent<PlayerDependencies>();
            weaponStates = GetComponent<WeaponStates>();
            player = playerDependencies.PlayerMovementState;
            weaponController = playerDependencies.WeaponReference;
            weaponBehaviour = playerDependencies.WeaponBehaviour;
            interactManager = playerDependencies.InteractManager;
            rb = GetComponent<Rigidbody>(); 
        }

        private void Update()
        {
            if (weaponController.Id == null) return;

            Animator currentAnimator = weaponController.Id.Animator;

            if (player.WallRunning && !weaponBehaviour.Reloading)
            {
                CowsinsUtilities.PlayAnim("walking", currentAnimator);
                return;

            }
            if (weaponBehaviour.Reloading || player.IsCrouching || !player.Grounded || rb.linearVelocity.magnitude < 0.1f || weaponBehaviour.IsAiming
                || currentAnimator.GetCurrentAnimatorStateInfo(0).IsName("Unholster")
                || currentAnimator.GetCurrentAnimatorStateInfo(0).IsName("reloading")
                || currentAnimator.GetCurrentAnimatorStateInfo(0).IsName("shooting"))
            {
                CowsinsUtilities.StopAnim("walking", currentAnimator);
                CowsinsUtilities.StopAnim("running", currentAnimator);
                return;
            }

            if (rb.linearVelocity.magnitude > player.CrouchSpeed && player.CurrentSpeed < player.RunSpeed && player.Grounded && !interactManager.Inspecting) CowsinsUtilities.PlayAnim("walking", currentAnimator);
            else CowsinsUtilities.StopAnim("walking", currentAnimator);

            if (player.CurrentSpeed >= player.RunSpeed && player.Grounded) CowsinsUtilities.PlayAnim("running", currentAnimator);
            else CowsinsUtilities.StopAnim("running", currentAnimator);

            if(!interactManager.Inspecting)
            {
                CowsinsUtilities.StopAnim("inspect", currentAnimator);
                CowsinsUtilities.StopAnim("finishedInspect", currentAnimator);
            }    
        }

        public void StopWalkAndRunMotion()
        {
            if (weaponController == null) return; 
            Animator weapon = weaponController.Id.Animator;
            CowsinsUtilities.StopAnim("inspect", weapon);
            CowsinsUtilities.StopAnim("walking", weapon);
            CowsinsUtilities.StopAnim("running", weapon);
        }

        public void HideWeapon()
        {
            weaponStates.ForceChangeState(weaponStates._States.Hidden());
        }

        public void ShowWeapon()
        {
            weaponStates.ForceChangeState(weaponStates._States.Default());
            Invoke(nameof(ResetParentConstraintWeight), .2f);
        }

        public void SetParentConstraintSource(Transform transform)
        {
            if(parentConstraint.sourceCount > 0) parentConstraint.RemoveSource(0);

            ConstraintSource source = new ConstraintSource
            {
                sourceTransform = transform,
                weight = .5f                 
            };

            parentConstraint.AddSource(source);
        }

        public void SetParentConstraintWeight(float weight) => parentConstraint.weight = weight;

        public void ResetParentConstraintWeight()
        {
            if (player.IsClimbing) return; 
            StartCoroutine(ResetParentConstraintWeightCoroutine());
        }

        private IEnumerator ResetParentConstraintWeightCoroutine()
        {
            SetParentConstraintWeight(0);
            SetParentConstraintSource(weaponController.Id?.HeadBone ?? null);

            // Gradually increase weight
            float targetWeight = 0.5f;
            float weightIncrementDuration = 1f;
            float elapsedTime = 0f;

            while (elapsedTime < weightIncrementDuration)
            {
                elapsedTime += Time.deltaTime;
                float weight = Mathf.Lerp(0, targetWeight, elapsedTime / weightIncrementDuration);
                SetParentConstraintWeight(weight);
                yield return null;
            }

            // Ensure the final weight is exactly 0.5
            SetParentConstraintWeight(targetWeight);
        }

        #region INSPECT
        public void InitializeInspection()
        {
            WeaponIdentification wiD = weaponController.Id;
            CowsinsUtilities.PlayAnim("inspect", wiD.Animator);
            CowsinsUtilities.StopAnim("finishedInspect", wiD.Animator);
        }

        public void DisableInspection()
        {
            WeaponIdentification wID = weaponController.Id;
            CowsinsUtilities.PlayAnim("finishedInspect", wID.Animator);
            CowsinsUtilities.StopAnim("inspect", wID.Animator);
        }
        #endregion
    }
}