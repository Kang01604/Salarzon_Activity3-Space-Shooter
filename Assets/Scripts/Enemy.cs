// Enemy.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SBS.ME; // Ensure this namespace is correct for your MeshExploder

public class Enemy : MonoBehaviour
{
    [Header("Approach & Hover Movement")]
    public float forwardSpeed = 30f;
    public float stopZ = 45f; // Z-position where enemy stops forward movement
    public float stopZVariance = 5f; // Random variance for stopZ
    public float hoverAmplitudeZ = 2f; // How much enemy bobs up and down when at stopZ
    public float hoverSpeedZ = 1f; // Speed of Z-axis bobbing

    [Header("Horizontal Aggressive Movement")]
    public float horizontalSpeed = 5f; // Base speed for left/right movement
    public float minX = -10f, maxX = 10f; // Playable area X bounds
    public float dodgeSmoothness = 0.5f; // How smoothly enemy changes horizontal direction (lower is snappier)

    [Header("Visuals")]
    public float tiltAngle = 20f; // Angle for visual tilt based on horizontal movement

    [Header("Avoidance (Collision with other enemies/meteors)")]
    public float separationRadius = 3f; // Radius to detect nearby objects for avoidance
    public float separationStrength = 1f; // How strongly to push away from detected objects
    public LayerMask avoidanceLayers; // Layers containing other enemies or meteors

    [Header("Base Stats")]
    public int baseMaxHealth = 3;
    public int baseProjectileDamage = 5;
    public int baseBurstShotCount = 3; // Number of shots in a burst

    [Header("Health & Damage")]
    private int maxHealth; // This will be calculated based on difficulty
    private int currentHealth;
    public float invincibilityDuration = 3f; // Duration of invincibility after taking damage
    public float flashInterval = 0.1f; // How fast the enemy flashes when invincible
    public float deathDelay = 2.0f; // Delay before enemy GameObject is destroyed after death

    [Header("Build-Up & Score Values")] // Combined header for consistency
    public int buildUpOnDefeat = 50;
    public int scoreOnDefeat = 500;

    [Header("Firing Properties")]
    public GameObject enemyProjectilePrefab; // Assign your projectile prefab here
    public Transform firePoint; // Assign the transform where projectiles spawn from
    public float baseBurstFireRate = 3.0f; // Time between bursts (lower is faster)
    public float burstFireRateVariance = 0.5f; // Random variation added to burst fire rate
    private int burstShotCount; // Calculated based on difficulty
    public float timeBetweenBurstShots = 0.15f; // Delay between individual shots in a burst
    public float playerDetectionRange = 20f; // Max distance to detect player for firing
    [Range(0.0f, 1.0f)]
    public float predictiveAimingStrength = 0.7f; // Chance to predict player movement for aiming
    public float minAimToleranceX = 0.5f; // How close enemy X needs to be to player X for a 'perfect' shot
    public float maxAimToleranceX = 1.0f; // Max X difference allowed to fire (Enemy will try to get within minAimToleranceX)
    [Range(0.0f, 1.0f)]
    public float homingShotChance = 0.2f; // Percentage chance for a projectile to be homing
    private float nextFireTime; // Internal timer for next burst

    [Header("Progressive Difficulty Scaling")]
    public float aggressionRampUpDuration = 120f; // Time in seconds for max difficulty (e.g., 2 minutes)
    public float maxHorizontalSpeedMultiplier = 1.5f; // Max multiplier for horizontal movement speed (e.g., 1.0 to 1.5)
    public float maxPredictiveAimingIncrease = 0.2f; // Max additional chance for predictive aiming (e.7 to 0.9 total)
    public float maxFireRateReductionFactor = 0.5f; // Max reduction for fire rate (e.g., 0.5 means fire rate becomes 50% faster, so 1.5s becomes 0.75s)
    public float maxEvasionSpeedMultiplierIncrease = 0.5f; // Additional multiplier for evasion speed (e.g., 3.0 to 3.5)

