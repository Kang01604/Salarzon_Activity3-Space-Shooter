// EnemyProjectile.cs
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Base Settings")]
    public float speed = 15f;
    public float lifeTime = 5f;
    public int damage = 10; // Damage this projectile will deal

    [Header("Homing Settings")]
    public bool canHome = false; // Set to true if this projectile type can home
    public float turnSpeed = 2f;
    private Transform target; 
    private bool isHomingShot = false; // Internal flag for homing

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // SetTarget method re-enabled for homing projectiles
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isHomingShot = true; // Activate homing behavior for this instance
        // Ensure projectile initially faces target if homing
        if (target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    void Update()
    {
        if (isHomingShot && target != null && canHome) // Only home if flag is set AND canHome is true
        {
            // Calculate direction to target
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            // Rotate towards the target smoothly
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }
        
        // Always move forward based on current rotation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}