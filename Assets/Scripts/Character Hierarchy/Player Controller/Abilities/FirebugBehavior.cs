using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class FirebugBehavior : DeployableAbility
{
    public GameObject firebugObject;
    public Light2D firebugLight;

    public float autoReturnDistance = 20f;

    [System.NonSerialized]
    public int firebugIndex;

    public override GameObject deployedObject { get { return firebugObject; } }

    protected override float getAbilityRadius() => firebugLight.pointLightOuterRadius;

    protected override void Update()
    {
        base.Update();

        if (isDeployed && Vector2.Distance(transform.position, PlayerController.instance.transform.position) > autoReturnDistance)
            recall();
    }

    protected override void onLaunched()
    {
        FirebugUIBehavior.instance.notifyDeployed(firebugIndex);
    }

    protected override void onDeployed()
    {
        abilityRangeCircle.gameObject.SetActive(false);
    }

    protected override void onRecalled()
    {
        FirebugUIBehavior.instance.notifyReturned(firebugIndex);
    }

    protected override void onPlacerModeActivated()
    {
        FirebugUIBehavior.instance.notifyPlacerModeStart();
    }

    protected override void onPlacerModeDeactivated()
    {
        FirebugUIBehavior.instance.notifyPlacerModeEnd();
    }
}
