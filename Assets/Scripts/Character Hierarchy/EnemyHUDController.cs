using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/**
    DESCRIPTION: Manages health and shield bar UI for enemy characters.
    Extracted from Enemy.cs — Enemy should call Initialize() in Start(),
    then OnDamaged() / OnRecharging() / SetVisible() as needed.
**/

public class EnemyHUDController : MonoBehaviour
{
    [Header("HUD: References")]
    public GameObject healthBarContainers;
    public GameObject shieldBarGrouper;

    private Image[] shieldFills;
    private Image[] shieldFlashFills;
    private Slider healthSlider;
    private Slider healthFlashSlider;
    private Color originalShieldColor;
    private Coroutine healthCoroutine;
    private Coroutine shieldCoroutine;

    private float cachedMaxHealth;
    private float cachedMaxShields;

    /// <summary>
    /// Called once from Enemy.Start() after act-scaling health/shields.
    /// Sets up slider sizes and shield pip images.
    /// </summary>
    public void Initialize(float health, float maxHealth, float maxShields)
    {
        cachedMaxHealth = maxHealth;
        cachedMaxShields = maxShields;

        if (healthBarContainers == null)
            healthBarContainers = GetComponentInChildren<Canvas>().gameObject;

        healthSlider      = healthBarContainers.transform.GetChild(1).GetComponent<Slider>();
        healthFlashSlider = healthBarContainers.transform.GetChild(0).GetComponent<Slider>();

        // Initialize shield pips
        if (maxShields == 0 && shieldBarGrouper != null)
        {
            shieldBarGrouper.gameObject.SetActive(false);
        }
        else
        {
            shieldFills      = new Image[(int)maxShields];
            shieldFlashFills = new Image[(int)maxShields];

            for (int i = 0; i < maxShields; i++)
            {
                if (i > shieldBarGrouper.transform.childCount)
                {
                    Debug.LogWarning("Enemy has more than 10 shields, this will not display properly. Please increase the size of the shield bar grouper in the prefab.");
                    break;
                }

                shieldFills[i]      = shieldBarGrouper.transform.GetChild(i).GetChild(1).GetComponent<Image>();
                shieldFlashFills[i] = shieldBarGrouper.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            }

            originalShieldColor = shieldFills[0].color;
        }

        // Hide unused shield pips
        for (int i = (int)maxShields; i < shieldBarGrouper.transform.childCount; i++)
        {
            shieldBarGrouper.transform.GetChild(i).gameObject.SetActive(false);
        }

        healthSlider.GetComponent<RectTransform>().sizeDelta      = new Vector2(1.515f * health, 12f);
        healthFlashSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(1.515f * health, 12f);
    }

    /// <summary>
    /// Show or hide the entire health bar canvas (e.g. Burrower retreating/emerging, on death).
    /// </summary>
    public void SetVisible(bool visible)
    {
        healthBarContainers.SetActive(visible);
    }

    public void ResetValues(float health, float shields)
    {
        if (healthSlider == null || healthFlashSlider == null)
            return;

        float healthValue = cachedMaxHealth > 0f ? health / cachedMaxHealth : 0f;
        healthSlider.value = healthValue;
        healthFlashSlider.value = healthValue;

        if (shieldFills != null)
            updateShieldFill(shieldFills, shields, true);
        if (shieldFlashFills != null)
            updateShieldFill(shieldFlashFills, shields, false);
    }

    /// <summary>
    /// Call from Enemy.damageCharacter() to update bars after a hit.
    /// </summary>
    public void OnDamaged(float health, float shields)
    {
        if (!healthBarContainers.activeSelf)
            healthBarContainers.SetActive(true);

        float originalHealth = healthSlider.value;
        healthSlider.value = health / cachedMaxHealth;

        if (originalHealth != healthSlider.value)
        {
            if (healthCoroutine != null)
            {
                StopCoroutine(healthCoroutine);
                healthCoroutine = null;
            }
            healthCoroutine = StartCoroutine(moveFlashToCatchUp(healthSlider, healthFlashSlider));
        }

        updateShieldFill(shieldFills, shields, true);
        checkForShieldFlash(shields);
    }

    /// <summary>
    /// Call from Enemy.Update() while isRecharging to keep bars in sync.
    /// </summary>
    public void OnRecharging(float health, float shields)
    {
        healthSlider.value      = health / cachedMaxHealth;
        healthFlashSlider.value = health / cachedMaxHealth;

        updateShieldFill(shieldFills,      shields, true);
        updateShieldFill(shieldFlashFills, shields, false);
    }

    // -------------------------------------------------------------------------

    private void updateShieldFill(Image[] images, float shields, bool changeColors)
    {
        bool restOfShieldsAreEmpty = false;
        for (int i = 0; i < cachedMaxShields; i++)
        {
            if (restOfShieldsAreEmpty)
            {
                images[i].fillAmount = 0f;
            }
            else if (shields >= i + 1)
            {
                if (images[i].color != originalShieldColor && changeColors)
                    images[i].color = originalShieldColor;

                images[i].fillAmount = 1f;
            }
            else
            {
                if (images[i].color == originalShieldColor && changeColors)
                    images[i].color = Color.blue;

                images[i].fillAmount = shields % 1;
                restOfShieldsAreEmpty = true;
            }

            if (i == cachedMaxShields - 1 && images[i].fillAmount > 0.99f) // edge case
            {
                if (images[i].color != originalShieldColor && changeColors)
                    images[i].color = originalShieldColor;

                images[i].fillAmount = 1f;
            }
        }
    }

    private void checkForShieldFlash(float shields)
    {
        for (int i = 0; i < cachedMaxShields; i++)
        {
            if (shieldFills[i].fillAmount != shieldFlashFills[i].fillAmount)
            {
                if (shieldFlashFills[i].fillAmount != 1) // no lerping if it was recharging
                    shieldFlashFills[i].fillAmount = shieldFills[i].fillAmount;
                else
                    StartCoroutine(flashShieldsAndCatchUp(shieldFills[i], shieldFlashFills[i]));
            }
        }
    }

    private IEnumerator flashShieldsAndCatchUp(Image mainFill, Image flashFill)
    {
        yield return new WaitForSeconds(0.25f);
        float original = flashFill.fillAmount;
        float time = 0.0f;
        while (time < 0.25f)
        {
            float t = TEMLib.getSmoothLerpT(time, 0.25f);
            flashFill.fillAmount = Mathf.Lerp(original, mainFill.fillAmount, t);
            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator moveFlashToCatchUp(Slider mainSlider, Slider flashSlider)
    {
        Color originalColor = Color.HSVToRGB(29f / 360f, 61f / 100f, 70f / 100f);
        originalColor.a = 1f;
        mainSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.white;
        yield return new WaitForSeconds(0.1f);
        mainSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = originalColor;

        yield return new WaitForSeconds(0.15f);
        float original = flashSlider.value;
        float time = 0.0f;
        while (time < 0.25f)
        {
            float t = TEMLib.getSmoothLerpT(time, 0.25f);
            flashSlider.value = Mathf.Lerp(original, mainSlider.value, t);
            time += Time.deltaTime;
            yield return null;
        }
    }
}
