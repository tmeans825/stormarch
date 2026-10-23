using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.Universal;

// Written by Steve Streeting 2017, Appended by Taylor Means 2021
// License: CC0 Public Domain http://creativecommons.org/publicdomain/zero/1.0/

/// <summary>
/// Component which will flicker a linked light while active by changing its
/// intensity and outer radius between the min and max values given. The flickering can be
/// sharp or smoothed depending on the value of the smoothing parameters.
///
/// Just activate / deactivate this component as usual to pause / resume flicker
/// </summary>
public class LightFlickerEffect : MonoBehaviour
{  
    [Tooltip("Minimum random light intensity")]
    public float minIntensity = 0f;
    [Tooltip("Maximum random light intensity")]
    public float maxIntensity = 1f;
    [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
    [Range(1, 50)]
    public int smoothing = 5;

    [Tooltip("Minimum random radius")]
    public float minRadius = 0f;
    [Tooltip("Maximum random radius")]
    public float maxRadius = 1f;
    [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
    [Range(1, 50)]
    public int radiusSmoothing = 5;

    private Light2D light;
    // Continuous average calculation via FIFO queue
    // Saves us iterating every time we update, we just change by the delta
    Queue<float> smoothQueue;
    float lastSum = 0;
    Queue<float> radiusQueue;
    float lastRadiusSum = 0;


    /// <summary>
    /// Reset the randomness and start again. You usually don't need to call
    /// this, deactivating/reactivating is usually fine but if you want a strict
    /// restart you can do.
    /// </summary>
    public void Reset(Queue<float> sq)
    {
        sq.Clear();
        lastSum = 0;
    }

    void Start()
    {
        smoothQueue = new Queue<float>(smoothing);
        radiusQueue = new Queue<float>(radiusSmoothing);

        // External or internal light?
        if (light == null)
        {
            light = GetComponent<Light2D>();
        }
    }

    void Update()
    {
        if (light == null)
            return;

        // pop off an item if too big
        while (smoothQueue.Count >= smoothing)
        {
            lastSum -= smoothQueue.Dequeue();
        }
        while (radiusQueue.Count >= radiusSmoothing)
        {
            lastRadiusSum -= radiusQueue.Dequeue();
        }

        // Generate random new item, calculate new average
        float newVal = Random.Range(minIntensity, maxIntensity);
        smoothQueue.Enqueue(newVal);
        lastSum += newVal;

        newVal = Random.Range(minRadius, maxRadius);
        radiusQueue.Enqueue(newVal);
        lastRadiusSum += newVal;
        // Calculate new smoothed average
        light.intensity = lastSum / (float)smoothQueue.Count;
        light.pointLightOuterRadius = lastRadiusSum / (float)radiusQueue.Count;
    }

}
