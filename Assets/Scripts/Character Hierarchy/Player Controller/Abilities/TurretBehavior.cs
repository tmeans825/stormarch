using System.Collections;
using UnityEngine;

public class TurretBehavior : DeployableAbility
{
    public GameObject turretObject;
    public GameObject projectile;

    public float shootRadius = 10f;
    public float bulletSpeed = 30f;
    public float bulletLifetime = 0.3f;

    private float fireCooldown = 5f;
    private float lifeTime = 25f;
    private float stunLength = 3f;

    private Animator headAnimator;

    public override GameObject deployedObject { get { return turretObject; } }

    protected override void Awake()
    {
        base.Awake();
        headAnimator = turretObject.transform.GetChild(0).GetComponent<Animator>();
    }

    // Called by TurretBehavior only — still uses base canPlace() for the check
    public bool canPlaceTurret()
    {
        return canPlace();
    }

    public bool activateTurretMode()
    {
        return activateDeployMode();
    }

    protected override float getAbilityRadius() => shootRadius;

    protected override void onDeployed()
    {
        abilityRangeCircle.gameObject.SetActive(true);
        StartCoroutine(processTurretBehavior());
    }

    private IEnumerator processTurretBehavior()
    {
        audioManager.PlaySound("Turret Loop");
        float lastFire = fireCooldown - 1f;
        float timeAlive = 0f;

        while(timeAlive < lifeTime)
        {
            timeAlive += Time.deltaTime;

            if (lastFire > fireCooldown)
            {
                GameObject target = findTarget();

                if(target != null)
                {
                    fireBullet(target.transform.position);
                    lastFire = 0f;
                    headAnimator.Play("turret_head_shoot");
                }
            }

            lastFire += Time.deltaTime;

            yield return null;
        }

        audioManager.StopSound("Turret Loop");
        turretObject.SetActive(false);
        StartCoroutine(returnFlight());
    }

    protected override void onRecalled()
    {
        audioManager.StopSound("Turret Loop");
        PlayerController.instance.signalTurretComplete();
        audioManager.PlaySound(recallSound);
    }

    private GameObject findTarget()
    {
        int mask = 1 << LayerMask.NameToLayer("EnemyBlockingLayer");
        Collider2D[] possibleTargets = Physics2D.OverlapCircleAll(transform.position, shootRadius, mask);

        if(possibleTargets.Length == 0)
            return null;

        int randomTargetInt = Random.Range(0, possibleTargets.Length - 1);

        return possibleTargets[randomTargetInt].gameObject;
    }

    private void fireBullet(Vector2 target)
    {
        Vector2 direction = target - (Vector2)transform.position;

        GameObject currentBullet = (GameObject)Instantiate(projectile, transform.position, Quaternion.identity, ObjectPooler.instance.transform);
        currentBullet.GetComponent<ProjectileScript>().bulletSpeed = bulletSpeed;
        currentBullet.GetComponent<ProjectileScript>().initialize(bulletLifetime, 0, stunLength, false);

        currentBullet.GetComponent<Rigidbody2D>().velocity = new Vector2(direction.x * bulletSpeed, direction.y * bulletSpeed);
    }
}
