using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using UnityEngine.Experimental.Rendering.Universal;
using Com.LuisPedroFonseca.ProCamera2D;

public class OwensAct1BeatManager : sceneBeatManager
{
    [ES3NonSerializable]
    public GameObject storyOwen;
    [ES3NonSerializable]
    public GameObject melina;
    [ES3NonSerializable]
    public GameObject homesteadDoor;
    [ES3NonSerializable]
    public GameObject guardian;
    [ES3NonSerializable]
    public GameObject shotBlocker;
    [ES3NonSerializable]
    public DialogueSystemTrigger melinaConfrontationTrigger;
    [ES3NonSerializable]
    public GameObject combatMelina;
    [ES3NonSerializable]
    public GameObject preDreamMilo;
    [ES3NonSerializable]
    public GameObject dreamFadeTrigger;
    [ES3NonSerializable]
    public Light2D universalLight;
    [ES3NonSerializable]
    public GameObject miloWakesYouUpTrigger;
    [ES3NonSerializable]
    public GameObject fourthChair;
    [ES3NonSerializable]
    public GameObject hoverBike;
    [ES3NonSerializable]
    public GameObject owenNewRheaTrigger;
    [ES3NonSerializable]
    public GameObject milo;


    protected override void initializeData()
    {
        beatMatrix = new int[2];

        // Beat line 0 - Owen Welcome:
        // 1 => Owen started running back to house
        // 2 => Melina conversation done, collect crates, explore
        // 3 => Milo is actively in trouble
        // 4 => Guardian is dead
        // 5 => Player has entered dream world
        // 6 => Player is fully woken up, ready to go.

        // Beat line 1 - Melina's injury
        // 1 => Melina is hurt

    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();
        PlayerController.instance.hasBaton = true;
    }

    protected override void loadBeats()
    {
        if (shouldSkipBeats())
            beatMatrix[0] = 5;

        if (beatMatrix[0] >= 1)
            storyOwen.transform.position = new Vector3(22.2f, -24.33f, 0f);

        if(beatMatrix[0] >= 3)
            homesteadDoor.SetActive(false);

        if (beatMatrix[0] >= 4)
        {
            shotBlocker.SetActive(false);
            storyOwen.transform.position = new Vector3(14.05f, -14.6f);

            if(beatMatrix[1] == 1) // Melina hurt
            {
                melina.SetActive(true);
                melina.transform.position = new Vector3(12.27f, -14.42f);
                melina.GetComponent<Character>().faceObject(Vector2.right, false);
            }
            else
            {
                melina.SetActive(true);
                melina.transform.position = new Vector3(12.37f, -16.42f);
            }

            preDreamMilo.transform.position = new Vector3(9.24f, -15.63f);
            preDreamMilo.SetActive(true);
        }

        if(beatMatrix[0] == 5)
        {
            owenNewRheaTrigger.SetActive(true);
            miloWakesYouUpTrigger.SetActive(true);
            progressBeat(0);
        }

        if(beatMatrix[0] >= 5)
        {
            preDreamMilo.SetActive(false);
            milo.SetActive(true);
            milo.transform.position = new Vector3(30.5f, -17f);
            storyOwen.transform.position = new Vector3(21f, -16.42f);
            PlayerController.instance.transform.position = new Vector3(29.76f, -14.5f);

            storyOwen.GetComponent<DialogueSystemTrigger>().conversation = "Homestead/Act 1/Owen/Intro 2";

            hoverBike.transform.position = new Vector3(20.4f, -25.1f);

            BoxCollider2D[] allBoxColls = hoverBike.GetComponents<BoxCollider2D>();
            foreach (BoxCollider2D boxColl in allBoxColls)
                boxColl.enabled = true;

            fourthChair.SetActive(true);
        }
    }

    // EVENTS
    public void startOwenWalking()
    {
        storyOwen.GetComponent<NonCombatChar>().startMovingWaitingForChar(new Vector3(22.2f, -24.33f, 0f));
        progressBeat(0);
    }

    public void startMiloIssues()
    {
        if (beatMatrix[0] != 2)
            return;

        homesteadDoor.SetActive(false);
        AstarPath.active.UpdateGraphs(homesteadDoor.GetComponent<BoxCollider2D>().bounds);

        progressBeat(0);
        AudioMaster.instance.PlayMusic("Firing_Squad_Loop", 1f);
    }

    public void deployToMilo()
    {
        guardian.transform.position = new Vector3(0.7f, 10.05f, 0f);
        guardian.GetComponent<SoldierBehavior>().resetArrestAnim();
        PlayerController.instance.canSave = false;
        storyOwen.GetComponent<NonCombatChar>().startMoving(new Vector3(-3.66f, 4.77f));
        melina.GetComponent<NonCombatChar>().startMoving(new Vector3(3.51f, 3.76f));
    }

