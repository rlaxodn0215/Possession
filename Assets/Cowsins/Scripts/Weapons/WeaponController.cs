/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Presets;
#endif

namespace cowsins
{
    #region GLOBAL CLASSES
    [System.Serializable]
    public class Events
    {
        public UnityEvent OnShoot, onStartReload, OnFinishReload, OnAim, OnAiming, OnStopAim, OnHit,OnCriticalHit, OnInventorySlotChanged, OnEquipWeapon, OnUnholsterWeapon, OnSecondaryMelee, OnAttachAttachment;
    }
    [System.Serializable]
    public class ImpactEffects
    {
        public GameObject grassImpact, metalImpact, mudImpact, woodImpact, enemyImpact;
    }
    [System.Serializable]
    public class CustomShotMethods
    {
        public Weapon_SO weapon;
        public UnityEvent OnShoot;
    }
    #endregion

    public class WeaponController : MonoBehaviour, IWeaponReferenceProvider, IWeaponBehaviourProvider, IWeaponRecoilProvider
    {
        #region VARIABLES
        // INVENTORY
        [Tooltip("max amount of weapons you can have"), SerializeField] private int inventorySize;
        [Tooltip("An array that includes all your initial weapons.")] public Weapon_SO[] initialWeapons;

        // REFERENCES
        [Tooltip("Attach your main camera"), SerializeField] private Camera mainCamera;
        [Tooltip("Attach your camera pivot object"), SerializeField] private Transform cameraPivot;
        [Tooltip("Attach your weapon holder")] public Transform weaponHolder;

        // VARIABLES
        [Tooltip("Do you want to resize your crosshair on shooting ? "), SerializeField] private bool resizeCrosshair;
        [Tooltip("If true you won´t have to press the reload button when you run out of bullets"), SerializeField] private bool autoReload;
        [Tooltip("Time (in seconds) it takes to reload automatically when Auto Reload is triggered."), SerializeField, Min(0)] private float autoReloadDelay = .1f;
        [SerializeField, Tooltip("If true, the player will reload when pressing the reload key even if it´s currently unholstering a weapon.")] private bool allowReloadWhileUnholstering;
        [Tooltip("If false, hold to aim, and release to stop aiming."), SerializeField] private bool alternateAiming;
        [Tooltip("Do not draw the crosshair when aiming a weapon"), SerializeField] private bool removeCrosshairOnAiming;
        [Tooltip("What objects should be hit"), SerializeField] private LayerMask hitLayer;

        // SECONDARY ATTACK: MELEE
        [SerializeField] private bool canMelee;
        [SerializeField] private GameObject meleeObject;
        [SerializeField] private Transform meleeHeadBone;
        public float meleeAttackDamage, meleeRange, meleeCamShakeAmount, meleeDelay, reEnableMeleeAfterAction;

        // EFFECTS
        [SerializeField] private ImpactEffects effects;

        // EVENTS
        public Events events;
        [Tooltip("Used for weapons with custom shot method. Here, " +
            "you can attach your scriptable objects and assign the method you want to call on shoot. " +
            "Please only assign those scriptable objects that use custom shot methods, Otherwise it won´t work or you will run into issues."), SerializeField]
        private CustomShotMethods[] customPrimaryShot;
        #endregion

        #region ACCESSORS 
        public bool AutoReload => autoReload;
        public bool CanMelee => canMelee;
        public GameObject MeleeObject => meleeObject;
        public bool AllowReloadWhileUnholstering => allowReloadWhileUnholstering;
        public CustomShotMethods[] CustomPrimaryShot => customPrimaryShot;  
        #endregion

        #region INTERNAL USE
        private Transform[] firePoint;

        private float spread;// Current spread in use. Changes to baseSpread or baseAimSpread depending on whether the player is aiming or not.

        private float aimingCamShakeMultiplier, crouchingCamShakeMultiplier = 1;

        private bool isAiming;
        private float aimingOutSpeed;

        private bool reloading;
        private float coolSpeed;

        private bool isMeleeAvailable;

        private int grassLayer;
        private int metalLayer;
        private int mudLayer;
        private int woodLayer;
        private int enemyLayer;

        private PlayerDependencies playerDependencies;
        private IPlayerMovementStateProvider playerMovement; // IPlayerMovementStateProvider is implemented in PlayerMovement.cs
        private IPlayerMovementEventsProvider playerMovementEvents; // IPlayerMovementEventsProvider is implemented in PlayerMovement.cs
        private IPlayerStatsProvider statsProvider; // IPlayerStatsProvider is implemented in PlayerStats.cs
        private IPlayerControlProvider playerControl; // IPlayerControlProvider is implemented in PlayerControl.cs
        private CameraFOVManager cameraFOVManager;
        private CameraEffects cameraEffects;
        private PlayerMultipliers playerMultipliers;
        private WeaponAnimator weaponAnimator;
        private WeaponStates weaponStates;
        private PlayerMultipliers multipliers;

#pragma warning disable CS0414
        private bool canShoot;

        public bool CanShoot => canShoot;
#pragma warning restore CS0414

        private int currentWeaponIndex;

        private WeaponIdentification[] inventory;
        private Weapon_SO weapon { get; set; }

