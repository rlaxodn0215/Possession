using UnityEngine;

namespace cowsins
{
    public class CameraFOVManager : MonoBehaviour
    {
        [SerializeField] private Rigidbody player;

        private float baseFOV;
        private Camera cam;
        private IPlayerMovementStateProvider movement; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private IWeaponReferenceProvider weaponProvider; // IWeaponReferenceProvider is implemented in WeaponController.cs
        private IWeaponBehaviourProvider weaponStateProvider; // IWeaponBehaviourProvider is implemented in WeaponController.cs
        private float targetFOV;
        private float lerpSpeed; 

        private void Start()
        {
            cam = GetComponent<Camera>();
            movement = player.GetComponent<IPlayerMovementStateProvider>();
            weaponProvider = player.GetComponent<IWeaponReferenceProvider>();
            weaponStateProvider = player.GetComponent<IWeaponBehaviourProvider>();

            baseFOV = movement.NormalFOV; // Initialize baseFOV once in Start
            targetFOV = baseFOV;

            cam.fieldOfView = baseFOV;
        }

        private void Update()
        {
            // Smoothly interpolate FOV towards the target value
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * lerpSpeed);
        }
        private bool AllowChangeFOV()
        {
            return weaponStateProvider.IsAiming && weaponProvider.Weapon != null;
        }

        public void SetFOV(float fov, float speed)
        {
            if (AllowChangeFOV())
                return; // Not applicable if aiming
            targetFOV = fov;
            lerpSpeed = speed;  
        }

        public void SetFOV(float fov)
        {
            if (AllowChangeFOV())
                return; // Not applicable if aiming
            targetFOV = fov;
            lerpSpeed = movement.FadeFOVAmount; 
        }

        public void ForceAddFOV(float fov)
        {
            cam.fieldOfView -= fov;
            lerpSpeed = movement.FadeFOVAmount;
        }
    }
}
