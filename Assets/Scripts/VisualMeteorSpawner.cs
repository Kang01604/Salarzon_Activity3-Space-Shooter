// VisualMeteorSpawner.cs
using UnityEngine;
using System.Collections; // Required for Coroutines

public class VisualMeteorSpawner : MonoBehaviour
{
    [Header("Visual Meteor Prefabs")]
    [Tooltip("Drag your prepared visual meteor prefabs here (e.g., duplicates of your Small, Medium, Big meteors with 'Is Visual Only' checked).")]
    public GameObject[] visualMeteorPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Minimum time between meteor spawns.")]
    public float minSpawnInterval = 1.0f;
    [Tooltip("Maximum time between meteor spawns.")]
    public float maxSpawnInterval = 3.0f;
    [Tooltip("How fast the visual meteors should move. This overrides Meteor's own speed for visual meteors.")]
    public float visualMeteorSpeed = 10f;
    [Tooltip("How far from the camera to spawn meteors (Z-axis).")]
    public float spawnZDistance = 50f;
    [Tooltip("Max random X and Y offset from the center for spawning.")]
    public float spawnRadius = 20f;
    [Tooltip("Where meteors should despawn (Z-axis) after flying past.")]
    public float despawnZPosition = -5f; // Meteors below this Z-position will be destroyed

    void Start()
    {
        if (visualMeteorPrefabs == null || visualMeteorPrefabs.Length == 0)
        {
            Debug.LogError("VisualMeteorSpawner: No visual meteor prefabs assigned! Please assign them in the Inspector.");
            enabled = false;
            return;
        }

        StartCoroutine(SpawnVisualMeteorsRoutine());
    }

    IEnumerator SpawnVisualMeteorsRoutine()
    {
        while (true) // Loop indefinitely
        {
            float spawnDelay = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(spawnDelay);

            SpawnRandomVisualMeteor();
        }
    }

    void SpawnRandomVisualMeteor()
    {
        if (visualMeteorPrefabs == null || visualMeteorPrefabs.Length == 0) return;

        // Choose a random meteor prefab from the array
        GameObject meteorPrefabToSpawn = visualMeteorPrefabs[Random.Range(0, visualMeteorPrefabs.Length)];

        // Calculate a random spawn position
        Vector3 spawnPosition = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(-spawnRadius, spawnRadius),
            spawnZDistance
        );

        // Instantiate the meteor
        GameObject newMeteorGO = Instantiate(meteorPrefabToSpawn, spawnPosition, Random.rotation);
        
        // Get the Meteor script component
        Meteor newMeteor = newMeteorGO.GetComponent<Meteor>();
        if (newMeteor != null)
        {
            // Crucially set this meteor to be visual-only
            newMeteor.isVisualOnly = true;
            // Initialize with the visual-specific speed
            newMeteor.Initialize(visualMeteorSpeed, newMeteor.currentSize); // currentSize can be anything here, it's just for init method
            
            // Override despawn settings for visual meteors to use a fixed Z
            newMeteor.despawnOffsetFromPlayer = 0; // Not relevant for visual
            newMeteor.fixedDespawnZPosition = despawnZPosition;
            newMeteor.despawnDelay = 1f; // Short delay after passing fixed Z
        }
        else
        {
            Debug.LogWarning($"VisualMeteorSpawner: Instantiated object {meteorPrefabToSpawn.name} does not have a Meteor component. It will not behave as expected.");
        }
    }
}