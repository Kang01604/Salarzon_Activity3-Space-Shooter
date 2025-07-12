// PlayerController.cs
using UnityEngine;
using System.Collections; // Required for Coroutines
using SBS.ME; // Import MeshExploder namespace (ensure you have this asset or remove if not used)
using System.Collections.Generic; // For List and other collections
using UnityEngine.SceneManagement; // NEW: Required for SceneManagement to ensure GameManager is initialized for scene transitions

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float tiltAngle = 20f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePointObj;
    public float fireRate = 0.3f; // Base fire rate
    public float projectileSpreadAngle = 15f; // Angle between projectiles for multi-shot
    private float nextFireTime;
    
    // Projectile Burst and Reload
    public int maxProjectilesInBurst = 3; // Maximum projectiles before reload
    public float reloadDelay = 1f;        // Delay after burst before firing again
    private int currentProjectilesInBurst;
    private bool isReloading = false;

    // --- AUDIO SECTION ---
    [Header("Audio Clips")] // New Header for Audio
    public AudioClip fireSound; // Assign your firing sound here in the Inspector
    public AudioClip hurtSound; // Assign your hurt sound here in the Inspector
    public AudioClip deathSound; // Assign your death/explosion sound here in the Inspector
    public AudioClip healSound; // Assign your healing sound here in the Inspector
    private AudioSource audioSource; // Reference to the AudioSource component on this GameObject
    // --- END AUDIO SECTION ---
    
    [Header("Health & Damage")]
    public int maxHealth = 30; // Player now has 30 HP (base value)
    public float invincibilityDuration = 2f;
    public float flashInterval = 0.1f;
    private int currentHealth;
    private bool isInvincible = false;
    private Renderer playerRenderer;
    private Color originalColor;
    private MeshExploder meshExploder;
    private Rigidbody playerRigidbody;

    // Damage Values (from non-enemy sources)
    [Header("Damage Values")]
    public int smallMeteorDamage = 2;
    public int mediumMeteorDamage = 5;
    public int bigMeteorDamage = 10;
    [Header("Camera Shake on Hit")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;
    private CameraShake mainCameraShake; // Reference to CameraShake script

    // UI Health Bar Reference
    private GameUI gameUI; // Correctly referencing GameUI (assuming you have this)
    // NEW: Reference to the UIFollowTarget script for the indicator
    private UIFollowTarget reloadIndicatorFollower;
    [Header("Healing Effect")]
    public float healGlowDuration = 0.5f; // How long the green glow lasts
    public float healGlowInterval = 0.05f; // How fast it flashes during healing
    private bool isHealingGlowActive = false; // To prevent multiple healing glows

    // Player Stats that can be upgraded
    [Header("Player Upgradable Stats")]
    private int currentProjectileDamage; // Actual damage dealt by player projectiles
    private float currentProjectileSpeed; // Actual speed of player projectiles
    private float currentFireRate; // Actual fire rate (lower value = faster)
    private int currentMaxHealth; // Tracks the current maximum health, including upgrades
    private int numberOfProjectiles = 1; // Number of projectiles to fire at once

    // Upgrade UI Reference
    public GameObject upgradePanelPrefab; // Assign your UI Panel Prefab here in the Inspector
    private GameObject currentUpgradePanelInstance; // To hold the instantiated UI panel

    // Enum for upgrade types (must match UpgradeSelectionUI)
    public enum UpgradeType
    {
        IncreasedProjectileDamage,
        IncreasedProjectileSpeed,
        IncreasedHP,
        IncreasedFireRate,
        IncreasedDifficulty, // New Upgrade
        IncreasedProjectile // New Upgrade
    }

    void Awake()
    {
        // Get components at Awake
        playerRenderer = GetComponent<Renderer>();
        meshExploder = GetComponent<MeshExploder>();
        playerRigidbody = GetComponent<Rigidbody>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("PlayerController: AudioSource component not found on player. Firing sound will not play.");
        }
    }

    void Start()
    {
        // Initialize player health
        currentHealth = maxHealth;
        currentMaxHealth = maxHealth; // Set currentMaxHealth to initial maxHealth

        // Store original color for flashing effects
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
        }
        
        // Initialize projectile burst count
        currentProjectilesInBurst = maxProjectilesInBurst;
        // Initialize current stats with base values
        currentProjectileDamage = 2;
        // You can make this public if you want to set it in inspector
        currentProjectileSpeed = 20f;
        // You can make this public
        currentFireRate = fireRate;
        // Use the public fireRate as base

        // Get reference to CameraShake script
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            mainCameraShake = mainCamera.GetComponent<CameraShake>();
            if (mainCameraShake == null)
            {
                Debug.LogWarning("PlayerController: CameraShake script not found on Main Camera. Camera shake will not occur.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerController: Main Camera with 'MainCamera' tag not found. Camera shake will not occur.");
        }

        // Correctly reference GameUI for health initialization
        GameObject uiManager = GameObject.FindWithTag("UIManager");
        if (uiManager != null)
        {
            gameUI = uiManager.GetComponentInChildren<GameUI>();
            if (gameUI != null)
            {
                gameUI.InitializeHealthBar(currentHealth, maxHealth);
                // Use 'maxHealth' for initial setup
                Debug.Log("PlayerController: Found GameUI and initialized health bar.");
                // NEW: Get reference to the UIFollowTarget script and set its target
                if (gameUI.reloadIndicatorImage != null) // Check if the image itself is assigned in GameUI
                {
                    reloadIndicatorFollower = gameUI.reloadIndicatorImage.GetComponent<UIFollowTarget>();
                    if (reloadIndicatorFollower != null)
                    {
                        reloadIndicatorFollower.target = this.transform;
                        // Set player as target
                        // Ensure indicator is hidden initially via GameUI
                        gameUI.SetReloadIndicatorVisibility(false);
                    }
                    else
                    {
                        Debug.LogWarning("PlayerController: UIFollowTarget script not found on the Reload Indicator Image GameObject. Indicator will not follow player.");
                    }
                }
                else
                {
                   Debug.LogWarning("PlayerController: Reload Indicator Image not assigned in GameUI. Cannot set up UIFollowTarget.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerController: GameUI script not found on UIManager or its children. Health UI will not function.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerController: UIManager GameObject not found. Health UI will not function.");
        }

        // Subscribe to GameManager's OnBuildUpFull event
        GameManager.OnBuildUpFull += PromptUpgrade;
        // Subscribe to GameManager's OnBuildUpFull event for healing
        GameManager.OnBuildUpFull += HandleBuildUpFullHealing;
        // Subscribe to the event
    }

    void OnDestroy()
    {
        // Unsubscribe from GameManager events to prevent memory leaks
        GameManager.OnBuildUpFull -= PromptUpgrade;
        GameManager.OnBuildUpFull -= HandleBuildUpFullHealing; // Unsubscribe
    }

    void Update()
    {
        // Only allow movement and shooting if game is not paused
        if (Time.timeScale > 0)
        {
            HandleMovement();
            HandleShooting();
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        transform.position += Vector3.right * h * moveSpeed * Time.deltaTime;

        float targetZ = -h * tiltAngle;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetZ);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
    }

    void HandleShooting()
    {
        bool fireKey = Input.GetKey(KeyCode.Space) ||
        Input.GetMouseButton(0);

        // Allow firing only if fire key is pressed, fire rate allows, projectiles are available, and not reloading
        if (fireKey && Time.time >= nextFireTime && currentProjectilesInBurst > 0 && !isReloading)
        {
            nextFireTime = Time.time + currentFireRate;
            // Use currentFireRate for calculations

            // --- MULTI-SHOT LOGIC ---
            float angleStep = (numberOfProjectiles > 1) ?
            projectileSpreadAngle : 0f;
            float startingAngle = (numberOfProjectiles > 1) ? -angleStep * (numberOfProjectiles - 1) / 2f : 0f;
            for (int i = 0; i < numberOfProjectiles; i++)
            {
                float currentAngle = startingAngle + i * angleStep;
                Quaternion spreadRotation = Quaternion.Euler(0, currentAngle, 0);
                Quaternion projectileRotation = firePointObj.rotation * spreadRotation;
                // Instantiate projectile
                GameObject projectileGO = Instantiate(
                    projectilePrefab,
                    firePointObj.position,
                    projectileRotation
                );
                // Set the projectile's damage and speed based on player's current stats
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.damageAmount = currentProjectileDamage;
                    projectile.speed = currentProjectileSpeed;
                }
            }
            
            if (audioSource != null && fireSound != null)
            {
                audioSource.PlayOneShot(fireSound);
                // Play the sound effect once per volley
            }

            currentProjectilesInBurst--;
            // Decrement projectile count
            
            // If no projectiles left, start reload
            if (currentProjectilesInBurst <= 0)
            {
                StartCoroutine(ReloadBurst());
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check for healing cube first, as it ignores invincibility
        if (other.CompareTag("HealingCube"))
        {
            // The HealingCube script handles calling player.Heal() and its own destruction.
            return; // Exit as this collision is handled by the HealingCube script
        }

        if (isInvincible) return;
        // Ignore damage if invincible

        if (other.CompareTag("EnemyProjectile"))
        {
            EnemyProjectile enemyProjectile = other.GetComponent<EnemyProjectile>();
            if (enemyProjectile != null)
            {
                TakeDamage(enemyProjectile.damage);
                // Use damage from projectile
            }
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Meteor"))
        {
            Meteor hitMeteor = other.GetComponent<Meteor>();
            if (hitMeteor != null)
            {
                hitMeteor.ExplodeMeteor(false);
                // Corrected: Pass false as it's not a projectile kill
                // Determine damage based on meteor size
                switch (hitMeteor.currentSize)
                {
                    case Meteor.MeteorSize.Small:
                        TakeDamage(smallMeteorDamage);
                        break;
                    case Meteor.MeteorSize.Medium:
                        TakeDamage(mediumMeteorDamage);
                        break;
                    case Meteor.MeteorSize.Big:
                        TakeDamage(bigMeteorDamage);
                        break;
                    default:
                        TakeDamage(smallMeteorDamage);
                        // Fallback damage
                        Debug.LogWarning("PlayerController: Unknown meteor size, applying small meteor damage.");
                        break;
                }
            }
        }
    }

    public void Heal(int healAmount)
    {
        // Only heal if not at max health
        if (currentHealth < currentMaxHealth) // Use currentMaxHealth
        {
            currentHealth = Mathf.Min(currentHealth + healAmount, currentMaxHealth);
            Debug.Log($"PlayerController: Player healed for {healAmount}. Current Health: {currentHealth}/{currentMaxHealth}");

            // Play healing sound
            if (audioSource != null && healSound != null) 
            {
                audioSource.PlayOneShot(healSound);
            }

            // Update UI Health Bar
            if (gameUI != null)
            {
                gameUI.UpdateHealthBar(currentHealth, currentMaxHealth);
            }
            else
            {
                Debug.LogError("PlayerController: gameUI reference is NULL when trying to update health bar during healing!");
            }

            // Start the healing glow effect if not already active
            if (!isHealingGlowActive && playerRenderer != null)
            {
                StartCoroutine(HealingGlowEffect());
            }
        }
        else
        {
            Debug.Log("PlayerController: Health is already full, no healing needed.");
        }
    }

    // New method to handle healing when build-up meter is full
    private void HandleBuildUpFullHealing()
    {
        int healAmount = Mathf.CeilToInt(currentMaxHealth * 0.5f);
        // Calculate 50% of currentMaxHealth
        Heal(healAmount);
        // Call the existing Heal method
        Debug.Log("PlayerController: Healing for 50% max HP due to build-up full.");
    }

    void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"PlayerController: Player taking {damageAmount} damage. Current Health: {currentHealth}/{currentMaxHealth}");

        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
            // Play the hurt sound effect
        }

        // Update UI Health Bar
        if (gameUI != null)
        {
            gameUI.UpdateHealthBar(currentHealth, currentMaxHealth);
            // Use currentMaxHealth for UI
        }
        else
        {
            Debug.LogError("PlayerController: gameUI reference is NULL when trying to update health bar!");
        }

        // Trigger Camera Shake
        if (mainCameraShake != null)
        {
            mainCameraShake.StartShake(shakeDuration, shakeMagnitude);
        }

        if (currentHealth <= 0)
        {
            ExplodeAndDie();
            // Player defeated
        }
        else
        {
            StartCoroutine(InvincibilityFlash());
            // Start invincibility
        }
    }

    void ExplodeAndDie()
    {
        Debug.Log("PlayerController: Player Defeated!");
        // Play the death sound *before* the script is disabled or GameObject is potentially destroyed/hidden.
        if (audioSource != null && deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        this.enabled = false;
        // Disable script

        // Disable collider to prevent further interactions
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Enable physics for explosion/ragdoll effect if Rigidbody is present
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.useGravity = true;      
        }
        else
        {
            Debug.LogWarning("PlayerController: No Rigidbody found on " + gameObject.name + ". Gravity will not affect the main object directly.");
        }

        // Trigger MeshExploder if available, otherwise just hide/destroy
        if (meshExploder != null)
        {
            meshExploder.EXPLODE();
        }
        else
        {
            Debug.LogWarning("PlayerController: MeshExploder component not found on player. Hiding renderer.");
            if (playerRenderer != null) playerRenderer.enabled = false;
        }

        // NEW: Call GameManager.Instance.EndGame() to handle game over scene transition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndGame();
        }
        else
        {
            Debug.LogError("PlayerController: GameManager.Instance is null! Cannot trigger EndGame(). Ensure GameManager is present in the scene and persistent.");
            // Fallback: If GameManager is not found, you might want to manually load the scene
            // SceneManager.LoadScene("GameOver"); // Replace "GameOverScene" with your actual game over scene name
        }
    }

    // Coroutine for invincibility visual feedback (flashing)
    IEnumerator InvincibilityFlash()
    {
        isInvincible = true;
        float endTime = Time.time + invincibilityDuration;

        while (Time.time < endTime)
        {
            if (playerRenderer != null)
            {
                playerRenderer.material.color = Color.red;
                yield return new WaitForSeconds(flashInterval);
                playerRenderer.material.color = originalColor;
                yield return new WaitForSeconds(flashInterval);
            }
        }
        
        // Ensure the color is reset to original after the effect
        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }
        isInvincible = false;
    }

    // Coroutine for healing glow effect
    IEnumerator HealingGlowEffect()
    {
        isHealingGlowActive = true;
        float endTime = Time.time + healGlowDuration;

        while (Time.time < endTime)
        {
            if (playerRenderer != null)
            {
                playerRenderer.material.color = Color.green;
                // Glow green
                yield return new WaitForSeconds(healGlowInterval);
                playerRenderer.material.color = originalColor; // Return to original
                yield return new WaitForSeconds(healGlowInterval);
            }
        }

        // Ensure it ends on original color
        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }
        isHealingGlowActive = false;
    }

    // Coroutine for reloading projectiles
    IEnumerator ReloadBurst()
    {
        isReloading = true;
        // Set reloading flag
        Debug.Log("Reloading projectiles...");
        // Show reload indicator when reloading starts
        if (gameUI != null)
        {
            gameUI.SetReloadIndicatorVisibility(true);
            // Start the fill animation
            StartCoroutine(DoReloadFillAnimation(reloadDelay));
        }

        yield return new WaitForSeconds(reloadDelay);
        // Wait for the reload delay

        currentProjectilesInBurst = maxProjectilesInBurst;
        // Reset projectile count
        isReloading = false;
        // Reset reloading flag
        Debug.Log("Reload complete! Projectiles are ready.");
        // Flash and hide reload indicator when reload is complete
        if (gameUI != null)
        {
            gameUI.StartReloadCompleteFlash();
        }
    }

    // NEW: Coroutine to manage the reload fill animation
    private IEnumerator DoReloadFillAnimation(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float fillPercentage = timer / duration;
            if (gameUI != null)
            {
                gameUI.UpdateReloadFill(fillPercentage);
            }
            yield return null;
        }
        if (gameUI != null)
        {
            gameUI.UpdateReloadFill(1f);
            // Ensure it's 100% filled at the end of duration
        }
    }


    // Method to prompt upgrade selection
    void PromptUpgrade()
    {
        Debug.Log("Time to choose an upgrade!");
        // Pause the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Time.timeScale = 0f;
            // Fallback pause
        }

        // Instantiate and display the upgrade UI
        if (upgradePanelPrefab != null)
        {
            // Find the UIManager's transform to parent the upgrade panel to it
            // Assuming UIManager has the tag "UIManager" and is the Canvas root or a direct child of Canvas.
            Transform uiManagerTransform = GameObject.FindGameObjectWithTag("UIManager")?.transform;

            if (uiManagerTransform != null)
            {
                // Instantiate the prefab as a child of the UIManager's transform
                currentUpgradePanelInstance = Instantiate(upgradePanelPrefab, uiManagerTransform);
                // Reset its local position to zero to center it within the parent's area
                currentUpgradePanelInstance.transform.localPosition = Vector3.zero;
                // Ensure its local scale is 1,1,1, as UI elements can inherit scale issues
                currentUpgradePanelInstance.transform.localScale = Vector3.one;
                UpgradeSelectionUI upgradeUI = currentUpgradePanelInstance.GetComponent<UpgradeSelectionUI>();
                if (upgradeUI != null)
                {
                    upgradeUI.Initialize(this);
                    // Pass PlayerController reference to the UI script
                    upgradeUI.DisplayUpgrades();
                }
                else
                {
                    Debug.LogError("UpgradePanelPrefab is missing UpgradeSelectionUI component! Cannot display upgrades.");
                    // If UI script is missing, unpause game as fallback
                    if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
                    else Time.timeScale = 1f;
                    Destroy(currentUpgradePanelInstance); // Destroy the panel if it's not configured correctly
                    currentUpgradePanelInstance = null;
                }
            }
            else
            {
                Debug.LogError("UIManager GameObject with tag 'UIManager' not found! Cannot parent upgrade panel. Ensure UIManager exists and has the correct tag.");
                // If UIManager is not found, unpause game as fallback
                if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
                else Time.timeScale = 1f;
            }
        }
        else
        {
            Debug.LogError("Upgrade Panel Prefab is not assigned in PlayerController! Game will remain paused.");
            // If UI prefab is not assigned, game remains paused. Consider unpausing here if it's not critical.
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            else Time.timeScale = 1f;
        }
    }

    // Method called by UI when an upgrade is chosen
    public void ApplyUpgrade(UpgradeType chosenUpgrade)
    {
        switch (chosenUpgrade)
        {
            case UpgradeType.IncreasedProjectileDamage:
                currentProjectileDamage += 1;
                Debug.Log($"Upgrade: Projectile Damage increased to {currentProjectileDamage}");
                break;
            case UpgradeType.IncreasedProjectileSpeed:
                currentProjectileSpeed *= 1.05f;
                // +5%
                Debug.Log($"Upgrade: Projectile Speed increased to {currentProjectileSpeed:F2}");
                break;
            case UpgradeType.IncreasedHP:
                currentMaxHealth += 10;
                // Increase max health
                currentHealth = currentMaxHealth;
                // Heal to new max health
                if (gameUI != null) gameUI.UpdateHealthBar(currentHealth, currentMaxHealth);
                Debug.Log($"Upgrade: Max HP increased to {currentMaxHealth}, Current HP: {currentHealth}");
                break;
            case UpgradeType.IncreasedFireRate:
                currentFireRate *= 0.98f;
                // +2% faster, so fireRate decreases by 2%
                currentFireRate = Mathf.Max(0.05f, currentFireRate);
                // Set a minimum fire rate to avoid extremely fast firing
                Debug.Log($"Upgrade: Fire Rate increased (faster) to {currentFireRate:F2}");
                break;
            case UpgradeType.IncreasedProjectile:
                numberOfProjectiles++;
                Debug.Log($"Upgrade: Now firing {numberOfProjectiles} projectiles.");
                break;
            case UpgradeType.IncreasedDifficulty:
                if (GameManager.Instance != null) GameManager.Instance.IncreaseDifficultyManually();
                Debug.Log("Upgrade Chosen: Enemies have become stronger!");
                break;
        }

        // Destroy the upgrade UI panel
        if (currentUpgradePanelInstance != null)
        {
            Destroy(currentUpgradePanelInstance);
            currentUpgradePanelInstance = null;
        }

        // Unpause the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Time.timeScale = 1f;
            // Fallback unpause
        }
        Debug.Log("Game unpaused.");
    }
}