// CameraShake.cs
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPosition; // To store the camera's initial local position

    // Store the original position when the script starts
    void Start()
    {
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// Initiates a camera shake effect.
    /// </summary>
    /// <param name="duration">How long the camera should shake (in seconds).</param>
    /// <param name="magnitude">How strong the shake should be (amplitude of displacement).</param>
    public IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        // Loop until the shake duration is over
        while (elapsed < duration)
        {
            // Generate random offsets for X and Y, scaled by magnitude
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Apply the offset to the original position
            // We use localPosition because the camera might be parented to another object.
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);

            // Increment elapsed time
            elapsed += Time.deltaTime;

            yield return null; // Wait for the next frame
        }

        // Reset the camera back to its original position after shaking
        transform.localPosition = originalPosition;
    }

    // You might want a public method to start the shake from other scripts easily
    public void StartShake(float duration, float magnitude)
    {
        // Stop any ongoing shake before starting a new one to prevent overlapping
        StopAllCoroutines(); 
        StartCoroutine(Shake(duration, magnitude));
    }
}