        private WeaponIdentification id;

        private AudioClips audioSFX;

        private Crosshair crosshair;

        private delegate IEnumerator Reload();

        private Reload reload;

        private bool isAddonAvailable = false;

        public bool ResizeCrosshair => resizeCrosshair;

        #endregion

        #region INTERFACES
        public Camera MainCamera => mainCamera;
        public bool IsAiming => isAiming;
        public bool IsShooting { get; set; }
        public Weapon_SO Weapon
        {
            get => weapon;
            set => weapon = value;
        }
        public WeaponIdentification Id
        {
            get => id;
            set => id = value;
        }
        public int CurrentWeaponIndex
        {
            get => currentWeaponIndex;
            set => currentWeaponIndex = value;
        }
        public WeaponIdentification[] Inventory
        {
            get => inventory;
            set => inventory = value;
        }
        public int InventorySize => inventorySize;
        public bool RemoveCrosshairOnAiming => removeCrosshairOnAiming;

        public bool IsMeleeAvailable { get { return isMeleeAvailable; } set { isMeleeAvailable = value; } }
        public bool Reloading { get { return reloading; } set { reloading = value; } }
        public bool AlternateAiming => alternateAiming;

        #endregion

        #region INITIALIZATION
        private void Start()
        {
            GetReferences();
            InitialSettings();
            UIController.instance.CreateInventoryUI(inventorySize);
            GetInitialWeapons();

            // Subscribe to the method.
            // Each time we click on the attachment UI, we should perform the assignment.
            UIEvents.onAttachmentUIElementClickedNewAttachment += AssignNewAttachment;

            playerMovementEvents.AddCrouchListener(SetCrouchCamShakeMultiplier);
            playerMovementEvents.AddUncrouchListener(ResetCrouchCamShakeMultiplier);
        }

        private void OnDisable()
        {
            // Unsubscribe from the method to avoid issues
            UIEvents.onAttachmentUIElementClickedNewAttachment -= AssignNewAttachment;

            playerMovementEvents.RemoveCrouchListener(SetCrouchCamShakeMultiplier);
            playerMovementEvents.RemoveUncrouchListener(ResetCrouchCamShakeMultiplier);
        }

        private void GetReferences()
        {
            playerDependencies = GetComponent<PlayerDependencies>();
            playerMovement = playerDependencies.PlayerMovementState;
            playerMovementEvents = playerDependencies.PlayerMovementEvents;
            playerControl = playerDependencies.PlayerControl;
            statsProvider = playerDependencies.PlayerStats;
            cameraEffects = GetComponent<CameraEffects>();
            playerMultipliers = GetComponent<PlayerMultipliers>();
            weaponAnimator = GetComponent<WeaponAnimator>();
            weaponStates = GetComponent<WeaponStates>();
            multipliers = GetComponent<PlayerMultipliers>();
            inventory = new WeaponIdentification[inventorySize];
            cameraFOVManager = mainCamera.GetComponent<CameraFOVManager>();
        }

        private void InitialSettings()
        {
            crosshair = UIController.instance.crosshair;
            currentWeaponIndex = 0;
            canShoot = true;
            IsMeleeAvailable = true;
            isAddonAvailable = AddonManager.instance.isInventoryAddonAvailable;

            // Register Pool
            foreach (var effect in new[] { effects.grassImpact, effects.metalImpact, effects.mudImpact, effects.woodImpact, effects.enemyImpact })
                PoolManager.Instance.RegisterPool(effect, PoolManager.Instance.WeaponEffectsSize);

            // Store Layers to avoid expensive calls later on during runtime
            grassLayer = LayerMask.NameToLayer("Grass");
            metalLayer = LayerMask.NameToLayer("Metal");
            mudLayer = LayerMask.NameToLayer("Mud");
            woodLayer = LayerMask.NameToLayer("Wood");
            enemyLayer = LayerMask.NameToLayer("Enemy");
        }
        #endregion

        private void Update()
        {
            HandleUI();
            HandleRecoil();
            HandleHeatRatio();
        }

        #region AIMING 
        public void Aim()
        {
            if (!isAiming)
            {
                events.OnAim.Invoke(); // Invoke your custom method on stop aiming

                aimingOutSpeed = (weapon != null) ? id.aimSpeed : 2;
                cameraFOVManager.SetFOV(weapon.aimingFOV, aimingOutSpeed);
            }

            isAiming = true;

            if (weapon.applyBulletSpread) UpdateSpread(true);
            aimingCamShakeMultiplier = weapon.camShakeAimMultiplier;

            events.OnAiming.Invoke();

            // Get distance from camera to weapons
            float cameraDistance = mainCamera.nearClipPlane + weapon.aimDistance;

            // Calculate new Aim Position locally
            Vector3 localForwardCamera = mainCamera.transform.position + mainCamera.transform.forward * cameraDistance;
            // Calculate Scope Offset if there´s any scope available
            Vector3 scopeOffset = Vector3.zero;
            if (id.Scope)
            {
                Scope scopeComponent = id.Scope.GetComponent<Scope>();
                scopeOffset = mainCamera.transform.TransformVector(scopeComponent.aimingOffset);
            }
            Vector3 aimPosition = localForwardCamera + scopeOffset;

            id.aimPoint.position = Vector3.Lerp(id.aimPoint.position, aimPosition, id.aimSpeed * Time.deltaTime);
            id.aimPoint.localRotation = Quaternion.Lerp(id.aimPoint.localRotation, Quaternion.Euler(id.aimingRotation), id.aimSpeed * Time.deltaTime);
        }

