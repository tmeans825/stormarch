using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EyeAnimation : MonoBehaviour
{
    public float minRandom = 5.0f;
    public float maxRandom = 20.0f;
    public float blinkLength = 0.8f;
    public float doubleBlinkChance = 0.5f;

    private SpriteRenderer sprite;
    private Image imageFallback;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();

        if (sprite == null)
            imageFallback = GetComponent<Image>();

        if (sprite == null && imageFallback == null)
        {
            Debug.LogError("Couldn't find Sprite or Image for blinking animation.");
            return;
        }

        StartCoroutine(blinkAnimation());
    }

    private IEnumerator blinkAnimation()
    {
        if (sprite != null)
            sprite.enabled = false;

        if (imageFallback != null)
            imageFallback.enabled = false;

        while (true)
        {
            toggleEnable(false);
            yield return new WaitForSeconds(Random.Range(minRandom, maxRandom)); // TIME OFF
            
            toggleEnable(true);
            yield return new WaitForSeconds(Random.Range(blinkLength * 0.7f, blinkLength * 1.3f)); // TIME ON

            if (Random.Range(0, 1f) < doubleBlinkChance)
            {
                toggleEnable(false);
                yield return new WaitForSeconds(Random.Range(blinkLength * 0.7f, blinkLength * 1.3f)); // TIME OFF

                toggleEnable(true);
                yield return new WaitForSeconds(Random.Range(blinkLength * 0.7f, blinkLength * 1.3f)); // TIME ON
            }
        }

    }

    private void toggleEnable(bool enabled)
    {
        if (sprite != null)
            sprite.enabled = enabled;
        if (imageFallback != null)
            imageFallback.enabled = enabled;
    }
}
