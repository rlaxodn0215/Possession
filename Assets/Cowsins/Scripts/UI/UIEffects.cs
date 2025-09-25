using System.Collections;
using UnityEngine;

namespace cowsins
{
    /// <summary>
    /// Contains a collection of useful UI Effects, from Sway, to Tilt or Jump Motion.
    /// This component can be found on "UIEffects" Object, inside PlayerUI Prefab.
    /// </summary>
    public class UIEffects : MonoBehaviour
    {
        [SerializeField, Title("SHARED REFERENCES")] private PlayerDependencies playerDependencies;

        [SerializeField, Title("UI SWAY"), Header("Position")] private float amount = 0.02f;
        [SerializeField] private float maxAmount = 0.06f;
        [SerializeField] private float smoothAmount = 6f;

        [SerializeField, Header("Tilt")] private float tiltAmount = 4f;
        [SerializeField] private float maxTiltAmount = 5f;
        [SerializeField] private float smoothTiltAmount = 12f;

        [SerializeField, Title("JUMP MOTION", upMargin = 10)] private AnimationCurve jumpMotion;
        [SerializeField] private AnimationCurve groundedMotion;
        [SerializeField] private float distance;
        [SerializeField] private float rotationAmount;
        [SerializeField, Min(1)] private float evaluationSpeed;

        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private float InputX;
        private float InputY;

        private Vector3 swayPositionOffset = Vector3.zero;
        private Quaternion swayRotationOffset = Quaternion.identity;

        private Vector3 jumpPositionOffset = Vector3.zero;
        private Quaternion jumpRotationOffset = Quaternion.identity;

        private Coroutine jumpMotionCoroutine;

        private IPlayerMovementEventsProvider playerMovementProvider; // Reference to PlayerMovement.cs ( IPlayerMovementEventsProvider is implemented in PlayerMovement.cs )
        private IPlayerControlProvider playerControl; // Reference to PlayerControl.cs ( IPlayerControlProvider is implemented in PlayerControl.cs )

        private void Start()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;

            playerMovementProvider = playerDependencies.PlayerMovementEvents;
            playerControl = playerDependencies.PlayerControl;

            // Listen to PlayerMovement events when Jumping and landing to handle Jump Motion accordingly.
            playerMovementProvider.AddJumpListener(OnJump);
            playerMovementProvider.AddLandListener(OnLand);
        }

        private void OnDisable()
        {
            playerMovementProvider?.RemoveJumpListener(OnJump);
            playerMovementProvider?.RemoveLandListener(OnLand);
        }

        private void Update()
        {
            if (playerControl.IsControllable)
            {
                SimpleSway();
            }

            // Apply combined transforms here
            ApplyCombinedTransform();
        }

        private void SimpleSway()
        {
            CalculateSway();
            CalculateSwayOffsets();
        }

        private void CalculateSway()
        {
            InputX = -InputManager.mousex / 10f - 2f * InputManager.controllerx;
            InputY = -InputManager.mousey / 10f - 2f * InputManager.controllery;
        }

        private void CalculateSwayOffsets()
        {
            float moveX = Mathf.Clamp(InputX * amount, -maxAmount, maxAmount);
            float moveY = Mathf.Clamp(InputY * amount, -1f, 1f);
            Vector3 targetPos = new Vector3(moveX, moveY, 0f);

            swayPositionOffset = Vector3.Lerp(swayPositionOffset, targetPos, Time.deltaTime * smoothAmount);

            float tiltX = Mathf.Clamp(InputX * tiltAmount, -maxTiltAmount, maxTiltAmount);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, tiltX);

            swayRotationOffset = Quaternion.Slerp(swayRotationOffset, targetRot, Time.deltaTime * smoothTiltAmount);
        }

        private void ApplyCombinedTransform()
        {
            // Combine the initial position + sway + jump position offsets
            Vector3 combinedPosition = initialPosition + swayPositionOffset + jumpPositionOffset;

            // Combine the initial rotation * sway rotation * jump rotation
            Quaternion combinedRotation = initialRotation * swayRotationOffset * jumpRotationOffset;

            transform.localPosition = combinedPosition;
            transform.localRotation = combinedRotation;
        }

        private void OnJump()
        {
            if (jumpMotionCoroutine != null) StopCoroutine(jumpMotionCoroutine);
            jumpMotionCoroutine = StartCoroutine(ApplyMotion(jumpMotion));
        }

        private void OnLand()
        {
            if (jumpMotionCoroutine != null) StopCoroutine(jumpMotionCoroutine);
            jumpMotionCoroutine = StartCoroutine(ApplyMotion(groundedMotion));
        }

        private IEnumerator ApplyMotion(AnimationCurve motionCurve)
        {
            float motion = 0f;

            while (motion < 1f)
            {
                motion += Time.deltaTime * evaluationSpeed;
                float evaluatedMotion = motionCurve.Evaluate(motion);

                // Update the Jump Offsets
                jumpPositionOffset = new Vector3(0f, evaluatedMotion * distance, 0f);
                jumpRotationOffset = Quaternion.Euler(evaluatedMotion * rotationAmount, 0f, 0f);

                yield return null;
            }

            // Reset Jump Offsets after motion ends to avoid getting stuck
            jumpPositionOffset = Vector3.zero;
            jumpRotationOffset = Quaternion.identity;
        }
    }
}