using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomesteadTerrainHelper : MonoBehaviour
{
    [System.NonSerialized]
    public static HomesteadTerrainHelper instance;

    private BoxCollider2D boxCollider;
    private bool startBuildingTrigger = false;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;
    }

    public void developTerrain(GameObject[] tierTerrain, int tierToRemove, GameObject[,] allConstructionPlots)
    {
        for (int i = 0; i < allConstructionPlots.GetLength(1); i++)
        {
            if (allConstructionPlots[tierToRemove - 1, i] != null)
                allConstructionPlots[tierToRemove - 1, i].SetActive(true);
        }
        //yield return new WaitForSeconds(0.1f);

        tierTerrain[tierToRemove - 1].SetActive(false);

        for (int i = 0; i < allConstructionPlots.GetLength(1); i++)
        {
            if (allConstructionPlots[tierToRemove - 1, i] != null)
                allConstructionPlots[tierToRemove - 1, i].transform.GetChild(0).gameObject.SetActive(true);
        }

        boxCollider.enabled = true; // only enable for graph updates, or it can block mouseovers
        AstarPath.active.UpdateGraphs(boxCollider.bounds);
        boxCollider.enabled = false;
    }

    public void developBuilding(GameObject buildingObj, GameObject[,] allConstructionPlots, int xCoordinate, int yCoordinate, bool deployOwen)
    {
        buildingObj.transform.localPosition = allConstructionPlots[xCoordinate, yCoordinate].transform.localPosition;

        if(deployOwen)
            OwenBehavior.instance.startMoving(buildingObj.transform.position);

        // THOUGHTS - may move the below to coroutine then have owen call the addbuilding coroutine when he's ready 
        StartCoroutine(addBuilding(buildingObj, allConstructionPlots, xCoordinate, yCoordinate, deployOwen));
    }

    private IEnumerator addBuilding(GameObject buildingObj, GameObject[,] allConstructionPlots, int xCoordinate, int yCoordinate, bool deployOwen)
    {
        if(deployOwen)
        {
            while (!startBuildingTrigger)
                yield return null;

            startBuildingTrigger = false;
        }

        buildingObj.SetActive(true);

        // Only if not loading...
        if (!HomesteadUIBehavior.instance.isLoadingIn)
        {
            yield return new WaitForSeconds(0.85f);           
        }

        buildingObj.transform.GetChild(0).gameObject.SetActive(true);

        allConstructionPlots[xCoordinate, yCoordinate].SetActive(false);

        boxCollider.enabled = true; // only enable for graph updates, or it can block mouseovers
        AstarPath.active.UpdateGraphs(boxCollider.bounds);
        boxCollider.enabled = false;
  
    }

    public void triggerBuilding()
    {
        startBuildingTrigger = true;
    }

    public void triggerMenusBack(float time)
    {
        StartCoroutine(turnMenusBackOn(time));
    }

    private IEnumerator turnMenusBackOn(float time)
    {
        yield return new WaitForSeconds(time);
        AudioMaster.instance.PlaySound("Menu Tab");
        HomesteadUIBehavior.instance.gameObject.SetActive(true);
    }

}
