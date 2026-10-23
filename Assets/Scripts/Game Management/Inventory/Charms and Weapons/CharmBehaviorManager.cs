using Language.Lua;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

public class CharmBehaviorManager : MonoBehaviour
{
    public static CharmBehaviorManager instance;
    public List<Charm> equippedCharms = new List<Charm>();
    public List<Gun> gunsJammedLast = new List<Gun>();
    public GameObject fireBombSlideObj;
    public GameObject teleportationParticles;

    private float drainAmmoIdleTimer = 0f;
    private bool isTeleporting = false; // prevents overlapping teleports
    private bool usePositiveForkAngle = true;

    // bulk_plate: heavy-plate slow that applies only while in combat
    private bool bulkPlateSlowApplied = false;  // is the -BULK_PLATE_SLOW currently applied to baseSpeed
    private bool bulkPlateCombatActive = false; // are we currently in combat (per aggro/unaggro)
    private const float BULK_PLATE_SLOW = 2f;

    void Awake() { instance = this; } // TODO: check for singleton / existing instance

    void OnEnable()
    {
        if (GameEvents.current == null)
        {
            Debug.LogError("GameEvents not yet made into 'current' reference - charms nonfunctional.");
            return;
        }

        // Seed combat state so a re-enable / spawn-during-combat doesn't miss active combat.
        // (Only this initialization reads enemiesInCombat; the event handlers must not - see handleAggro.)
        bulkPlateCombatActive = GameEvents.current.enemiesInCombat > 0;

        GameEvents.current.onGunEquip += handleGunEquip;
        GameEvents.current.onGunUnequip += handleGunUnequip;
        GameEvents.current.onChestEquip += handleChestEquip;
        GameEvents.current.onChestUnequip += handleChestUnequip;

        GameEvents.current.onReloadStart += handleReloadStart;
        GameEvents.current.onReloadComplete += handleReloadComplete;
        GameEvents.current.onBulletCreate += handleBulletCreate;
        GameEvents.current.onPlayerHurt += handlePlayerHurt;
        GameEvents.current.onShotAttempt += handleAttemptedShot;
        GameEvents.current.onGunCreate += handleGunGenerated;
        GameEvents.current.onEnemyKilled += handleEnemyKilled;

        GameEvents.current.onRechargeStart += handleRechargeStart;
        GameEvents.current.onFireDamageTaken += handleFireDamageTaken;
        GameEvents.current.onShieldsBroken += handleShieldsBroken;
        GameEvents.current.onSlideStart += handleSlideStart;

        GameEvents.current.onAggro += handleAggro;
        GameEvents.current.onUnaggro += handleUnaggro;
    }
    void OnDisable()
    {
        GameEvents.current.onGunEquip -= handleGunEquip;
        GameEvents.current.onGunUnequip -= handleGunUnequip;
        GameEvents.current.onChestEquip -= handleChestEquip;
        GameEvents.current.onChestUnequip -= handleChestUnequip;

        GameEvents.current.onReloadStart -= handleReloadStart;
        GameEvents.current.onReloadComplete -= handleReloadComplete;
        GameEvents.current.onBulletCreate -= handleBulletCreate;
        GameEvents.current.onPlayerHurt -= handlePlayerHurt;
        GameEvents.current.onShotAttempt -= handleAttemptedShot;
        GameEvents.current.onGunCreate -= handleGunGenerated;
        GameEvents.current.onEnemyKilled -= handleEnemyKilled;

        GameEvents.current.onRechargeStart -= handleRechargeStart;
        GameEvents.current.onFireDamageTaken -= handleFireDamageTaken;
        GameEvents.current.onShieldsBroken -= handleShieldsBroken;
        GameEvents.current.onSlideStart -= handleSlideStart;

        GameEvents.current.onAggro -= handleAggro;
        GameEvents.current.onUnaggro -= handleUnaggro;

        // Defensive: don't leave baseSpeed stuck slowed if disabled mid-combat while player lives.
        if (bulkPlateSlowApplied && PlayerController.instance != null)
        {
            PlayerController.instance.baseSpeed += BULK_PLATE_SLOW;
            bulkPlateSlowApplied = false;
        }
    }

    public int getGunTypeFromCharms(List<Charm> generatedCharms)
    {
        if (generatedCharms.Exists(c => c.id == "explosive"))
        {
            return 2;
        }

        if (generatedCharms.Exists(c => c.id == "charge_laser"))
        {
            return 1;
        }

        return 0;
    }

    // Helpers
    bool Has(string id) => equippedCharms.Exists(c => c.id == id);

