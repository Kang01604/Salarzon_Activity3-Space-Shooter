// UIFollowTarget.cs
using UnityEngine;

public class UIFollowTarget : MonoBehaviour
{
    public Transform target; // Assign the player's transform in the Inspector
    public Vector2 screenOffset; // Adjust in Inspector for desired position relative to player

    private RectTransform rectTransform;
    private Camera mainCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main; // Cache main camera
        if (mainCamera == null)
        {
            Debug.LogError("UIFollowTarget: Main Camera not found! Please ensure your camera is tagged 'MainCamera'.");
            this.enabled = false;
        }
        if (target == null)
        {
            Debug.LogWarning("UIFollowTarget: Target Transform not assigned. This script will not function.");
            this.enabled = false;
        }
    }

    void LateUpdate() // Use LateUpdate to ensure target has moved for this frame
    {
        if (target == null || mainCamera == null || rectTransform == null)
        {
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
        
        // Apply offset relative to the screen position
        rectTransform.position = new Vector3(screenPos.x + screenOffset.x, screenPos.y + screenOffset.y, screenPos.z);

        // Optional: If you want it to hide when the player is off-screen
        // if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
        // {
        //     rectTransform.gameObject.SetActive(false);
        // }
        // else
        // {
        //     rectTransform.gameObject.SetActive(true);
        // }
    }
}