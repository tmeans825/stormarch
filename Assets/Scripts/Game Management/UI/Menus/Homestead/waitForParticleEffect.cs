using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waitForParticleEffect : MonoBehaviour
{
    [ES3Serializable]
    public bool hasPlayed = false; // only need to save this one

    [ES3NonSerializable]
    public bool isUpgrade = true;
    [ES3NonSerializable]
    public bool overrideBecausePlot = false;

    [ES3NonSerializable]
    public GameObject[] objsToEnable;
    [ES3NonSerializable]
    public GameObject[] objsToDisable;

    [ES3NonSerializable]
    public float delayBeforeEnablingObjs = 0.4f;
    [ES3NonSerializable]
    public float delayBeforeParticlesForBuildings = 0.3f;

 
    private void Start()
    {
        checkForPlay();
    }

    private void checkForPlay()
    {
        if (hasPlayed)
        {
            // if it's already played, just turn on associated GOs and destroy yourself so we don't play particles
            activateObjects();
            // Destroy(this.gameObject);
            this.gameObject.SetActive(false);
            return;
        }

        if (isUpgrade && !overrideBecausePlot)
        {
            // play upgrade audio
            AudioMaster.instance.PlaySound("Upgrade");
            StartCoroutine(kickOffDelay());
        }
        else if (!overrideBecausePlot)
        {
            // play building audio
            AudioMaster.instance.PlaySound("Building");
            StartCoroutine(buildingDelay());
        }

        hasPlayed = true;
    }

    private IEnumerator kickOffDelay()
    {
        yield return new WaitForSeconds(delayBeforeEnablingObjs);

        activateObjects();
    }

    private IEnumerator buildingDelay()
    {
        yield return new WaitForSeconds(delayBeforeParticlesForBuildings);

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);

        StartCoroutine(kickOffDelay());
    }

    private void activateObjects()
    {
        foreach (GameObject obj in objsToEnable)
            obj.SetActive(true);

        foreach (GameObject obj in objsToDisable)
            obj.SetActive(false);
    }

}
