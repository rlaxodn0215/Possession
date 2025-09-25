/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace cowsins
{
    // Add a rigidbody if needed, PlayerMovement.cs requires a rigidbody to work 
    [RequireComponent(typeof(Rigidbody))]
    //[RequireComponent(typeof(____))] Player Movement also requires a non trigger collider. Attach your preffered collider method
    public class PlayerMovement : MonoBehaviour, IPlayerMovementStateProvider, IPlayerMovementActionsProvider, IPlayerMovementEventsProvider
    {
        #region Classes & Enums

        [System.Serializable]
        public class Events // Store your events
        {
            public UnityEvent OnMove, OnJump, OnLand, OnCrouch, OnStopCrouch, OnSprint, OnSpawn, OnSlide, OnStartWallRun, OnStopWallRun,
                                OnWallBounce, OnStartDash, OnDashing, OnEndDash, OnStartClimb, OnClimbing, OnEndClimb,
                                OnStartGrapple, OnGrappling, OnStopGrapple, OnGrappleEnabled;
        }
        [System.Serializable]
        public class FootStepsAudio // store your footsteps audio
        {
            public AudioClip[] defaultStep, grassStep, metalStep, mudStep, woodStep;
        }
        [System.Serializable]
        public enum CrouchCancelMethod // Different methods to determine how crouch should stop
        {
            Smooth, FullStop
        }
        [System.Serializable]
        public enum CancelWallRunMethod // Different methods to determine when wallrun should stop
        {
            None, Timer
        }
        [System.Serializable]
        public enum DirectionalJumpMethod // Different methods to determine the jump method to apply
        {
            None, InputBased, ForwardMovement
        }
        [System.Serializable]
        public enum DashMethod // Different methods to determine the jump method to apply
        {
            ForwardAlways, InputBased, Free
        }
        [System.Serializable]
        public enum GrapplingHookMethod
        {
            Linear, Swing, Combined
        }

        [System.Serializable]
        public class Sounds
        {
            public AudioClip jumpSFX, landSFX;
        }

        [System.Serializable]
        public class GrappleSounds
        {
            public AudioClip startGrappleSFX, grappleImpactSFX;
        }

        #endregion

        #region Variables

        #region Assignables

        [Tooltip("This is where the parent of your camera should be attached.")]
        public Transform playerCam;

        [Tooltip("Handles the field of view of the camera.")]
        public CameraFOVManager cameraFOVManager;

        [Tooltip("Displays speed lines effects at certain speed")] public bool useSpeedLines;

        [SerializeField, Tooltip("Speed lines particle system.")] private ParticleSystem speedLines;

        [SerializeField, Min(0), Tooltip("Speed Lines will only be shown above this speed.")] private float minSpeedToUseSpeedLines;

        [SerializeField, Range(.1f, 2), Tooltip("Being 1 = default, amount of speed lines displayed.")] private float speedLinesAmount = 1;

        #endregion

        #region Movement

        [Tooltip("Enable this to instantly run without needing to press the sprint button down."), SerializeField] private bool autoRun;

        [Tooltip("If false, hold to sprint, and release to stop sprinting."), SerializeField]private bool alternateSprint;

        [Tooltip("If false, hold to crouch, and release to uncrouch."), SerializeField]private bool alternateCrouch;

        [Tooltip("If true: Speed while running backwards = runSpeed." +
        "       if false: Speed while running backwards = walkSpeed")]
        public bool canRunBackwards;

        [Tooltip("If true: Speed while running sideways = runSpeed." +
        "       if false: Speed while running sideways = walkSpeed"), SerializeField]
        private bool canRunSideways;

        [Tooltip("If true: Speed while shooting = runSpeed." +
       "       if false: Speed while shooting = walkSpeed")]
        public bool canRunWhileShooting;

        [Tooltip("Player deceleration from running speed to walking"), SerializeField]
        private float loseSpeedDeceleration;

        [Tooltip("Capacity to gain speed."), SerializeField]
        private float acceleration = 4500;

        [Min(0.01f), SerializeField]
        private float runSpeed, walkSpeed, crouchSpeed;

        [Min(0.01f)] public float crouchTransitionSpeed;

        [Tooltip("Set to true to enable the player to crouch")] public bool allowCrouch;

        [Tooltip("Determine the method to exit the crouch state.")] public CrouchCancelMethod crouchCancelMethod;

        [SerializeField, Tooltip("Distance to detect a roof. If an obstacle is detected within this distance, the player will not be able to uncrouch")] private float roofCheckDistance = 3.5f;

        [Min(0.01f)]
        [Tooltip("Max speed the player can reach. Velocity is clamped by this value.")] public float maxSpeedAllowed = 40;

        [Tooltip("Every object with this layer will be detected as ground, so you will be able to walk on it"), SerializeField] private LayerMask whatIsGround;

        [Tooltip("Distance from the bottom of the player to detect ground"), SerializeField, Min(0)] private float groundCheckDistance;

        [Range(0, 1f)]
        [Tooltip("Previously named Friction Force Amount (<1.3.6). Controls the snappiness of the character. The higher, the more responsive."), SerializeField]
        private float controlsResponsiveness = 0.175f;

        [Tooltip("Maximum slope angle that you can walk through."), SerializeField, Range(10, 80)]
        private float maxSlopeAngle = 35f;

        [Tooltip("Maximum Height that the player can climb upstairs"), SerializeField] private float maxStepHeight = 0.3f;
        [Tooltip("Distance to detect a staircase from the player"), SerializeField] private float maxStepDistance = 1f;
        [Tooltip("Amount to climb upstairs. Adjusts smoothness of the movement based on the size of the steps."), SerializeField] private float stepUpHeight = 0.1f;
        [Tooltip("Speed to lerp from current position to upstairs"), SerializeField] private float stepUpSpeed = 5f;
        #endregion

        #region Camera

        [Tooltip("Maximum Vertical Angle for the camera"), Range(20, 89.7f)]
        public float maxCameraAngle = 89.7f;

        [Tooltip("Horizontal sensitivity (X Axis)")] public float sensitivityX = 4;

        [Tooltip("Vertical sensitivity (Y Axis)")] public float sensitivityY = 4;

        [SerializeField] private bool invertYSensitivty = false;

        [Tooltip("Horizontal sensitivity (X Axis) using controllers"), SerializeField] private float controllerSensitivityX = 35;

        [Tooltip("Vertical sensitivity (Y Axis) using controllers"), SerializeField] private float controllerSensitivityY = 35;

        [SerializeField] private bool invertYControllerSensitivty = false;

        [Range(.1f, 1f), Tooltip("Sensitivity will be multiplied by this value when aiming"), SerializeField] private float aimingSensitivityMultiplier = .4f;

        [Tooltip("Default field of view of your camera"), Range(1, 179), SerializeField] private float normalFOV;

        [Tooltip("Running field of view of your camera"), Range(1, 179), SerializeField] private float runningFOV;

        [Tooltip("Wallrunning field of view of your camera"), Range(1, 179)] public float wallrunningFOV;

        [Tooltip("Amount of field of view that will be added to your camera when dashing."), Range(-179, 179)] public float fovToAddOnDash;

        [Tooltip("Fade Speed - Start Transition for the field of view")] public float fadeFOVAmount;

        [SerializeField, Range(-30, 30), Tooltip("Rotation of the camera when sliding. The rotation direction is defined by the sign of the value.")]
        private float slidingCameraTiltAmount;

        [SerializeField, Tooltip("Speed of the tilt camera movement. This is essentially used for wall running")] private float cameraTiltTransitionSpeed;

        #endregion

        #region Sliding

        [Tooltip("When true, player will be allowed to slide.")]
        public bool allowSliding;

        [Tooltip("Force added on sliding."), SerializeField]
        private float slideForce = 400;

        [Tooltip("If true, the player will be able to move while sliding."), SerializeField]
        private bool allowMoveWhileSliding;

        [Range(0, 1f), SerializeField, Tooltip("Force applied to counter movement when sliding")] private float slideFrictionForceAmount;

        #endregion

        #region Jumping

        [Tooltip("Enable this if your player can jump.")] public bool allowJump;

        [Tooltip("Amount of jumps you can do without touching the ground")][Min(1)] public int maxJumps;

        [Tooltip("Gains jump amounts when wallrunning.")] public bool resetJumpsOnWallrun;

        [Tooltip("Gains jump amounts when wallrunning.")] public bool resetJumpsOnWallBounce;

        [Tooltip("Gains jump amounts when grapple starts.")] public bool resetJumpsOnGrapple;

        [Tooltip("Double jump will reset fall damage, only if your player controller is optable to take fall damage")] public bool doubleJumpResetsFallDamage;

        [Tooltip("Method to apply on jumping when the player is not grounded, related to the directional jump")]
        public DirectionalJumpMethod directionalJumpMethod;

        [Tooltip("Force applied on an object in the direction of the directional jump"), SerializeField]
        private float directionalJumpForce;

        [Tooltip("The higher this value is, the higher you will get to jump."), SerializeField]
        private float jumpForce = 550f;

        [Tooltip("How much control you own while you are not grounded. Being 0 = no control of it, 1 = Full control.")]
        [Range(0, 1), SerializeField]
        private float controlAirborne = .5f;

        [Tooltip("Turn this on to allow the player to crouch while jumping")]
        public bool allowCrouchWhileJumping;

        [Tooltip(" Allow the player to jump mid-air")]
        public bool canJumpWhileCrouching;

        [Tooltip("Interval between jumping")][Min(.25f), SerializeField] private float jumpCooldown = .25f;

        [Range(0, .3f), Tooltip("Coyote jump allows users to perform more satisfactory and responsive jumps, especially when jumping off surfaces")] public float coyoteJumpTime;

        #endregion

        #region Aim Assist

        [Tooltip("Determine wether to apply aim assist or not.")]
        public bool applyAimAssist;

        [Min(.1f), SerializeField]
        private float maximumDistanceToAssistAim;

        [Tooltip("Snapping speed."), SerializeField]
        private float aimAssistSpeed;

        [Tooltip("size of the aim assist range."), SerializeField]
        private float aimAssistSensitivity = 3f;

        #endregion

        #region Stamina 

        [Tooltip("You will lose stamina on performing actions when true.")]
        public bool usesStamina;

        [Tooltip("Minimum stamina required to being able to run again."), SerializeField]
        private float minStaminaRequiredToRun;

        [Tooltip("Max amount of stamina."), SerializeField]
        private float maxStamina;

        [SerializeField, Min(1), Tooltip("Stamina Regeneration Speed")] private float staminaRegenMultiplier;

        [SerializeField] private bool LoseStaminaWalking;

        [Tooltip("Amount of stamina lost on jumping."), SerializeField]
        private float staminaLossOnJump;

        [Tooltip("Amount of stamina lost on sliding."), SerializeField]
        private float staminaLossOnSlide;

        [Tooltip("Amount of stamina lost on dashing."), SerializeField]
        private float staminaLossOnDash;

        [Tooltip("Our Slider UI Object. Stamina will be shown here."), SerializeField]
        private Slider staminaSlider;

        #endregion

        #region Advanced Movement

        #region WallRun

        [Tooltip("When enabled, it will allow the player to wallrun on walls")] public bool canWallRun;

        [SerializeField, Tooltip("Define wallrunnable wall layers. By default, this is set to the same as whatIsGround.")] private LayerMask whatIsWallRunWall;

        [Tooltip("When enabled, gravity will be applied on the player while wallrunning. If disabled, the player won´t lose height while wallrunning.")] public bool useGravity;

        [SerializeField, Min(0), Tooltip("Since we do not want to apply all the gravity force directly to the player, we shall define the force that will counter gravity. This force goes in the opposite direction from gravity.")]
        private float wallrunGravityCounterForce;

        [SerializeField, Min(0), Tooltip("Maximum speed reachable while wall running.")] private float maxWallRunSpeed;

        [SerializeField, Min(0), Tooltip("When wall jumping, force applied on the X axis, relative to the normal of the wall.")]
        private float normalWallJumpForce;

        [SerializeField, Min(0), Tooltip("When wall jumping, force applied on the Y axis.")]
        private float upwardsWallJumpForce;

        [SerializeField, Min(0), Tooltip("Impulse applied on the player when wall run is cancelled. This results in a more satisfactory movement. Note that this force goes in the direction of the normal of the wall the player is wall running.")]
        private float stopWallRunningImpulse;


        [SerializeField, Min(0), Tooltip("Minimum height above ground (in units) required to being able to start wall run. Wall run motion will be cancelled for heights below this.")]
        private float wallMinimumHeight;

        [SerializeField, Range(0, 30), Tooltip("Rotation of the camera when wall running. The rotation direction gets automatically adjusted by FPS Engine.")]
        private float wallrunCameraTiltAmount;

        [SerializeField, Min(.1f), Tooltip("Duration of wall run for cancelWallRunMethod = TIMER.")] private float wallRunDuration;

        [Tooltip("Method to determine length of wallRun.")] public CancelWallRunMethod cancelWallRunMethod;

        #endregion

        #region Wall Bounce

        [Tooltip("When enabled, it will allow the player to wall bounce on walls.")] public bool canWallBounce;

        [SerializeField, Tooltip("Force applied to the player on wall bouncing. Note that this force is applied on the direction of the reflection of both the player movement and the wall normal.")]
        private float wallBounceForce;

        [SerializeField, Tooltip("Force applied on the player on wall bouncing ( Y axis – Vertical Force ).")]
        private float wallBounceUpwardsForce;

        [SerializeField, Range(0.1f, 2), Tooltip("maximum Distance to detect a wall you can bounce on. This will use the same layer as wall run walls.")]
        private float oppositeWallDetectionDistance = 1;

        #endregion

        #region Dashing 
        [Tooltip("When enabled, it will allow the player to perform dashes.")]
        public bool canDash;

        [Tooltip("Method to determine how the dash will work")]
        public DashMethod dashMethod;

        [Tooltip("When enabled, it will allow the player to perform dashes.")] public bool infiniteDashes;

        [Min(1), Tooltip("maximum ( initial ) amount of dashes. Dashes will be regenerated up to “amountOfDashes”, you won´t be able to gain more dashes than this value, check dash Cooldown."), SerializeField]
        private int amountOfDashes = 3;

        [SerializeField, Min(.1f), Tooltip("Time to regenerate a dash on performing a dash motion.")]
        private float dashCooldown;

        [Tooltip("When enabled, player will not receive damage.")]
        public bool damageProtectionWhileDashing;

        [Tooltip("force applied when dashing. Note that this is a constant force, not an impulse, so it is being applied while the dash lasts.")]
        public float dashForce;

        [Range(.1f, .5f), Tooltip("Duration of the ability ( dash ).")]
        public float dashDuration;

        [Tooltip("When enabled, it will allow the player to shoot while dashing.")]
        public bool canShootWhileDashing;
        #endregion

        #region Grappling Hook

        [Tooltip("If true, allows the player to use a customizable grappling hook.")] public bool allowGrapple;

        [SerializeField, Tooltip("Maximum distance allowed for the player to begin a grapple action")] private float maxGrappleDistance = 50;

        [Tooltip("Defines the grappling hook's behavior: 'Linear' pulls the player directly to the point, while 'Swing' allows physics-based swinging.")] public GrapplingHookMethod grapplingHookMethod;

        [SerializeField, Tooltip("Length of the grapple rope once connected (used for swinging)")] private float grappleRopeLength = 10f;

        [SerializeField, Tooltip("Once the grapple has finished, time to enable it again.")] private float grappleCooldown = 1;

        [SerializeField, Tooltip("If the distance between the grapple end point is equal or less than this value ( in Unity units ), the grapple will break.")] private float distanceToBreakGrapple = 2;

        [SerializeField, Tooltip("Force applied to the player on grappling.")] private float grappleForce = 400;

        [SerializeField, Tooltip("Grappling Hook only: influences player movement while swinging based on camera direction.")] private float cameraInfluence;

        [SerializeField, Tooltip("Spring Force applied to the rope on grappling.")] private float grappleSpringForce = 4.5f;

        [SerializeField, Tooltip("Damper applied to reduce ropes´ oscillation.")] private float grappleDamper = 7f;

        [SerializeField, Tooltip("Time that takes the rope to draw from start to end."), Header("Grapple Hook Effects")] private float drawDuration = .2f;

        [SerializeField, Tooltip("Amount of vertices the rope has. The bigger this value, the better it will look, but the less performant the effect will be.")] private int ropeResolution = 20;

        [SerializeField, Tooltip("Size of the wave effect on grappling. This effect is based on sine.")] private float waveAmplitude = 1.4f;

        [SerializeField, Tooltip("Speed to decrease the wave amplitude over time.")] private float waveAmplitudeMitigation = 6;

        #endregion

        #region Climbing Ladders

        [Tooltip("If true, allows the player to use a customizable grappling hook.")] public bool canClimb = true;

        [SerializeField, Min(0)]
        private float maxLadderDetectionDistance = 1;

        [SerializeField, Min(0)]
        private float climbSpeed = 15;

        [SerializeField, Min(0)]
        private float topReachedUpperForce = 10;

        [SerializeField, Min(0)]
        private float topReachedForwardForce = 10;

        [SerializeField]
        private bool allowVerticalLookWhileClimbing = true;

        [SerializeField]
        private bool hideWeaponWhileClimbing = true;

        #endregion

        #endregion

        #region Others

        [SerializeField] private Sounds sounds;

        [SerializeField] private GrappleSounds grappleSounds;

        [SerializeField] private FootStepsAudio footsteps;

        [Tooltip("Volume of the AudioSource."), SerializeField]
        private float footstepVolume;

        [Tooltip("Play speed of the footsteps."), SerializeField, Range(.1f, .95f)] private float footstepSpeed;

        public Events userEvents;

        #endregion

        #region Internal Use

        public LayerMask WhatIsGround => whatIsGround;

        //Rotation and look
        private float cameraPitch;
        private float cameraYaw;
        private float cameraRoll;

        // Controls the current sensitivity.
        // Sensitivity can be overrided by the Game Settings Manager
        private float currentSensX, currentSensY, currentControllerSensX, currentControllerSensY;

        private PlayerOrientation orientation;
        private Vector3 moveDirection;
        private float currentSpeed;
        private bool grounded;

        private readonly float frictionThreshold = 0.1f;

        // Stairs
        private bool isSteppingStairs;
        public bool showStairsDebugInfo = false; 

        //Crouch & Slide
        public Vector3 crouchScale { get; private set; } = new Vector3(1, 0.5f, 1);
        private Vector3 playerScale;
        public Vector3 PlayerScale => playerScale;
        private RaycastHit slopeHit;

        //Jumping
        private bool enoughStaminaToJump = true;
        private bool readyToJump = true;
        public bool ReadyToJump => readyToJump;
        // Footsteps
        // Store the layers to avoid Allocations during runtime
        private int groundLayer, grassLayer, metalLayer, mudLayer, woodLayer;

        // Stamina
        private float stamina;

        private bool enoughStaminaToRun;

        // Wall Run
        private bool wallRunning = false;

        // Dashing
        public bool dashing;
        public int currentDashes;

        // Crouching
        public bool isCrouching;

        // Audio
        private AudioSource _audio;
        private float stepTimer;

        // GRAPPLE
        private float elapsedTime;

        private float currentWaveAmplitude = 0;

        private bool grappling;

        private bool grappleEnabled = true;

        private LineRenderer grappleRenderer;

        private Vector3 grapplePoint;

        private SpringJoint joint;

        public int jumpCount;
        private bool hasJumped = false;

        public bool canCoyote;
        private float coyoteTimer;

        private float currentFloorAngle;
        private bool cancellingGrounded;
        private Coroutine ungroundCoroutine;

        public bool wallLeft { get; private set; }
        public bool wallRight { get; private set; }
        public bool wallOpposite { get; private set; }

        private RaycastHit wallLeftHit, wallRightHit, wallOppositeHit;

        private Vector3 wallNormal;

        private Vector3 wallDirection;

        private float wallRunTimer = 0;

        private bool grappleImpacted = false;

        // CLIMB
        private bool isClimbing;

        // IPlayerMovementProvider
        public PlayerOrientation Orientation => orientation;
        public float CurrentSpeed => currentSpeed;
        public float RunSpeed => runSpeed;
        public float WalkSpeed => walkSpeed;
        public float CrouchSpeed => crouchSpeed;
        public bool Grounded => grounded;
        public bool IsCrouching => isCrouching;
        public bool WallRunning => wallRunning;
        public bool Dashing => dashing;
        public bool CanShootWhileDashing => canShootWhileDashing;
        public bool DamageProtectionWhileDashing => damageProtectionWhileDashing;
        public float NormalFOV => normalFOV;
        public float FadeFOVAmount => fadeFOVAmount;
        public float WallRunningFOV => wallrunningFOV;
        public bool AlternateSprint => alternateSprint;
        public bool AlternateCrouch => alternateCrouch;

        public event Action OnJump;
        public event Action OnLand;
        public event Action OnCrouch;
        public event Action OnUncrouch;
        public void AddJumpListener(Action listener) => OnJump += listener;
        public void RemoveJumpListener(Action listener) => OnJump -= listener;
        public void AddLandListener(Action listener) => OnLand += listener;
        public void RemoveLandListener(Action listener) => OnLand -= listener;
        public void AddCrouchListener(Action listener) => OnCrouch += listener;
        public void RemoveCrouchListener(Action listener) => OnCrouch -= listener;
        public void AddUncrouchListener(Action listener) => OnUncrouch += listener;
        public void RemoveUncrouchListener(Action listener) => OnUncrouch -= listener;


        // References
        private PlayerDependencies playerDependencies;
        private IWeaponReferenceProvider weaponReference; // IWeaponReferenceProvider is implemented in WeaponController.cs
        private IWeaponBehaviourProvider weaponController; // IWeaponBehaviourProvider is implemented in WeaponController.cs
        private IWeaponRecoilProvider weaponRecoil; // IWeaponRecoilProvider is implemented in WeaponController.cs
        private IPlayerStatsProvider playerStatsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private IFallHeightProvider fallHeightProvider; // IFallHeightProvider is implemented in PlayerStats.cs
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs
        private PlayerMultipliers playerMultipliers;
        private Rigidbody rb;
        private Collider playerCollider;
        private CapsuleCollider playerCapsuleCollider;
        private PlayerStates playerStates;
        private WeaponAnimator weaponAnimator;

        #endregion

        #region Getters
        public float RoofCheckDistance => roofCheckDistance;
        public bool EnoughStaminaToJump { get { return enoughStaminaToJump; } set { enoughStaminaToJump = value; } }

        public float StaminaLossOnDash => staminaLossOnDash;
        public bool IsClimbing
        {
            get { return isClimbing; }
            set { isClimbing = value; }
        }
        #endregion

        #endregion

        #region PlayerMovement Logic

        #region Basic

        private void Awake()
        {
            orientation = new PlayerOrientation(transform.position + Vector3.up * 2, Quaternion.identity);
            playerScale = transform.localScale;
        }

        private void Start()
        {
            GetAllReferences();
            GatherSensitivityValues();
            InputManager.Instance.SetPlayerInputModes(alternateSprint, alternateCrouch);

            enoughStaminaToRun = true;
            enoughStaminaToJump = true;
            jumpCount = maxJumps;

            if (canDash && !infiniteDashes)
            {
                UIEvents.onInitializeDashUI?.Invoke(amountOfDashes);
                currentDashes = amountOfDashes;
            }

            ResetStamina();
            userEvents.OnSpawn.Invoke();
        }

        private void Update()
        {
            CheckGrounded();

            if (canWallBounce) CheckOppositeWall();

            if (InputManager.jumping && wallOpposite && canWallBounce && playerControl.IsControllable && CheckHeight()) WallBounce();

            HandleStairs(moveDirection);
        }
        private void FixedUpdate()
        {
            bool isPlayerOnSlope = IsPlayerOnSlope();
            // Added Gravity
            // Gravity is added only if we are not on a slope or climbing to prevent unvoluntary sliding
            if (!isPlayerOnSlope && !isClimbing) rb.AddForce(Vector3.down * 30.19f, ForceMode.Acceleration);

            if (rb.linearVelocity.magnitude > maxSpeedAllowed) rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeedAllowed);

            Stamina();

            if (!playerControl.IsControllable) return;

            if (canWallRun)
            {
                CheckWalls();
                if (InputManager.yMovementActioned && (wallLeft || wallRight) && CheckHeight() && currentSpeed * playerMultipliers.playerWeightMultiplier > walkSpeed && !grounded) WallRun();
                else StopWallRun();
            }
        }

        /// <summary>
        /// Basically find everything the script needs to work
        /// </summary>
        private void GetAllReferences()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            playerDependencies = GetComponent<PlayerDependencies>();
            rb = GetComponent<Rigidbody>();
            _audio = GetComponent<AudioSource>();
            weaponReference = playerDependencies.WeaponReference;
            weaponController = playerDependencies.WeaponBehaviour; // WeaponController.cs implements this interface
            weaponRecoil = playerDependencies.WeaponRecoil; // WeaponController.cs implements this interface
            playerStatsProvider = playerDependencies.PlayerStats; // PlayerStats.cs implements this interface & it provides Health Information
            fallHeightProvider = playerDependencies.FallHeight; // PlayerStats.cs implements this interface & it provides Fall Height Information
            playerControl = playerDependencies.PlayerControl; // PlayerControl.cs implements this interface & it provides COntrol Information
            playerStates = GetComponent<PlayerStates>();
            playerMultipliers = GetComponent<PlayerMultipliers>();
            playerCollider = GetComponent<Collider>();
            playerCapsuleCollider = GetComponent<CapsuleCollider>();
            weaponAnimator = GetComponent<WeaponAnimator>();
            if (allowGrapple)
                grappleRenderer = GetComponent<LineRenderer>();
            else Destroy(GetComponent<LineRenderer>());

            // Get the Layers when the game starts to avoid allocations later on during runtime
            groundLayer = LayerMask.NameToLayer("Ground");
            grassLayer = LayerMask.NameToLayer("Grass");
            metalLayer = LayerMask.NameToLayer("Metal");
            mudLayer = LayerMask.NameToLayer("Mud");
            woodLayer = LayerMask.NameToLayer("Wood");
        }

        private void GatherSensitivityValues()
        {
            if (GameSettingsManager.Instance)
            {
                currentSensX = GameSettingsManager.Instance.playerSensX;
                currentSensY = GameSettingsManager.Instance.playerSensY;
                currentControllerSensX = GameSettingsManager.Instance.playerControllerSensX;
                currentControllerSensY = GameSettingsManager.Instance.playerControllerSensY;
            }
            else
            {
                currentSensX = sensitivityX;
                currentSensY = sensitivityY;
                currentControllerSensX = controllerSensitivityX;
                currentControllerSensY = controllerSensitivityY;
            }
        }

        #endregion

        #region Velocities & Friction

        public void HandleVelocities()
        {
            HandleSpeedLines();

            if (weaponReference.Weapon != null && weaponController.IsAiming && weaponReference.Weapon.setMovementSpeedWhileAiming)
            {
                currentSpeed = weaponReference.Weapon.movementSpeedWhileAiming;
                return;
            }

            if ((InputManager.sprinting || autoRun) && enoughStaminaToRun)
            {
                bool movingBackward = InputManager.y < 0;
                bool shootingWhileDisallowed = InputManager.shooting && weaponReference.Weapon != null && !canRunWhileShooting;
                bool onlyStrafing = InputManager.x != 0 && InputManager.y == 0 && !canRunSideways;

                bool canRun = !((!canRunBackwards && movingBackward) ||
                    shootingWhileDisallowed ||
                    onlyStrafing);

                if (canRun)
                {
                    bool movingForward = Vector3.Dot(orientation.Forward, rb.linearVelocity) > 0;
                    bool forwardAllowed = canRunBackwards || movingForward;
                    bool sidewaysAllowed = canRunSideways || (InputManager.x == 0 && InputManager.y != 0);
                    bool shootingAllowed = canRunWhileShooting || !InputManager.shooting;

                    if (forwardAllowed && sidewaysAllowed && shootingAllowed)
                    {
      
                        if (currentSpeed != runSpeed && !wallRunning && rb.linearVelocity.magnitude > .1f)
                            cameraFOVManager.SetFOV(runningFOV);

                        currentSpeed = runSpeed;
                        userEvents.OnSprint.Invoke();
                        return;
                    }
                }

                currentSpeed = Mathf.MoveTowards(currentSpeed, walkSpeed, Time.deltaTime * loseSpeedDeceleration);
            }
            else
            {
                if (currentSpeed != walkSpeed && !wallRunning)
                    cameraFOVManager.SetFOV(normalFOV);

                currentSpeed = walkSpeed;
            }


            if (rb.linearVelocity.sqrMagnitude < 0.0001f)
            {
                if (currentSpeed != walkSpeed) cameraFOVManager.SetFOV(normalFOV);
                currentSpeed = walkSpeed;
            }
        }

        /// <summary>
        /// Add friction force to the player when it´s not airborne
        /// Please note that it counters movement, since it goes in the opposite direction to velocity
        /// </summary>
        private void FrictionForce(float x, float y, Vector2 mag)
        {
            // Prevent from adding friction on an airborne body
            if (!grounded || InputManager.jumping || hasJumped) return;

            float friction = IsSliding() ? slideFrictionForceAmount : controlsResponsiveness;
            
            // Counter movement ( Friction while moving )
            // Prevent from sliding not on purpose
            if (Math.Abs(mag.x) > frictionThreshold && Math.Abs(x) < 0.5f || (mag.x < -frictionThreshold && x > 0) || (mag.x > frictionThreshold && x < 0))
            {
                rb.AddForce(acceleration * orientation.Right * Time.deltaTime * -mag.x * friction);
            }
            if (Math.Abs(mag.y) > frictionThreshold && Math.Abs(y) < 0.05f || (mag.y < -frictionThreshold && y > 0) || (mag.y > frictionThreshold && y < 0))
            {
                rb.AddForce(acceleration * orientation.Forward * Time.deltaTime * -mag.y * friction);
            }
        }

        /// <summary>
        /// Limits diagonal velocity
        /// </summary>
        private void ClampToCurrentSpeed()
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float currentWeightedSpeed = currentSpeed * playerMultipliers.playerWeightMultiplier;
            if (horizontalVelocity.magnitude > currentWeightedSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * currentWeightedSpeed;
                rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
            }
        }

        /// <summary>
        /// Find the velocity relative to where the player is looking
        /// Useful for vectors calculations regarding movement and limiting movement
        /// </summary>
        /// <returns></returns>
        private Vector2 FindVelRelativeToLook()
        {
            // Convert velocity to local space relative to the player's look direction
            Vector3 localVel = Quaternion.Euler(0, -orientation.Yaw, 0) * rb.linearVelocity;
            return new Vector2(localVel.x, localVel.z);
        }

        #endregion

        #region Movement 
        /// <summary>
        /// Handle all the basics related to the movement of the player.
        /// </summary>
        public void Movement()
        {
            //Extra gravity
            rb.AddForce(Vector3.down * Time.fixedDeltaTime * 10);

            //Find actual velocity relative to where player is looking
            Vector2 relativeVelocity = FindVelRelativeToLook();
            float xRelativeVelocity = relativeVelocity.x, yRelativeVelocity = relativeVelocity.y;

            //Counteract sliding and sloppy movement
            FrictionForce(InputManager.x, InputManager.y, relativeVelocity);
            //If speed is larger than maxspeed, clamp the velocity so you don't go over max speed
            ClampToCurrentSpeed();

            if (rb.linearVelocity.sqrMagnitude < .02f) rb.linearVelocity = Vector3.zero;

            if (!playerControl.IsControllable)
            {
                if (grounded) rb.linearVelocity = Vector3.zero;
                return;
            }

            if (IsSliding() && !allowMoveWhileSliding) return;

            float airborneMultiplier = !grounded ? controlAirborne : 1;
            float movementMultipliers = acceleration * Time.deltaTime * airborneMultiplier;

            if (IsPlayerOnSlope())
            {
                moveDirection = GetSlopeDirection();
                rb.useGravity = false;
                if (rb.linearVelocity.y > 0) rb.AddForce(Vector3.down * 150);
            }
            else
            {
                moveDirection = (orientation.Forward * InputManager.y + orientation.Right * InputManager.x).normalized;
            }

            if(moveDirection.magnitude > .1f) userEvents.OnMove.Invoke();
            
            rb.AddForce(moveDirection * movementMultipliers);
        }
        public void HandleStairs(Vector3 moveDirection)
        {
            if (!grounded || moveDirection == Vector3.zero || IsPlayerOnSlope() || isClimbing || hasJumped)
            {
                if(moveDirection == Vector3.zero && rb.linearVelocity.y < 0 && grounded)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                isSteppingStairs = false;
                return;
            }

            Vector3 capsuleBottom = playerCapsuleCollider.transform.TransformPoint(
            playerCapsuleCollider.center - Vector3.up * (playerCapsuleCollider.height / 2)
            );
            Vector3 lowerRayOrigin = capsuleBottom + Vector3.up * .05f;
            Vector3 upperRayOrigin = lowerRayOrigin + Vector3.up * maxStepHeight;

            float rayOffset = maxStepDistance + playerCapsuleCollider.radius;
            moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;

            bool lowerHit = Physics.Raycast(lowerRayOrigin, moveDirection, out RaycastHit leftHit, rayOffset, whatIsGround);
            Debug.DrawRay(lowerRayOrigin, moveDirection.normalized * rayOffset, Color.red);

            if (lowerHit)
            {
                float hitDistance = leftHit.distance;
                bool upperHit = Physics.Raycast(upperRayOrigin, moveDirection, out RaycastHit upperRaycastHit, hitDistance, whatIsGround);
                Debug.DrawRay(upperRayOrigin, moveDirection.normalized * rayOffset, Color.yellow);

                if (!upperHit)
                {
                    isSteppingStairs = true;
                    SnapStairsPosition(capsuleBottom, rayOffset, maxStepHeight, stepUpHeight, stepUpHeight);
                }
            }
            else
            {
                // Avoid the little jump effect when stairs finish due to moving the player forward and vertically.
                // Cancels out any vertical velocity if dashing into a staircase.
                if (isSteppingStairs)
                {
                    var vel = rb.linearVelocity;
                    vel.y = 0;
                    rb.linearVelocity = vel;
                }
                else if(!isCrouching)
                {
                    Vector3 origin = playerCapsuleCollider.bounds.center;

                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance + maxStepDistance, whatIsGround))
                    {
                        if (IsFloor(hit.normal))
                        {
                            ResetGroundingState();
                        }
                    }
                }
                isSteppingStairs = false;
            }
        }

        private void SnapStairsPosition(Vector3 origin, float rayOffset, float stepHeight, float rayLength, float yPositionOffset)
        {
            Vector3 stepUpPosition = origin + moveDirection.normalized * rayOffset + Vector3.up * stepHeight;
            Debug.DrawRay(stepUpPosition, Vector3.down * rayLength, Color.green);
            if (Physics.Raycast(stepUpPosition, Vector3.down, out RaycastHit groundHit, rayLength, whatIsGround))
            {
                Vector3 targetPosition = new Vector3(stepUpPosition.x, groundHit.point.y + yPositionOffset, stepUpPosition.z);

                Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, stepUpSpeed * Time.deltaTime);

                rb.MovePosition(newPosition);
            }
        }
        #endregion

        #region Camera
        /// <summary>
        /// Handle all the basics related to camera movement 
        /// </summary>
        public void Look()
        {
            int sensYInverted = invertYSensitivty ? -1 : 1;
            int sensYInvertedController = invertYControllerSensitivty ? 1 : -1;
            float sensitivityMultiplier = (weaponController.IsAiming) ? aimingSensitivityMultiplier : 1;

            // Grab the Inputs from the user.
            float rawMouseX = InputManager.GatherRawMouseX(currentSensX, currentControllerSensX);
            float rawMouseY = InputManager.GatherRawMouseY(sensYInverted, sensYInvertedController, currentSensY, currentControllerSensY);
            float mouseX = rawMouseX * sensitivityMultiplier;
            float mouseY = rawMouseY * sensitivityMultiplier;

            // Calculate new yaw rotation ( around the y axis )
            cameraYaw = playerCam.transform.localRotation.eulerAngles.y + mouseX + weaponRecoil.RecoilYawOffset * Time.deltaTime;
            //Rotate Camera Pitch ( around x axis )
            cameraPitch -= mouseY - weaponRecoil.RecoilPitchOffset * Time.deltaTime; 
            // Make sure we dont over- or under-rotate.
            // The reason why the value is 89.7 instead of 90 is to prevent errors with the wallrun
            cameraPitch = Mathf.Clamp(cameraPitch, -maxCameraAngle, maxCameraAngle);

            CalculateCameraRoll();

            // Handle camera rotation
            playerCam.transform.localRotation = Quaternion.Euler(cameraPitch, cameraYaw, cameraRoll);
            orientation.UpdateOrientation(transform.position, cameraYaw);

            HandleAimAssist();
        }

        public void VerticalLook()
        {
            if (!allowVerticalLookWhileClimbing || PauseMenu.isPaused) return;

            int sensYInverted = invertYSensitivty ? -1 : 1;
            int sensYInvertedController = invertYControllerSensitivty ? -1 : 1;
            float sensitivityMultiplier = (weaponController.IsAiming) ? aimingSensitivityMultiplier : 1;

            // Grab the Inputs from the user. Since we will only move the camera vertically, we only need Mouse Y
            float rawMouseY = InputManager.GatherRawMouseY(sensYInverted, sensYInvertedController, currentSensY, currentControllerSensY);
            float mouseY = rawMouseY * sensitivityMultiplier;

            // Make sure we dont over- or under-rotate.
            // The reason why the value is 89.7 instead of 90 is to prevent errors with the wallrun
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxCameraAngle, maxCameraAngle);

            // Handle camera rotation
            playerCam.transform.localRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
        }

        private void CalculateCameraRoll()
        {
            if (wallRunning && canWallRun) cameraRoll = wallLeft ? Mathf.Lerp(cameraRoll, -wallrunCameraTiltAmount, Time.deltaTime * cameraTiltTransitionSpeed) : Mathf.Lerp(cameraRoll, wallrunCameraTiltAmount, Time.deltaTime * cameraTiltTransitionSpeed);
            else if (isCrouching && rb.linearVelocity.magnitude >= walkSpeed && allowSliding && !hasJumped) cameraRoll = Mathf.Lerp(cameraRoll, slidingCameraTiltAmount, Time.deltaTime * cameraTiltTransitionSpeed);
            else cameraRoll = Mathf.Lerp(cameraRoll, 0, Time.deltaTime * cameraTiltTransitionSpeed);
        }
        #endregion

        #region Crouch & Sliding

        public void StartCrouch()
        {
            if (!allowCrouch) return;

            isCrouching = true;
            OnCrouch?.Invoke(); // Internal Event

            if (crouchCancelMethod == CrouchCancelMethod.FullStop)
                currentSpeed = crouchSpeed;

            if (rb.linearVelocity.magnitude >= walkSpeed && grounded && allowSliding && !hasJumped)
            { // Handle sliding
                userEvents.OnSlide.Invoke(); // Invoke your own method on the moment you slid NOT WHILE YOU ARE SLIDING
                                         // Add the force on slide
                rb.AddForce(orientation.Forward * slideForce);
                //staminaLoss
                if (usesStamina) stamina -= staminaLossOnSlide;
            }
        }

        public void StopCrouch()
        {
            isCrouching = false;
            transform.localScale = Vector3.MoveTowards(transform.localScale, playerScale, Time.deltaTime * crouchTransitionSpeed);
        }

        public void UncrouchEvents()
        {
            OnUncrouch?.Invoke(); // Internal Event
            userEvents.OnStopCrouch?.Invoke();
        }

        public void SmoothCrouchCancel() => currentSpeed = Mathf.MoveTowards(currentSpeed, crouchSpeed, Time.deltaTime * crouchTransitionSpeed * 1.5f);

        private bool IsSliding()
        {
            return isCrouching && rb.linearVelocity.magnitude >= crouchSpeed;
        }

        /// <summary>
        /// Get the direction of movement in a slope
        /// </summary>
        /// <returns></returns>
        private Vector3 GetSlopeDirection()
        {
            return Vector3.ProjectOnPlane(orientation.Forward * InputManager.y + orientation.Right * InputManager.x, slopeHit.normal).normalized;
        }


        #endregion

        #region Jump

        public void Jump()
        {
            if (!allowJump || jumpCount <= 0) return;

            // Used for Internal Events
            OnJump?.Invoke();

            jumpCount--;
            readyToJump = false;
            hasJumped = true;
            Invoke(nameof(FinishCoyote), coyoteJumpTime);
            cancellingGrounded = false;

            // Gather & Store Movement Input
            float inputX = InputManager.x;
            float inputY = InputManager.y;

            // Store velocity
            Vector3 velocity = rb.linearVelocity;
            // Reset Y velocity before jumping
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            if (doubleJumpResetsFallDamage) fallHeightProvider?.SetFallHeight(transform.position.y);

            //Add jump forces
            if (wallRunning)
            {
                // When we wallrun, we want to add extra side forces
                rb.AddForce(transform.up * upwardsWallJumpForce);
                rb.AddForce(wallNormal * normalWallJumpForce, ForceMode.Impulse);
            }
            else
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                // Handle directional jumping
                if (!grounded && directionalJumpMethod != DirectionalJumpMethod.None && maxJumps > 1 && !wallOpposite)
                {
                    if (Vector3.Dot(rb.linearVelocity, new Vector3(inputX, 0, inputY)) > .5f)
                        rb.linearVelocity /= 2f;

                    if (directionalJumpMethod == DirectionalJumpMethod.InputBased) // Input based method for directional jumping
                    {
                        rb.AddForce(orientation.Right * inputX * directionalJumpForce, ForceMode.Impulse);
                        rb.AddForce(orientation.Forward * inputY * directionalJumpForce, ForceMode.Impulse);
                    }
                    if (directionalJumpMethod == DirectionalJumpMethod.ForwardMovement) // Forward Movement method for directional jumping, dependant on orientation
                        rb.AddForce(orientation.Forward * Mathf.Abs(inputY) * directionalJumpForce, ForceMode.VelocityChange);
                }
            }

            //staminaLoss
            if (usesStamina) stamina -= staminaLossOnJump;

            SoundManager.Instance.PlaySound(sounds.jumpSFX, 0, 0, false);
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        public void HandleCoyoteJump()
        {
            if (grounded) coyoteTimer = 0;
            else coyoteTimer -= Time.deltaTime;

            if (hasJumped) return;
            canCoyote = coyoteTimer > 0 && readyToJump;
        }

        private void FinishCoyote()
        {
            hasJumped = false;
        }
        private void ResetJump() => readyToJump = true;

        #endregion

        #region Stamina

        private void Stamina()
        {
            // Check if we def wanna use stamina
            if (!usesStamina || playerStatsProvider.IsDead || !playerControl.IsControllable) return;

            float oldStamina = stamina; // Store stamina before we change its value

            // We ran out of stamina
            if (stamina <= 0)
            {
                enoughStaminaToRun = false;
                enoughStaminaToJump = false;
                stamina = 0;
            }

            // Wait for stamina to regenerate up to the min value allowed to start running and jumping again
            if (stamina >= minStaminaRequiredToRun)
            {
                enoughStaminaToRun = true; enoughStaminaToJump = true;
            }

            // Regen stamina
            if (stamina < maxStamina)
            {
                if (currentSpeed <= walkSpeed && !LoseStaminaWalking
                    || currentSpeed < runSpeed && (!LoseStaminaWalking || LoseStaminaWalking && InputManager.x == 0 && InputManager.y == 0))
                    stamina += Time.deltaTime * staminaRegenMultiplier;
            }

            // Lose stamina
            if (currentSpeed == runSpeed && enoughStaminaToRun && !wallRunning) stamina -= Time.deltaTime;
            if (currentSpeed < runSpeed && LoseStaminaWalking && (InputManager.x != 0 || InputManager.y != 0)) stamina -= Time.deltaTime * (walkSpeed / runSpeed);

            // Stamina UI not found might be a problem, it won´t be shown but you will get notified 
            if (staminaSlider == null)
            {
                Debug.LogWarning("REMEMBER: You forgot to attach your StaminaSlider UI Component, so it won´t be shown.");
                return;
            }

            // Handle stamina UI 
            if (oldStamina != stamina)
                staminaSlider.gameObject.SetActive(true);
            else
                staminaSlider.gameObject.SetActive(false);

            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = stamina;
        }

        public void ReduceStamina(float amount) => stamina -= amount;

        public void ResetStamina() => stamina = maxStamina;

        #endregion

        #region WallRun

        private void WallRun()
        {
            wallNormal = wallRight ? wallRightHit.normal : wallLeftHit.normal;
            wallDirection = Vector3.Cross(wallNormal, transform.up).normalized * 10;
            // Fixing wallrunning directions depending on the orientation 
            if ((orientation.Forward - wallDirection).magnitude > (orientation.Forward + wallDirection).magnitude) wallDirection = -wallDirection;

            // Handling WallRun Cancel
            if (OppositeVectors() < -.5f) StopWallRun();

            if (cancelWallRunMethod == CancelWallRunMethod.Timer)
            {
                wallRunTimer -= Time.deltaTime;
                if (wallRunTimer <= 0)
                {
                    rb.AddForce(wallNormal * stopWallRunningImpulse, ForceMode.Impulse);
                    StopWallRun();
                }
            }

            // Start Wallrunning
            if (!wallRunning) StartWallRunning();

            rb.useGravity = useGravity;

            if (rb.linearVelocity.y < 0)
            {
                if(useGravity)
                    rb.AddForce(transform.up * wallrunGravityCounterForce, ForceMode.Force);
                else rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxWallRunSpeed);

            if (!(wallRight && InputManager.x < 0) && !(wallLeft && InputManager.x > 0)) rb.AddForce(-wallNormal * 100, ForceMode.Force);

            rb.AddForce(wallDirection, ForceMode.Force);


        }

        private void StartWallRunning()
        {
            wallRunning = true;
            wallRunTimer = wallRunDuration;
            userEvents.OnStartWallRun.Invoke();
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(wallDirection, ForceMode.Impulse);
            if (resetJumpsOnWallrun) jumpCount = maxJumps;

            // Reset footsteps so they immediately play when starting to wall run
            stepTimer = 0;
            cameraFOVManager.SetFOV(wallrunningFOV);
        }


        private void StopWallRun()
        {
            if (wallRunning)
            {
                userEvents.OnStopWallRun.Invoke();
                rb.AddForce(wallNormal * stopWallRunningImpulse, ForceMode.Impulse);
                if (resetJumpsOnWallrun) jumpCount = maxJumps - 1;


                cameraFOVManager.SetFOV(normalFOV);
            }
            wallRunning = false;
            if (!isClimbing && !IsPlayerOnSlope())
                rb.useGravity = true;
        }

        private void CheckWalls()
        {
            wallLeft = Physics.Raycast(transform.position, -orientation.Right, out wallLeftHit, .8f, whatIsWallRunWall);
            wallRight = Physics.Raycast(transform.position, orientation.Right, out wallRightHit, .8f, whatIsWallRunWall);
        }

        private bool CheckHeight() { return !Physics.Raycast(transform.position, Vector3.down, wallMinimumHeight, whatIsWallRunWall); }

        #endregion

        #region Wall Bounce

        private float OppositeVectors() { return Vector3.Dot(wallDirection, orientation.Forward); }

        private void CheckOppositeWall()
        {
            wallOpposite = Physics.Raycast(transform.position, orientation.Forward, out wallOppositeHit, oppositeWallDetectionDistance, whatIsWallRunWall);
        }

        private void WallBounce()
        {
            if (resetJumpsOnWallBounce) jumpCount = maxJumps - 1;
            userEvents.OnWallBounce.Invoke();
            Vector3 direction = Vector3.Reflect(orientation.Forward, wallOppositeHit.normal);
            rb.AddForce(direction * wallBounceForce, ForceMode.VelocityChange);
            rb.AddForce(transform.up * wallBounceUpwardsForce, ForceMode.Impulse);
        }

        #endregion

        #region Dashing

        private void RegainDash()
        {
            // Gain a dash
            currentDashes += 1;
            UIEvents.onDashGained?.Invoke(currentDashes);
        }
        public void RegainDash(object s, EventArgs e)
        {
            // Wait to regain a new dash
            Invoke(nameof(RegainDash), dashCooldown);
            UIEvents.onDashUsed?.Invoke(currentDashes);
        }
        public void ResetDashes()
        {
            currentDashes = amountOfDashes;
        }

        #endregion

        #region Grappling Hook

        public void HandleGrapple()
        {
            if (!grappling) return;

            Vector3 toGrapple = grapplePoint - transform.position;
            float distance = toGrapple.magnitude;

            if (distance < distanceToBreakGrapple)
            {
                StopGrapple();
                return;
            }

            Vector3 normalizedDirection = toGrapple.normalized;
            Vector3 force = normalizedDirection * grappleForce;

            if (grapplingHookMethod == GrapplingHookMethod.Linear)
            {
                // Scale force based on distance
                float distanceMultiplier = Mathf.Clamp(distance, 1f, 10f);
                force *= distanceMultiplier;

                // Reset vertical velocity to avoid bouncing
                Vector3 currentVelocity = rb.linearVelocity;
                currentVelocity.y = 0;
                rb.linearVelocity = currentVelocity;

                rb.AddForce(force, ForceMode.Force);
            }
            else if(grapplingHookMethod == GrapplingHookMethod.Combined)
            {
                Vector3 cameraDir = playerCam.forward;

                Vector3 flatCameraDir = new Vector3(cameraDir.x, 0f, cameraDir.z).normalized;
                float alignment = Vector3.Dot(normalizedDirection, cameraDir);
                alignment = Mathf.Clamp01(alignment);
                force += flatCameraDir * cameraInfluence * alignment;

                Vector3 velocity = rb.linearVelocity;

                float verticalDamping = 0.1f;
                velocity.y *= (1f - verticalDamping);

                rb.linearVelocity = velocity;

                rb.AddForce(force, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > maxSpeedAllowed)
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeedAllowed;
            }
        }
        public void StartGrapple()
        {
            if (!allowGrapple || !grappleEnabled || grappling || !playerControl.IsControllable || weaponReference.MainCamera == null) return;
            RaycastHit hit;

            if (Physics.Raycast(weaponReference.MainCamera.transform.position, weaponReference.MainCamera.transform.forward, out hit, maxGrappleDistance, whatIsGround))
            {
                grappling = true;
                grapplePoint = hit.point;

                joint = gameObject.AddComponent<SpringJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = grapplePoint;

                float distanceFromAnchor = Vector3.Distance(this.transform.position, grapplePoint);

                joint.maxDistance = grappleRopeLength;
                joint.minDistance = grappleRopeLength * 0.25f;

                joint.spring = grappleSpringForce;
                joint.damper = grappleDamper;
                joint.massScale = 4.5f;

                userEvents.OnStartGrapple?.Invoke();// Perform custom methods
                if (grappleSounds.startGrappleSFX)
                    SoundManager.Instance.PlaySound(grappleSounds.startGrappleSFX, 0, 0, false);

                if (resetJumpsOnGrapple) jumpCount = maxJumps;
            }
        }

        public void StopGrapple()
        {
            if (!allowGrapple || !grappling) return;
            grappling = false;
            grappleEnabled = false;
            CancelInvoke(nameof(EnableGrapple));
            Invoke(nameof(EnableGrapple), grappleCooldown);
            grappleRenderer.positionCount = 0; // Reset the quality/resolution of the rope
            Destroy(joint);

            userEvents.OnStopGrapple?.Invoke();// Perform custom methods
        }
        public void UpdateGrappleRenderer()
        {
            if (grappling)
            {
                userEvents.OnGrappling?.Invoke(); // Perform custom methods


                if (grappleRenderer.positionCount == 0)
                {
                    // Initial setup when starting to grapple
                    grappleRenderer.positionCount = ropeResolution;
                    for (int i = 0; i < ropeResolution; i++)
                    {
                        grappleRenderer.SetPosition(i, transform.position);
                        currentWaveAmplitude = waveAmplitude;
                    }
                    elapsedTime = 0f;
                    grappleImpacted = false;
                }

                // Update the line renderer progressively
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= drawDuration && grappleSounds.grappleImpactSFX && !grappleImpacted)
                {
                    SoundManager.Instance.PlaySound(grappleSounds.grappleImpactSFX, 0, 0, false);
                    grappleImpacted = true;
                }
                float t = Mathf.Clamp01(elapsedTime / drawDuration);

                currentWaveAmplitude -= Time.deltaTime * waveAmplitudeMitigation;
                currentWaveAmplitude = Mathf.Clamp(currentWaveAmplitude, 0, 1.4f);
                Vector3 startPos = weaponReference.Id != null && weaponReference.Weapon != null && weaponReference.Weapon.grapplesFromTip ?
                    weaponReference.Id.FirePoint[0].position : transform.position;
                Vector3 endPos = Vector3.Lerp(transform.position, grapplePoint, t);
                Vector3[] points = new Vector3[ropeResolution];
                points[0] = startPos;
                points[ropeResolution - 1] = endPos;

                // Apply wave effect to the rope
                for (int i = 1; i < ropeResolution - 1; i++)
                {

                    float waveOffset = (float)i / (ropeResolution - 1);
                    // Using sine function for oscillation
                    float yOffset = Mathf.Sin(Time.time * 60 + waveOffset * Mathf.PI * 2f) * currentWaveAmplitude;
                    Vector3 pointPos = Vector3.Lerp(startPos, endPos, waveOffset);
                    pointPos.y += yOffset;
                    points[i] = pointPos;
                }

                grappleRenderer.SetPositions(points);
            }
            else
            {
                // Stop grappling
                grappleRenderer.positionCount = 0;
            }
        }

        private void EnableGrapple()
        {
            grappleEnabled = true;
            userEvents.OnGrappleEnabled?.Invoke(); // Perform custom methods
        }
        #endregion

        #region Climbing Ladders

        // Returns true only if the player is detecting a ladder and the player is allowed to climb ladders
        public bool DetectLadders()
        {
            if (!canClimb) return false;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, orientation.Forward, out hit, maxLadderDetectionDistance))
            {
                if (!hit.transform.CompareTag("Ladder") || InputManager.y <= 0) return false;
                if (hideWeaponWhileClimbing)
                    weaponAnimator?.HideWeapon();
                return true;
            }
            return false;
        }

        public bool DetectTopLadder()
        {
            if (!canClimb) return false;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, orientation.Forward, out hit, maxLadderDetectionDistance))
            {
                if (!hit.transform.CompareTag("Ladder")) return false;
                return false;
            }
            return true;
        }

        public void HandleLadderFinishMotion()
        {
            if (hideWeaponWhileClimbing)
                weaponAnimator.ShowWeapon();
        }


        // Handles the movement while climbing
        public void HandleClimbMovement()
        {
            if (PauseMenu.isPaused)
            {
                return;
            }

            weaponAnimator.SetParentConstraintWeight(0);
            weaponAnimator.SetParentConstraintSource(null);

            float verticalInput = InputManager.y;

            RaycastHit hit;
            bool isObstacleAbove = Physics.Raycast(transform.position, transform.up, out hit, 2f, WhatIsGround);
            if (isObstacleAbove && verticalInput > 0f) verticalInput = 0;

            if (verticalInput != 0)
            {
                Vector3 targetPosition = transform.position + transform.up * verticalInput * climbSpeed * Time.deltaTime;
                rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, 0.5f));
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }

        // Handles the logic when the player reaches the top of a ladder, so it does not get stuck at the top
        public void ReachTopLadder()
        {
            userEvents.OnEndClimb.Invoke();
            rb.AddForce(transform.up * topReachedUpperForce, ForceMode.Impulse);
            rb.AddForce(orientation.Forward * topReachedForwardForce, ForceMode.Impulse);
        }

        #endregion

        #region GroundDetection

        private Vector3[] groundedOffsets = new Vector3[3];

        /// <summary>
        /// Handle ground detection. Contributed by Chris Can. Thank you!
        /// </summary>
        private void CheckGrounded()
        {
            if(isSteppingStairs)
            {
                ResetGroundingState(); 
                return;
            }

            float radius = playerCapsuleCollider.radius * .95f;
            Vector3 origin = playerCapsuleCollider.bounds.center;

            bool foundGround = false;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, whatIsGround))
            {
                if (IsFloor(hit.normal))
                {
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                if (!grounded)
                {
                    jumpCount = maxJumps;
                    hasJumped = false;

                    SoundManager.Instance.PlaySound(sounds.landSFX, 0, 0, false);
                    userEvents.OnLand.Invoke(); // Used for User Custom Events through the Inspector
                    OnLand?.Invoke(); // Used for Internal Events
                }

                ResetGroundingState(); 
            }
            else
            {
                if (grounded || !cancellingGrounded)
                {
                    cancellingGrounded = true;

                    if (ungroundCoroutine == null)
                    {
                        ungroundCoroutine = StartCoroutine(StopGroundedCoroutine());
                    }
                }
            }
        }

        private void ResetGroundingState()
        {
            grounded = true;
            cancellingGrounded = false;

            if (ungroundCoroutine != null)
            {
                StopCoroutine(ungroundCoroutine);
                ungroundCoroutine = null;
            }
        }

        private IEnumerator StopGroundedCoroutine()
        {
            yield return new WaitForSeconds(Time.deltaTime * 3f);

            if (!Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, whatIsGround))
            {
                grounded = false;
            }

            StopGrounded();
        }
        private void StopGrounded()
        {
            grounded = false;
            coyoteTimer = coyoteJumpTime;
            cancellingGrounded = false;
        }

        /// <summary>
        /// Determine wether this is determined as floor or not
        /// </summary>
        private bool IsFloor(Vector3 v)
        {
            currentFloorAngle = Vector3.Angle(Vector3.up, v);
            return currentFloorAngle < maxSlopeAngle;
        }
        /// <summary>
        /// Determine wether this is determined as slope or not
        /// </summary>
        private bool IsPlayerOnSlope()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerScale.y + groundCheckDistance) && grounded)
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }
            return false;
        }

        #endregion

        #region AimAssist

        private void HandleAimAssist()
        {
            // Decide wether to use aim assist or not
            if (!applyAimAssist) return;
            Transform target = AimAssistHit();
            if (target == null || Vector3.Distance(target.position, transform.position) > maximumDistanceToAssistAim) return;
            // Get the direction to look at
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smoothly override our current camera rotation towards the selected enemy
            playerCam.transform.localRotation = Quaternion.Lerp(playerCam.transform.localRotation, targetRotation, Time.deltaTime * aimAssistSpeed);
        }

        /// <summary>
        /// Returns the transform you want your camera to be sticked to 
        /// </summary>
        private Transform AimAssistHit()
        {
            // Aim assist will work on enemies only, since we dont wanna snap our camera on any object around the environment
            // max range to snap
            float range = 40;
            // Max range depends on the weapon range if you are holding a weapon
            if (weaponReference.Weapon != null) range = weaponReference.Weapon.bulletRange;

            // Detect our potential transform
            RaycastHit hit;
            if (Physics.SphereCast(playerCam.transform.GetChild(0).position, aimAssistSensitivity, playerCam.transform.GetChild(0).transform.forward, out hit, range) && hit.transform.CompareTag("Enemy"))
            {
                return hit.collider.transform;
            }
            else return null;
        }

        #endregion

        #region Effects

        public void HandleSpeedLines()
        {
            if (speedLines == null) return;
            // Check if we want to use speedlines. If false, stop and return.
            if (!useSpeedLines || PauseMenu.isPaused || isClimbing)
            {
                speedLines.Stop();
                return;
            }

            if (rb.linearVelocity.magnitude >= minSpeedToUseSpeedLines)
            {
                speedLines.Play(); // Play speedlines
            }
            else
            {
                speedLines.Stop(); // Stop speedlines
            }

            // HandleEmission
            var emission = speedLines.emission;
            float emissionRate = (rb.linearVelocity.magnitude > runSpeed) ? 200 : 70;
            emission.rateOverTime = emissionRate * speedLinesAmount;
        }

        public void StopSpeedlines() => speedLines.Stop();

        /// <summary>
        /// Manage the footstep audio playing
        /// </summary>
        public void FootSteps()
        {
            // Reset timer if conditions are met + dont play the footsteps
            if (!grounded && !wallRunning || rb.linearVelocity.sqrMagnitude <= .1f)
            {
                stepTimer = 1 - footstepSpeed;
                return;
            }

            // Wait for the next time to play a sound
            stepTimer -= Time.deltaTime * rb.linearVelocity.magnitude / 15;

            // Play the sound and reset
            if (stepTimer <= 0)
            {
                stepTimer = 1 - footstepSpeed;
                _audio.pitch = UnityEngine.Random.Range(.7f, 1.3f); // Add variety to avoid boring and repetitive sounds while walking
                                                                    // Remember that you can also add a few more sounds to each of the layers to add even more variety to your sfx.
                Vector3 footstepCheckDirection = !wallRunning ? Vector3.down : 
                    (wallLeft ? -orientation.Right : orientation.Right) * 2;

                if (Physics.Raycast(playerCam.position, footstepCheckDirection, out RaycastHit hit, 2.5f, whatIsGround))
                {
                    int i = 0;
                    int layer = hit.transform.gameObject.layer;

                    switch (layer)
                    {
                        case int l when l == groundLayer: // Ground
                            i = UnityEngine.Random.Range(0, footsteps.defaultStep.Length);
                            _audio.PlayOneShot(footsteps.defaultStep[i], footstepVolume);
                            break;
                        case int l when l == grassLayer: // Grass
                            i = UnityEngine.Random.Range(0, footsteps.grassStep.Length);
                            _audio.PlayOneShot(footsteps.grassStep[i], footstepVolume);
                            break;
                        case int l when l == metalLayer: // Metal
                            i = UnityEngine.Random.Range(0, footsteps.metalStep.Length);
                            _audio.PlayOneShot(footsteps.metalStep[i], footstepVolume);
                            break;
                        case int l when l == mudLayer: // Mud
                            i = UnityEngine.Random.Range(0, footsteps.mudStep.Length);
                            _audio.PlayOneShot(footsteps.mudStep[i], footstepVolume);
                            break;
                        case int l when l == woodLayer: // Wood
                            i = UnityEngine.Random.Range(0, footsteps.woodStep.Length);
                            _audio.PlayOneShot(footsteps.woodStep[i], footstepVolume);
                            break;
                        default: // Default
                            i = UnityEngine.Random.Range(0, footsteps.defaultStep.Length);
                            _audio.PlayOneShot(footsteps.defaultStep[i], footstepVolume);
                            break;
                    }
                }
            }
        }

        #endregion

        #region Collisions
        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Weapons"))
            {
                Physics.IgnoreCollision(collision.collider, playerCollider);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Teleport the player to the specified position and rotation.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void TeleportPlayer(Vector3 position, Quaternion rotation, bool resetStamina, bool resetDashes)
        {
            rb.position = position;
            playerCam.rotation = rotation;

            if(resetStamina) ResetStamina();
            if(resetDashes) ResetDashes();


            playerStates.ForceChangeState(playerStates._States.Default());
        }
        public void SwitchToJumpState()
        {
            // Check Jump
            if (playerControl.IsControllable && allowJump && readyToJump && (enoughStaminaToJump && (grounded || canCoyote) || wallRunning || jumpCount > 0 && maxJumps > 1 && enoughStaminaToJump))
                playerStates.ForceChangeState(playerStates._States.Jump());
        }
        public void SwitchToDashState()
        {
            // Check Dash
            if (playerControl.IsControllable && canDash && (infiniteDashes || currentDashes > 0 && !infiniteDashes))
                playerStates.ForceChangeState(playerStates._States.Dash());
        }

        #endregion

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying || !showStairsDebugInfo) return;

            if (playerCapsuleCollider == null) playerCapsuleCollider = GetComponent<CapsuleCollider>();
            float rayOffset = maxStepDistance + playerCapsuleCollider.radius;

            Vector3 capsuleBottom = playerCapsuleCollider.transform.TransformPoint(
                playerCapsuleCollider.center - Vector3.up * (playerCapsuleCollider.height / 2)
            );

            Vector3 lowerRayOrigin = capsuleBottom + Vector3.up * .05f;
            Vector3 upperRayOrigin = lowerRayOrigin + Vector3.up * maxStepHeight;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lowerRayOrigin, 0.05f);
            Gizmos.DrawRay(lowerRayOrigin, playerCam.forward * rayOffset);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(upperRayOrigin, 0.05f);
            Gizmos.DrawRay(upperRayOrigin, playerCam.forward * rayOffset);

            Handles.color = Color.red;
            Handles.Label(lowerRayOrigin + Vector3.up * 0.1f, "Stairs Lower Ray Origin");
            Gizmos.color = Color.blue;
            Handles.Label(upperRayOrigin + Vector3.up * 0.1f, "Stairs Upper Ray Origin");
        }
#endif
    }
}