    bool gunHas(Gun gun, string id) => gun.charms.Exists(c => c.id == id);

    void equipAllCharms(List<Charm> charms)
    {
        foreach (var c in charms)
            equippedCharms.Add(c);
    }

    void unequipAllCharms(List<Charm> charms)
    {
        foreach (var c in charms)
            equippedCharms.Remove(c);
    }

    // ----------------- Handlers -----------------

    void Update()
    {
        // Freeze (don't reset) the idle drain while a real conversation is active; barks don't count.
        if (Has("drain_ammo_idle") && !DialogueManager.isConversationActive)
        {
            drainAmmoIdleTimer += Time.deltaTime;
            if (drainAmmoIdleTimer >= 10f)
            {
                InventoryManager.instance.useBullet(PlayerController.instance.equippedGun.mType, 1);
                drainAmmoIdleTimer = 0f;
                WorldspaceCanvas.instance.popUpExclaimEmote(PlayerController.instance.transform.position + new Vector3(0f, 2f, 0f));
                PlayerController.instance.audioManager.PlaySound("Empty Magazine");
            }
        }
    }

    void handleReloadStart(reloadContext ctx)
    {
        if (Has("slower_reload"))
        {
            ctx.reloadTime *= 2f;
        }

        if (Has("no_reload"))
        {
            if (ctx.gun.bulletsInMag != 0)
            {
                ctx.failReload = true;
                WorldspaceCanvas.instance.popUpXEmote(PlayerController.instance.transform.position + new Vector3(0f, 2f, 0f));
                AudioMaster.instance.PlaySound("Kipling Error");
            }
        }

        if(Has("slow_time_reload"))
        {
            if(ctx.gun.bulletsInMag == 0)
                StartCoroutine(slowTime(1f));
        }
    }

    void handleReloadComplete(completedReloadContext ctx)
    {
        if (Has("reload_jam"))
        {
            // 10% chance to actually trigger the reload jam
            if (Random.value <= 0.1f)
            {
                ctx.failReload = true;
                WorldspaceCanvas.instance.popUpXEmote(PlayerController.instance.transform.position + new Vector3(0f, 2f, 0f));
                AudioMaster.instance.PlaySound("Kipling Error");
                return; // no other charms happen if reload actually failed
            }
        }

        if (Has("bleed_on_reload"))
        {
            // 50% chance to bleed on reload
            if (Random.value <= 0.5f)
            {
                PlayerController.instance.damageCharacter(1f, PlayerController.instance.transform.position, false, true, false, false);
            }
        }

        if (Has("teleport_gun"))
        {
            // Only start a new teleport if one is not already in progress
            if (!isTeleporting && PlayerController.instance != null)
            {
                Vector3 startPos = PlayerController.instance.transform.position;

                Vector2 moveDir = PlayerController.instance.getMovementDirection();
                Vector3 direction;
                if (moveDir != Vector2.zero)
                {
                    // Teleport along the player's current movement heading (full distance).
                    direction = (Vector3)moveDir * 5f;
                }
                else
                {
                    // Fallback: toward the mouse cursor, as before.
                    direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - startPos;
                }

                Vector3 targetPos = startPos + (direction.normalized * Mathf.Min(direction.magnitude, 5f));

                // TODO: SFX AND VFX

                // Start physics-aligned coroutine to lerp the player using MovePosition
                StartCoroutine(TeleportLerp(PlayerController.instance.gameObject,targetPos, 0.1f, true));
            }
        }

        if(Has("overload_on_reload"))
        {
            if (ctx.bulletsInChamber == 0)
                PlayerController.instance.initiateOverload(1, PlayerController.instance.transform.position);
        }

        if(Has("overshield"))
        {
            PlayerController.instance.rechargeShields(10f * ctx.bulletsReloaded); // 10 shield per bullet reloaded
        }
    }


