// GameUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Required for Coroutines
using TMPro; // Required for TextMeshPro
using UnityEngine.SceneManagement; // Required for SceneManagement to load scenes

public class GameUI : MonoBehaviour
{
    [Header("UI References - Health")]
    public Image healthFillImage;
    public RectTransform healthBarBackgroundRect;

    [Header("UI References - Build-Up Meter")]
    public Image buildUpFillImage;
    public RectTransform buildUpBackgroundRect;

    [Header("UI References - Score")]
    public TextMeshProUGUI scoreText;

    [Header("UI References - Reload Indicator")]
    public Image reloadIndicatorImage;

    [Header("Reload Indicator Animation")]
    public float reloadCompleteFlashDuration = 0.2f;

    [Header("Health Bar Damage Flash Settings")]
    public float damageFlashDuration = 0.1f;
    public Color damageFlashColor = Color.red;

    [Header("Health Bar Low Health Flash Settings")]
    public float lowHealthThreshold = 0.3f;
    public float lowHealthFlashSpeed = 2f;
    public float lowHealthMinAlpha = 0.4f;
    public float lowHealthMaxAlpha = 1.0f;

    [Header("Initial Animation Settings")]
    public float initialFillAnimationDuration = 0.8f;

    // NEW: Pause UI Elements - Panel and text only
    [Header("UI References - Pause Menu")]
    public GameObject pauseMenuPanel; // Assign your PauseMenuPanel GameObject here (only for "GAME PAUSED" background/panel)
    public TextMeshProUGUI pausedText; // Assign your "GAME PAUSED" TextMeshProUGUI here

    // NEW: Global UI Elements - Always visible buttons
    [Header("UI References - Global Buttons")]
    public Button mainMenuButton; // Assign your Main Menu Button here (should be on Canvas, not pause panel)
    
    // MODIFIED: Mute button now takes the Button component AND the Image component from its child
    public Button muteButton; // Assign your Mute Button (the GameObject with the Button component) here
    public Image muteIconImage; // <--- ASSIGN THE IMAGE COMPONENT FROM THE CHILD GAMEOBJECT HERE!

    // RENAMED for clarity: Assign the sprite that shows sound is ON (speaker with waves)
    public Sprite muteSpeakerIcon; 
    // RENAMED for clarity: Assign the sprite that shows sound is OFF (speaker with cross)
    public Sprite muteMutedIcon; 

    // NEW: Confirmation Dialog Elements
    [Header("UI References - Confirmation Dialog")]
    public GameObject confirmationPanel; // Assign your ConfirmationPanel GameObject here
    public Button yesButton; // Assign your Yes Button here
    public Button noButton; // Assign your No Button here

    private float healthBarFullWidth;
    private float buildUpBarFullWidth;
    private Coroutine damageFlashCoroutine;
    private Coroutine lowHealthFlashCoroutine;
    private Coroutine healthFillCoroutine;
    private Coroutine reloadFlashCoroutine;
    private Coroutine reloadFillCoroutine;

    private Color currentHealthBarColorDisplayed;
    private int previousHealth;
    private Color originalReloadIndicatorColor;

    // Audio state
    private bool isMuted = false;


