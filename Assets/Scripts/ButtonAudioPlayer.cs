// ButtonAudioPlayer.cs
using UnityEngine;
using UnityEngine.UI; // Required for Button component
using UnityEngine.EventSystems; // Required for pointer events if using non-UI Toolkit buttons

[RequireComponent(typeof(Button))] // Ensures this script is on an object with a Button component
[RequireComponent(typeof(AudioSource))] // Ensures this script is on an object with an AudioSource component
public class ButtonAudioPlayer : MonoBehaviour
{
    [Tooltip("The AudioSource component on THIS GameObject that will play the sound.")]
    private AudioSource audioSource; // Will be automatically assigned if component exists

    [Tooltip("The AudioClip to play when this button is clicked.")]
    public AudioClip clickSound; // Assign your specific sound for this button here

    void Awake()
    {
        // Get the AudioSource component on this GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError($"ButtonAudioPlayer on {gameObject.name}: No AudioSource component found. Please add one to this GameObject for sound playback.", this);
            enabled = false; // Disable script if no AudioSource is found
            return;
        }

        // Set AudioSource defaults for UI button sound
        audioSource.playOnAwake = false; // Do not play sound when scene starts
        audioSource.loop = false;       // Do not loop the sound
        audioSource.spatialBlend = 0;   // Ensure it's 2D sound for UI

        // Get the Button component on this GameObject
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"ButtonAudioPlayer on {gameObject.name}: No Button component found. This script requires a Button.", this);
            enabled = false; // Disable script if no Button is found
            return;
        }

        // Add a listener to the button's onClick event to play the sound
        button.onClick.AddListener(PlayButtonClickSound);
    }

    /// <summary>
    /// Plays the assigned clickSound using this GameObject's AudioSource.
    /// This method is automatically called when the button is clicked.
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
            // Debug.Log($"Played sound '{clickSound.name}' from button '{gameObject.name}'.", this); // Uncomment for debugging
        }
        else if (audioSource == null)
        {
            Debug.LogWarning($"ButtonAudioPlayer on {gameObject.name}: AudioSource is missing or null. Cannot play sound.", this);
        }
        else if (clickSound == null)
        {
            Debug.LogWarning($"ButtonAudioPlayer on {gameObject.name}: No AudioClip assigned for clickSound. Cannot play.", this);
        }
    }
}