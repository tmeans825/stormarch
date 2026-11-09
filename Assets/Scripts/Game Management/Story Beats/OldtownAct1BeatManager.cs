using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine.Experimental.Rendering.Universal;

public class OldtownAct1BeatManager : sceneBeatManager
{
    [ES3NonSerializable]
    public NonCombatChar owen;
    [ES3NonSerializable]
    public Vector3 townSquareTarget;
    [ES3NonSerializable]
    public NonCombatChar[] factoryWorkersWalking;
    [ES3NonSerializable]
    public NonCombatChar runningNull;
    [ES3NonSerializable]
    public GameObject mainNull;
    [ES3NonSerializable]
    public GameObject factoryDoor;
    [ES3NonSerializable]
    public GameObject factoryInterior;
    [ES3NonSerializable]
    public GameObject[] executionItemsToEnable;
    [ES3NonSerializable]
    public GameObject smokeBombParticles;
    [ES3NonSerializable]
    public Light2D globalLight;
    [ES3NonSerializable]
    public GameObject wolfDenDoor;
    [ES3NonSerializable]
    public SPSBehavior2 safetyProtocolSystem;
    [ES3NonSerializable]
    public Bridge rheaBridge;
    [ES3NonSerializable]
    public GameObject resumeCampDiscussionDialogue;
    [ES3NonSerializable]
    public float factoryEscapeSeconds = 120f;
    [ES3NonSerializable]
    public GameObject factoryExitTrigger;
    [ES3NonSerializable]
    [Tooltip("Autosaved objects the bomb turns on. Forced back off on load until the player escapes.")]
    public GameObject[] factoryBombOnlyObjects;
    [ES3NonSerializable]
    public ProCamera2DCinematics factoryEscapeCinematic;
    [ES3NonSerializable]
    public ExplosionBarrage factoryExplosions;
    [ES3NonSerializable]
    public GameObject factorySmoke;
    [ES3NonSerializable]
    public NpcResponderBurst[] factoryResponders;
    [ES3NonSerializable]
    public PolygonCollider2D weaponAllowedZone;

    private static int wolvesToFind = 3;
    private static Vector3 positionByElevator = new Vector3(194.6f, -4.67f, 0f);
    private static Vector3 elevatorLocation = new Vector3(199.39f, 39.08f);

    private const string ESCAPE_BANNER = "ESCAPE!";
    private const string ESCAPE_MUSIC = "Firing_Squad_Loop";
    private const float DETONATION_RADIUS = 8f;
    private const int DETONATION_EXPLOSIONS = 12;


    protected override void Start()
    {
        base.Start();
    }

    protected override void initializeData()
    {
        beatMatrix = new int[5];

        // Beat line 0 - Main beats:
        // 1 -> have found all the wolves
        // 2 -> has been pointed to the factory
        // 3 -> factory has blown up
        // 4 -> Passport in hand
        // 5 -> Bridge extended
        // 6 -> Entered New Rhea
        // 7 -> Talked to the Dr.
        // 8 -> Separated from Owen

        // Beat line 1 - Hans
        // 1 -> executed

        // Beat line 2 - track found wolves
        // 1 -> 1
        // 2 -> 2
        // 3 -> All found

        // Beat line 3 - Owen & Camp
        // 1 => Player has radio
        // 2 => Player has called down the camp

        // Beat line 4 - Alers Industrial contact
        // 1 => Player has access to hideout

    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();
        PlayerController.instance.hasBaton = true;

        PlayerController.instance.changeHairSprites("hair_light_tier1_blue");
        // Soundtrack + City Sound come from AudioMaster's scene ambient and SubLocationManager.

        // The zone's triggers only fire on crossing its edge, so a load outside it would never lock guns.
        PlayerController.instance.weaponsAllowed = weaponAllowedZone.OverlapPoint(PlayerController.instance.transform.position);

        StartCoroutine(initiateWalkingFactoryWorkerLoop());
    }

    protected override void loadBeats()
    {
        
        if(getBeatProgress(0) == 1)
        {
            allWolvesFound();
        }
        if(getBeatProgress(0) >= 2)
        {
            mainNull.SetActive(true);
        }
        if (getBeatProgress(0) == 2)
        {
            factoryTaskGiven();
            resetFactoryBomb();
        }
        if(getBeatProgress(0) >= 3)
        {
            wolfDenDoor.SetActive(false);
            factoryInterior.SetActive(false);
            factorySmoke.SetActive(true);
            foreach (NpcResponderBurst burst in factoryResponders)
            {
                burst.spawnAtRally();
            }
        }
        if(getBeatProgress(0) >= 4)
        {
            safetyProtocolSystem.turnRed = false;
        }
        if(getBeatProgress(0) >= 5)
        {
            rheaBridge.forcePowerOn();
        }


        if(getBeatProgress(1) == 1)
        {
            foreach (GameObject item in executionItemsToEnable)
            {
                item.SetActive(true);
            }
        }


        // Owen location
        if(getBeatProgress(0) >= 2 && getBeatProgress(0) < 5)
        {
            owen.transform.position = positionByElevator;
        }
        else if (getBeatProgress(0) <= 7)
        {
            owen.transform.position = PlayerController.instance.transform.position + new Vector3(2f, 0f, 0f);
            owen.startFollowingPlayer();
        }
        else
        {
            owen.gameObject.SetActive(false);
        }
    }
    public override void debugProgressQuests()
    {
        QuestLog.StartQuest("Where is Hank Alers?");
        QuestLog.SetQuestEntryState("Where is Hank Alers?", 1, "active");
        QuestLog.SetQuestEntryState("Where is Hank Alers?", 2, "active");
    }

