// UpgradeSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshProUGUI
using System.Collections.Generic; // For List
using System.Linq; // For OrderBy (used for shuffling)

public class UpgradeSelectionUI : MonoBehaviour
{
    // Assign your three UI Buttons here in the Inspector
    public Button[] upgradeButtons;
    // Assign the TextMeshPro text components for each button in the Inspector
    public TextMeshProUGUI[] upgradeTexts;
    // NEW: Assign your TextMeshProUGUI for the title here
    public TextMeshProUGUI titleText;
    private PlayerController playerController;

    // List of all possible upgrades and their rarities (higher weight = more common)
    private List<(PlayerController.UpgradeType type, int weight)> allUpgrades = new List<(PlayerController.UpgradeType, int)>
    {
        (PlayerController.UpgradeType.IncreasedHP, 10),              // Most common
        (PlayerController.UpgradeType.IncreasedProjectileDamage, 7),
        (PlayerController.UpgradeType.IncreasedProjectileSpeed, 7),
        (PlayerController.UpgradeType.IncreasedFireRate, 5),
        (PlayerController.UpgradeType.IncreasedDifficulty, 4),
        (PlayerController.UpgradeType.IncreasedProjectile, 1)        // Rarest
    };
    /// <summary>
    /// Initializes the UI with a reference to the PlayerController.
    /// </summary>
    /// <param name="controller">The PlayerController instance.</param>
    public void Initialize(PlayerController controller)
    {
        playerController = controller;
    }

    /// <summary>
    /// Displays the randomly selected upgrades on the UI cards.
    /// </summary>
    public void DisplayUpgrades()
    {
        // Basic validation for assigned UI elements
        if (upgradeButtons == null || upgradeButtons.Length < 3 || upgradeTexts == null || upgradeTexts.Length < 3)
        {
            Debug.LogError("UpgradeSelectionUI: Not enough upgrade buttons or text fields assigned! Please assign 3 buttons and 3 text elements.");
            return;
        }

        // NEW: Validate titleText
        if (titleText == null)
        {
            Debug.LogError("UpgradeSelectionUI: Title Text (TextMeshProUGUI) not assigned! Title will not be displayed.");
        }

        // Select 3 random upgrades based on rarity, allowing for duplicates
        List<PlayerController.UpgradeType> chosenUpgrades = SelectRandomUpgrades(3);
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (i < chosenUpgrades.Count)
            {
                PlayerController.UpgradeType upgrade = chosenUpgrades[i];
                upgradeButtons[i].gameObject.SetActive(true); // Make sure the button is visible
                upgradeTexts[i].text = GetUpgradeDescription(upgrade);
                // Set text

                // Clear previous listeners to avoid multiple calls if the panel is reused
                upgradeButtons[i].onClick.RemoveAllListeners();
                // Add a new listener for this specific upgrade.
                // Using a lambda expression to capture the 'upgrade' value for the button.
                upgradeButtons[i].onClick.AddListener(() => OnUpgradeChosen(upgrade));
            }
            else
            {
                // In case we ever choose less than 3, hide extra buttons
                upgradeButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Sets the title text of the upgrade selection UI.
    /// </summary>
    /// <param name="isScoreTriggered">True if triggered by score threshold, false if by build-up.</param>
    public void SetTitleText(bool isScoreTriggered)
    {
        if (titleText != null)
        {
            if (isScoreTriggered)
            {
                titleText.text = "SCORE! SELECT YOUR POWER UP:";
            }
            else
            {
                titleText.text = "LEVELED UP! SELECT YOUR POWER UP:";
            }
        }
    }

    /// <summary>
    /// Selects a specified number of random upgrades based on their defined weights.
    /// Allows for duplicate upgrades in the selection.
    /// </summary>
    /// <param name="count">The number of upgrades to select.</param>
    /// <returns>A list of chosen UpgradeType enums.</returns>
    private List<PlayerController.UpgradeType> SelectRandomUpgrades(int count)
    {
        List<PlayerController.UpgradeType> selected = new List<PlayerController.UpgradeType>();
        // Create a temporary list where each upgrade type appears 'weight' times.
        List<PlayerController.UpgradeType> weightedUpgradesPool = new List<PlayerController.UpgradeType>();
        foreach (var upgradeTuple in allUpgrades)
        {
            for (int i = 0; i < upgradeTuple.weight; i++)
            {
                weightedUpgradesPool.Add(upgradeTuple.type);
            }
        }

        // Shuffle the weighted pool to ensure randomness in selection order.
        // Using LINQ's OrderBy with a random value is a common way to shuffle a list.
        weightedUpgradesPool = weightedUpgradesPool.OrderBy(a => Random.value).ToList();

        // Pick 'count' upgrades from the shuffled pool.
        for (int i = 0; i < count; i++)
        {
            if (weightedUpgradesPool.Count > 0)
            {
                // Select a random index from the current weighted pool
                int randomIndex = Random.Range(0, weightedUpgradesPool.Count);
                selected.Add(weightedUpgradesPool[randomIndex]);
                // We do not remove from the pool, allowing the same upgrade to be picked multiple times.
            }
            else
            {
                // Fallback: If for some reason the weighted pool becomes empty,
                // pick a completely random upgrade.
                Debug.LogWarning("Weighted upgrade pool is empty! Falling back to basic random selection.");
                PlayerController.UpgradeType fallbackUpgrade = 
                    (PlayerController.UpgradeType)Random.Range(0, System.Enum.GetValues(typeof(PlayerController.UpgradeType)).Length);
                selected.Add(fallbackUpgrade);
            }
        }

        return selected;
    }

    /// <summary>
    /// Returns a descriptive string for a given upgrade type.
    /// </summary>
    /// <param name="type">The UpgradeType enum.</param>
    /// <returns>A formatted string description.</returns>
    private string GetUpgradeDescription(PlayerController.UpgradeType type)
    {
        string description = "";
        switch (type)
        {
            case PlayerController.UpgradeType.IncreasedProjectileDamage:
                description = "+ Projectile Damage\n(+1 Damage)";
                break;
            case PlayerController.UpgradeType.IncreasedProjectileSpeed:
                description = "+ Projectile Speed\n(+5% Speed)";
                break;
            case PlayerController.UpgradeType.IncreasedHP:
                description = "+ Extra HP\n(+10 Health)";
                break;
            case PlayerController.UpgradeType.IncreasedFireRate:
                description = "+ Fire Rate\n(Faster Firing)";
                break;
            case PlayerController.UpgradeType.IncreasedProjectile:
                description = "Extra Projectile\n(Fires an additional projectile)";
                break;
            case PlayerController.UpgradeType.IncreasedDifficulty:
                description = "Stronger Enemies\n(Increases Difficulty Level)";
                break;
            default:
                description = "Unknown Upgrade";
                break;
        }
        // NEW: Wrap the description in bold tags for TextMeshPro
        return "<b>" + description + "</b>";
    }

    /// <summary>
    /// Callback method when an upgrade button is clicked.
    /// </summary>
    /// <param name="chosenUpgrade">The UpgradeType chosen by the player.</param>
    private void OnUpgradeChosen(PlayerController.UpgradeType chosenUpgrade)
    {
        if (playerController != null)
        {
            playerController.ApplyUpgrade(chosenUpgrade);
            // Notify PlayerController of the choice
        }
        else
        {
            Debug.LogError("PlayerController reference is null in UpgradeSelectionUI! Cannot apply upgrade.");
            // If playerController is null, manually unpause the game as a fallback
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }
            Destroy(gameObject);
            // Destroy the UI panel
        }
    }
}