        public void StopAim()
        {
            if (weapon != null && weapon.applyBulletSpread) UpdateSpread(false);

            if (isAiming)
            {
                events.OnStopAim.Invoke(); // Invoke your custom method on stop aiming
                isAiming = false;
                cameraFOVManager.SetFOV(playerMovement.WallRunning ? playerMovement.WallRunningFOV : playerMovement.NormalFOV);
            }

            isAiming = false;
            aimingCamShakeMultiplier = 1;

            if (id == null) return;
            // Change the position and FOV
            id.aimPoint.localPosition = Vector3.Lerp(id.aimPoint.localPosition, id.originalAimPointPos, id.aimSpeed * Time.deltaTime);
            id.aimPoint.localRotation = Quaternion.Lerp(id.aimPoint.localRotation, Quaternion.Euler(id.originalAimPointRot), aimingOutSpeed * Time.deltaTime);

            weaponHolder.localRotation = Quaternion.Lerp(weaponHolder.transform.localRotation, Quaternion.Euler(Vector3.zero), aimingOutSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Forces the Weapon to go back to its initial position 
        /// </summary>
        public void ForceAimReset()
        {
            isAiming = false;
            if (id?.aimPoint)
            {
                id.aimPoint.localPosition = id.originalAimPointPos;
                id.aimPoint.localRotation = Quaternion.Euler(id.originalAimPointRot);
            }
            weaponHolder.localRotation = Quaternion.Euler(Vector3.zero);
        }

        #endregion

        #region SHOOTING

        /// <summary>
        /// Moreover, cowsins´ FPS ENGINE also supports melee attacking
        /// Use this for Swords, knives etc
        /// </summary>
        private void MeleeAttack(float attackRange, float damage)
        {
            RaycastHit hit;
            Vector3 basePosition = id != null ? id.transform.position : transform.position;
            Collider[] col = Physics.OverlapSphere(basePosition + mainCamera.transform.parent.forward * attackRange / 2, attackRange, hitLayer);

            float dmg = damage * multipliers.damageMultiplier;

            foreach (var c in col)
            {
                if (c.CompareTag("Critical") || c.CompareTag("BodyShot"))
                {
                    CowsinsUtilities.GatherDamageableParent(c.transform).Damage(dmg, false);
                    break;
                }

                IDamageable damageable = c.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.Damage(dmg, false);
                    break;
                }
            }

            //VISUALS
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out hit, attackRange, hitLayer))
            {
                Hit(hit.collider.gameObject.layer, 0f, hit, false);
            }
        }
        public void SecondaryMeleeAttack()
        {
            IsMeleeAvailable = false;
            meleeObject.SetActive(true);
            weaponAnimator.SetParentConstraintSource(meleeHeadBone ?? null); 
            Invoke(nameof(Melee), meleeDelay);
        }

