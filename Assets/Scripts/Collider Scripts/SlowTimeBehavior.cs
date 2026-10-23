using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class SlowTimeBehavior : MonoBehaviour
{
    public float colliderMaxSize = 0.5f;
    public float maxLightRadius = 7.5f;
    private Transform followTarget;

    private CircleCollider2D mainCollider;
    private Light2D displayLight;

    private void Start()
    {
        mainCollider = GetComponent<CircleCollider2D>();
        displayLight = GetComponent<Light2D>();
        StartCoroutine(lerpCollisionZone());
    }

    void Update()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.transform.position;
        }
    }

    public void setFollowTarget(Transform target)
    {
        followTarget = target;
    }

    private IEnumerator lerpCollisionZone()
    {
        float duration = 0.5f;
        float time = 0.0f;

        while (time < duration)
        {
            float t = TEMLib.getSmoothLerpT(time, duration);
            mainCollider.radius = Mathf.Lerp(0f, colliderMaxSize, t);
            displayLight.pointLightOuterRadius = Mathf.Lerp(0f, maxLightRadius, t);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;
        while (time < duration)
        {
            float t = TEMLib.getSmoothLerpT(time, duration);
            mainCollider.radius = Mathf.Lerp(colliderMaxSize, 0f, t);
            displayLight.pointLightOuterRadius = Mathf.Lerp(maxLightRadius, 0f, t);
            time += Time.deltaTime;
            yield return null;
        }
    }
}