    void Awake()
    {
        // Existing UI Setup (Health, Build-Up, Score, Reload Indicator)
        if (healthFillImage == null || healthBarBackgroundRect == null)
        {
            Debug.LogError("GameUI: Health Fill Image or Health Bar Background Rect not assigned. Disabling script.", this);
            this.enabled = false;
            return;
        }
        healthBarFullWidth = healthFillImage.rectTransform.rect.width;
        healthFillImage.rectTransform.sizeDelta = new Vector2(0, healthFillImage.rectTransform.sizeDelta.y);

        if (buildUpFillImage == null || buildUpBackgroundRect == null)
        {
            Debug.LogError("GameUI: Build Up Fill Image or Build Up Background Rect not assigned. Disabling script.", this);
            this.enabled = false;
            return;
        }
        buildUpBarFullWidth = buildUpFillImage.rectTransform.rect.width;
        buildUpFillImage.rectTransform.sizeDelta = new Vector2(0, buildUpFillImage.rectTransform.sizeDelta.y);

        if (scoreText == null)
        {
            Debug.LogError("GameUI: Score Text (TextMeshProUGUI) not assigned. Disabling script.", this);
            this.enabled = false;
            return;
        }

        if (reloadIndicatorImage == null)
        {
            Debug.LogWarning("GameUI: Reload Indicator Image not assigned. Indicator will not function.");
        }
        else
        {
            originalReloadIndicatorColor = reloadIndicatorImage.color;
            reloadIndicatorImage.gameObject.SetActive(false);
            reloadIndicatorImage.type = Image.Type.Filled;
            reloadIndicatorImage.fillMethod = Image.FillMethod.Radial360;
            reloadIndicatorImage.fillOrigin = (int)Image.Origin360.Top;
            reloadIndicatorImage.fillClockwise = true;
            reloadIndicatorImage.fillAmount = 0;
        }

        previousHealth = -1;
        currentHealthBarColorDisplayed = healthFillImage.color;

        // Pause Menu Panel Setup (Modified)
        if (pauseMenuPanel == null)
        {
            Debug.LogError("GameUI: Pause Menu Panel (for background/text) not assigned. Pause visual feedback will be limited.");
        } else {
            pauseMenuPanel.SetActive(false); // Ensure it starts hidden
        }

        if (pausedText == null) Debug.LogWarning("GameUI: Paused Text (TextMeshProUGUI) not assigned.");

        // Global Buttons Setup (Main Menu & Mute)
        if (mainMenuButton == null) Debug.LogWarning("GameUI: Main Menu Button not assigned.");
        else mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);

        if (muteButton == null)
        {
            Debug.LogError("GameUI: Mute Button (Button component) not assigned. Mute functionality will not work!");
        }
        else
        {
            muteButton.onClick.AddListener(OnMuteButtonClicked);
            // Check if muteIconImage is assigned
            if (muteIconImage == null)
            {
                Debug.LogError("GameUI: Mute Icon Image (the Image component on the child of the button) is NOT ASSIGNED! Mute icon will not change!");
            }
        }
        
        // Check if mute sprites are assigned
        if (muteSpeakerIcon == null) Debug.LogWarning("GameUI: Mute Speaker Icon (Unmuted) not assigned. Mute icon may not display correctly.");
        if (muteMutedIcon == null) Debug.LogWarning("GameUI: Mute Muted Icon (Muted) not assigned. Mute icon may not display correctly.");


        // Confirmation Dialog Setup
        if (confirmationPanel == null)
        {
            Debug.LogError("GameUI: Confirmation Panel not assigned. Main Menu prompt will not function.");
        }
        else
        {
            confirmationPanel.SetActive(false); // Ensure it starts hidden
        }
        if (yesButton == null) Debug.LogWarning("GameUI: Yes Button not assigned for confirmation dialog.");
        else yesButton.onClick.AddListener(OnYesConfirmQuit);

        if (noButton == null) Debug.LogWarning("GameUI: No Button not assigned for confirmation dialog.");
        else noButton.onClick.AddListener(OnNoConfirmQuit);


