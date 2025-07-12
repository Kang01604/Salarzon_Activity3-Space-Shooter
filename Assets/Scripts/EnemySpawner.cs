// EnemySpawner.cs
using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings - Enemy Ship")]
    public GameObject[] enemyPrefabs;       // An array of your enemy prefabs, tagged "Enemy"
    public GameObject mothershipPrefab;     // The Mothership prefab, tagged "Enemy"
    public float enemySpawnZ = 400f;        // Z-distance in front of player
    public float enemyMinX = -12f, enemyMaxX = 12f;
    public float enemySpawnInterval = 5f; // Check interval. Spawning only happens under certain conditions.
    public int maxEnemyShips = 4;           // Cap on concurrent enemy ships
    public float enemySpawnDelay = 10f;     // Delay before any enemies start spawning

    [Header("Enemy Ship Platoon")]
    [Tooltip("Horizontal spacing between ships in a platoon.")]
    public float platoonSpacingX = 2f;

    // Internal state for the new spawn event system
    private enum SpawnState { Idle, WaveActive }
    private enum NextWaveType { None, Platoon, Mothership }
    private SpawnState currentState = SpawnState.Idle;
    private NextWaveType nextGuaranteedWave = NextWaveType.None;

    [Header("Spawn Settings - Meteor")]
    public GameObject smallMeteorPrefab;
    public GameObject mediumMeteorPrefab;
    public GameObject bigMeteorPrefab;
    public float meteorSpawnZ = 400f;
    public float meteorMinX = -12f, meteorMaxX = 12f;
    public float meteorSpawnInterval = 10f;
    public int maxMeteors = 10;
    public float meteorBaseSpeed = 20f;

    void Start()
    {
        // Subscribe to the wave cleared event from the GameManager
        GameManager.OnWaveCleared += HandleWaveCleared;

        StartCoroutine(SpawnEnemyShipLoop());
        StartCoroutine(SpawnMeteorLoop());
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameManager.OnWaveCleared -= HandleWaveCleared;
    }

    // This method is called by the GameManager event when a wave is cleared
    void HandleWaveCleared()
    {
        currentState = SpawnState.Idle;
    }

    IEnumerator SpawnEnemyShipLoop()
    {
        yield return new WaitForSeconds(enemySpawnDelay);
        Quaternion rotationToFacePlayer = Quaternion.Euler(0, 180, 0);

        while (true)
        {
            // If a special wave is active, the spawner's only job is to wait.
            if (currentState == SpawnState.WaveActive)
            {
                yield return new WaitForSeconds(enemySpawnInterval);
                continue;
            }

            int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            // If the field is clear, it's time to spawn a new wave or event.
            if (currentEnemyCount == 0)
            {
                // First, check if there's a guaranteed wave to spawn
                if (nextGuaranteedWave == NextWaveType.Mothership)
                {
                    SpawnMothership(rotationToFacePlayer);
                }
                else if (nextGuaranteedWave == NextWaveType.Platoon)
                {
                    SpawnPlatoon(rotationToFacePlayer);
                }
                else // If no guarantee, roll the dice for a random event
                {
                    float rand = Random.Range(0f, 100f);

                    // Check if score is over 10,000 before considering Mothership spawn
                    if (GameManager.Instance.score >= 10000 && rand < 5f) // 5% chance for Mothership, only if score >= 10k
                    {
                        SpawnMothership(rotationToFacePlayer);
                    }
                    else if (rand < 25f) // 20% chance for Platoon (5 to 24.99)
                    {
                        SpawnPlatoon(rotationToFacePlayer);
                    }
                    else // 75% chance for a normal single enemy (or 80% if Mothership didn't spawn due to score)
                    {
                        SpawnSingleEnemy(rotationToFacePlayer);
                    }
                }
            }
            // If the field is NOT clear, but no special wave is active, spawn normal enemies.
            else if (currentEnemyCount < maxEnemyShips)
            {
                SpawnSingleEnemy(rotationToFacePlayer);
            }

            yield return new WaitForSeconds(enemySpawnInterval);
        }
    }

    // Helper method to get a random enemy prefab from the array
    GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("Enemy Prefabs array is not assigned or is empty in the Spawner!");
            return null;
        }
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        return enemyPrefabs[randomIndex];
    }

    void SpawnSingleEnemy(Quaternion rotation)
    {
        GameObject prefab = GetRandomEnemyPrefab();
        if (prefab == null) return;

        float x = Random.Range(enemyMinX, enemyMaxX);
        Vector3 pos = new Vector3(x, 0f, enemySpawnZ);
        Instantiate(prefab, pos, rotation);
        Debug.Log("Spawned a single normal enemy.");
    }

    void SpawnPlatoon(Quaternion rotation)
    {
        float effectiveMinX = enemyMinX + platoonSpacingX;
        float effectiveMaxX = enemyMaxX - platoonSpacingX;
        float centerX = Random.Range(effectiveMinX, effectiveMaxX);

        if (effectiveMinX > effectiveMaxX)
        {
            centerX = (enemyMinX + enemyMaxX) / 2f;
        }

        for (int i = -1; i <= 1; i++)
        {
            GameObject prefab = GetRandomEnemyPrefab();
            if (prefab != null)
            {
                Vector3 pos = new Vector3(centerX + (i * platoonSpacingX), 0f, enemySpawnZ);
                Instantiate(prefab, pos, rotation);
            }
        }
        
        // Register this wave with the GameManager
        GameManager.Instance.RegisterSpecialWave(3);
        currentState = SpawnState.WaveActive;
        // Only guarantee a mothership if the score is already high enough
        if (GameManager.Instance.score >= 10000)
        {
            nextGuaranteedWave = NextWaveType.Mothership; // Guarantee a mothership next
            Debug.Log("Spawned a Platoon. Waiting for wave clear. Next guaranteed wave: Mothership.");
        }
        else
        {
            nextGuaranteedWave = NextWaveType.None; // No guaranteed mothership yet
            Debug.Log("Spawned a Platoon. Waiting for wave clear. Score too low for Mothership guarantee.");
        }
    }

    void SpawnMothership(Quaternion rotation)
    {
        if (mothershipPrefab == null)
        {
            Debug.LogError("Mothership Prefab is not assigned in the Spawner!");
            nextGuaranteedWave = NextWaveType.None; // Reset guarantee if prefab is missing
            return;
        }

        // Add the score check here as well for guaranteed spawns
        if (GameManager.Instance.score < 10000)
        {
            Debug.Log("Cannot spawn Mothership yet, score is below 10,000.");
            // If a Mothership was guaranteed but score isn't met,
            // we should not try to spawn it and reset the guaranteed wave.
            nextGuaranteedWave = NextWaveType.None; 
            SpawnSingleEnemy(rotation); // Spawn a normal enemy instead for now
            return;
        }

        float x = Random.Range(enemyMinX, enemyMaxX);
        Vector3 pos = new Vector3(x, 0f, enemySpawnZ);
        Instantiate(mothershipPrefab, pos, rotation);

        // Register this wave with the GameManager
        GameManager.Instance.RegisterSpecialWave(1);
        currentState = SpawnState.WaveActive;
        nextGuaranteedWave = NextWaveType.Platoon; // Guarantee a platoon next
        Debug.Log("Spawned a Mothership. Waiting for wave clear. Next guaranteed wave: Platoon.");
    }


    IEnumerator SpawnMeteorLoop()
    {
        while (true)
        {
            int count = GameObject.FindGameObjectsWithTag("Meteor").Length;
            if (count < maxMeteors)
            {
                float x = Random.Range(meteorMinX, meteorMaxX);
                Vector3 pos = new Vector3(x, 0f, meteorSpawnZ);
                Meteor.MeteorSize chosenSize = GetRandomMeteorSizeByPercentage();
                GameObject meteorPrefabToSpawn = GetMeteorPrefabBySize(chosenSize);

                if (meteorPrefabToSpawn != null)
                {
                    GameObject newMeteorGO = Instantiate(meteorPrefabToSpawn, pos, Quaternion.identity);
                    Meteor newMeteor = newMeteorGO.GetComponent<Meteor>();
                    if (newMeteor != null)
                    {
                        newMeteor.Initialize(meteorBaseSpeed, chosenSize);
                    }
                }
            }
            yield return new WaitForSeconds(meteorSpawnInterval);
        }
    }

    private GameObject GetMeteorPrefabBySize(Meteor.MeteorSize size)
    {
        switch (size)
        {
            case Meteor.MeteorSize.Small: return smallMeteorPrefab;
            case Meteor.MeteorSize.Medium: return mediumMeteorPrefab;
            case Meteor.MeteorSize.Big: return bigMeteorPrefab;
            default: return null;
        }
    }

    private Meteor.MeteorSize GetRandomMeteorSizeByPercentage()
    {
        float rand = Random.Range(0f, 100f);
        if (rand < 70f) return Meteor.MeteorSize.Small;
        if (rand < 95f) return Meteor.MeteorSize.Medium;
        return Meteor.MeteorSize.Big;
    }
}