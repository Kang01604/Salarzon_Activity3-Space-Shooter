// Meteor.cs
using UnityEngine;
using System.Collections;
using SBS.ME; // Import MeshExploder namespace

public class Meteor : MonoBehaviour
{
    public enum MeteorSize { Small, Medium, Big }

    [Header("Meteor Properties")]
    public MeteorSize currentSize; // This will be set by the spawner/splitting logic
    public float speed = 20f; // Base speed, will be multiplied by spawner and difficulty
    public float rotationSpeed = 50f; // Degrees per second
    public int maxHealth = 1; // ALL METEORS WILL HAVE 1 HP NOW

    [Header("Explosion & Spawning")]
    // Assign these in the Inspector on the respective Meteor Prefabs!
    public GameObject smallMeteorPrefab;
    public GameObject mediumMeteorPrefab;
    public MeshExploder meshExploder; // Reference to the MeshExploder component

    [Header("Healing Drop")]
    public GameObject healingCubePrefab; // Assign your HealingCube Prefab here
    [Range(0f, 1f)]
    public float healingDropChance = 0.2f; // 20% chance to drop a healing cube

    [Header("Build-Up & Score Values")] // NEW: Build-up and Score values for meteors
    public int smallMeteorBuildUp = 5;  // Adjusted
    public int mediumMeteorBuildUp = 10; // Adjusted
    public int bigMeteorBuildUp = 15;    // Adjusted
    public int smallMeteorScore = 50;    // Adjusted
    public int mediumMeteorScore = 100;  // Adjusted
    public int bigMeteorScore = 250;     // Adjusted

    [Header("Despawn Settings")]
    public float despawnOffsetFromPlayer = 10f; // Distance behind player before despawn timer starts
    public float despawnDelay = 5f; // Additional seconds before deletion after passing despawn line
    public float fixedDespawnZPosition = -5f; // Despawn Z if no player, before delay
    public float visualMeteorDespawnZ = -30f; // NEW: Specific Z-position for visual meteors to despawn

    // --- ADDED FOR VISUAL-ONLY METEORS ---
    [Header("Visual Settings")]
    [Tooltip("If true, this meteor is for visual purposes only and will not affect gameplay (score, build-up, healing drops).")]
    public bool isVisualOnly = false; // Set this in the Inspector for your visual meteors
    // --- END ADDED FOR VISUAL-ONLY METEORS ---

    // --- AUDIO SECTION ---
    [Header("Audio Clips")] // New Header for Audio
    public AudioClip hitSound;   // Sound played when meteor is hit
    public AudioClip explodeSound; // Sound played when meteor explodes
    private AudioSource audioSource; // Reference to the AudioSource component
    // --- END AUDIO SECTION ---

    // Private variables
    private int currentHealth;
    private Transform playerTransform; // Added for robust despawning
    private Renderer meteorRenderer;
    private bool hasStartedDespawnTimer = false; // Flag to track if despawn timer has been initiated


