using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class b3ltBehavior : Entity
{
    public ParticleSystem ps;

    public static b3ltBehavior instance;

    private bool isDeployed = false;
    private bool isPacing = false;
    private bool closeInSignal = false;
    private bool returnHome = false;
    private float timeBetweenPace = 3f;
    private float timePassed = 0f;
    private float paceDuration = 0.5f;
    private bool freeze = false;

    private float randomizedTime;

    private Vector3 startPosition;
    public float patrolRadius = 1.6f;
    public Animator wheelAnimator;
    private AudioManager am;
    

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    protected override void Start()
    {
        randomizedTime = Random.Range(timeBetweenPace * 0.5f, timeBetweenPace * 1.5f);
        startPosition = transform.position;
        am = GetComponent<AudioManager>();

        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        // if actively moving, is not 'listening' for next move
        if (isPacing || freeze)
            return;

        // listen for random pacing
        if (!isDeployed)
        {
            timePassed += Time.deltaTime;
            if (timePassed > randomizedTime)
            {
                pace();               
                timePassed = 0f;
                randomizedTime = Random.Range(timeBetweenPace * 0.8f, timeBetweenPace * 1.2f);
            }
        }
        // listen for owen getting too far away, or moving to position
        else if(!closeInSignal && !returnHome)
        {
            // check for distance to Owen - too far, move to him.
            if (Vector3.Distance(transform.position, OwenBehavior.instance.transform.position) >= patrolRadius)
                StartCoroutine(moveTo(OwenBehavior.instance.transform.position, false, false));
        }
        else if(closeInSignal)
        {
            // to do, add offset
            Vector3 offset = new Vector3(-1.15f, -0.16f);
            StartCoroutine(moveTo(OwenBehavior.instance.transform.position + offset, true, false));
        }
        else if(returnHome)
        {
            StartCoroutine(moveTo(startPosition, false, true));
        }
    }

    public void deploy()
    {
        isDeployed = true;
    }

    public void closeIn()
    {
        closeInSignal = true;
    }

    public void comeHome()
    {
        returnHome = true;
    }

    public void unFreeze()
    {
        freeze = false;
    }

    private void pace()
    {
        float angle = Random.Range(0.0f, 2 * Mathf.PI); // Get a random angle between 0 and 2π
        Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // Convert the angle to a vector direction
        StartCoroutine(moveTo(startPosition + Random.Range(0f, patrolRadius) * (Vector3)randomDirection, false, false));
    }

    private IEnumerator moveTo(Vector2 target, bool closeIn, bool home)
    {
        isPacing = true;
        am.PlaySound("Move", true);
        //ps.Play();
        wheelAnimator.Play("b3lt_wheels_spin");
        float time = 0.0f;

        Vector3 currentPosition = transform.position;
        Vector3 currentAngle = transform.eulerAngles;
        Vector3 targetAngle;

        // to-do: calculate target angle
        if(currentPosition.x < target.x)
        {
            targetAngle = new Vector3(0f, 0f, 6f);
        }
        else
        {
            targetAngle = new Vector3(0f, 0f, -6f);
        }

        float rotateDuration = paceDuration / 2f;
        float rotateTime = 0f;

        while (time < paceDuration)
        {
            float t = TEMLib.getSmoothLerpT(time, paceDuration);
            transform.localPosition = Vector3.Lerp(currentPosition, target, t);

            if(rotateTime > rotateDuration)
            {
                rotateTime = 0f;
                targetAngle = currentAngle;
                currentAngle = transform.eulerAngles;
            }

            float r = TEMLib.getSmoothLerpT(rotateTime, rotateDuration);

            transform.eulerAngles = new Vector3(
                Mathf.LerpAngle(currentAngle.x, targetAngle.x, r),
                Mathf.LerpAngle(currentAngle.y, targetAngle.y, r),
                Mathf.LerpAngle(currentAngle.z, targetAngle.z, r));


            rotateTime += Time.deltaTime;
            time += Time.deltaTime;                                            
            yield return null;
        }

        isPacing = false;
        wheelAnimator.Play("b3lt_wheels_idle");
        //ps.Stop();

        if(closeIn)
        {
            freeze = true;
            closeInSignal = false;
            OwenBehavior.instance.startAnim();
        }
        else if(home)
        {
            returnHome = false;
            isDeployed = false;
        }
    }

    // TEST SCRIPT
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);
    }
}
