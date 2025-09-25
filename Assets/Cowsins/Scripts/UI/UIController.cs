/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; 
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace cowsins
{
    /// <summary>
    /// Manage UI actions.
    /// This is still subject to change and optimize.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        // REFERENCES
        [SerializeField] private PauseMenu pauseMenu;

        // HEALTH
        [Tooltip("Use image bars to display player statistics."), SerializeField] private bool barHealthDisplay;
        [Tooltip("Use text to display player statistics."), SerializeField] private bool numericHealthDisplay;
        [Tooltip("Slider that will display the health on screen"), SerializeField] private Slider healthSlider;
        [Tooltip("Slider that will display the shield on screen"), SerializeField] private Slider shieldSlider;
        [SerializeField, Tooltip("UI Element ( TMPro text ) that displays current and maximum health.")] private TextMeshProUGUI healthTextDisplay;
        [SerializeField, Tooltip("UI Element ( TMPro te¡xt ) that displays current and maximum shield.")] private TextMeshProUGUI shieldTextDisplay;
        [Tooltip("This image shows damage and heal states visually on your screen, you can change the image" +
                "to any you like, but note that color will be overriden by the script"), SerializeField] private Image healthStatesEffect;
        [Tooltip(" Color of healthStatesEffect on different actions such as being hurt or healed"), SerializeField] private Color damageColor, healColor, coinCollectColor, xpCollectColor;
        [Tooltip("Time for the healthStatesEffect to fade out"), SerializeField] private float fadeOutTime;

        // INTERACTION
        [Tooltip("Attach the UI you want to use as your interaction UI"),  SerializeField] private GameObject interactUI;
        [SerializeField] private AudioClip allowedInteractionSFX;
        [Tooltip("Displays the current progress of your interaction"), SerializeField] private Image interactUIProgressDisplay;
        [SerializeField, Tooltip("UI that displays incompatible interactions.")] private GameObject forbiddenInteractionUI;
        [Tooltip("Inside the interact UI, this is the text that will display the object you want to interact with " +
           "or any custom method you would like." +
           "Do check Interactable.cs for that or, if you want, read our documentation or contact the cowsins support " +
           "in order to make custom interactions."), SerializeField] private TextMeshProUGUI interactText;

        // ATTACHMENTS
        [Tooltip("UI enabled when inspecting."), SerializeField] private CanvasGroup inspectionUI;
        [SerializeField] private float inspectionFadeDuration = 0.5f;
        [SerializeField, Tooltip("Text that displays the name of the current weapon when inspecting.")] private TextMeshProUGUI weaponDisplayText_AttachmentsUI;
        [SerializeField, Tooltip("Prefab of the UI element that represents an attachment on-screen when inspecting")] private GameObject attachmentDisplay_UIElement;
        [SerializeField, Tooltip("Group of attachments. Attachment UI elements are wrapped inside these.")]
        private GameObject
            barrels_AttachmentsGroup,
            scopes_AttachmentsGroup,
            stocks_AttachmentsGroup,
            grips_AttachmentsGroup,
            magazines_AttachmentsGroup,
            flashlights_AttachmentsGroup,
            lasers_AttachmentsGroup;
        [SerializeField, Tooltip("Color of an attachment UI element when it is equipped.")] private Color usingAttachmentColor;
        [SerializeField, Tooltip("Color of an attachment UI element when it is unequipped. This is the default color.")] private Color notUsingAttachmentColor;

        // WEAPON
        [Tooltip("Attach the appropriate UI here"), SerializeField] private TextMeshProUGUI bulletsUI, magazineUI, reloadUI, lowAmmoUI;
        [Tooltip("Image that represents heat levels of your overheating weapon"), SerializeField] private Image overheatUI;
        [Tooltip("Display an icon of your current weapon"), SerializeField] private Image currentWeaponDisplay;
        [Tooltip(" Attach the CanvasGroup that contains the inventory"), SerializeField] private CanvasGroup inventoryContainer;
        [SerializeField] private WeaponsInventoryUISlot inventoryUISlot;
        public Crosshair crosshair;

        // DASHING
        [SerializeField, Tooltip("Contains dashUIElements in game.")] private Transform dashUIContainer;
        [SerializeField, Tooltip("Displays a dash slot in-game. This keeps stored at dashUIContainer during runtime.")] private Transform dashUIElement;

        // EXPERIENCE
        [SerializeField] private Image xpImage;
        [SerializeField] private TextMeshProUGUI currentLevel, nextLevel;
        [SerializeField] private float lerpXpSpeed;

        // OTHERS
        [SerializeField] private GameObject coinsUI;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private Hitmarker hitmarker;

        // UI EVENTS
        [Tooltip("An object showing death events will be displayed on kill"), SerializeField] private bool displayEvents;
        [Tooltip("UI element which contains the killfeed. Where the kilfeed object will be instantiated and parented to"), SerializeField]
        private GameObject killfeedContainer;
        [Tooltip("Object to spawn"), SerializeField] private GameObject killfeedObject;
        [SerializeField] private string killfeedMessage;
        [Tooltip("Add a pop up showing the damage that has been dealt. Recommendation: use the already made pop up included in this package. "), SerializeField]
        private GameObject damagePopUp;
        [Tooltip("Horizontal randomness variation"), SerializeField] private float xVariation;

        private Coroutine inspectFadeRoutine;
        private Coroutine inventoryFadeCoroutine;

        // GETTERS
        public GameObject DamagePopUp => damagePopUp;
        public CrosshairShape crosshairShape {  get; private set; } 
        public bool BarHealthDisplay => barHealthDisplay;
        public bool NumericHealthDisplay => numericHealthDisplay;
        public bool DisplayEvents => displayEvents; 
        public Action<float, float> healthDisplayMethod;
        public static UIController instance { get; set; }

        private void Awake()
        {
            instance = this;

            crosshairShape = crosshair.GetComponent<CrosshairShape>();
        }
        private void Start()
        {
            if (!CoinManager.Instance.useCoins && coinsUI != null) coinsUI.SetActive(false);

            StartFadingInventory();
            if(inspectionUI) inspectionUI.alpha = 0;

            // Register Pool
            if (damagePopUp) PoolManager.Instance.RegisterPool(damagePopUp, PoolManager.Instance.DamagePopUpsSize);
        }
 
        // INVENTORY /////////////////////////////////////////////////////////////////////////////////////////
        public void StartFadingInventory()
        {
            inventoryContainer.alpha = 1f;

            if (inventoryFadeCoroutine != null) StopCoroutine(inventoryFadeCoroutine);
            inventoryFadeCoroutine = StartCoroutine(FadeInventory());
        }

        private IEnumerator FadeInventory()
        {
            while (inventoryContainer.alpha > 0)
            {
                inventoryContainer.alpha -= Time.deltaTime;
                yield return null;
            }
        }

        // HEALTH SYSTEM /////////////////////////////////////////////////////////////////////////////////////////
        private void UpdateHealthUI(float health, float shield, bool damaged)
        {
            healthDisplayMethod?.Invoke(health, shield);

            Color colorSelected = damaged ? damageColor : healColor;
            healthStatesEffect.color = colorSelected;

            StartCoroutine(ReduceHealthStatesAlpha());
        }

        public void UpdateCoinsPanel()
        {
            healthStatesEffect.color = coinCollectColor;
            StartCoroutine(ReduceHealthStatesAlpha());
        }

        private void UpdateXPPanel()
        {
            healthStatesEffect.color = xpCollectColor;
            StartCoroutine(ReduceHealthStatesAlpha());
        }

        private IEnumerator FillExperienceBar()
        {
            float targetXp = ExperienceManager.instance.GetCurrentExperience() / ExperienceManager.instance.experienceRequirements[ExperienceManager.instance.playerLevel];
            xpImage.fillAmount = 0; 
            while (xpImage.fillAmount < targetXp)
            {
                xpImage.fillAmount = Mathf.Lerp(xpImage.fillAmount, targetXp, lerpXpSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator ReduceHealthStatesAlpha()
        {
            Color currentColor = healthStatesEffect.color;
            while (currentColor.a > 0)
            {
                healthStatesEffect.color -= new Color(0, 0, 0, Time.deltaTime * fadeOutTime);
                yield return null;
            }
        }
        private void HealthSetUp(float health, float shield, float maxHealth, float maxShield)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
            }
            if (shieldSlider != null)
            {
                shieldSlider.maxValue = maxShield;
            }

            healthDisplayMethod?.Invoke(health, shield);

            if (shield == 0) shieldSlider.gameObject.SetActive(false);
        }

        private void BarHealthDisplayMethod(float health, float shield)
        {
            if (healthSlider != null)
                healthSlider.value = health;

            if (shieldSlider != null)
                shieldSlider.value = shield;
        }
        private void NumericHealthDisplayMethod(float health, float shield)
        {
            if (healthTextDisplay != null)
            {
                healthTextDisplay.text = health > 0 && health <= 1 ? 1.ToString("F0") : health.ToString("F0");
            }

            if (shieldTextDisplay != null)
                shieldTextDisplay.text = shield.ToString("F0");
        }

        // INTERACTION /////////////////////////////////////////////////////////////////////////////////////////
        private void AllowedInteraction(string displayText)
        {
            forbiddenInteractionUI.SetActive(false);
            interactUI.SetActive(true);
            interactText.text = displayText;
            interactUI.GetComponent<Animation>().Play();
            SoundManager.Instance.PlaySound(allowedInteractionSFX, 0,0, false);

            // Adjust the width of the background based on the length of the displayText
            RectTransform imageRect = interactUI.GetComponentInChildren<Image>().GetComponent<RectTransform>();
            string plainText = Regex.Replace(displayText, "<.*?>", string.Empty);
            float textLength = plainText.Length;
            imageRect.sizeDelta = new Vector2(100 + textLength * 10, imageRect.sizeDelta.y);
        }

        private void ForbiddenInteraction()
        {
            forbiddenInteractionUI.SetActive(true);
            interactUI.SetActive(false);
        }

        private void DisableInteractionUI()
        {
            forbiddenInteractionUI.SetActive(false);
            interactUI.SetActive(false);
        }
        private void InteractionProgressUpdate(float value)
        {
            interactUIProgressDisplay.gameObject.SetActive(true);
            interactUIProgressDisplay.fillAmount = value;
        }
        private void FinishInteraction()
        {
            interactUIProgressDisplay.gameObject.SetActive(false);
        }

        // UI EVENTS /////////////////////////////////////////////////////////////////////////////////////////
        public void AddKillfeed(string name)
        {
            GameObject killfeed = PoolManager.Instance.GetFromPool(killfeedObject, transform.position, Quaternion.identity);
            killfeed.transform.SetParent(killfeedContainer.transform);
            killfeed.transform.GetChild(0).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{killfeedMessage} {name}";
        }

        public void AddDamagePopUp(Vector3 position, float damage)
        {
            float xRand = UnityEngine.Random.Range(-xVariation, xVariation);
            Vector3 posculatedPos = position + new Vector3(xRand, 0, 0);
            GameObject popup = PoolManager.Instance.GetFromPool(damagePopUp, posculatedPos, Quaternion.identity, .4f);
            TMP_Text text = popup.transform.GetChild(0).GetComponent<TMP_Text>();
            if (damage / Mathf.FloorToInt(damage) == 1)
                text.text = damage.ToString("F0");
            else
                text.text = damage.ToString("F1");
        }

        public void Hitmarker(bool headshot)
        {
            hitmarker.Play(headshot);
        }

        // WEAPON INVENTORY UI /////////////////////////////////////////////////////////////////////////////////////////

        public WeaponsInventoryUISlot[] weaponsInventoryUISlots;
        /// <summary>
        /// Procedurally generate the Inventory UI depending on your needs
        /// </summary>
        public void CreateInventoryUI(int inventorySize)
        {
            // Adjust the inventory size 
            weaponsInventoryUISlots = new WeaponsInventoryUISlot[inventorySize];
            int j = 0; // Control variable
            while (j < inventorySize)
            {
                // Load the slot, instantiate it and set it to the slots array
                WeaponsInventoryUISlot slot = Instantiate(inventoryUISlot, Vector3.zero, Quaternion.identity, inventoryContainer.transform) as WeaponsInventoryUISlot;
                weaponsInventoryUISlots[j] = slot;
                j++;
            }
        }

        public void SelectInventoryUISlot(int selectedIndex)
        {
            foreach (WeaponsInventoryUISlot slot in weaponsInventoryUISlots)
            {
                slot.Deselect();
            }
            weaponsInventoryUISlots[selectedIndex].Select();
        }
        public void SetInventoryUISlotWeapon(int selectedIndex, Weapon_SO newWeapon) => weaponsInventoryUISlots[selectedIndex].SetWeapon(newWeapon);

        // INSPECT   /////////////////////////////////////////////////////////////////////////////////////////
        private void InspectionUIFadeIn() => StartFadeCoroutine(1f);

        private void InspectionUIFadeOut() => StartFadeCoroutine(0f);

        private void StartFadeCoroutine(float targetAlpha)
        {
            if (inspectFadeRoutine != null)
                StopCoroutine(inspectFadeRoutine);

            inspectFadeRoutine = StartCoroutine(FadeInspectionUI(targetAlpha));
        }

        private IEnumerator FadeInspectionUI(float targetAlpha)
        {
            inspectionUI.gameObject.SetActive(true);

            float startAlpha = inspectionUI.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < inspectionFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                inspectionUI.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / inspectionFadeDuration);
                yield return null;
            }

            inspectionUI.alpha = targetAlpha;

            if (Mathf.Approximately(targetAlpha, 0f))
                inspectionUI.gameObject.SetActive(false);
        }

        public float GetInspectionUIAlpha() { return inspectionUI.alpha; }

        public void GenerateInspectionUI(PlayerDependencies playerDependencies)
        {
            IWeaponReferenceProvider wRef = playerDependencies.WeaponReference;
            WeaponIdentification weapon = wRef.Id;
            bool displayCurrentAttachments = playerDependencies.GetComponent<InteractManager>().DisplayCurrentAttachmentsOnly;
            weaponDisplayText_AttachmentsUI.text = wRef.Weapon._name;

            CleanAttachmentGroup(barrels_AttachmentsGroup);
            CleanAttachmentGroup(scopes_AttachmentsGroup);
            CleanAttachmentGroup(stocks_AttachmentsGroup);
            CleanAttachmentGroup(grips_AttachmentsGroup);
            CleanAttachmentGroup(magazines_AttachmentsGroup);
            CleanAttachmentGroup(flashlights_AttachmentsGroup);
            CleanAttachmentGroup(lasers_AttachmentsGroup);

            WeaponIdentification wID = wRef.Id;
            CompatibleAttachments compAttachments = weapon.compatibleAttachments;
            DefaultAttachment defAttachments = weapon.GetDefaultAttachments();

            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Barrel), barrels_AttachmentsGroup, wID.Barrel, weapon.GetDefaultAttachment(AttachmentType.Barrel));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Scope), scopes_AttachmentsGroup, wID.Scope, weapon.GetDefaultAttachment(AttachmentType.Scope));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Stock), stocks_AttachmentsGroup, wID.Stock, weapon.GetDefaultAttachment(AttachmentType.Stock));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Grip), grips_AttachmentsGroup, wID.Grip, weapon.GetDefaultAttachment(AttachmentType.Grip));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Magazine), magazines_AttachmentsGroup, wID.Magazine, weapon.GetDefaultAttachment(AttachmentType.Magazine));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Flashlight), flashlights_AttachmentsGroup, wID.Flashlight, weapon.GetDefaultAttachment(AttachmentType.Flashlight));
            GenerateAttachmentGroup(displayCurrentAttachments, compAttachments.GetCompatible(AttachmentType.Laser), lasers_AttachmentsGroup, wID.Laser, weapon.GetDefaultAttachment(AttachmentType.Laser));
        }

        private void GenerateAttachmentGroup(bool displayCurrentAttachments, IReadOnlyList<Attachment> attachments, GameObject attachmentsGroup, Attachment atc, Attachment defaultAttachment)
        {
            if (attachments.Count == 0 || displayCurrentAttachments && atc == null)
            {
                attachmentsGroup.SetActive(false);
                return;
            }
            AttachmentGroupUI atcG = attachmentsGroup.GetComponent<AttachmentGroupUI>();
            if (atc != null)
                atcG.target = atc.transform;
            else if (attachments[0] != null)
                atcG.target = attachments[0].transform;

            attachmentsGroup.SetActive(true);
            for (int i = 0; i < attachments.Count; i++)
            {
                if (attachments[i] == defaultAttachment || displayCurrentAttachments && attachments[i] != atc) continue; // Do not add default attachments to the UI 
                GameObject display = Instantiate(attachmentDisplay_UIElement, attachmentsGroup.transform);
                AttachmentUIElement disp = display.GetComponent<AttachmentUIElement>();

                if (attachments[i].attachmentIdentifier.icon != null)
                    disp.SetIcon(attachments[i].attachmentIdentifier.icon);
                disp.assignedColor = usingAttachmentColor;
                disp.unAssignedColor = notUsingAttachmentColor;
                disp.DeselectAll(atc);
                if (attachments[i] == atc)
                    disp.SelectAsAssigned();
                disp.atc = attachments[i];
                disp.id = i;
                display.SetActive(false);
            }
        }

        private void CleanAttachmentGroup(GameObject attachmentsGroup)
        {
            for (int i = 1; i < attachmentsGroup.transform.childCount; i++)
            {
                Destroy(attachmentsGroup.transform.GetChild(i).gameObject);
            }
        }
        // MOVEMENT    ////////////////////////////////////////////////////////////////////////////////////////

        private List<GameObject> dashElements; // Stores the UI Elements required to display the current dashes amount

        /// <summary>
        /// Draws the dash UI 
        /// </summary>
        private void DrawDashUI(int amountOfDashes)
        {
            dashElements = new List<GameObject>(amountOfDashes);
            for (int i = 0; i < amountOfDashes; i++)
            {
                var uiElement = Instantiate(dashUIElement.gameObject, dashUIContainer);
                dashElements.Add(uiElement);
            }
        }

        private void GainDash(int dashIndex)
        {
            // Adapt the index since lists begin at 0, not 1.
            dashIndex--;
            if (dashIndex >= 0 && dashIndex < dashElements.Count)
            {
                dashElements[dashIndex].SetActive(true);
            }
        }

        private void DashUsed(int dashIndex)
        {
            if (dashIndex >= 0 && dashIndex < dashElements.Count)
            {
                dashElements[dashIndex].SetActive(false);
            }
        }

        // WEAPON    /////////////////////////////////////////////////////////////////////////////////////////

        private void DetectReloadMethod(bool enable, bool useOverheat)
        {
            bulletsUI.gameObject.SetActive(enable);
            magazineUI.gameObject.SetActive(enable);
            overheatUI.transform.parent.gameObject.SetActive(useOverheat);
        }

        private void UpdateHeatRatio(float heatRatio)
        {
            overheatUI.fillAmount = heatRatio;
        }
        private void UpdateBullets(int bullets, int mag, bool activeReloadUI, bool activeLowAmmoUI)
        {
            bulletsUI.SetText("{0}", bullets);
            magazineUI.SetText("{0}", mag);
            reloadUI.gameObject.SetActive(activeReloadUI);
            lowAmmoUI.gameObject.SetActive(activeLowAmmoUI);
        }
        private void DisableWeaponUI()
        {
            overheatUI.transform.parent.gameObject.SetActive(false);
            bulletsUI.gameObject.SetActive(false);
            magazineUI.gameObject.SetActive(false);
            currentWeaponDisplay.gameObject.SetActive(false);
            reloadUI.gameObject.SetActive(false);
            lowAmmoUI.gameObject.SetActive(false);
        }

        private void SetWeaponDisplay(Weapon_SO weapon) => currentWeaponDisplay.sprite = weapon.icon;

        private void EnableDisplay() => currentWeaponDisplay.gameObject.SetActive(true);

        // OTHERS    /////////////////////////////////////////////////////////////////////////////////////////

        public void UpdateXP(bool updatePanel)
        {
            int playerLevel = ExperienceManager.instance.playerLevel; 
            currentLevel.text = (playerLevel + 1).ToString();
            nextLevel.text = (playerLevel + 2).ToString();

            // Stop the previous fill coroutine to prevent overlap
            StopCoroutine(FillExperienceBar());
            StartCoroutine(FillExperienceBar());

            if (updatePanel) UpdateXPPanel();
        }

        public void ChangeScene(int scene) => SceneManager.LoadScene(scene);

        public void UpdateCoins(int amount) => coinsText.text = CoinManager.Instance.coins.ToString();

        // MOUSE VISIBILITY

        public void UnlockMouse() => SetMouseLockState(false);
        
        public void LockMouse()
        {
            if (PauseMenu.isPaused) return;
            SetMouseLockState(true);
        }

        public void SetMouseLockState(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }

        private void OnEnable()
        {
            UIEvents.onExperienceCollected += UpdateXP;
            UIEvents.onHealthChanged += UpdateHealthUI;
            UIEvents.basicHealthUISetUp += HealthSetUp;
            if (barHealthDisplay) healthDisplayMethod += BarHealthDisplayMethod;
            if (numericHealthDisplay) healthDisplayMethod += NumericHealthDisplayMethod;
            UIEvents.allowedInteraction += AllowedInteraction;
            UIEvents.forbiddenInteraction += ForbiddenInteraction;
            UIEvents.disableInteractionUI += DisableInteractionUI;
            UIEvents.onInteractionProgressChanged += InteractionProgressUpdate;
            UIEvents.onFinishInteractionProgress += FinishInteraction;
            UIEvents.onGenerateInspectionUI += GenerateInspectionUI;
            UIEvents.onInitializeDashUI += DrawDashUI;
            UIEvents.onDashGained += GainDash;
            UIEvents.onDashUsed += DashUsed;
            UIEvents.onEnemyHit += Hitmarker;
            UIEvents.showDamagePopUp += AddDamagePopUp;
            UIEvents.onEnemyKilled += AddKillfeed;
            UIEvents.onDetectReloadMethod += DetectReloadMethod;
            UIEvents.onHeatRatioChanged += UpdateHeatRatio;
            UIEvents.onBulletsChanged += UpdateBullets;
            UIEvents.disableWeaponUI += DisableWeaponUI;
            UIEvents.setWeaponDisplay += SetWeaponDisplay;
            UIEvents.enableWeaponDisplay += EnableDisplay;
            UIEvents.onCoinsChange += UpdateCoins;
            UIEvents.onStartInspection += InspectionUIFadeIn;
            UIEvents.onStopInspection += InspectionUIFadeOut; 

            interactUI.SetActive(false);

            pauseMenu.OnPause += UnlockMouse;
            pauseMenu.OnUnPause += LockMouse;
        }
        private void OnDisable()
        {
            UIEvents.onHealthChanged = null;
            UIEvents.basicHealthUISetUp = null;
            healthDisplayMethod = null;
            UIEvents.allowedInteraction = null;
            UIEvents.forbiddenInteraction = null;
            UIEvents.disableInteractionUI = null;
            UIEvents.onInteractionProgressChanged = null;
            UIEvents.onFinishInteractionProgress = null;
            UIEvents.onGenerateInspectionUI = null;
            UIEvents.onInitializeDashUI = null;
            UIEvents.onDashGained = null;
            UIEvents.onDashUsed = null;
            UIEvents.onEnemyHit = null;
            UIEvents.showDamagePopUp = null;
            UIEvents.onEnemyKilled = null;
            UIEvents.onDetectReloadMethod = null;
            UIEvents.onHeatRatioChanged = null;
            UIEvents.onBulletsChanged = null;
            UIEvents.disableWeaponUI = null;
            UIEvents.setWeaponDisplay = null;
            UIEvents.enableWeaponDisplay = null;
            UIEvents.onCoinsChange = null;  
            UIEvents.onExperienceCollected = null;
            UIEvents.onStartInspection = null;
            UIEvents.onStopInspection = null;

            pauseMenu.OnPause -= UnlockMouse;
            pauseMenu.OnUnPause -= LockMouse;
        }

    }
}