    // EVENTS
    public void calledCamp()
    {
        if(beatMatrix[3] == 1)
        {
            progressBeat(3);
            resumeCampDiscussionDialogue.SetActive(true);
        }
    }

    public void OwenRunToCamp()
    {
        owen.stopFollowingPlayer();
        owen.startMoving(new Vector2(-30.99f, -17.25f));
    }

    public void resumeOwenFollowing()
    {
        owen.startFollowingPlayer();
    }   

    public void executeHansStoryBeat()
    {
        PlayerPrefs.SetInt("Beams On?", 1);
        progressBeat(1);
    }

    public void wolfFound()
    {
        progressBeat(2);

        if (getBeatProgress(2) == wolvesToFind)
        {
            allWolvesFound();
            progressBeat(0);
        }
    }


    public void spyFlee(GameObject spy)
    {
        spy.GetComponent<NonCombatChar>().startMoving(new Vector3(-25.6f, 29.1f, 0f));
    }

    public void runToWolfDen(NonCombatChar character)
    {
        character.stopFollowingPlayer();
        Instantiate(smokeBombParticles, character.transform.position, Quaternion.identity);
        StartCoroutine(smokebombFlash());
        character.gameObject.SetActive(false);
        mainNull.SetActive(true);
    }

    public void runToPlayer(NonCombatChar character)
    {
        character.startMoving(PlayerController.instance.transform.position);
    }

    public void triggerPickPocket(NonCombatChar guardian)
    {
        guardian.startMoving(new Vector3(-17.89f, 50.2f));
    }

    public void allWolvesFound()
    {
        runningNull.gameObject.SetActive(true);
        Instantiate(smokeBombParticles, runningNull.transform.position, Quaternion.identity);
        runningNull.startFollowingPlayer();
    }

    public void factoryTaskGiven()
    {
        factoryDoor.SetActive(false);
        factoryInterior.SetActive(true);
    }

    public void startFactoryBomb()
    {
        if (getBeatProgress(0) != 2 || MissionCountdown.isRunning)
            return;

        AudioMaster.instance.PlaySound("Alarm");
        MissionCountdown.instance.startCountdown(ESCAPE_BANNER, factoryEscapeSeconds, onFactoryTimerExpired, ESCAPE_MUSIC);
    }

    private void onFactoryTimerExpired()
    {
        factoryExitTrigger.SetActive(false); // the corpse can slide, and must not finish the mission
        factoryExplosions.playAround(PlayerController.instance.transform.position, DETONATION_RADIUS, DETONATION_EXPLOSIONS);
        PlayerController.instance.killInstantly(PlayerController.instance.transform.position); // same frame: saves reopen once the countdown ends
    }

    public void factoryEscaped()
    {
        MissionCountdown.instance.stopCountdown();
        PlayerController.instance.freezeChar(true);
        foreach (NonCombatChar worker in factoryWorkersWalking)
        {
            EventManager.instance.endLoopingPath(worker);
        }

        factoryExplosions.playInArea(startFactoryAftermath);
        factoryEscapeCinematic.Play();
    }

    private void startFactoryAftermath()
    {
        factorySmoke.SetActive(true);
        foreach (NpcResponderBurst burst in factoryResponders)
        {
            burst.spawnAndRun();
        }
    }

    public void onFactoryEscapeCinematicFinished()
    {
        PlayerController.instance.freezeChar(false);
        SaveManager.instance.saveSceneData();
    }

    public void throwSmokeBomb(GameObject spawnerObj)
    {
        Instantiate(smokeBombParticles, spawnerObj.transform.position, Quaternion.identity);
        StartCoroutine(smokebombFlash());
        spawnerObj.SetActive(false);
    }

    public void runToElevator(NonCombatChar character)
    {
        character.stopFollowingPlayer();
        character.startMoving(positionByElevator);
    }

    public void receivePassport()
    {
        safetyProtocolSystem.turnRed = false;
        progressBeat(0);
    }

    public void allowWeapons(bool input)
    {
        PlayerController.instance.weaponsAllowed = input;
    }

    public void extendRheaBridge()
    {
        rheaBridge.forcePowerOn();
    }

    public void discoverNewRhea()
    {
        LocationDiscoveryManager.instance.discoverLocation("NEW RHEA");
    }

    public void owenLeaves()
    {
        owen.stopFollowingPlayer();
        owen.startMoving(new Vector3(306.7f, 388.4f));
        progressBeat(0);
    }

    // HELPER
    // The death save runs with the bomb chain live, and these objects autosave their active state.
    private void resetFactoryBomb()
    {
        foreach (GameObject item in factoryBombOnlyObjects)
        {
            item.SetActive(false);
        }
    }

    private IEnumerator initiateWalkingFactoryWorkerLoop()
    {
        foreach (NonCombatChar character in factoryWorkersWalking)
        {
            if (getBeatProgress(0) >= 3) // factory's gone
            {
                character.gameObject.SetActive(false);
                continue;
            }

            EventManager.instance.startLoopingPath(character, new Vector3(Random.Range(-15f,-17f), Random.Range(-36f,-38f), 0f), 30f);
            yield return new WaitForSeconds(5f);
        }
    }

    private IEnumerator smokebombFlash()
    {
        AudioMaster.instance.PlaySound("Explosion");
        float timeElapsed = 0f;
        float duration = 0.25f;
        float originalIntensity = globalLight.intensity;
        globalLight.intensity = 10f;

        while (timeElapsed < duration)
        {
            globalLight.intensity = 10f - ((10f - originalIntensity) * (timeElapsed / duration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        globalLight.intensity = originalIntensity;
    }
}