    void handleBulletCreate(ProjectileScript projectile)
    {
        if (projectile.bulletType == 3) // do not confer special abilities to taser
            return;

        if (Has("shoots_backward"))
        {
            projectile.shootBackward();
        }

        if (Has("desync"))
        {
            projectile.desyncShot();
        }

        if (Has("fork_shot"))
        {
            projectile.changeScaleByMultiple(0.5f);

            float forkAngle = 10f;
            if (usePositiveForkAngle)
                usePositiveForkAngle = false;
            else
            {
                forkAngle *= -1f;
                usePositiveForkAngle = true;
            }

            projectile.applyAngle(forkAngle);
        }

        if (Has("tiny_bullets"))
        {
            projectile.changeScaleByMultiple(0.25f);
        }

        if (Has("multi_pellet"))
        {
            projectile.changeScaleByMultiple(0.2f);
        }

        if (Has("zigzag"))
        {
            projectile.startZigZag();
        }

        if (Has("spread_increase"))
        {
            projectile.applyRandomAngle(-20f, 20f);
        }

        if (Has("weak_pierce"))
        {
            projectile.onlyOnePierce = true;
        }

        if (Has("bonus_v_wolvolins"))
        {
            projectile.extraWolvoDmg = true;
        }

        if (Has("pierce"))
        {
            projectile.bulletType = 1;
        }

        if (Has("bounce"))
        {
            projectile.wallBounces = 3;
        }

        if (Has("big_bullets"))
        {
            projectile.changeScaleByMultiple(2f);
        }

        if (Has("knock_back"))
        {
            projectile.knockBackStrength = 2f;
            projectile.knockBackDuration = 0.3f;
        }

        if (Has("homing"))
        {
            projectile.startHoming();
        }

        if (Has("slow_time"))
        {
            projectile.slowTime();
        }

        if (Has("ignite"))
        {
            projectile.incendiaryAmmo = true;
        }

        if (Has("damage_while_bleeding"))
        {
            if (PlayerController.instance.isBleeding)
                projectile.damage *= 1.5f;
        }

        if(Has("less_dmg_healthy"))
        {
            float healthRatio = PlayerController.instance.health / PlayerController.instance.maxHealth;

            if(healthRatio > 0.75f)
            {
                projectile.damage *= 0.75f;
            }
            else if(healthRatio > 0.5f)
            {
                projectile.damage *= 0.9f;
            }
        }

        if (Has("explosive"))
        {
            projectile.changeExplodeRadius(1f);
        }
    }

    void handlePlayerHurt(damageContext ctx)
    {
        if (Has("double_damage"))
        {
            ctx.damageAmount *= 2f;
        }

        if(Has("thin_vest"))
        {
           ctx.damageAmount *= 1.5f;
        }

        if(Has("scav_tech"))
        {
            ctx.damageAmount *= 0.75f;
        }
    }

    void handleAttemptedShot(shootAttemptContext ctx)
    {
        if (Has("jam_first"))
        {
            if (ctx.gun.bulletsInMag == ctx.gun.magSize && !gunsJammedLast.Contains(ctx.gun))
            {
                ctx.additionalFireTime += 1f;
                ctx.preventShot = true;
                gunsJammedLast.Add(ctx.gun);
                WorldspaceCanvas.instance.popUpXEmote(PlayerController.instance.transform.position + new Vector3(0f, 2f, 0f));
                PlayerController.instance.audioManager.PlaySound("Empty Magazine");
                return; // no other charms happen if shot is jammed
            }
            else
            {
                gunsJammedLast.Remove(ctx.gun);
            }
        }

        // only happens if not jammed

        if (Has("slow_fire"))
        {
            ctx.additionalFireTime = ctx.gun.fireSpeed; // double the fire speed delay
        }

        if (Has("double_ammo"))
        {
            InventoryManager.instance.useBullet(ctx.gun.mType, 1);
        }

        if (Has("erratic_burst"))
        {
            int burstCount = Random.Range(0, 3);

            StartCoroutine(additionalBurstFire(burstCount, ctx.gun));
        }

        if (Has("slow_spool"))
        {
            ctx.gun.speedUpNextFire();
        }

        if(Has("triple_shot"))
        {
            StartCoroutine(additionalBurstFire(2, ctx.gun));
        }

        if(Has("auto_reload"))
        {
            if(ctx.gun.bulletsInMag == 1 && Random.Range(0f, 1f) > 0.5f) // 50% chance to auto reload on last bullet
                StartCoroutine(reloadAfterDelay(ctx));
        }
    }