        private void Melee()
        {
            events.OnSecondaryMelee?.Invoke();
            MeleeAttack(meleeRange, meleeAttackDamage);
            cameraEffects?.ShootShake(meleeCamShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
        }

        public void FinishMelee()
        {
            meleeObject.SetActive(false);
            Invoke(nameof(ReEnableMelee), reEnableMeleeAfterAction);
        }

        private void ReEnableMelee() => IsMeleeAvailable = true;

        public void Shoot() => id?.Shoot(spread, playerMultipliers.damageMultiplier, aimingCamShakeMultiplier * crouchingCamShakeMultiplier);

        public void ReduceAmmo() => id?.ReduceAmmo();

        private void OnShoot()
        {
            events.OnShoot?.Invoke();

            // Determine if we want to add an effect for FOV
            if (weapon.applyFOVEffectOnShooting) cameraFOVManager?.ForceAddFOV(-weapon.FOVValueToSubtract);
            cameraEffects?.ShootShake(weapon.camShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
        }

        private void OnShootHitscanProjectile()
        {
            if(weapon == null) return;

            if (weapon.timeBetweenShots > 0)
            {
                // Rest the bullets that have just been shot
                ReduceAmmo();
            }
            
            cameraEffects?.ShootShake(id.camShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
            if (weapon.useProceduralShot) ProceduralShot.Instance.Shoot(weapon.proceduralShotPattern);

            // Determine if we want to add an effect for FOV
            if (weapon.applyFOVEffectOnShooting)
            {
                float fovAdjustment = isAiming ? weapon.AimingFOVValueToSubtract : weapon.FOVValueToSubtract;
                cameraFOVManager.ForceAddFOV(-fovAdjustment);
            }
            foreach (var p in firePoint)
            {
                if (id.muzzleVFX != null)
                {
                    var vfx = PoolManager.Instance.GetFromPool(id.muzzleVFX, p.position, mainCamera.transform.rotation);
                    vfx.transform.SetParent(mainCamera.transform);
                }
            }
            CowsinsUtilities.ForcePlayAnim("shooting", Id.Animator);
            if (weapon.timeBetweenShots > float.Epsilon) SoundManager.Instance.PlaySound(Id.GetFireSFX(), 0, weapon.pitchVariationFiringSFX, true);

            ProgressRecoil();

            events.OnShoot.Invoke();
            if (resizeCrosshair && crosshair != null) crosshair.Resize(weapon.crosshairResize * 10);
        }

        public void SpawnBulletShells()
        {
            if (weapon == null) return;  
            
            foreach (var p in firePoint)
            {
                // Adding a layer of realism, bullet shells get instantiated and interact with the world
                // We should  first check if we really wanna do this
                if (weapon.showBulletShells && (int)weapon.shootStyle != 2)
                {
                    var bulletShell = PoolManager.Instance.GetFromPool(weapon.bulletGraphics.gameObject, p.position, Quaternion.identity);
                    Rigidbody shellRigidbody = bulletShell.GetComponent<Rigidbody>();
                    float torque = UnityEngine.Random.Range(-15f, 15f);
                    Vector3 shellForce = mainCamera.transform.right * 5 + mainCamera.transform.up * 5;
                    shellRigidbody.AddTorque(mainCamera.transform.right * torque, ForceMode.Impulse);
                    shellRigidbody.AddForce(shellForce, ForceMode.Impulse);
                }
            }
            if (weapon.timeBetweenShots == 0) SoundManager.Instance.PlaySound(id.GetFireSFX(), 0, weapon.pitchVariationFiringSFX, true);
        }

        /// <summary>
        /// If you landed a shot onto an enemy, a hit will occur
        /// This is where that is being handled
        /// </summary>
        public void Hit(int layer, float damage, RaycastHit h, bool damageTarget)
        {
            events.OnHit?.Invoke();

            if (weapon == null) return;

            HandleBulletHoleImpacts(layer, h);

            // Apply damage
            if (!damageTarget || h.collider == null) return;

            float finalDamage = damage * GetDistanceDamageReduction(h.collider.transform);
            Transform hitTransform = h.collider.transform;

            // Check if a head shot was landed
            if (hitTransform.CompareTag("Critical"))
            {
                events.OnCriticalHit?.Invoke();
                CowsinsUtilities.GatherDamageableParent(h.collider.transform).Damage(finalDamage * weapon.criticalDamageMultiplier, true);
            }
            // Check if a body shot was landed ( for children colliders )
            else if (hitTransform.CompareTag("BodyShot"))
            {
                CowsinsUtilities.GatherDamageableParent(h.collider.transform).Damage(finalDamage, false);
            }
            else
            {
                var damageable = h.collider.GetComponent<IDamageable>();
                damageable?.Damage(finalDamage, false);
            }
        }

        private void HandleBulletHoleImpacts(int layer, RaycastHit h)
        {
            GameObject impactVFX = null;
            GameObject bulletHoleImpact = null;

            Quaternion normalRot = Quaternion.LookRotation(h.normal);
            Vector3 hitPoint = h.point;

            // Check the passed layer
            // If it matches any of the provided layers by FPS Engine, then:
            // Instantiate according effect and rotate it accordingly to the surface.
            // Instantiate bullet holes as well.
            switch (layer)
            {
                case int l when l == grassLayer:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.grassImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.grassImpact, hitPoint, Quaternion.identity);
                    break;
                case int l when l == metalLayer:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.metalImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.metalImpact, hitPoint, Quaternion.identity);
                    break;
                case int l when l == mudLayer:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.mudImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.grassImpact, hitPoint, Quaternion.identity);
                    break;
                case int l when l == woodLayer:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.woodImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.woodImpact, hitPoint, Quaternion.identity);
                    break;
                case int l when l == enemyLayer:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.enemyImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.enemyImpact, hitPoint, Quaternion.identity);
                    break;
                default:
                    impactVFX = PoolManager.Instance.GetFromPool(effects.metalImpact, hitPoint, normalRot);
                    bulletHoleImpact = PoolManager.Instance.GetFromPool(weapon.bulletHoleImpact.groundImpact, hitPoint, Quaternion.identity);
                    break;
            }

            if (bulletHoleImpact != null && h.collider != null)
            {
                bulletHoleImpact.transform.rotation = normalRot;
                bulletHoleImpact.transform.SetParent(h.collider.transform, worldPositionStays: true);
            }
        }

        private void AllowShoot() => canShoot = true;

        #endregion

        #region RELOAD
        public void StartReload() => StartCoroutine(reload());
        public void StopReload()
        {
            reloading = false;
            canShoot = true;
            StopCoroutine(reload());
        }

        /// <summary>
        /// Handle Reloading
        /// </summary>
        private IEnumerator DefaultReload()
        {
            reloading = true;
            // Run custom event
            events.onStartReload.Invoke();

            yield return new WaitForSeconds(autoReload && id.bulletsLeftInMagazine <= 0 ? autoReloadDelay : .1f);

            // Play reload sound
            SoundManager.Instance.PlaySound(id.bulletsLeftInMagazine == 0 ? weapon.audioSFX.emptyMagReload : weapon.audioSFX.reload, .1f, 0, true);

            // Play animation
            CowsinsUtilities.PlayAnim("reloading", id.Animator);

            // Wait reloadTime seconds, assigned in the weapon scriptable object.
            yield return new WaitForSeconds(id.reloadTime);

            canShoot = true;
            if (!reloading) yield break;

            reloading = false;

            // Set the proper amount of bullets, depending on magazine type.
            if (!weapon.limitedMagazines) id.bulletsLeftInMagazine = id.magazineSize;
            else
            {
                if (id.totalBullets > id.magazineSize) // You can still reload a full magazine
                {
                    id.totalBullets = id.totalBullets - (id.magazineSize - id.bulletsLeftInMagazine);
                    id.bulletsLeftInMagazine = id.magazineSize;
                }
                else if (id.totalBullets == id.magazineSize) // You can only reload a single full magazine more
                {
                    id.totalBullets = id.totalBullets - (id.magazineSize - id.bulletsLeftInMagazine);
                    id.bulletsLeftInMagazine = id.magazineSize;
                }
                else if (id.totalBullets < id.magazineSize) // You cant reload a whole magazine
                {
                    int bulletsLeft = id.bulletsLeftInMagazine;
                    if (id.bulletsLeftInMagazine + id.totalBullets <= id.magazineSize)
                    {
                        id.bulletsLeftInMagazine = id.bulletsLeftInMagazine + id.totalBullets;
                        if (id.totalBullets - (id.magazineSize - bulletsLeft) >= 0) id.totalBullets = id.totalBullets - (id.magazineSize - bulletsLeft);
                        else id.totalBullets = 0;
                    }
                    else
                    {
                        int ToAdd = id.magazineSize - id.bulletsLeftInMagazine;
                        id.bulletsLeftInMagazine = id.bulletsLeftInMagazine + ToAdd;
                        if (id.totalBullets - ToAdd >= 0) id.totalBullets = id.totalBullets - ToAdd;
                        else id.totalBullets = 0;
                    }
                }
            }
            // Reload has finished
            events.OnFinishReload.Invoke();
        }

        private IEnumerator OverheatReload()
        {
            // Currently reloading
            canShoot = false;

            float waitTime = weapon.cooledPercentageAfterOverheat;

            // Stop being able to shoot, prevents from glitches
            CancelInvoke(nameof(AllowShoot));

            // Wait until the heat ratio is appropriate to keep shooting
            yield return new WaitUntil(() => id.heatRatio <= waitTime);

            // Reload has finished
            events.OnFinishReload.Invoke();

            reloading = false;
            canShoot = true;
        }

        // Handles overheat weapons reloading.
        private void HandleHeatRatio()
        {
            if (weapon == null || id.magazineSize == 0 || weapon.reloadStyle == ReloadingStyle.defaultReload) return;

            // Dont keep cooling if it is completely cooled
            if (id.heatRatio > 0) id.heatRatio -= Time.deltaTime * coolSpeed;
            if (id.heatRatio > 1) id.heatRatio = 1;
        }

        private void UpdateReloadUI()
        {
            bool isInfinite = weapon.infiniteBullets;
            bool isOverheat = weapon.reloadStyle == ReloadingStyle.Overheat;

            UIEvents.onDetectReloadMethod?.Invoke(!isInfinite && !isOverheat, isOverheat && !isInfinite);
        }

        #endregion

        #region WEAPON UNHOLSTER & SET-UP 

        /// <summary>
        /// Active your new weapon
        /// </summary>
        public void UnHolster(GameObject weaponObj, bool playAnim)
        {
            canShoot = true;

            weaponObj.SetActive(true);
            id = weaponObj.GetComponent<WeaponIdentification>();

            var animator = weaponObj.GetComponentInChildren<Animator>(true);
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = true;
            if (playAnim)
                CowsinsUtilities.PlayAnim("unholster", animator);

            // Get Shooting Style 
            // Set the proper IShootStyle based on the Weapon Selected Shoot Style.
            // Also, properly assign callbacks for shooting and hitting.
            // The Shoot logic for each IShootStyle will run when calling Shoot() from WeaponController
            switch ((int)weapon.shootStyle)
            {
                case 0: 
                    var hitscanShootWrapper = new HitscanShootStyle(playerDependencies, mainCamera, hitLayer);
                    hitscanShootWrapper.SetOnShootEvent(OnShootHitscanProjectile);
                    hitscanShootWrapper.SetOnHitEvent(Hit); 
                    id.SetShootStyle(hitscanShootWrapper); 
                    break;
                case 1: 
                    var projectileShootStyle = new ProjectileShootStyle(playerDependencies, this, mainCamera);
                    projectileShootStyle.SetOnShootEvent(OnShootHitscanProjectile);
                    id.SetShootStyle(projectileShootStyle);
                    break;
                case 2: 
                    var meleeShootStyle = new MeleeShootStyle(playerDependencies, mainCamera, hitLayer);
                    meleeShootStyle.SetOnShootEvent(OnShoot);
                    meleeShootStyle.SetOnHitEvent(Hit);
                    id.SetShootStyle(meleeShootStyle);
                    break;
                case 3: 
                    var customShootStyle = new CustomShootStyle(playerDependencies, mainCamera, hitLayer);
                    customShootStyle.SetOnShootEvent(OnShoot);
                    id.SetShootStyle(customShootStyle);
                    break;
            }

            SoundManager.Instance.PlaySound(weapon.audioSFX.unholster, .1f, 0, true);

            UpdateSpread(isAiming);
            // Grab the modifiers for weight
            GetWeaponWeightModifier();

            weaponAnimator.StopWalkAndRunMotion();
            weaponAnimator.SetParentConstraintSource(id.HeadBone);


            // Define reloading method
            SetupReloadMethods();

            firePoint = inventory[currentWeaponIndex].FirePoint;

            // UI & OTHERS
            UpdateReloadUI();
            UIController.instance.crosshairShape.SetCrosshair(weapon.crosshairParts);

            if ((int)weapon.shootStyle == 2) UIEvents.onDetectReloadMethod?.Invoke(false, false);

            // Register Pools
            RegisterWeaponPools();

            UIEvents.setWeaponDisplay?.Invoke(weapon);
            events.OnUnholsterWeapon.Invoke();
        }

        private void SetupReloadMethods()
        {
            if (weapon.reloadStyle == ReloadingStyle.defaultReload)
            {
                reload = DefaultReload;
            }
            else
            {
                reload = OverheatReload;
                coolSpeed = weapon.coolSpeed;
            }
        }

        public void SelectWeapon()
        {
            canShoot = false;

            crosshair.SpotEnemy(false);
            events.OnInventorySlotChanged.Invoke(); // Invoke your custom method
            weapon = null;
            // Spawn the appropriate weapon in the inventory

            foreach (WeaponIdentification weapon_ in inventory)
            {
                if (weapon_ != null)
                {
                    weapon_.gameObject.SetActive(false);
                    weapon_.Animator.enabled = false;
                    if (weapon_ == inventory[currentWeaponIndex])
                    {
                        weapon = inventory[currentWeaponIndex].weapon;

                        weapon_.Animator.enabled = true;
                        UnHolster(weapon_.gameObject, true);
                    }
                }
            }
            GetWeaponWeightModifier();

            // Handle the UI Animations
            UIController.instance.SelectInventoryUISlot(currentWeaponIndex);

            CancelInvoke(nameof(AllowShoot));

            events.OnEquipWeapon.Invoke(); // Invoke your custom method

        }

        private void GetInitialWeapons()
        {
            if (initialWeapons.Length == 0) return;

            int i = 0;
            while (i < initialWeapons.Length)
            {
                InstantiateWeapon(initialWeapons[i], i);
                i++;
            }
            weapon = initialWeapons[0];
        }

        public void InstantiateWeapon(Weapon_SO newWeapon, int inventoryIndex) => InstantiateWeapon(newWeapon, inventoryIndex, null, null, null);

        public void InstantiateWeapon(Weapon_SO newWeapon, int inventoryIndex, int? _bulletsLeftInMagazine, int? _totalBullets) 
            => InstantiateWeapon(newWeapon, inventoryIndex, _bulletsLeftInMagazine, _totalBullets, null);

        public void InstantiateWeapon(Weapon_SO newWeapon, int inventoryIndex, int? _bulletsLeftInMagazine, int? _totalBullets, List<AttachmentIdentifier_SO> attachmentsToAssign)
        {
            if (newWeapon == null)
            {
                Debug.LogError("<color=red>[COWSINS]</color> <b><color=yellow>The weapon to instantiate is null!</color></b> " +
                               "Please ensure the proper <b><color=cyan>Weapon_SO</color></b> is assigned.");
                return;
            }

            // Instantiate Weapon
            var weaponPicked = Instantiate(newWeapon.weaponObject, weaponHolder);
            weaponPicked.transform.localPosition = newWeapon.weaponObject.transform.localPosition;

            // Destroy the Weapon if it already exists in the same slot
            if (inventory[inventoryIndex] != null) Destroy(inventory[inventoryIndex].gameObject);

            // Set the Weapon
            inventory[inventoryIndex] = weaponPicked;

            // Select weapon if it is the current Weapon
            if (inventoryIndex == currentWeaponIndex)
            {
                weapon = newWeapon;
            }
            else weaponPicked.gameObject.SetActive(false);

            // if _bulletsLeftInMagazine is null, calculate magazine size. If not, simply assign _bulletsLeftInMagazine
            inventory[inventoryIndex].bulletsLeftInMagazine = _bulletsLeftInMagazine ?? newWeapon.magazineSize;
            inventory[inventoryIndex].totalBullets = _totalBullets ??
            (newWeapon.limitedMagazines
                ? newWeapon.magazineSize * newWeapon.totalMagazines
                : newWeapon.magazineSize);

            List<AttachmentIdentifier_SO> attachments = attachmentsToAssign == null || attachmentsToAssign.Count <= 0 ? weaponPicked.GetDefaultAttachmentsIdentifiers() : attachmentsToAssign;
            foreach (AttachmentIdentifier_SO attachmentIdentifier in attachments)
            {
                AssignNewAttachmentToWeapon(attachmentIdentifier, inventoryIndex); 
            }

            //UI
            UIController.instance.SetInventoryUISlotWeapon(inventoryIndex, newWeapon);

            UIController.instance.crosshairShape.SetCrosshair(weapon?.crosshairParts);

            if (inventoryIndex == currentWeaponIndex) SelectWeapon();

        }


        private readonly GameObject[] sharedImpactPrefabs = new GameObject[5];

        private void RegisterWeaponPools()
        {
            var bulletHoles = weapon.bulletHoleImpact;

            sharedImpactPrefabs[0] = bulletHoles.grassImpact;
            sharedImpactPrefabs[1] = bulletHoles.metalImpact;
            sharedImpactPrefabs[2] = bulletHoles.mudImpact;
            sharedImpactPrefabs[3] = bulletHoles.woodImpact;
            sharedImpactPrefabs[4] = bulletHoles.enemyImpact;

            foreach (var effect in sharedImpactPrefabs)
            {
                if (effect)
                    PoolManager.Instance.RegisterPool(effect, PoolManager.Instance.WeaponEffectsSize);
            }

            if (weapon.bulletGraphics.gameObject != null)
                PoolManager.Instance.RegisterPool(weapon.bulletGraphics.gameObject, PoolManager.Instance.BulletGraphicsSize);
        }

        public void ReleaseCurrentWeapon() => ReleaseWeapon(currentWeaponIndex);

        public void ReleaseWeapon(int index)
        {
            Destroy(inventory[index].gameObject);
            inventory[index] = null;
            weapon = null;
            UIController.instance.SetInventoryUISlotWeapon(index, null);
            GetWeaponWeightModifier();
        }
        #endregion

        #region ATTACHMENTS
        public void AssignNewAttachment(AttachmentIdentifier_SO attachmentIdentifier_SO) => AssignNewAttachmentToWeapon(attachmentIdentifier_SO, currentWeaponIndex);

        public void AssignNewAttachmentToWeapon(AttachmentIdentifier_SO attachmentIdentifier_SO, int index)
        {
            (bool compatible, Attachment newAttachment, int atcIndex) = CowsinsUtilities.CompatibleAttachment(inventory[index], attachmentIdentifier_SO);
            if (!compatible) return;

            AssignAttachmentToWeapon(newAttachment, index);
        }

        /// <summary>
        /// Equips the passed attachment.
        /// </summary>
        /// <param name="attachment">Attachment to equip.</param>
        /// <param name="attachmentID">Order ID of the attachment in the WeaponIdentification's compatible attachment array.</param>
        public void AssignNewAttachment(Attachment attachment) => AssignAttachmentToWeapon(attachment, currentWeaponIndex);

        /// <summary>
        /// Equips the passed attachment. 
        /// </summary>
        /// <param name="attachment">Attachment to equip</param>
        /// <param name="attachmentID">Order ID of the attachment to equip in the WeaponIdentification compatible attachment array.</param>
        public void AssignAttachmentToWeapon(Attachment attachment, int index)
        {
            if (attachment == null) return;

            WeaponIdentification curWeapon = inventory[index];

            (bool compatible, Attachment newAttachment, int atcIndex) = CowsinsUtilities.CompatibleAttachment(curWeapon, attachment.attachmentIdentifier);
            if (!compatible) return;
            
            AttachmentType type = attachment.attachmentIdentifier.attachmentType;

            Attachment currentAttachment = curWeapon.GetCurrentAttachment(type);
            Attachment defaultAttachment = curWeapon.GetDefaultAttachment(type);

            if (currentAttachment != null)
            {
                currentAttachment.gameObject.SetActive(false);

                if (currentAttachment != defaultAttachment)
                {
                    GetComponent<InteractManager>().DropAttachment(currentAttachment, false);
                }
            }

            newAttachment.gameObject.SetActive(true);
            newAttachment.Attach(curWeapon);

            events.OnAttachAttachment?.Invoke();
        }

        public void ToggleFlashLight()
        {
            Attachment flashlight = inventory[currentWeaponIndex]?.Flashlight;

            if (flashlight == null) return;

            flashlight.AttachmentAction();
        }
        #endregion

        #region MODIFIERS
        private void GetWeaponWeightModifier()
        {
            // Only apply the weight modification if the Inventory Pro Manager add-on is not available. If it is available, the weight of the player is calculated by the inventory
            if (isAddonAvailable) return;

            playerMultipliers.playerWeightMultiplier = weapon != null ? weapon.weightMultiplier : 1;
        }

        private void UpdateSpread(bool isAiming) => spread = isAiming ? id.baseAimSpread : id.baseSpread;

        private float GetDistanceDamageReduction(Transform target)
        {
            if (!weapon.applyDamageReductionBasedOnDistance) return 1;
            if (Vector3.Distance(target.position, transform.position) > weapon.minimumDistanceToApplyDamageReduction)
                return (weapon.minimumDistanceToApplyDamageReduction / Vector3.Distance(target.position, transform.position)) * weapon.damageReductionMultiplier;
            else return 1;
        }

        private void SetCrouchCamShakeMultiplier()
        {
            if (weapon)
                crouchingCamShakeMultiplier = weapon.camShakeCrouchMultiplier;
        }

        private void ResetCrouchCamShakeMultiplier()
        {
            crouchingCamShakeMultiplier = 1;
        }
        #endregion

        #region UI
        private void HandleUI()
        {
            // If we dont own a weapon yet, do not continue
            if (weapon == null)
            {
                UIEvents.disableWeaponUI?.Invoke();
                return;
            }

            UIEvents.enableWeaponDisplay?.Invoke();

            if (weapon.reloadStyle == ReloadingStyle.defaultReload)
            {
                if (!weapon.infiniteBullets)
                {

                    bool activeReloadUI = id.bulletsLeftInMagazine == 0 && !autoReload && !weapon.infiniteBullets;
                    bool activeLowAmmoUI = id.bulletsLeftInMagazine < id.magazineSize / 3.5f && id.bulletsLeftInMagazine > 0;
                    // Set different display settings for each shoot style 
                    if (weapon.limitedMagazines)
                    {
                        UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.totalBullets, activeReloadUI, activeLowAmmoUI);
                    }
                    else
                    {
                        UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.magazineSize, activeReloadUI, activeLowAmmoUI);
                    }
                }
                else
                {
                    UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.totalBullets, false, false);
                }
            }
            else
            {
                UIEvents.onHeatRatioChanged?.Invoke(id.heatRatio);
            }



            //Crosshair Management
            // If we dont use a crosshair stop right here
            if (UIController.instance.crosshair == null)
            {
                return;
            }
            // Detect enemies on aiming
            RaycastHit hit_;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit_, weapon.bulletRange) && hit_.transform.CompareTag("Enemy") || Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit_, weapon.bulletRange) && hit_.transform.CompareTag("Critical"))
                crosshair.SpotEnemy(true);
            else crosshair.SpotEnemy(false);
        }

        /// <summary>
        /// Change you current slots, core of the inventory
        /// </summary>
        public void HandleInventory()
        {
            if (InputManager.reloading) return; // Do not change weapons while reloading
                                                // Change slot
            if (InputManager.scrolling > 0 || InputManager.previousweapon)
            {
                UIController.instance.StartFadingInventory();
                ForceAimReset(); // Move Weapon back to the original position
                if (currentWeaponIndex < inventorySize - 1)
                {
                    currentWeaponIndex++;
                    SelectWeapon();
                }
            }
            if (InputManager.scrolling < 0 || InputManager.nextweapon)
            {
                UIController.instance.StartFadingInventory();
                ForceAimReset(); // Move Weapon back to the original position
                if (currentWeaponIndex > 0)
                {
                    currentWeaponIndex--;
                    SelectWeapon();
                }
            }
        }

        #endregion

        #region RECOIL
        private float evaluationProgress;
        private float recoilPitchOffset = 0f;
        private float recoilYawOffset = 0f;

        public float RecoilPitchOffset => recoilPitchOffset;
        public float RecoilYawOffset => recoilYawOffset;

        private void HandleRecoil()
        {
            // Relax back to 0 if weapon is null or the current weapon does not apply recoil
            if (weapon == null || !weapon.applyRecoil || id.bulletsLeftInMagazine <= 0)
            {
                recoilPitchOffset = Mathf.Lerp(recoilPitchOffset, 0, 3 * Time.deltaTime);
                recoilYawOffset = Mathf.Lerp(recoilYawOffset, 0, 3 * Time.deltaTime);
                return;
            }

            // If not shooting, relax back to 0
            if (!InputManager.shooting || weapon.shootMethod == ShootingMethod.Press || reloading || !playerControl.IsControllable)
            {
                recoilPitchOffset = Mathf.Lerp(recoilPitchOffset, 0, weapon.recoilRelaxSpeed * Time.deltaTime);
                recoilYawOffset = Mathf.Lerp(recoilYawOffset, 0, weapon.recoilRelaxSpeed * Time.deltaTime);
                evaluationProgress = 0; 
                return;
            }

            // If shooting, calculate the pitch and yaw. These will be gathered by PlayerMovement inside Look()
            if (InputManager.shooting)
            {
                float xamount = (weapon.applyDifferentRecoilOnAiming && isAiming) ? weapon.xRecoilAmountOnAiming : weapon.xRecoilAmount;
                float yamount = (weapon.applyDifferentRecoilOnAiming && isAiming) ? weapon.yRecoilAmountOnAiming : weapon.yRecoilAmount;

                float targetPitchRecoil = -weapon.recoilY.Evaluate(evaluationProgress) * yamount * 1f;
                float targetYawRecoil = -weapon.recoilX.Evaluate(evaluationProgress) * xamount * 1f;

                recoilPitchOffset = Mathf.Lerp(recoilPitchOffset, targetPitchRecoil, weapon.recoilRelaxSpeed * Time.deltaTime);
                recoilYawOffset = Mathf.Lerp(recoilYawOffset, targetYawRecoil, weapon.recoilRelaxSpeed * Time.deltaTime);
            }
        }

        private void ProgressRecoil()
        {
            if (weapon.applyRecoil)
            {
                evaluationProgress += 1f / weapon.magazineSize;
            }
        }

        #endregion
    }
}



