/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine;
using System.Collections;

namespace cowsins
{
    public class CrouchTilt : MonoBehaviour
    {
        [Tooltip("Rotation desired when crouching"), SerializeField] private Vector3 tiltRot, tiltPosOffset;
        [Tooltip("Tilting / Rotation velocity"), SerializeField] private float tiltSpeed;

        private IPlayerMovementEventsProvider player; // IPlayerMovementEventsProvider is implemented in PlayerMovement.cs
        private IWeaponBehaviourProvider wp; // IWeaponBehaviourProvider is implemented in WeaponController.cs
        private PlayerDependencies playerDependencies;

        private Quaternion origRot;
        private Vector3 origPos;
        private Coroutine tiltCoroutine;

        void Start()
        {
            playerDependencies = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerDependencies>();
            player = playerDependencies.PlayerMovementEvents;
            wp = playerDependencies.WeaponBehaviour;

            origRot = transform.localRotation;
            origPos = transform.localPosition;

            player.AddCrouchListener(StartCrouch);
            player.AddUncrouchListener(StopCrouch);
        }

        private void OnDisable()
        {
            player?.RemoveCrouchListener(StartCrouch);
            player?.RemoveUncrouchListener(StopCrouch);
        }

        private void StartCrouch()
        {
            if (!wp.IsAiming) StartTilting(tiltRot, origPos + tiltPosOffset);
        }

        private void StopCrouch()
        {
            StartTilting(origRot.eulerAngles, origPos);
        }

        private void StartTilting(Vector3 targetRotation, Vector3 targetPosition)
        {
            if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
            tiltCoroutine = StartCoroutine(TiltRoutine(targetRotation, targetPosition));
        }

        private IEnumerator TiltRoutine(Vector3 targetRotation, Vector3 targetPosition)
        {
            while (Quaternion.Angle(transform.localRotation, Quaternion.Euler(targetRotation)) > 0.1f ||
                   Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * tiltSpeed);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * tiltSpeed);
                yield return null;  // Wait for the next frame before continuing
            }
        }
    }
}
