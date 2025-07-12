using UnityEngine;

public class HealingCube : MonoBehaviour
{
    [Header("Healing Cube Properties")]
    public int healAmount = 25; // Amount of HP to restore
    public float homingSpeed = 5f; // Speed at which the cube homes to the player

    private Transform playerTransform;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("HealingCube: Player not found. Healing cube will not home.");
            // Optionally, destroy the cube if no player is found to prevent it from floating aimlessly.
            Destroy(gameObject, 5f); // Destroy after 5 seconds if no player
        }

        // Make sure the healing cube doesn't collide with other physics objects
        // by setting its Rigidbody to kinematic or ensuring it has no Rigidbody.
        // If it has a Collider, make it a trigger.
        Collider cubeCollider = GetComponent<Collider>();
        if (cubeCollider != null)
        {
            cubeCollider.isTrigger = true;
        }
        Rigidbody cubeRigidbody = GetComponent<Rigidbody>();
        if (cubeRigidbody != null)
        {
            cubeRigidbody.isKinematic = true; // Ensure it doesn't get pushed around
            cubeRigidbody.useGravity = false; // No gravity
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            // Move towards the player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            transform.position += directionToPlayer * homingSpeed * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Heal(healAmount); // Call the new Heal method on the PlayerController
                Destroy(gameObject); // Destroy the healing cube after it's collected
            }
        }
    }
}