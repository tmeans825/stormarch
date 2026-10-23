using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class SentinelExplode : Explode
{
    [Header("Debris Stats")]
    public int debrisCount = 0;
    public GameObject debris;
    public float debrisDelay = 0.2f;

    [Header("Post-Explosion Smoke")]
    public GameObject smoke;
    public float smokeDelay = 0.2f;

    [Header("Other")]
    public Light2D explosionIndicatorLight;

    private void Start()
    {
        explosionIndicatorLight.pointLightOuterRadius = 0f; // to do - lerp
        StartCoroutine(expandIndicatorLight());
    }

    protected override void blowUp(Transform t, Vector3 offset)
    {
        StartCoroutine(endLife());
        base.blowUp(ObjectPooler.instance.transform, offset);
    }
    private IEnumerator endLife()
    {
        explosionIndicatorLight.enabled = false;
        Vector3 spawnPos = transform.position;

        //Debug.Log("End Life Called");
        yield return new WaitForSeconds(debrisDelay);
        if (debrisCount > 0 && debris != null)
        {
            for (int i = 0; i < debrisCount; i++)
                Instantiate(debris, spawnPos, Quaternion.identity, ObjectPooler.instance.transform);
        }

        yield return new WaitForSeconds(smokeDelay);
        if (smoke != null)
        {
            Instantiate(smoke, spawnPos + new Vector3(0.0f, 0.3f, 1.0f), Quaternion.identity, ObjectPooler.instance.transform);
        }

        Destroy(gameObject);
    }

    private IEnumerator expandIndicatorLight()
    {
        float time = 0.0f;
        float duration = 1f;
        while (time < duration)
        {
            float t = TEMLib.getSmoothLerpT(time, duration);
            explosionIndicatorLight.pointLightOuterRadius = Mathf.Lerp(0f, radius, t);
            time += Time.deltaTime;
            yield return null;
        }
    }
}
