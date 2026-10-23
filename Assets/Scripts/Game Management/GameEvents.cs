using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/* 
 * THIS SCRIPT IS USED FOR SENDING MESSAGES TO DOWNSTREAM OBJECTS WHEN ANOTHER OBJECT TRIGGERS AN EVENT.
 * IT ALSO HANDLES CERTAIN DATA STRUCTURES DEPENDENT ENTIRELY ON THESE EVENTS, SUCH AS ENEMIES IN COMBAT.
 */

public class GameEvents : MonoBehaviour
{
    [System.NonSerialized]
    public static GameEvents current;

    [System.NonSerialized]
    public int enemiesInCombat = 0;
    private bool isLowHealthState = false;
    private bool isLowAmmoState = false;

    void Awake()
    {
        if (current == null && this != current)
            current = this;
        else
            Destroy(this);
    }

    // Actions
    public event Action beginInfestation;
    public void infestationBegin()
    {
        beginInfestation?.Invoke();
        EventManager.instance.startTempest();
        AudioMaster.instance.PlayMusic("Infestation", 0.5f);
        LightWeatherManager.instance.beginTempestWeather();
    }

    public event Action endInfestation;
    public void infestationEnd()
    {
        endInfestation?.Invoke();
        EventManager.instance.endTempest();
        AudioMaster.instance.stopMusicAndResumeAmbient();
        LightWeatherManager.instance.endTempestWeather();
    }

    // Guarded variant: endInfestation listeners aren't all idempotent (e.g. toggle-style
    // handlers flip state in the wrong direction if fired when no tempest is active).
    public void tryEndInfestation()
    {
        if (EventManager.instance.isTempestActive())
            infestationEnd();
    }

    public event Action beginCamping;
    public void campingBegin()
    {
        beginCamping?.Invoke();
    }

    public event Action onPlayerHeal;
    public void playerHeal()
    {
        checkLowHealth();
        onPlayerHeal?.Invoke();
    }

    public event Action onPlayerDeath;
    public void playerDeath()
    {
        onPlayerDeath?.Invoke();
    }

    public event Action<bool> signalLowHealth;
    public void checkLowHealth()
    {
        if (!isLowHealthState && PlayerController.instance.health < 30f)
        {
            isLowHealthState = true;
            signalLowHealth?.Invoke(true);
        }
        else if(isLowHealthState && PlayerController.instance.health >= 33f)
        {
            isLowHealthState = false;
            signalLowHealth?.Invoke(false);
        }
    }

    public event Action<bool> signalLowAmmo;
    public void onAmmoChange(int remainingBullets)
    {
        if (!isLowAmmoState && remainingBullets < 12)
        {
            isLowAmmoState = true;
            signalLowAmmo?.Invoke(true);
        }
        else if(isLowAmmoState && remainingBullets > 12)
        {
            isLowAmmoState = false;
            signalLowAmmo?.Invoke(false);
        }
    }

    public event Action onAggro;
    public void aggro(int faction)
    {
        if (enemiesInCombat == 0)
        {
            AudioMaster.instance.PlayMusic(getFactionCombatSong(faction), 2.0f);
            MouseManager.instance.swapReticle(true);
            onAggro?.Invoke();
        }
        enemiesInCombat++;
        
    }

    public event Action onUnaggro;
    public void unaggro(int faction)
    {
        enemiesInCombat--;
        if (enemiesInCombat <= 0)
        {
            AudioMaster.instance.StopMusic(getFactionCombatSong(faction), 0.2f);
            AudioMaster.instance.playAmbient(); 
            enemiesInCombat = 0;
            MouseManager.instance.swapReticle(false);
            onUnaggro?.Invoke();
        }
    }

    public event Action onReturnToTitle;
    public void returnToTitle()
    {
        onReturnToTitle?.Invoke();
    }

    public event Action onNoteRecordingPickup;
    public void noteRecordingPickup()
    {
        onNoteRecordingPickup?.Invoke();
    }

