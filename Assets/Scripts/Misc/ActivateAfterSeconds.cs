using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateAfterSeconds : MonoBehaviour
{
    public GameObject[] objsToActivate;
    public float[] timeToActivate;
    public bool sequential = true;

    private void Start()
    {
        StartCoroutine(activateObjs());
    }

    private IEnumerator activateObjs()
    {
        for (int i = 0; i < objsToActivate.Length; i++)
        {
            if (sequential)
            {
                yield return new WaitForSeconds(timeToActivate[i]);
                objsToActivate[i].SetActive(true);
            }
            else
                StartCoroutine(activateObjAfterSecs(objsToActivate[i], timeToActivate[i]));
        }
    }

    private IEnumerator activateObjAfterSecs(GameObject obj, float time)
    { 
        yield return new WaitForSeconds(time);
        obj.SetActive(true);
    }
}
