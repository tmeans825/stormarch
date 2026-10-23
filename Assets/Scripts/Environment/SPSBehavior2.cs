using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Events;

public class SPSBehavior2 : MonoBehaviour
{
    public GameObject spriteObj;
    public GameObject coneObj;
    public GameObject spotlightObj;

    public Vector3[] targets;
    public float movementDuration;
    public float pauseLength;

    public float maxPursuitDistance = 10f;

    public bool muteAlarm = false;
    public bool turnRed = true;

    public UnityEvent eventOnDetect;
    public UnityEvent eventOnUndetect;

    private int currIndex = 0;
    private bool overrideFollowPlayer = false;

    private Light2D coneLight;
    private Light2D spotLight;

    private Color originalSpotlightColor;

    private AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        coneLight = coneObj.GetComponent<Light2D>();

        spotLight = spotlightObj.GetComponent<Light2D>();

        originalSpotlightColor = coneLight.color;

        spotlightObj.transform.localPosition = targets[0];

        am = GetComponent<AudioManager>();

        if (targets.Length > 1)
        {
            StartCoroutine(patrolLight(1));
        }       
    }

    // Update is called once per frame
    void Update()
    {
        // Update cone OBJ

        // FIRST: Rotation
        // update rotation to track spotlight
        Vector3 directionToSpotlight = spotlightObj.transform.position - coneObj.transform.position;
        directionToSpotlight.z = 0;

        // Calculate the angle to the player
        float angle = Mathf.Atan2(directionToSpotlight.y, directionToSpotlight.x) * Mathf.Rad2Deg;
        angle -= 90f;

        // Apply rotation
        coneObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // NEXT: Radius + Distance
        float distance = Vector3.Distance(spotlightObj.transform.position, coneObj.gameObject.transform.position);

        // Adjust the radius of Light B to at least the distance to Light A
        // You might want to add a small offset value to ensure it encompasses Light A entirely
        coneLight.pointLightOuterRadius = distance + 1f;
        coneLight.pointLightInnerRadius = coneLight.pointLightOuterRadius / 2f;
        coneLight.pointLightOuterAngle = CalculateAngleBasedOnDistance(distance);

        if (PlayerController.instance.isSliding && overrideFollowPlayer && (Vector3.Distance(PlayerController.instance.transform.position, transform.position) >= maxPursuitDistance))
            undetectPlayer();
    }

    private IEnumerator snapToPlayer()
    {
        float time = 0.0f;
        Vector3 startPos = spotlightObj.transform.position;

        while (time < 0.25f)
        {
            float t = TEMLib.getSmoothLerpT(time, 0.25f);
            spotlightObj.transform.position = Vector3.Lerp(startPos, PlayerController.instance.transform.position, t);
            time += Time.deltaTime;
            yield return null;
        }

        while(overrideFollowPlayer && (Vector3.Distance(PlayerController.instance.transform.position, transform.position) < maxPursuitDistance))
        {
            spotlightObj.transform.position = PlayerController.instance.transform.position;
            yield return null;
        }
    }

    private float CalculateAngleBasedOnDistance(float distance)
    {
        // Safety check to avoid division by zero or negative values
        if (distance <= 0 || spotLight.pointLightOuterRadius <= 0)
        {
            return 0f; // Default to a full circle if something's off
        }

        // Calculate half angle using trigonometry (tangent of the angle)
        // tan(angle/2) = opposite/adjacent = lightARadius / distance
        float halfAngleRadians = Mathf.Atan(spotLight.pointLightOuterRadius / distance);

        // Convert radians to degrees and double the result to get the full angle
        float angleDegrees = 2 * Mathf.Rad2Deg * halfAngleRadians;

        return angleDegrees;
    }

    private IEnumerator patrolLight(int index)
    {
        currIndex = index;

        float time = 0.0f;
        Vector3 startPos = spotlightObj.transform.localPosition;
        am.PlaySound("Move", true);

        while (time < movementDuration)
        {
            float t = TEMLib.getSmoothLerpT(time, movementDuration);
            spotlightObj.transform.localPosition = Vector3.Lerp(startPos, targets[index], t);
            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(pauseLength);
        index++;

        if (index >= targets.Length)
            index = 0;

        if (targets.Length > 1)
            StartCoroutine(patrolLight(index));
    }

    public void detectPlayer()
    {
        if (overrideFollowPlayer) // already on player, don't detect
            return;

        // first - see if raycast?
        StopAllCoroutines();
        overrideFollowPlayer = true;

        if(!muteAlarm)
            am.PlaySound("Alarm");

        WorldspaceCanvas.instance.popUpExclaimEmote(transform.position + new Vector3(0f,1f,0f));
        StartCoroutine(callForHelp());
        StartCoroutine(snapToPlayer());

        eventOnDetect.Invoke();

        if (turnRed)
        {
            coneLight.color = Color.red;
            spotLight.color = Color.red;
        }
        
    }

    public void undetectPlayer()
    {
        if(!muteAlarm)
            am.StopSound("Alarm");

        overrideFollowPlayer = false;
        coneLight.color = originalSpotlightColor;
        spotLight.color = originalSpotlightColor;

        StartCoroutine(patrolLight(currIndex));

        eventOnUndetect.Invoke();
    }

    private IEnumerator callForHelp()
    {
        yield return new WaitForSeconds(0.7f);

        //Debug.Log("Alerting");
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, 20f, 1 << LayerMask.NameToLayer("EnemyBlockingLayer"));
        for (int i = 0; i < colls.Length; i++)
        {
            Enemy e = colls[i].gameObject.GetComponent<Enemy>();
            if (e != null && e.state == Enemy.State.Idle)
                e.aggro();
        }
    }
}
