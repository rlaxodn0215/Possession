using UnityEngine;

namespace cowsins
{
    public class CameraEffects : MonoBehaviour
    {

        [SerializeField, Header("SHARED REFERENCES")] private Transform playerCamera;
        [SerializeField] private Transform camShakeTarget;

        [SerializeField, Header("TILT")] private float tiltSpeed;
        [SerializeField] private float tiltAmount;

        [SerializeField, Tooltip("Maximum Head Bob"), Header("HEAD BOB")] private float headBobAmplitude = 0.2f;
        [SerializeField, Tooltip("Speed to reach the Maximum Head Bob ( headBobAmplitude)")] private float headBobFrequency = 2f;
        [SerializeField, Tooltip("Maximum Breathing Amount"), Header("BREATHING EFFECT")] private float breathingAmplitude = 0.2f;
        [SerializeField, Tooltip("Breathing Speed")] private float breathingFrequency = 2f;
        [SerializeField, Tooltip("Enables Rotation for the Breathing Effect")] private bool applyBreathingRotation;

        // Camera Shake
        private float trauma;
        public float Trauma { get { return trauma; } set { trauma = Mathf.Clamp01(value); } }

        private float power = 16;
        private float movementAmount = 0.8f;
        private float rotationAmount = 17f;

        private float traumaDepthMag = 0.6f;
        private float traumaDecay = 1.3f;

        float timeCounter = 0;


        private IPlayerMovementStateProvider player; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private IPlayerControlProvider playerControlProvider; // IPlayerControlProvider is implemented in PlayerControl.cs
        private Rigidbody playerRigidbody;

        private Vector3 origPos;
        private Quaternion origRot;

        private void Awake()
        {
            origPos = playerCamera.localPosition;
            origRot = playerCamera.localRotation;

            player = GetComponent<IPlayerMovementStateProvider>();
            playerControlProvider = GetComponent<IPlayerControlProvider>();
            playerRigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!playerControlProvider.IsControllable) return;

            UpdateTilt();

            UpdateHeadBob();
            UpdateBreathing();

            HandleCamShake();
        }

        private void UpdateTilt()
        {
            if (player.CurrentSpeed == 0) return;

            Quaternion rot = CalculateTilt();
            playerCamera.localRotation = Quaternion.Lerp(playerCamera.localRotation, rot, Time.deltaTime * tiltSpeed);
        }

        private Quaternion CalculateTilt()
        {
            float x = InputManager.x;
            float y = InputManager.y;

            Vector3 vector = new Vector3(y, 0, -x).normalized * tiltAmount;

            return Quaternion.Euler(vector);
        }

        private void UpdateHeadBob()
        {
            if (playerRigidbody.linearVelocity.magnitude < player.WalkSpeed || InputManager.jumping)
            {
                playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, origPos, Time.deltaTime * 2f);
                playerCamera.localRotation = Quaternion.Lerp(playerCamera.localRotation, origRot, Time.deltaTime * 2f);
                return;
            }

            float angle = Time.timeSinceLevelLoad * headBobFrequency;
            float distanceY = headBobAmplitude * Mathf.Sin(angle) / 400f;
            float distanceX = headBobAmplitude * Mathf.Cos(angle) / 100f;

            playerCamera.position = new Vector3(playerCamera.position.x, playerCamera.position.y + Vector3.up.y * distanceY, playerCamera.position.z);
            playerCamera.Rotate(distanceX, 0, 0, Space.Self);
        }

        private void UpdateBreathing()
        {
            float angle = Time.timeSinceLevelLoad * breathingFrequency;
            float distance = breathingAmplitude * Mathf.Sin(angle) / 400f;
            float distanceRot = breathingAmplitude * Mathf.Cos(angle) / 100f;

            playerCamera.position = new Vector3(playerCamera.position.x, playerCamera.position.y + Vector3.up.y * distance, playerCamera.position.z);

            if (applyBreathingRotation)
            {
                playerCamera.Rotate(distanceRot, 0, 0, Space.Self);
            }
        }

        #region CameraShake
        private float GetFloat(float seed) { return (Mathf.PerlinNoise(seed, timeCounter) - 0.5f) * 2f; }

        private Vector3 GetVec3() { return new Vector3(GetFloat(1), GetFloat(10), GetFloat(100) * traumaDepthMag); }

        private void HandleCamShake()
        {
            if (Trauma > 0)
            {
                timeCounter += Time.deltaTime * Mathf.Pow(Trauma, 0.3f) * power;

                Vector3 newPos = GetVec3() * movementAmount * Trauma;
                camShakeTarget.localPosition = newPos;

                camShakeTarget.localRotation = Quaternion.Euler(newPos * rotationAmount);

                Trauma -= Time.deltaTime * traumaDecay * (Trauma + 0.3f);
            }
            else
            {
                //lerp back towards default position and rotation once shake is done
                Vector3 newPos = Vector3.Lerp(camShakeTarget.localPosition, Vector3.zero, Time.deltaTime);
                camShakeTarget.localPosition = newPos;
                camShakeTarget.localRotation = Quaternion.Euler(newPos * rotationAmount);
            }
        }

        public void Shake(float amount, float _power, float _movementAmount, float _rotationAmount)
        {
            Trauma = amount;
            power = _power;
            movementAmount = _movementAmount;
            rotationAmount = _rotationAmount;
        }

        public void ShootShake(float amount)
        {
            Trauma += amount;
            power = 20;
            movementAmount = .8f;
            rotationAmount = 17f;
        }

        public void ExplosionShake(float distance)
        {
            Trauma += 10f / distance;
            power = 30;
            movementAmount = 1f;
            rotationAmount = 30f;
        }

        #endregion
    }
}
