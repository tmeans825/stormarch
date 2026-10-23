using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Com.LuisPedroFonseca.ProCamera2D;

public class OwenBehavior : NonCombatChar
{
    public ProCamera2DCinematics BuildCinematic;
    public GameObject influencePoint;
    public float constructionSiteTargetBuffer = 10.0f; // used for how far away Owen should stop from construction sites

    private bool isDeployed = false; // used for when Owen is 'out and about'
    private bool isReturning = false; // used for when Owen is returning to original location

    private float originalTargetBuffer;
    private bool returnTrigger = false;

    public GameObject b3lt;
    private GameObject chest;
    private GameObject head;
    private GameObject legs;
    private GameObject shadow;

    public static OwenBehavior instance;
    public Animator hammerAnim;

    protected override void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
        base.Awake();
    }

    protected override void Start()
    {
        originalTargetBuffer = aStarTargetBuffer;
        chest = transform.GetChild(0).gameObject;
        head = transform.GetChild(1).gameObject;
        legs = transform.GetChild(2).gameObject;
        shadow = transform.GetChild(3).gameObject;
        base.Start();
    }

    protected override void stopMoving()
    {
        base.stopMoving();

        if (isDeployed && !isReturning)
        {
            b3ltBehavior.instance.closeIn();
            StartCoroutine(waitForStartMove());  
        }
        else if (isDeployed && isReturning)
        {
            isReturning = false;
            isDeployed = false;

            b3ltBehavior.instance.comeHome();
            
            // we done. can maybe signal to zoom out or something
            StartCoroutine(lerpHome(0.25f));
        }
    }

    // Call this from b3lt getting to the final location
    public void startAnim()
    {
        rescaleBody(Vector3.zero);
        hammerAnim.gameObject.SetActive(true);
        hammerAnim.Play("Owen Hammer Animation");
    }

    public void triggerReturn()
    {
        returnTrigger = true;
    }


    private IEnumerator waitForStartMove()
    {
        while(!returnTrigger)
        {
            yield return null;
        }

        rescaleBody(new Vector3(10f, 10f, 1f));
        hammerAnim.gameObject.SetActive(false);
        returnTrigger = false;

        aStarTargetBuffer = originalTargetBuffer;

        yield return new WaitForSeconds(1f);

        //BuildCinematic.Stop();
        BuildCinematic.GoToNextTarget();

        startMoving(startingSpawn);
        isReturning = true;
        b3ltBehavior.instance.unFreeze();
    }

    // called by cinematic when it's zoomed back to the player
    public void cinematicFinished()
    {
        influencePoint.SetActive(true);
        HomesteadTerrainHelper.instance.triggerMenusBack(0.01f);
    }

    private IEnumerator lerpHome(float duration)
    {
        Vector3 startPosition = transform.position;

        float time = 0.0f;
        while (time < duration)
        {
            float t = TEMLib.getSmoothLerpT(time, duration);
                transform.localPosition = Vector3.Lerp(startPosition, startingSpawn, t);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public override void startMoving(Vector2 t)
    {
        base.startMoving(t);

        if (!isDeployed)
        {
            influencePoint.SetActive(false);
            BuildCinematic.Play();

            isDeployed = true;
            b3ltBehavior.instance.deploy();
            aStarTargetBuffer = constructionSiteTargetBuffer; // TO DO - adjust on the fly
        }
    }

    private void rescaleBody(Vector3 scale)
    {
        chest.transform.localScale = scale;
        legs.transform.localScale = scale;
        head.transform.localScale = scale;
        shadow.transform.localScale = scale;
        b3lt.transform.localScale = scale;
    }
}