    [Header("Evasion (Dodging Player Projectiles)")]
    public float evasionRadius = 10f; // Radius to detect incoming player projectiles
    public float evasionSpeedMultiplier = 3f; // Base multiplier for dodge speed when evading.
    public LayerMask evasionLayers; // Layers containing player projectiles (CRITICAL: MUST BE SET)
    public float evasionPredictionTime = 0.75f; // How far in future to predict threat collision
    public float evasionMinThreatDistance = 2.0f; // Minimum horizontal distance to a threat to trigger evasion
    public float aggressiveDodgeThreshold = 3f; // Threat distance that triggers immediate evasion (more reactive)
    public float dodgeDecisionUpdateRate = 0.1f; // How often the enemy checks for dodge opportunities (lower is more frequent)

    // --- AUDIO SECTION ---
    [Header("Audio Clips")] // New Header for Audio
    public AudioClip hitSound; // Sound played when enemy is hit
    public AudioClip deathSound; // Sound played when enemy is destroyed
    public AudioClip fireSound; // Sound played when enemy fires a projectile
    private AudioSource audioSource; // Reference to the AudioSource component
    // --- END AUDIO SECTION ---

    // Internal state variables
    private Transform playerTransform;
    private Vector3 playerMoveDirection; // Stores player's velocity
    private Vector3 lastPlayerPosition;
    public float playerInputAnalysisInterval = 0.1f; // How often to calculate player's movement direction
    private float nextPlayerAnalysisTime;
    
    private float actualStopZ; // Randomized Z-stop position for this enemy instance
    private float currentDodgeDirection; // -1 for left, 0 for none, 1 for right
    private bool isEvading = false;
    private float nextDodgeDecisionTime; // Timer for next evasion check
    
    private Renderer enemyRenderer;
    private Color originalColor;
    private Color currentHealthTintedColor;      
    private bool isInvincible = false;
    private Coroutine flashCoroutine;
    private MeshExploder meshExploder;       
    private Rigidbody enemyRigidbody;
    private float timeAlive; // Tracks how long this specific enemy instance has been alive