    void handleGunGenerated(Gun gun) // uses gunHas so they process once - currently an issue since we create mutliple copies.
    {
        if (gunHas(gun, "reduce_mag"))
        {
            // divide magazine size by two (integer division -> rounds down)
            gun.magSize = gun.magSize / 2;

            // ensure bullets currently in mag do not exceed the new mag size
            gun.bulletsInMag = Mathf.Min(gun.bulletsInMag, gun.magSize);
        }

        if (gunHas(gun, "fork_shot"))
        {
            gun.bulletCount = gun.bulletCount * 2;
            gun.damage[0] *= 0.8f;
            gun.damage[1] *= 0.8f;

            if (gun.damage[0] < 1f)
                gun.damage[0] = 1f; // minimum 1 damage
            if (gun.damage[1] < 1f)
                gun.damage[1] = 1f; // minimum 1 damage
        }

        if (gunHas(gun, "multi_pellet"))
        {
            gun.bulletCount = gun.bulletCount * 4;
            gun.damage[0] *= 0.3f;
            gun.damage[1] *= 0.3f;
            if (gun.damage[1] < 1f)
                gun.damage[1] = 1f; // minimum 1 damage
            if (gun.damage[0] < 1f)
                gun.damage[0] = 1f; // minimum 1 damage
        }

        if (gunHas(gun, "faster_shots"))
        {
            gun.fireSpeed *= 0.75f;
        }

        if(gunHas(gun, "charge_laser"))
        {
            gun.gType = 1;
            gun.fireSpeed *= 2f;
            gun.bulletSpeed *= 3f;
            gun.range *= 3f;
            gun.magSize = (int)((float)gun.magSize/0.5f);
        }

        if(gunHas(gun, "explosive"))
        {
            gun.gType = 2;
            gun.bulletSpeed *= 0.75f;
            gun.fireSpeed *= 1.25f;
            gun.aoeSize = 1f;
        }

        if (gunHas(gun, "self_recoil"))
        {
            gun.kickBackAmount += 1f;
        }
    }

    void handleEnemyKilled()
    {
        if(Has("ammo_recycler"))
        {
            InventoryManager.instance.addBullet(PlayerController.instance.equippedGun.mType, 2);
        }

        if(Has("stop_bleed_on_kill"))
        {
            PlayerController.instance.heal(0f, -1); 
        }
    }

    // Combat entry/exit drive the bulk_plate slow. These must NOT read enemiesInCombat:
    // GameEvents.aggro() invokes onAggro before incrementing the counter, so the event
    // itself is the only reliable signal that combat just started.
    void handleAggro()   { bulkPlateCombatActive = true;  updateBulkPlateSlow(); }
    void handleUnaggro() { bulkPlateCombatActive = false; updateBulkPlateSlow(); }

    // Idempotent: applies/removes the slow at most once based on (equipped && in combat).
    // Called from combat events and from chest equip/unequip so any order resolves correctly.
    void updateBulkPlateSlow()
    {
        if (PlayerController.instance == null)
            return;

        bool shouldSlow = bulkPlateCombatActive && Has("bulk_plate");

        if (shouldSlow && !bulkPlateSlowApplied)
        {
            PlayerController.instance.baseSpeed -= BULK_PLATE_SLOW;
            bulkPlateSlowApplied = true;
        }
        else if (!shouldSlow && bulkPlateSlowApplied)
        {
            PlayerController.instance.baseSpeed += BULK_PLATE_SLOW;
            bulkPlateSlowApplied = false;
        }
    }

    void handleRechargeStart()
    {
        if(Has("sparking_circuitry"))
        {
            // Only punish recharging from fully-depleted shields, not partial top-ups.
            if (PlayerController.instance.shields <= 0f)
                PlayerController.instance.damageCharacter(3f, PlayerController.instance.transform.position, true, true, true, false);
        }
    }

    void handleFireDamageTaken(damageContext ctx)
    {
        if(Has("cool_fans"))
        {
            ctx.damageAmount *= 0.5f;
        }
    }

    void handleShieldsBroken()
    {
        if(Has("stim_injector"))
        {
            PlayerController.instance.heal(10f, -1, false);
        }

        if(Has("overload_break"))
        {
            PlayerController.instance.initiateOverload(1, PlayerController.instance.transform.position);
        }
    }

    void handleSlideStart()
    {
        if(Has("pulse_sync"))
        {
            PlayerController.instance.rechargeShields(0.2f);
        }

        if(Has("fire_jet"))
        {
            Instantiate(fireBombSlideObj, PlayerController.instance.transform.position, Quaternion.identity);
        }
    }

    // ----------------- Equip / Unequip -----------------
    void handleGunEquip(Gun gun)
    {
        equipAllCharms(gun.charms);

        
    }

    void handleGunUnequip(Gun gun)
    {
        unequipAllCharms(gun.charms);
    }