    void Awake() // Changed from Start to Awake to ensure audioSource is ready before Start
    {
        meshExploder = GetComponent<MeshExploder>();
        meteorRenderer = GetComponent<Renderer>();
        
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("Meteor: AudioSource component not found on " + gameObject.name + ". Hit/Explosion sounds will not play.");
        }
    }

    void Start()
    {
        SetMeteorProperties();
        
        // Only try to find player if this is NOT a visual-only meteor
        if (!isVisualOnly)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
        else // For visual only meteors, ensure they are destroyed if they go too far
        {
            // Removed DestroyAfterTime(30f) as we now have a specific Z despawn for visual meteors
        }
    }

    void Update()
    {
        // Only move/rotate if not already exploding or exploded by MeshExploder
        if (meshExploder == null || !meshExploder.explodeNOW)
        {
            transform.position -= new Vector3(0, 0, speed * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, rotationSpeed * 0.5f * Time.deltaTime, Space.Self);
        }
        
        // NEW: Despawn visual meteors at a specific Z position
        if (isVisualOnly)
        {
            if (transform.position.z <= visualMeteorDespawnZ)
            {
                ExplodeMeteor(false); // Explode without gameplay effects
                return; // Exit update to prevent further processing
            }
        }
        else // Existing despawn logic for non-visual meteors
        {
            float currentDespawnZ;
            if (playerTransform != null)
            {
                currentDespawnZ = playerTransform.position.z - despawnOffsetFromPlayer;
            }
            else
            {
                currentDespawnZ = fixedDespawnZPosition;
            }

            if (transform.position.z < currentDespawnZ && !hasStartedDespawnTimer)
            {
                hasStartedDespawnTimer = true;
                StartCoroutine(DelayedDespawn());
            }
        }
    }

    void SetMeteorProperties()
    {
        maxHealth = 1;
        currentHealth = maxHealth;
    }

    public void Initialize(float baseSpeed, MeteorSize assignedSize)        
    {
        // Only get difficulty level if this is NOT a visual-only meteor
        if (!isVisualOnly)
        {
            // Get the current difficulty level from the GameManager
            int difficultyLevel = (GameManager.Instance != null) ? GameManager.CurrentDifficultyLevel : 0;

            // Calculate the speed multiplier: +2% for each difficulty level
            float speedMultiplier = 1.0f + (difficultyLevel * 0.02f);

            // Apply the difficulty-scaled speed
            this.speed = baseSpeed * speedMultiplier;
        }
        else
        {
            // For visual meteors, baseSpeed is the actual speed, no difficulty scaling
            this.speed = baseSpeed;
        }

        this.currentSize = assignedSize;
        SetMeteorProperties();
    }

    // Called when the mouse button is clicked over the collider
    void OnMouseDown()
    {
        // Only explode if this is a visual-only meteor and not already exploding
        if (isVisualOnly && !meshExploder.explodeNOW)
        {
            ExplodeMeteor(true); // Treat as if clicked (killed) to trigger explosion effect
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // If this is a visual-only meteor, it shouldn't interact with gameplay projectiles
        if (isVisualOnly) return; 

        if (other.CompareTag("PlayerProjectile"))
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

    void TakeDamage(int damage)
    {
        // If this is a visual-only meteor, it shouldn't take damage from projectiles
        if (isVisualOnly) return; 

        currentHealth -= damage;

        // --- ADDED FOR AUDIO ---
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound); // Play hit sound
        }
        // --- END ADDED FOR AUDIO ---

        if (currentHealth <= 0)
        {
            ExplodeMeteor(true); // Meteor was killed by a player projectile
        }
    }

    // Modified to accept a boolean indicating if it was killed by a player projectile
    public void ExplodeMeteor(bool killedByPlayerProjectile)
    {
        this.enabled = false;        

        if (hasStartedDespawnTimer)
        {
            StopAllCoroutines();        
        }

        Collider meteorCollider = GetComponent<Collider>();
        if (meteorCollider != null)
        {
            meteorCollider.enabled = false;
        }
        
        // --- ADDED FOR AUDIO ---
        // Play the explosion sound *before* the MeshExploder potentially destroys the GameObject
        if (explodeSound != null) 
        {
            AudioSource.PlayClipAtPoint(explodeSound, transform.position);
        }
        // --- END ADDED FOR AUDIO ---

        if (meshExploder != null)
        {
            meshExploder.EXPLODE();
        }
        else
        {
            Debug.LogWarning("MeshExploder component not found on meteor. Destroying manually.");
            Destroy(gameObject);
        }

        // --- MODIFIED: ONLY do gameplay effects if NOT a visual-only meteor ---
        if (!isVisualOnly && killedByPlayerProjectile)
        {
            // Try to drop healing cube
            AttemptDropHealingCube();

            if (GameManager.Instance != null)
            {
                // Pass the string representation of the meteor type to GameManager
                GameManager.Instance.PlayerKilledEnemy(currentSize.ToString() + "Meteor");
            }
            else
            {
                Debug.LogWarning("GameManager.Instance not found when notifying player-killed meteor.");
            }
        }
        // If it was not killed by a player projectile (e.g., despawn, collided with player),
        // it does not contribute to the build-up meter or score based on the new mechanics.
        else if (!isVisualOnly) 
        {
            //Debug.Log("Meteor destroyed, but not by player projectile (e.g., despawn or collision with player). No build-up or score added.");
        }

        // --- Allow splitting for both visual and non-visual meteors ---
        if (currentSize == MeteorSize.Big)
        {
            if (mediumMeteorPrefab != null)
            {
                SpawnSplitMeteors(mediumMeteorPrefab, MeteorSize.Medium, 2);          
            }
            else
            {
                Debug.LogError("Medium Meteor Prefab is not assigned on Big Meteor! Splitting aborted.");
            }
        }
        else if (currentSize == MeteorSize.Medium)
        {
            if (smallMeteorPrefab != null)
            {
                SpawnSplitMeteors(smallMeteorPrefab, MeteorSize.Small, 2);
            }
            else
            {
                Debug.LogError("Small Meteor Prefab is not assigned on Medium Meteor! Splitting aborted.");
            }
        }
    }

    void SpawnSplitMeteors(GameObject meteorPrefabToSpawn, MeteorSize newSize, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (meteorPrefabToSpawn != null)
            {
                Vector3 spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
                GameObject newMeteorGO = Instantiate(meteorPrefabToSpawn, transform.position + spawnOffset, Quaternion.identity);
                Meteor newMeteor = newMeteorGO.GetComponent<Meteor>();
                if (newMeteor != null)
                {
                    // Pass down the visual-only flag to the spawned smaller meteors
                    newMeteor.isVisualOnly = this.isVisualOnly; 
                    newMeteor.Initialize(this.speed, newSize); // Pass current speed down
                    
                    Rigidbody newMeteorRb = newMeteorGO.GetComponent<Rigidbody>();
                    if (newMeteorRb != null)
                    {
                        newMeteorRb.isKinematic = false;
                        newMeteorRb.useGravity = false;

                        Vector3 explosionForceDirection = (newMeteorGO.transform.position - transform.position).normalized;
                        explosionForceDirection += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1.5f), Random.Range(-0.5f, 0.5f));
                        newMeteorRb.linearVelocity = explosionForceDirection.normalized * (this.speed * 0.5f);
                    }
                }
            }
        }
    }

    void AttemptDropHealingCube()
    {
        if (healingCubePrefab != null && Random.value <= healingDropChance)
        {
            Instantiate(healingCubePrefab, transform.position, Quaternion.identity);
            Debug.Log("Meteor: Dropped a healing cube!");
        }
    }

    IEnumerator DelayedDespawn()
    {
        yield return new WaitForSeconds(despawnDelay);
        // On despawn, the meteor is not killed by a player projectile, so pass 'false'.
        ExplodeMeteor(false);
    }
}