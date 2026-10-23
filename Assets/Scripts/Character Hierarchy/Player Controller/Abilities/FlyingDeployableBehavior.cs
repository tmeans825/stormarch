using System.Collections;
using UnityEngine;

public class FlyingDeployableBehavior : MonoBehaviour
{
    public float flightDuration = 1f;

    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private AudioManager audioManager;
    private GameObject ps;

    private Vector3 cachedPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
        audioManager = GetComponent<AudioManager>();
        ps = transform.GetChild(0).gameObject;
        if (spriteRenderer == null || audioManager == null || ps == null)
        {
            Debug.LogError("FlyingDeployableBehavior: SpriteRenderer or AudioManager or ParticleSystem missing on the GameObject.");
        }
    }

    public void deploy(Vector3 targetPosition, DeployableAbility callBackScript)
    {
        gameObject.SetActive(true);
        ps.SetActive(true);
        StartCoroutine(lerpToTarget(targetPosition, callBackScript));
    }

    private IEnumerator lerpToTarget(Vector3 targetPosition, DeployableAbility callBackScript)
    {
        cachedPosition = targetPosition;
        audioManager.PlaySound("Fly");
        spriteRenderer.enabled = true;
        trailRenderer.enabled = true;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        while (elapsedTime < flightDuration)
        {
            float t = TEMLib.getSmoothLerpT(elapsedTime, flightDuration);
            transform.position = Vector3.Lerp(startPosition, cachedPosition, t);
            elapsedTime += Time.deltaTime;

            Vector2 direction = cachedPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, -angle);
            }
            yield return null;
        }
        transform.position = cachedPosition;

        audioManager.StopSound("Fly");
        gameObject.SetActive(false);
        callBackScript.playLandSound();
        callBackScript.signalSuccessfulFlight();
    }

    public void updateTarget(Vector3 targetPosition)
    {
        cachedPosition = targetPosition;
    }
}