    public void saveMiloDecisionTimer()
    {
        StartCoroutine(beginGuardianCountdown());
    }

    private IEnumerator beginGuardianCountdown()
    {
        yield return new WaitForSeconds(12f);

        progressBeat(1); // melina death
        melinaConfrontationTrigger.OnUse();
        melina.GetComponent<NonCombatChar>().startMoving(new Vector3(2.87f, 9.88f));
    }

    public void guardianShootsMelina()
    {
        guardian.GetComponent<Enemy>().shoot(melina.transform.position, 0);
        combatMelina.transform.position = melina.transform.position;
        combatMelina.SetActive(true);
        melina.SetActive(false);
        guardian.GetComponent<AudioManager>().PlaySound("Trad_Pistol_SFX");
        StartCoroutine(killMelina());
    }

    private IEnumerator killMelina()
    {
        yield return new WaitForSeconds(0.1f);
        combatMelina.GetComponent<Enemy>().damageCharacter(1, combatMelina.transform.position, false, true, true, false);
        PlayerController.instance.updateAlignment(0, 10);
    }

    public void guardianAggros()
    {
        guardian.GetComponent<Enemy>().isDocile = false;
        StartCoroutine(delayAggro());
    }

    private IEnumerator delayAggro()
    {
        yield return new WaitForSeconds(0.5f);
        guardian.GetComponent<Enemy>().aggro();
    }

    public void guardianJustDied()
    {
        StopAllCoroutines();

        progressBeat(0);
        PlayerController.instance.canSave = true;
        AudioMaster.instance.PlayMusic("Firing_Squad_Final", 1f, 1f);

        if(beatMatrix[1] != 1) // melina not hurt
        {
            PlayerController.instance.updateAlignment(0, -10);
        }
    }

    public void guardianFinishedDeath()
    {
        storyOwen.GetComponent<NonCombatChar>().startMoving(new Vector3(-1.85f, 9.09f));

        if(getBeatProgress(1) != 1)
            melina.GetComponent<NonCombatChar>().startMoving(new Vector3(1.23f, 8.9f));
    }

    public void teleportToHouse()
    {
        // fade in black first
        CanvasMaster.instance.cutToBlack();

        StartCoroutine(teleportCoroutine());

        progressBeat(0); // move to beat 5
    }

    private IEnumerator teleportCoroutine()
    {
        yield return new WaitForSeconds(0.6f); // wait for fade
        combatMelina.SetActive(false);
        storyOwen.transform.position = new Vector3(14.05f, -14.6f);
        melina.SetActive(true);
        melina.transform.position = new Vector3(12.27f, -14.42f);
        melina.GetComponent<Character>().faceObject(Vector2.right, false);
        preDreamMilo.transform.position = new Vector3(9.24f, -15.63f);
        PlayerController.instance.transform.position = new Vector3(13.69f, -17.35f);
    }

    public void goToBed()
    {
        PlayerController.instance.freezeChar(true);
        CanvasMaster.instance.cutToBlack();
        StartCoroutine(bedCoroutine());
    }

    private IEnumerator bedCoroutine()
    {
        yield return new WaitForSeconds(0.6f); // wait for fade
        
        PlayerController.instance.transform.position = new Vector3(29.6f, -14.72f);
        preDreamMilo.transform.position = new Vector3(11.18f, -14.03f);
        melina.transform.position = new Vector3(12.27f, -14.42f);
        melina.GetComponent<NonCombatChar>().faceObject(Vector2.right, false);
        universalLight.intensity = 0.1f;
        progressBeat(0);

        SaveManager.instance.saveSceneData(); // Save bed position, beat

        PlayerController.instance.hasKiplingAccess(false);
        PlayerController.instance.faceObject(Vector2.left, false);

        dreamFadeTrigger.SetActive(true); // execute scene events

        storyOwen.transform.position = new Vector3(35.03f, -17.87f);
        storyOwen.GetComponent<NonCombatChar>().faceObject(Vector2.right, false);


        EventManager.instance.startRain(0.05f);

        yield return new WaitForSeconds(1f);

        LightWeatherManager.instance.triggerLightning();

        yield return new WaitForSeconds(3.5f);

        AudioMaster.instance.PlayMusic("Owen_Flute", 1);
    }

    public void goToDreamworld()
    {
        StartCoroutine(waitThenTransitionScenesToDreamWorld());
    }

    private IEnumerator waitThenTransitionScenesToDreamWorld()
    {
        yield return new WaitForSeconds(3f);
        
        LevelLoader.instance.changeScene("???");

    }

}
