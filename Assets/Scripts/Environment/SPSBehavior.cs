using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class SPSBehavior : MonoBehaviour
{
    public GameObject spriteObj;
    public GameObject visionObj;

    public Quaternion[] targets;
    public float movementDuration;
    public float pauseLength;

    private int currIndex = 0;
    private bool overrideFollowPlayer = false;
    private Light2D spotlight;
    private Color originalSpotlightColor;

    // Start is called before the first frame update
    void Start()
    {
        spotlight = visionObj.GetComponent<Light2D>();
        originalSpotlightColor = spotlight.color;
        visionObj.transform.rotation = targets[0];
        StartCoroutine(startRot(1));
    }

    private void Update()
    {
        if (!overrideFollowPlayer)
            return;

        // update rotation to track player
        Vector3 directionToPlayer = PlayerController.instance.transform.position - visionObj.transform.position;
        // Make sure the direction vector is in the XY plane since we're working in 2D
        directionToPlayer.z = 0;

        // Calculate the angle to the player
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        // Unity's 2D default is right = 0 degrees. 
        // If you want to face the object so its "up" points at the player, subtract 90 degrees.
        // This aligns the object's up vector with the direction to the player.
        angle -= 90f;

        // Apply rotation
        visionObj.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator startRot(int index)
    {
        currIndex = index;

        float time = 0.0f;
        Quaternion startRotation = visionObj.transform.rotation;

        while (time < movementDuration)
        {
            float t = TEMLib.getSmoothLerpT(time, movementDuration);
            visionObj.transform.rotation = Quaternion.Lerp(startRotation, targets[index], t);
            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(pauseLength);
        index++;

        if (index >= targets.Length)
            index = 0;
       
        StartCoroutine(startRot(index));
    }

    public void detectPlayer()
    {
        // first - see if raycast?

        StopAllCoroutines();
        overrideFollowPlayer = true;
        // bark?
        spotlight.color = Color.red;
    }

    public void undetectPlayer()
    {
        overrideFollowPlayer = false;
        spotlight.color = originalSpotlightColor;
        StartCoroutine(startRot(currIndex));
    }
}