    void handleChestUnequip(Armor chest)
    {
        if (Has("reinforcement_lattice"))
        {
            PlayerController.instance.slideSpeed += 1f;
        }

        if (Has("compliance_cells"))
        {
            PlayerController.instance.shieldRechargeSpeed += 0.05f;
        }

        if (Has("shatter_ceramic"))
        {
            PlayerController.instance.bleedAmount -= 2f;
        }

        if (Has("rigged_capacitor"))
        {
            PlayerController.instance.shieldRechargeCooldown *= 2f;
        }

        if (Has("flexi_belt"))
        {
            PlayerController.instance.slideSpeed -= 0.5f;
        }

        if (Has("servo_stabilizer"))
        {
            PlayerController.instance.swayModifier += 0.1f;
        }

        if (Has("nanite_patchwork"))
        {
            PlayerController.instance.bleedCooldown -= 2f;
        }

        unequipAllCharms(chest.charms);

        // bulk_plate now removed from the set; recompute to lift any active slow.
        updateBulkPlateSlow();
    }

    void handleChestEquip(Armor chest)
    {
        equipAllCharms(chest.charms);

        // bulk_plate slow is combat-gated; recompute now that the charm set changed.
        updateBulkPlateSlow();

        if(Has("reinforcement_lattice"))
        {
            PlayerController.instance.slideSpeed -= 1f;
        }

        if(Has("compliance_cells"))
        {
            PlayerController.instance.shieldRechargeSpeed -= 0.05f; 
        }

        if(Has("shatter_ceramic"))
        {
            PlayerController.instance.bleedAmount += 2f;
        }

        if(Has("rigged_capacitor"))
        {
            PlayerController.instance.shieldRechargeCooldown *= 0.5f;
        }

        if(Has("flexi_belt"))
        {
            PlayerController.instance.slideSpeed += 0.5f;
        }

        if (Has("servo_stabilizer"))
        {
            PlayerController.instance.swayModifier -= 0.1f;
        }

        if(Has("nanite_patchwork"))
        {
            PlayerController.instance.bleedCooldown += 2f;
        }
    }

    // ----------------- Coroutines -----------------

    IEnumerator additionalBurstFire(int count, Gun gun)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(0.1f);

            if (gun.bulletsInMag > 0)
            {
                PlayerController.instance.createBullets();
                PlayerController.instance.nextFire = Time.time + gun.fireSpeed;
            }
        }
    }

    public IEnumerator TeleportLerp(GameObject thingToMove, Vector3 targetPos, float duration, bool useTeleportFX = false)
    {
        if (thingToMove == null)
            yield break;

        isTeleporting = true;

        if (useTeleportFX)
        {
            teleportationParticles.transform.position = thingToMove.transform.position;
            teleportationParticles.GetComponent<ParticleSystem>().Play();
            AudioMaster.instance.PlaySound("Teleport");
        }

        Vector3 startPos = thingToMove.transform.position;
        float elapsed = 0f;

        Rigidbody2D rb2D = thingToMove.gameObject.GetComponent<Rigidbody2D>();
        if (rb2D != null && rb2D.bodyType == RigidbodyType2D.Static)
        {
            isTeleporting = false;
            yield break;
        }

        bool canMoveWithRigidbody = rb2D != null && rb2D.bodyType != RigidbodyType2D.Static;

        // Use FixedUpdate aligned stepping so collisions/physics are respected
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);

            if (canMoveWithRigidbody)
            {
                rb2D.MovePosition(newPos);
            }
            else if (rb2D == null)
            {
                // Fallback if no rigidbody exists (less physical but still a lerp)
                thingToMove.transform.position = newPos;
            }

            if(useTeleportFX)
                teleportationParticles.transform.position = thingToMove.transform.position;

            yield return new WaitForFixedUpdate();
        }

        // Ensure final position set
        if (canMoveWithRigidbody)
            rb2D.MovePosition(targetPos);
        else if (rb2D == null)
            thingToMove.transform.position = targetPos;

        isTeleporting = false;
    }

    IEnumerator slowTime(float duration)
    {
        Time.timeScale = 0.5f;
        yield return new WaitForSeconds(duration);
        Time.timeScale = 1f;
    }

    IEnumerator reloadAfterDelay(shootAttemptContext ctxShoot)
    {
        yield return null; // wait a frame to avoid conflicts

        completedReloadContext ctxReload = new completedReloadContext();
        GameEvents.current.reloadComplete(ctxReload);

        if(!ctxReload.failReload)
            InventoryManager.instance.reload(ctxShoot.gun.gType);
    }

}
// ----------------- Context Classes -----------------

public sealed class damageContext
{
    public float damageAmount;
}

public sealed class reloadContext
{
    public float reloadTime;
    public bool failReload = false;
    public Gun gun;
}

public sealed class completedReloadContext
{
    public bool failReload = false;
    public int bulletsReloaded = 0;
    public int bulletsInChamber = 0;
}

public sealed class shootAttemptContext
{
    public float additionalFireTime = 0f;
    public bool preventShot = false;
    public Gun gun;
}
