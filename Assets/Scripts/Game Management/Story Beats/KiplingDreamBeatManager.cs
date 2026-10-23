using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class KiplingDreamBeatManager : sceneBeatManager
{
    [ES3NonSerializable]
    public GameObject globalLight;
    [ES3NonSerializable]
    public GameObject littleRoomLight;
    [ES3NonSerializable]
    public Volume postProcessing;
    [ES3NonSerializable]
    public NonCombatChar anglerhare;
    [ES3NonSerializable]
    public GameObject babyKipling;
    [ES3NonSerializable]
    public NonCombatChar wolvo;
    [ES3NonSerializable]
    public Critter deadAnglerhare;
    [ES3NonSerializable]
    public GameObject combatWolvolin;
    [ES3NonSerializable]
    public GameObject redSpotlight;
    [ES3NonSerializable]
    public GameObject finalCustceneLight;

    protected override void initializeData()
    {
        beatMatrix = new int[1];

        // Beat line 0 - Main beat line
        // 1 -> Have left the house
        // 2 -> Anglerhare is in
        // 3 -> Wolovolin is in
        // 4 -> wolvo attacks anglerhare
        // 5 -> final cutscene begins

        // Beat line 1 - xxx

    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();
        PlayerController.instance.hasKiplingAccess(false);
        PlayerController.instance.hasBaton = true;
    }

    protected override void loadBeats()
    {
        if (beatMatrix[0] >= 1 || shouldSkipBeats())
        {           
            globalLight.SetActive(true);
            littleRoomLight.SetActive(false);
            postProcessing.enabled = true;

            if (beatMatrix[0] < 3)
            {
                AudioMaster.instance.PlayMusic("Dreamscape", 0.5f);
            }
        }

        if(beatMatrix[0] > 1)
        {
            // move anglerhare
            anglerhare.gameObject.SetActive(true);
            anglerhare.transform.position = new Vector3(-42.59f, 51.26f);
        }

        if(beatMatrix[0] == 3)
        {
            AudioMaster.instance.PlayMusic("Night Terror", 0.9f);
            babyKipling.GetComponent<Animator>().Play("ShakeBabyKip");
            wolvo.gameObject.SetActive(true);
            wolvo.transform.position = new Vector3(-41.91f, 49.98f);
        }
        else if(beatMatrix[0] == 4)
        {
            combatWolvolin.SetActive(true);
            AudioMaster.instance.PlayMusic("Night Terror", 0.9f);
        }
    }

    // EVENTS
    public void exitHouse()
    {
        AudioMaster.instance.PlaySound("Metal Slam");
        AudioMaster.instance.PlayMusic("Dreamscape", 0.5f);
        beatMatrix[0]++;
    }

    public void moveAnglerhareToKip()
    {
        anglerhare.startMoving(babyKipling.transform.position);
        beatMatrix[0]++;
    }

    public void moveWolvoIn()
    {
        wolvo.startMoving(anglerhare.transform.position);
        AudioMaster.instance.PlayMusic("Night Terror", 0.9f);
        beatMatrix[0]++;
        babyKipling.GetComponent<Animator>().Play("ShakeBabyKip");
    }

    public void wolvoAttackTrigger()
    {
        beatMatrix[0]++;
        wolvo.gameObject.SetActive(false);
        anglerhare.gameObject.SetActive(false);
        deadAnglerhare.gameObject.SetActive(true);
        StartCoroutine(delayThenAnglerHareDie());
        combatWolvolin.SetActive(true);
    }

    private IEnumerator delayThenAnglerHareDie()
    {
        deadAnglerhare.damageCharacter(10, deadAnglerhare.transform.position, false, false, true, false);
        yield return null;
    }

    public void startFinalCutscene()
    {
        PlayerController.instance.canSave = false;
        beatMatrix[0]++;
        AudioMaster.instance.PlaySound("Metal Slam");
        AudioMaster.instance.StopMusic(1f);
    }

    public void endFinalCutscene()
    {
        StartCoroutine(finalCustceneEvents());
    }

    private IEnumerator finalCustceneEvents()
    {
        yield return new WaitForSeconds(5f);
        AudioMaster.instance.PlaySound("Firearm", true);
        yield return new WaitForSeconds(1f);
        AudioMaster.instance.PlaySound("Firearm", true);
        yield return new WaitForSeconds(5f);
        AudioMaster.instance.PlaySound("Door Open");
        redSpotlight.SetActive(true);
        yield return new WaitForSeconds(6f);
        AudioMaster.instance.PlaySound("Big Slam");
        redSpotlight.SetActive(false);
        finalCustceneLight.SetActive(false);
        yield return new WaitForSeconds(1f);
        PlayerController.instance.canSave = true;

        // TO DO: Load Owen's farm again.
        LevelLoader.instance.changeScene("OWEN'S FARM"); // saves this scene first
    }


}