    void Awake()
    {
        // Get components at Awake
        enemyRenderer = GetComponent<Renderer>();
        meshExploder = GetComponent<MeshExploder>();
        enemyRigidbody = GetComponent<Rigidbody>();
        
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("Enemy: AudioSource component not found on " + gameObject.name + ". Sounds will not play.");
        }
    }

    void Start()
    {
        // --- CALCULATE STATS BASED ON DIFFICULTY ---
        int difficultyLevel = (GameManager.Instance != null) ? GameManager.CurrentDifficultyLevel : 0;
        maxHealth = baseMaxHealth + (difficultyLevel * 25);
        burstShotCount = baseBurstShotCount + (difficultyLevel / 2); // Integer division handles the "every 20k" rule

        // Randomize the initial stop Z position for variety
        actualStopZ = stopZ + Random.Range(-stopZVariance, stopZVariance);
        currentHealth = maxHealth;
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
            originalColor.a = 1f;     
            enemyRenderer.material.color = originalColor;
            currentHealthTintedColor = originalColor; // Initialize with original color
        }

        // Set the first burst to be ready almost immediately after spawn.
        nextFireTime = Time.time + 0.1f; 

        // --- ROBUST PLAYER FINDING ---
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            lastPlayerPosition = playerObject.transform.position;
        }
        else
        {
            Debug.LogError("Enemy: Player GameObject with tag 'Player' not found! Please ensure your player has the 'Player' tag. Disabling Enemy script.", this);
            this.enabled = false; 
            return; 
        }
        
        // Ensure Rigidbody is kinematic at start if not handled elsewhere
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = true; // Prevents physics from moving it unless intended by script
            enemyRigidbody.useGravity = false; // No gravity by default
        }

        // Initialize timers
        nextPlayerAnalysisTime = Time.time + playerInputAnalysisInterval;
        nextDodgeDecisionTime = Time.time + dodgeDecisionUpdateRate;
        timeAlive = 0f; 
    }

    void Update()
    {
        timeAlive += Time.deltaTime;
        float aggressionProgress = Mathf.Clamp01(timeAlive / aggressionRampUpDuration); // Ramps from 0 to 1

        // --- ROBUST PLAYER CHECK DURING RUNTIME ---
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                lastPlayerPosition = playerObject.transform.position;
            }
            else
            {
                if (this.enabled) Debug.LogWarning("Enemy: Player lost during gameplay, disabling further actions.");
                this.enabled = false;
                return;
            }
        }

        // --- Handle Player Movement Analysis ---
        if (Time.time >= nextPlayerAnalysisTime)
        {
            AnalyzePlayerMovement();
            nextPlayerAnalysisTime = Time.time + playerInputAnalysisInterval;
        }

        // --- Handle Evasion Logic ---
        if (Time.time >= nextDodgeDecisionTime)
        {
            HandleEvasion(aggressionProgress);
            nextDodgeDecisionTime = Time.time + dodgeDecisionUpdateRate;
        }

        Vector3 currentPosition = transform.position;
        // --- Forward Movement to Stop Z ---
        if (currentPosition.z > actualStopZ)
        {
            currentPosition.z -= forwardSpeed * Time.deltaTime;
            if (currentPosition.z < actualStopZ)
            {
                currentPosition.z = actualStopZ; // Snap to stopZ if passed
            }
        }
        else // Once at Stop Z, handle hovering and firing
        {
            // Hovering (Z-axis bobbing)
            float hoverOffset = Mathf.Sin(Time.time * hoverSpeedZ) * hoverAmplitudeZ;
            float targetHoverZ = actualStopZ + hoverOffset;
            currentPosition.z = Mathf.Lerp(currentPosition.z, targetHoverZ, Time.deltaTime * (forwardSpeed * 0.1f));
            // --- HANDLE BURST FIRING WHEN ALIGNED ---
            HandleAimingAndShooting(aggressionProgress);
        }

        // --- HORIZONTAL MOVEMENT LOGIC (Always Active if Player Exists) ---
        float effectiveHorizontalSpeed = horizontalSpeed * (1f + aggressionProgress * (maxHorizontalSpeedMultiplier - 1f));
        float currentPredictiveAimChance = predictiveAimingStrength + (aggressionProgress * maxPredictiveAimingIncrease);
        currentPredictiveAimChance = Mathf.Clamp01(currentPredictiveAimChance);

        float targetXDirection = 0f; // Default to no horizontal movement

        if (isEvading) // Prioritize evasion
        {
            targetXDirection = currentDodgeDirection;
        }
        else if (playerTransform != null) // If not evading, home on player (or predict)
        {
            Vector3 predictedPlayerPos = playerTransform.position;
            if (Random.value < currentPredictiveAimChance)
            {
                // Predict player position based on current velocity and estimated projectile travel time
                predictedPlayerPos = playerTransform.position + playerMoveDirection * (Vector3.Distance(transform.position, playerTransform.position) / GetProjectileSpeed());
            }

            float differenceX = predictedPlayerPos.x - currentPosition.x;
            // Move to align with the predicted player position, aiming for the 'minAimToleranceX' band
            if (Mathf.Abs(differenceX) > minAimToleranceX) 
            {
                targetXDirection = Mathf.Sign(differenceX);
            } else {
                targetXDirection = 0f; // Stop horizontal movement if within desired aim tolerance
            }
        }
        
        // Smoothly interpolate current horizontal movement direction
        currentDodgeDirection = Mathf.Lerp(currentDodgeDirection, targetXDirection, dodgeSmoothness * 5f * Time.deltaTime);
        // Apply horizontal movement
        float horizontalMovement = currentDodgeDirection * effectiveHorizontalSpeed * Time.deltaTime;
        currentPosition.x += horizontalMovement;

        // --- Avoidance (other enemies/meteors) ---
        float avoidanceForceX = CalculateAvoidanceForce(currentPosition);
        currentPosition.x += avoidanceForceX * separationStrength * Time.deltaTime;

        // Clamp X position within bounds
        currentPosition.x = Mathf.Clamp(currentPosition.x, minX, maxX);
        // --- Add Boundary Reaction: If hitting a boundary, force change direction ---
        if (currentPosition.x <= minX + 0.1f && currentDodgeDirection < 0) 
        {
            currentDodgeDirection = 1f;
        }
        else if (currentPosition.x >= maxX - 0.1f && currentDodgeDirection > 0) 
        {
            currentDodgeDirection = -1f;
        }

        // --- Apply Visual Tilt ---
        ApplyTilt(effectiveHorizontalSpeed);
        // Update enemy position
        transform.position = currentPosition;
    }
    
    // --- Player Movement Analysis ---
    void AnalyzePlayerMovement()
    {
        if (playerTransform != null)
        {
            // Calculate player's current movement direction (velocity)
            playerMoveDirection = (playerTransform.position - lastPlayerPosition) / playerInputAnalysisInterval;
            lastPlayerPosition = playerTransform.position;
        }
    }

    // --- Get Projectile Speed from the prefab to use for prediction ---
    float GetProjectileSpeed()
    {
        if (enemyProjectilePrefab != null)
        {
            EnemyProjectile projectileComponent = enemyProjectilePrefab.GetComponent<EnemyProjectile>();
            if (projectileComponent != null)
            {
                return projectileComponent.speed;
            }
        }
        return 25f; // Default if prefab or component not found
    }

    // --- Handle Evasion (Dodging) ---
    void HandleEvasion(float aggressionProgress)
    {
        isEvading = false;
        if (playerTransform == null) return; 

        float currentEvasionSpeedMultiplier = evasionSpeedMultiplier * (1f + aggressionProgress * maxEvasionSpeedMultiplierIncrease);
        Collider[] threats = Physics.OverlapSphere(transform.position, evasionRadius, evasionLayers);
        Collider closestIncomingThreat = null;
        float minPredictedHorizontalDistance = float.MaxValue;
        foreach (var threatCollider in threats)
        {
            if (threatCollider.gameObject != gameObject && 
                (threatCollider.CompareTag("PlayerProjectile") || threatCollider.CompareTag("Meteor")))
            {
                Rigidbody threatRb = threatCollider.GetComponent<Rigidbody>();
                if (threatRb != null) 
                {
                    Vector3 localThreatVelocity = transform.InverseTransformDirection(threatRb.linearVelocity);
                    if (localThreatVelocity.z < -0.01f) 
                    {
                        float timeToImpactZ = (transform.InverseTransformPoint(threatCollider.transform.position).z - transform.InverseTransformPoint(transform.position).z) / Mathf.Abs(localThreatVelocity.z);
                        if (timeToImpactZ > 0 && timeToImpactZ < evasionPredictionTime) 
                        {
                            Vector3 predictedThreatWorldPos = threatCollider.transform.position + threatRb.linearVelocity * timeToImpactZ;
                            float horizontalDistanceToImpact = Mathf.Abs(predictedThreatWorldPos.x - transform.position.x);

                            if (horizontalDistanceToImpact < evasionMinThreatDistance || horizontalDistanceToImpact < aggressiveDodgeThreshold)
                            {
                                if (horizontalDistanceToImpact < minPredictedHorizontalDistance)
                                {
                                    minPredictedHorizontalDistance = horizontalDistanceToImpact;
                                    closestIncomingThreat = threatCollider;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (closestIncomingThreat != null)
        {
            isEvading = true;
            Rigidbody threatRb = closestIncomingThreat.GetComponent<Rigidbody>();
            
            float timeToImpactZWorld = (closestIncomingThreat.transform.position.z - transform.position.z) / Mathf.Abs(threatRb.linearVelocity.z);
            Vector3 predictedThreatWorldXPos = closestIncomingThreat.transform.position + threatRb.linearVelocity * timeToImpactZWorld;
            float idealDodgeDirection = (predictedThreatWorldXPos.x < transform.position.x) ? 1f : -1f; // Dodge away from threat

            float potentialNewX = transform.position.x + idealDodgeDirection * horizontalSpeed * currentEvasionSpeedMultiplier * Time.deltaTime;
            if (potentialNewX < minX || potentialNewX > maxX)
            {
                idealDodgeDirection *= -1f;
                float reversedPotentialNewX = transform.position.x + idealDodgeDirection * horizontalSpeed * currentEvasionSpeedMultiplier * Time.deltaTime;
                if (reversedPotentialNewX < minX || reversedPotentialNewX > maxX)
                {
                    idealDodgeDirection = 0f;
                }
            }
            
            currentDodgeDirection = idealDodgeDirection;
        }
    }

    // --- Handle Burst Firing Logic ---
    void HandleAimingAndShooting(float aggressionProgress)
    {
        if (playerTransform == null || enemyProjectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Enemy: Cannot fire! Missing player reference, projectile prefab, or fire point.", this);
            return; 
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        float fireRateReduction = aggressionProgress * maxFireRateReductionFactor;
        float actualBurstFireRate = baseBurstFireRate * (1f - fireRateReduction);
        actualBurstFireRate = Mathf.Max(0.5f, actualBurstFireRate); // Ensure a minimum burst rate

        // --- STRICT FIRING CONDITION ---
        // Only fire a burst if within detection range AND it's time AND horizontally aligned.
        if (distanceToPlayer <= playerDetectionRange && Time.time >= nextFireTime) 
        {
            // Calculate predicted player X position for aiming and alignment check
            Vector3 predictedPlayerPosForAim = playerTransform.position + playerMoveDirection * (distanceToPlayer / GetProjectileSpeed());
            float currentXDifference = Mathf.Abs(predictedPlayerPosForAim.x - firePoint.position.x);

            // This is the core of the accuracy requirement: only fire if VERY well aligned
            if (currentXDifference <= minAimToleranceX)
            {
                StartCoroutine(FireBurst(predictedPlayerPosForAim, true)); // True for perfect alignment
                SetNextFireTime(actualBurstFireRate);
            }
            // Allow firing if slightly off but within max tolerance, AND a random chance
            else if (currentXDifference <= maxAimToleranceX && Random.value < (predictiveAimingStrength + aggressionProgress * maxPredictiveAimingIncrease))
            {
                 StartCoroutine(FireBurst(predictedPlayerPosForAim, false)); // False for not perfectly aligned, but good enough
                 SetNextFireTime(actualBurstFireRate);
            }
        }
    }

    // --- Burst Fire Coroutine ---
    IEnumerator FireBurst(Vector3 targetPosition, bool perfectlyAligned)
    {
        for (int i = 0; i < burstShotCount; i++)
        {
            // Determine if this specific shot should be homing
            bool isHomingThisShot = false;
            if (!perfectlyAligned && Random.value < homingShotChance) // Only allow homing shots if not perfectly aligned (adds dynamism)
            {
                isHomingThisShot = true;
            } else if (perfectlyAligned && Random.value < (homingShotChance / 2f)) // Reduce homing chance for perfect alignment
            {
                isHomingThisShot = true;
            }

            FireSingleProjectile(targetPosition, isHomingThisShot);
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
    }

    // --- Fire Single Projectile (Now handles homing, damage, and plays sound) ---
    void FireSingleProjectile(Vector3 targetPosition, bool isHoming)
    {
        if (enemyProjectilePrefab == null || firePoint == null) return;
        // Instantiate the projectile
        GameObject projectileGO = Instantiate(enemyProjectilePrefab, firePoint.position, Quaternion.identity);
        EnemyProjectile enemyProjectile = projectileGO.GetComponent<EnemyProjectile>();

        if (enemyProjectile != null)
        {
            // --- Set damage based on difficulty ---
            int difficultyLevel = (GameManager.Instance != null) ? GameManager.CurrentDifficultyLevel : 0;
            enemyProjectile.damage = baseProjectileDamage + (difficultyLevel * 5);

            // Set homing target if this is a homing shot
            if (isHoming)
            {
                enemyProjectile.SetTarget(playerTransform);
            }
            else
            {
                // Calculate rotation for non-homing shots
                Vector3 directionToTarget = (targetPosition - firePoint.position).normalized;
                projectileGO.transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
        }
        else
        {
            Debug.LogWarning("EnemyProjectile component not found on projectile prefab!", enemyProjectilePrefab);
            // Fallback for non-component projectiles: just face target
            Vector3 directionToTarget = (targetPosition - firePoint.position).normalized;
            projectileGO.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }

        // --- ADDED FOR AUDIO ---
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound); // Play fire sound
        }
        // --- END ADDED FOR AUDIO ---
    }

    // --- Helper to Set Next Burst Fire Time ---
    void SetNextFireTime(float rate)
    {
        float randomInterval = Random.Range(-burstFireRateVariance, burstFireRateVariance);
        nextFireTime = Time.time + rate + randomInterval;
    }

    // --- Calculate Avoidance Force from other objects ---
    float CalculateAvoidanceForce(Vector3 currentPosition)
    {
        float avoidanceForceX = 0f;
        Collider[] hitColliders = Physics.OverlapSphere(currentPosition, separationRadius, avoidanceLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject != gameObject && 
                (hitCollider.CompareTag("Enemy") || hitCollider.CompareTag("Meteor")))
            {
                Vector3 directionToOther = currentPosition - hitCollider.transform.position;
                float distance = directionToOther.magnitude;

                if (distance < separationRadius && distance > 0.001f)
                {
                    float repulsionFactor = (separationRadius - distance) / separationRadius;
                    avoidanceForceX += directionToOther.x * repulsionFactor;
                }
            }
        }
        return avoidanceForceX;
    }

    // --- Apply Visual Tilt ---
    void ApplyTilt(float currentEffectiveHorizontalSpeed)
    {
        Quaternion baseRotation = Quaternion.Euler(0, 180, 0);
        float normalizedVelX = Mathf.Clamp(currentDodgeDirection * currentEffectiveHorizontalSpeed / (horizontalSpeed * maxHorizontalSpeedMultiplier), -1f, 1f);     
        float targetZTilt = -normalizedVelX * tiltAngle;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, targetZTilt);
        Quaternion combinedTargetRotation = baseRotation * tiltRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, combinedTargetRotation, dodgeSmoothness * 10f * Time.deltaTime);
    }

    // --- Health and Damage ---
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerProjectile") && !isInvincible)
        {
            // Get the Projectile component from the collided object
            Projectile playerProjectile = other.GetComponent<Projectile>();
            if (playerProjectile != null)
            {
                // FIX: Now correctly accessing 'damageAmount'
                TakeDamage(playerProjectile.damageAmount);
            }
            else
            {
                // Fallback damage if projectile script not found or damage not set
                TakeDamage(1);
            }
            Destroy(other.gameObject);
        }
    }

    // Changed to public and accepts an int parameter for damage amount
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        // --- ADDED FOR AUDIO ---
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound); // Play hit sound
        }
        // --- END ADDED FOR AUDIO ---

        if (enemyRenderer != null)
        {
            float redTintFactor = 1f - ((float)currentHealth / maxHealth);
            currentHealthTintedColor = Color.Lerp(originalColor, Color.red, redTintFactor * 1.0f);
            enemyRenderer.material.color = currentHealthTintedColor;
        }

        if (currentHealth <= 0)
        {
            ExplodeAndDie(true);
        }
        else
        {
            isInvincible = true;
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashEffect());
        }
    }

    void ExplodeAndDie(bool killedByPlayerProjectile)
    {
        this.enabled = false;
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.useGravity = true;           
        }

        // --- ADDED FOR AUDIO ---
        // Play the death sound *before* the MeshExploder potentially destroys the GameObject
        if (deathSound != null) // No need to check audioSource here, PlayClipAtPoint is static
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        // --- END ADDED FOR AUDIO ---

        if (meshExploder != null)
        {
            meshExploder.EXPLODE();
        }
        else
        {
            // Fallback if MeshExploder is not present or enabled
            Debug.LogWarning("Enemy: MeshExploder component not found. Hiding renderer instead.");
            if (enemyRenderer != null) enemyRenderer.enabled = false;
        }
        
        if (killedByPlayerProjectile && GameManager.Instance != null)
        {
            // Pass the string "EnemyShip" to GameManager.PlayerKilledEnemy
            GameManager.Instance.PlayerKilledEnemy("EnemyShip");
        }
        // The GameManager's PlayerKilledEnemy method now handles both score and build-up.
        // The EnemyDestroyed method is no longer needed for score/build-up.
        // If you still need EnemyDestroyed for other non-score/build-up related logic,
        // you would call it here:
        // else if (!killedByPlayerProjectile && GameManager.Instance != null)
        // {
        //     GameManager.Instance.EnemyDestroyed("EnemyShip");
        // }

        Destroy(gameObject, deathDelay);
    }

    IEnumerator FlashEffect()
    {
        float endTime = Time.time + invincibilityDuration;
        while (Time.time < endTime)
        {
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = Color.red;
                yield return new WaitForSeconds(flashInterval);
                enemyRenderer.material.color = currentHealthTintedColor;
                yield return new WaitForSeconds(flashInterval);
            }
        }
        
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = currentHealthTintedColor;
        }
        isInvincible = false;
        flashCoroutine = null;
    }
}