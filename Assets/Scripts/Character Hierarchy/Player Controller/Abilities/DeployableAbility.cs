using System.Collections;
using UnityEngine;

public abstract class DeployableAbility : MonoBehaviour
{
    public GameObject placerObject;
    public CircleDrawer playerRangeCircle;
    public CircleDrawer abilityRangeCircle;
    public GameObject flyingObject;

    public float deployRadius = 10f;

    public string placerSound = "Click";
    public string recallSound = "Click";
    public string landSound = "Land";

    protected bool inPlacerMode = false;
    protected bool isFlying = false;

    protected GameObject instantiatedFlying;
    protected AudioManager audioManager;

    public abstract GameObject deployedObject { get; }

    protected virtual void Awake()
    {
        audioManager = GetComponent<AudioManager>();
    }

    protected virtual void Update()
    {
        if (inPlacerMode && !isFlying)
            followMouseClamped();
    }

    public bool canPlace()
    {
        return !deployedObject.activeSelf;
    }

    public void activatePlacerMode()
    {
        followMouseClamped();
        toggleMode(true);
        audioManager.PlaySound(placerSound);
        playerRangeCircle.SetRadius(deployRadius);
        abilityRangeCircle.SetRadius(getAbilityRadius());
        onPlacerModeActivated();
    }

    protected virtual float getAbilityRadius() => deployRadius;

    protected virtual void onPlacerModeActivated() { }

    protected virtual void onPlacerModeDeactivated() { }

    public bool activateDeployMode()
    {
        if (!inPlacerMode)
            return false;

        playerRangeCircle.gameObject.SetActive(false);
        abilityRangeCircle.gameObject.SetActive(false);
        StartCoroutine(deploymentFlight());

        return true;
    }

    public bool isDeployed
    {
        get { return deployedObject.activeSelf && !inPlacerMode && !isFlying; }
    }

    public bool isActive
    {
        get { return deployedObject.activeSelf || inPlacerMode || isFlying; }
    }

    public void playLandSound()
    {
        audioManager.PlaySound(landSound);
    }

    public void signalSuccessfulFlight()
    {
        isFlying = false;
    }

    public void recall()
    {
        StopAllCoroutines();
        deployedObject.SetActive(false);
        StartCoroutine(returnFlight());
    }

    protected IEnumerator returnFlight()
    {
        isFlying = true;
        abilityRangeCircle.gameObject.SetActive(false);

        if (instantiatedFlying == null)
            instantiatedFlying = Instantiate(flyingObject, PlayerController.instance.transform.position, Quaternion.identity);

        instantiatedFlying.transform.position = transform.position;
        FlyingDeployableBehavior flyingBehavior = instantiatedFlying.GetComponent<FlyingDeployableBehavior>();
        flyingBehavior.deploy(PlayerController.instance.transform.position, this);

        while (isFlying)
        {
            flyingBehavior.updateTarget(PlayerController.instance.transform.position);
            yield return null;
        }

        onRecalled();
    }

    protected virtual void onRecalled() { }

    protected IEnumerator deploymentFlight()
    {
        isFlying = true;

        if (instantiatedFlying == null)
            instantiatedFlying = Instantiate(flyingObject, PlayerController.instance.transform.position, Quaternion.identity);

        instantiatedFlying.transform.position = PlayerController.instance.transform.position;
        instantiatedFlying.GetComponent<FlyingDeployableBehavior>().deploy(transform.position, this);

        onLaunched();

        while (isFlying)
            yield return null;

        toggleMode(false);
        onPlacerModeDeactivated();
        onDeployed();
    }

    protected virtual void onLaunched() { }

    protected abstract void onDeployed();

    protected void toggleMode(bool isPlacerMode)
    {
        inPlacerMode = isPlacerMode;
        placerObject.SetActive(isPlacerMode);
        playerRangeCircle.gameObject.SetActive(isPlacerMode);
        if (isPlacerMode)
            abilityRangeCircle.gameObject.SetActive(true);
        deployedObject.SetActive(!isPlacerMode);
        if (isPlacerMode)
            StopAllCoroutines();
    }

    protected void followMouseClamped()
    {
        Vector2 playerPos = PlayerController.instance.transform.position;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - playerPos;

        if (direction.magnitude > deployRadius)
            direction = direction.normalized * deployRadius;

        transform.position = playerPos + direction;
        playerRangeCircle.transform.position = playerPos;
    }
}