        // Initialize mute state based on current AudioListener.volume
        // This makes sure the icon is correct from the start if audio is already muted (e.g., from previous session)
        isMuted = AudioListener.volume == 0;
        UpdateMuteButtonIcon(); // Call this immediately to set the correct initial icon
    }

    void OnEnable()
    {
        GameManager.OnScoreChanged += UpdateScoreDisplay;
        GameManager.OnBuildUpChanged += UpdateBuildUpMeter;
        GameManager.OnGamePaused += ShowPauseMenu;
        GameManager.OnGameResumed += HidePauseMenu;
    }

    void OnDisable()
    {
        GameManager.OnScoreChanged -= UpdateScoreDisplay;
        GameManager.OnBuildUpChanged -= UpdateBuildUpMeter;
        GameManager.OnGamePaused -= ShowPauseMenu;
        GameManager.OnGameResumed -= HidePauseMenu;

        // Clean up button listeners
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
        if (muteButton != null) muteButton.onClick.RemoveListener(OnMuteButtonClicked);
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesConfirmQuit);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoConfirmQuit);
    }

    void Update()
    {
        // Listen for 'Escape' key to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null)
            {
                if (Time.timeScale > 0) // If game is running, pause it
                {
                    GameManager.Instance.PauseGame();
                }
                else // If game is paused, attempt to resume if confirmation dialog is NOT active
                {
                    // If confirmation panel is active, Escape key should effectively "Cancel" the confirmation
                    // and return to the normal pause menu state.
                    if (confirmationPanel != null && confirmationPanel.activeSelf)
                    {
                        OnNoConfirmQuit(); // Treat Escape as 'No', meaning resume game
                    }
                    else
                    {
                        GameManager.Instance.ResumeGame();
                    }
                }
            }
        }

        // Listen for 'M' key to toggle mute
        if (Input.GetKeyDown(KeyCode.M))
        {
            OnMuteButtonClicked();
        }

        // Handle click to resume when game is paused and no confirmation dialog is active
        HandleClickToResume();
    }

    private void HandleClickToResume()
    {
        // Only allow click to resume if game is paused and the main pause menu is visible, AND confirmation dialog is not active
        // This logic is for clicking outside buttons to resume from the 'GAME PAUSED' screen.
        if (Time.timeScale == 0f && pauseMenuPanel != null && pauseMenuPanel.activeSelf && (confirmationPanel == null || !confirmationPanel.activeSelf))
        {
            // Check for left mouse click or touch
            if (Input.GetMouseButtonDown(0))
            {
                // We need to check if the click was NOT over any active UI elements that should block resuming
                // This typically means checking if the click was over the main menu or mute buttons.
                bool clickedOnUI = false;
                if (mainMenuButton != null && RectTransformUtility.RectangleContainsScreenPoint(mainMenuButton.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    clickedOnUI = true;
                }
                // Check if the click was over the mute button (using its RectTransform)
                if (muteButton != null && RectTransformUtility.RectangleContainsScreenPoint(muteButton.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    clickedOnUI = true;
                }
                // Add more checks here for other interactive UI elements if they are present on the screen while paused.

                if (!clickedOnUI)
                {
                    // If the click was not on any critical UI, resume the game
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ResumeGame();
                    }
                }
            }
        }
    }


    public void InitializeHealthBar(int initialHealth, int initialMaxHealth)
    {
        previousHealth = initialMaxHealth + 1;
        UpdateHealthBarVisuals(initialHealth, initialMaxHealth, initialFillAnimationDuration);
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (currentHealth < previousHealth && previousHealth != -1)
        {
            if (damageFlashCoroutine != null)
            {
                StopCoroutine(damageFlashCoroutine);
            }
            damageFlashCoroutine = StartCoroutine(DoDamageFlash(currentHealthBarColorDisplayed));
        }

        UpdateHealthBarVisuals(currentHealth, maxHealth);
        previousHealth = currentHealth;
    }

    private void UpdateHealthBarVisuals(int currentHealth, int maxHealth, float animationDurationOverride = -1f)
    {
        float healthPercentage = (float)currentHealth / maxHealth;
        float targetWidth = healthBarFullWidth * healthPercentage;

        float effectiveAnimationDuration = (animationDurationOverride != -1f) ? animationDurationOverride : 0.2f;

        if (healthFillCoroutine != null)
        {
            StopCoroutine(healthFillCoroutine);
        }
        healthFillCoroutine = StartCoroutine(DoHealthBarFillAnimation(healthFillImage.rectTransform.sizeDelta.x, targetWidth, effectiveAnimationDuration));

        Color determinedBaseColor;
        if (healthPercentage > 0.6f)
        {
            if (!ColorUtility.TryParseHtmlString("#34F571", out determinedBaseColor)) { determinedBaseColor = Color.green; }
        }
        else if (healthPercentage > 0.3f)
        {
            if (!ColorUtility.TryParseHtmlString("#F5D234", out determinedBaseColor)) { determinedBaseColor = Color.yellow; }
        }
        else
        {
            if (!ColorUtility.TryParseHtmlString("#F53438", out determinedBaseColor)) { determinedBaseColor = Color.red; }
        }

        currentHealthBarColorDisplayed = determinedBaseColor;

        if (damageFlashCoroutine == null && lowHealthFlashCoroutine == null)
        {
            healthFillImage.color = currentHealthBarColorDisplayed;
        }

        if (healthPercentage <= lowHealthThreshold)
        {
            if (lowHealthFlashCoroutine == null)
            {
                lowHealthFlashCoroutine = StartCoroutine(DoLowHealthContinuousFlash(currentHealthBarColorDisplayed));
            }
        }
        else
        {
            if (lowHealthFlashCoroutine != null)
            {
                StopCoroutine(lowHealthFlashCoroutine);
                lowHealthFlashCoroutine = null;
                healthFillImage.color = currentHealthBarColorDisplayed;
            }
        }
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + newScore.ToString("000");
        }
    }

    private void UpdateBuildUpMeter(int currentBuildUp, int maxBuildUp)
    {
        if (buildUpFillImage == null) return;
        float buildUpPercentage = (float)currentBuildUp / maxBuildUp;
        float newWidth = buildUpBarFullWidth * buildUpPercentage;
        buildUpFillImage.rectTransform.sizeDelta = new Vector2(newWidth, buildUpFillImage.rectTransform.sizeDelta.y);
    }

    public void SetReloadIndicatorVisibility(bool isVisible)
    {
        if (reloadIndicatorImage != null)
        {
            reloadIndicatorImage.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                reloadIndicatorImage.color = new Color(originalReloadIndicatorColor.r, originalReloadIndicatorColor.g, originalReloadIndicatorColor.b, 0.6f);
                reloadIndicatorImage.fillAmount = 0;
                if (reloadFillCoroutine != null) StopCoroutine(reloadFillCoroutine);
            }
            else
            {
                reloadIndicatorImage.color = originalReloadIndicatorColor;
                if (reloadFillCoroutine != null) StopCoroutine(reloadFillCoroutine);
            }

            if (!isVisible && reloadFlashCoroutine != null)
            {
                StopCoroutine(reloadFlashCoroutine);
                reloadFlashCoroutine = null;
            }
        }
    }

    public void UpdateReloadFill(float percentage)
    {
        if (reloadIndicatorImage != null && reloadIndicatorImage.gameObject.activeSelf)
        {
            reloadIndicatorImage.fillAmount = Mathf.Clamp01(percentage);
        }
    }

    public void StartReloadCompleteFlash()
    {
        if (reloadIndicatorImage != null)
        {
            if (reloadFlashCoroutine != null)
            {
                StopCoroutine(reloadFlashCoroutine);
            }
            reloadFlashCoroutine = StartCoroutine(DoReloadCompleteFlash());
        }
    }

    private IEnumerator DoReloadCompleteFlash()
    {
        if (reloadFillCoroutine != null)
        {
            StopCoroutine(reloadFillCoroutine);
            reloadFillCoroutine = null;
        }

        reloadIndicatorImage.gameObject.SetActive(true);
        reloadIndicatorImage.color = originalReloadIndicatorColor;
        reloadIndicatorImage.fillAmount = 1;

        yield return new WaitForSeconds(reloadCompleteFlashDuration);
        reloadIndicatorImage.gameObject.SetActive(false);
        reloadFlashCoroutine = null;
    }


    private IEnumerator DoHealthBarFillAnimation(float startWidth, float targetWidth, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentWidth = Mathf.Lerp(startWidth, targetWidth, timer / duration);
            healthFillImage.rectTransform.sizeDelta = new Vector2(currentWidth, healthFillImage.rectTransform.sizeDelta.y);
            yield return null;
        }
        healthFillImage.rectTransform.sizeDelta = new Vector2(targetWidth, healthFillImage.rectTransform.sizeDelta.y);
        healthFillCoroutine = null;
    }


    private IEnumerator DoDamageFlash(Color finalColorAfterFlash)
    {
        float timer = 0f;
        healthFillImage.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        float fadeBackDuration = 0.2f;
        Color startColor = healthFillImage.color;
        Color targetColor = currentHealthBarColorDisplayed;

        while (timer < fadeBackDuration)
        {
            timer += Time.deltaTime;
            float targetAlpha = lowHealthFlashCoroutine != null ? healthFillImage.color.a : targetColor.a;
            healthFillImage.color = Color.Lerp(startColor, new Color(targetColor.r, targetColor.g, targetColor.b, targetAlpha), timer / fadeBackDuration);
            yield return null;
        }

        if (lowHealthFlashCoroutine == null)
        {
            healthFillImage.color = currentHealthBarColorDisplayed;
        }
        damageFlashCoroutine = null;
    }


    private IEnumerator DoLowHealthContinuousFlash(Color baseLowHealthColor)
    {
        while (true)
        {
            float timer = 0f;
            Color startColor = new Color(baseLowHealthColor.r, baseLowHealthColor.g, baseLowHealthColor.b, lowHealthMinAlpha);
            Color endColor = new Color(baseLowHealthColor.r, baseLowHealthColor.g, baseLowHealthColor.b, lowHealthMaxAlpha);
            float durationHalf = (1f / lowHealthFlashSpeed) / 2f;

            while (timer < durationHalf)
            {
                timer += Time.deltaTime;
                healthFillImage.color = Color.Lerp(startColor, endColor, timer / durationHalf);
                yield return null;
            }
            healthFillImage.color = endColor; // Ensure it reaches max alpha

            timer = 0f;
            startColor = endColor; // Start from where fade in ended
            endColor = new Color(baseLowHealthColor.r, baseLowHealthColor.g, baseLowHealthColor.b, lowHealthMinAlpha);
            while (timer < durationHalf)
            {
                timer += Time.deltaTime;
                healthFillImage.color = Color.Lerp(startColor, endColor, timer / durationHalf);
                yield return null;
            }
            healthFillImage.color = endColor; // Ensure it reaches min alpha
        }
    }

    // Pause Menu UI Methods (Modified to only show/hide text and background panel)
    private void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        if (pausedText != null) pausedText.gameObject.SetActive(true);
    }

    private void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        if (pausedText != null) pausedText.gameObject.SetActive(false);
        // Ensure confirmation panel is hidden if game is resumed by any means
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    // Global Button Click Handlers (MainMenu Modified to show confirmation)
    public void OnMainMenuButtonClicked()
    {
        if (confirmationPanel != null)
        {
            // Pause the game if it's not already, before showing confirmation
            if (GameManager.Instance != null && Time.timeScale > 0)
            {
                GameManager.Instance.PauseGame();
            }

            // Explicitly hide the pause menu panel when showing the confirmation dialog
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
            if (pausedText != null)
            {
                pausedText.gameObject.SetActive(false);
            }

            confirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("GameUI: Confirmation panel not assigned. Cannot prompt before returning to main menu.");
            // Fallback: Directly load if confirmation panel is missing
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadStartScene();
            }
        }
    }

    public void OnMuteButtonClicked()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0 : 1; // 0 for mute, 1 for unmute
        UpdateMuteButtonIcon(); // This is the call that updates the image
        Debug.Log($"Audio Muted: {isMuted}");
    }

    private void UpdateMuteButtonIcon()
    {
        // Now using muteIconImage, which is directly assigned
        if (muteIconImage != null && muteSpeakerIcon != null && muteMutedIcon != null)
        {
            // Log for debugging: What state are we trying to achieve?
            Debug.Log($"UpdateMuteButtonIcon: Current isMuted state is: {isMuted}.");
            Debug.Log($"UpdateMuteButtonIcon: muteSpeakerIcon assigned: {muteSpeakerIcon.name}. muteMutedIcon assigned: {muteMutedIcon.name}.");


            if (isMuted)
            {
                muteIconImage.sprite = muteMutedIcon; // If muted, show the muted icon
                Debug.Log("UpdateMuteButtonIcon: Setting sprite to muteMutedIcon.");
            }
            else
            {
                muteIconImage.sprite = muteSpeakerIcon; // If unmuted, show the speaker icon
                Debug.Log("UpdateMuteButtonIcon: Setting sprite to muteSpeakerIcon.");
            }
        }
        else
        {
            // Provides more specific error if resources are missing
            if (muteIconImage == null) Debug.LogError("UpdateMuteButtonIcon: muteIconImage is null! Make sure to drag the Image component from the CHILD of the Mute Button into the 'Mute Icon Image' slot in the Inspector.");
            if (muteSpeakerIcon == null) Debug.LogWarning("UpdateMuteButtonIcon: muteSpeakerIcon (Unmuted) is null! Cannot update icon.");
            if (muteMutedIcon == null) Debug.LogWarning("UpdateMuteButtonIcon: muteMutedIcon (Muted) is null! Cannot update icon.");
        }
    }

    // NEW: Confirmation Dialog Handlers
    private void OnYesConfirmQuit()
    {
        if (GameManager.Instance != null)
        {
            // Unpause the game before loading the scene to avoid issues in the new scene
            GameManager.Instance.ResumeGame(); // This will also hide the pause menu and confirmation panel
            GameManager.Instance.LoadStartScene();
        }
        else
        {
            // Fallback in case GameManager is null
            Debug.LogError("GameUI: GameManager.Instance is null! Cannot load Start Scene.");
            SceneManager.LoadScene("Start"); // Assuming "Start" is the correct scene name
        }
    }

    private void OnNoConfirmQuit()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false); // Hide the confirmation dialog
        }

        // Per user's explicit instruction: "No" means go back to the game immediately (resume)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame(); // This will also handle hiding the pause menu via event
        }
    }
}