    public event Action onPreviousSceneDataLoaded;
    public void previousSceneDataLoaded()
    {
        checkLowHealth();
        Application.targetFrameRate = 60; // Or match your monitor's refresh rate
        onPreviousSceneDataLoaded?.Invoke();
    }

    public event Action onSceneTransitionComplete;
    public void sceneTransitionComplete()
    {
        onSceneTransitionComplete?.Invoke();
    }

    public event Action onSaveStarted;
    public void saveStarted()
    {
        onSaveStarted?.Invoke();
    }

    public event Action onAutosaveIndicatorRequested;
    public void autosaveIndicatorRequested()
    {
        onAutosaveIndicatorRequested?.Invoke();
    }

    public event Action onMenuClosed;
    public void menuClosed()
    {
        onMenuClosed?.Invoke();
    }

    // CHARM TRIGGER ACTIONS

    // Guns
    public event Action<Gun> onGunEquip;
    public void gunEquip(Gun gun)
    {
        if(gun == null)
        {
            return;
        }
        onGunEquip?.Invoke(gun);
    }

    public event Action<Gun> onGunUnequip;
    public void gunUnequip(Gun gun)
    {
        if (gun == null)
        {
            return;
        }
        onGunUnequip?.Invoke(gun);
    }

    public event Action<reloadContext> onReloadStart;
    public void reloadStart(reloadContext ctx)
    {
        onReloadStart?.Invoke(ctx);
    }

    public event Action<completedReloadContext> onReloadComplete;
    public void reloadComplete(completedReloadContext ctx)
    {
        onReloadComplete?.Invoke(ctx);
    }

    public event Action<shootAttemptContext> onShotAttempt; 
    public void attemptingShot(shootAttemptContext ctx)
    {
        onShotAttempt?.Invoke(ctx);
    }

    public event Action<ProjectileScript> onBulletCreate;
    public void bulletCreate(ProjectileScript context) // ideally we can get rid of the need for things like on bullet impact and on kill by telling the bullet what to do on shooting it
    {
        onBulletCreate?.Invoke(context);
    }

    public event Action<damageContext> onPlayerHurt;
    public void playerHurt(damageContext ctx)
    {
        checkLowHealth();
        onPlayerHurt?.Invoke(ctx);
    }

    public event Action<Gun> onGunCreate;
    public void gunCreate(Gun gun)
    {
        onGunCreate?.Invoke(gun);
    }

    public event Action onEnemyKilled;
    public void enemyKilled()
    {
        onEnemyKilled?.Invoke();
    }

    // Armor
    public event Action<Armor> onChestEquip;
    public void chestEquip(Armor armor)
    {
        if (armor == null)
            return;
        onChestEquip?.Invoke(armor);
    }

    public event Action<Armor> onChestUnequip;
    public void chestUnequip(Armor armor)
    {
        if (armor == null)
            return;
        onChestUnequip?.Invoke(armor);
    }

    public event Action onRechargeStart;
    public void rechargeStart()
    {
        onRechargeStart?.Invoke();
    }

    public event Action<damageContext> onFireDamageTaken;
    public void fireDamageTaken(damageContext ctx)
    {
        onFireDamageTaken?.Invoke(ctx);
    }

    public event Action onShieldsBroken; // to do: how should we balance this with 'onPlayerHurt', which is health? what scenarios do we need to account for
    public void shieldsBroken()
    {
        onShieldsBroken?.Invoke();
    }

    public event Action onSlideStart;
    public void slideStart()
    {
        onSlideStart?.Invoke();
    }

    // Notable Outcomes
    public event Action<string, string> onOutcomeRecorded;
    public void outcomeRecorded(string outcomeId, string outcomeValue)
    {
        onOutcomeRecorded?.Invoke(outcomeId, outcomeValue);
    }

    // Helper Functions -----------------------------------------
    private string getFactionCombatSong(int faction)
    {
        if (faction != 7 && faction != 6)
            return "Hostile Lifeforms";
        else
            return "Anarchy";
    }
}

