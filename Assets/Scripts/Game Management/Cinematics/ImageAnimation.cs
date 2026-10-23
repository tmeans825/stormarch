using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
    public Sprite[] animationFrames;
    public float timePerFrame = 0.2f;

    public bool loop = false;
    public float loopDelay;

    private int currentFrame;
    private float timer;
    private Image imageComponent;

    void Start()
    {
        if (animationFrames.Length == 0) 
            return;

        imageComponent = GetComponent<Image>();

        if (imageComponent != null) 
            StartCoroutine(animationLoop());      
    }

    IEnumerator animationLoop()
    {
        int currentFrame = 0;
        imageComponent.sprite = animationFrames[currentFrame];

        while (currentFrame < animationFrames.Length - 1)
        {
            timer += Time.deltaTime;

            if (timer > timePerFrame)
            {
                timer = 0f;
                currentFrame++;
                imageComponent.sprite = animationFrames[currentFrame];                
            }

            yield return null;
        }

        if (!loop)
            yield break;

        yield return new WaitForSeconds(loopDelay);

        StartCoroutine(animationLoop());
    }
}
