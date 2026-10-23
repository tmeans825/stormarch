using UnityEngine;
using TMPro;
using PixelCrushers;

// One ability row in the alignment panel (Ability 1/2/3 under Abilities Grouper).
// Shows name + either the description (unlocked) or the lock-hint (locked), and swaps
// the arrow / lock icon. Localized text follows the existing LocalizeUI fieldName pattern.
public class AlignmentAbilityRow : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public GameObject arrowIcon; // shown when unlocked
    public GameObject lockIcon;  // shown when locked
    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public void bind(AlignmentAbility ability, bool unlocked)
    {
        if (ability == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        setLocalized(nameText, ability.nameKey);
        setLocalized(descText, unlocked ? ability.descKey : ability.lockHintKey);

        if (arrowIcon != null) arrowIcon.SetActive(unlocked);
        if (lockIcon != null) lockIcon.SetActive(!unlocked);

        if (nameText != null) nameText.color = unlocked ? unlockedColor : lockedColor;
        if (descText != null) descText.color = unlocked ? unlockedColor : lockedColor;
    }

    private void setLocalized(TextMeshProUGUI text, string key)
    {
        if (text == null)
            return;

        LocalizeUI loc = text.GetComponent<LocalizeUI>();
        if (loc != null)
        {
            loc.fieldName = key;
            loc.UpdateText();
        }
        else
        {
            text.text = key;
        }
    }
}
