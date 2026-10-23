using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using HighlightPlus2D;
using Com.LuisPedroFonseca.ProCamera2D;


public class LootBox : MonoBehaviour
{
    [Tooltip("0 = Supply Cache, 1 = Powered Chest")]
    [Range(0, 1)]
    public int lootBoxType = 0;

    [Header("Supply Cache Items")]
    public GameObject offLight;
    public GameObject onLight;
    public GameObject closedObject;
    public GameObject openObject;

    [Header("Powered Chest Items")]
    public GameObject redBottomLight;
    public GameObject greenBottomLight;
    public GameObject lidObject;
    public GameObject chestObject;
    public Light2D groundLight;
    public GameObject particleObj;
    public GameObject doneSprite;
    public GameObject depressurizeObj;
    [Tooltip("Powered Chest only: starts unlocked, no power puzzle needed")]
    [ES3NonSerializable]
    public bool startsUnlocked = false;

    const string SMALL_SHAKE = "Small Shake";

    private AudioManager am;
    [System.NonSerialized]
    public bool isLocked = true;

    [System.NonSerialized]
    [ES3Serializable]
    public bool isOpening = false;

    // Start is called before the first frame update
    void Start()
    {
        am = GetComponentInParent<AudioManager>();
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;

        if (lootBoxType == 1 && startsUnlocked)
            setPowered(true, false);
    }

    public void open()
    {
        if (isOpening)
            return;

        switch(lootBoxType)
        {
            case 0:
                openCache();
                return;
            case 1:
                openPoweredChest();
                return;
        }
    }

    // CACHE ------------
    private void openCache()
    {
        offLight.SetActive(false);
        onLight.SetActive(true);
        am.PlaySound("Cache Beep");
        isOpening = true;
        StartCoroutine(cacheRoutine());
    }

    private IEnumerator cacheRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        am.PlaySound("Cache Open");

        openObject.SetActive(true);

        DropManager.instance.generateDrop(dropCategories.classification.Cache, transform.position);
        ProCamera2DShake.Instance.Shake(SMALL_SHAKE);
        EventManager.instance.hitFreeze();

        closedObject.SetActive(false);
    }

    // CHEST -----------
    
    // call from puzzle script. also bound by a UnityEvent in Act1_Wilds - keep the signature
    public void powerChest(bool power)
    {
        setPowered(power, !isOpening);
    }

    private void setPowered(bool power, bool playSound)
    {
        bool stateChanged = isLocked == power;

        if (power)
        {
            redBottomLight.SetActive(false);
            greenBottomLight.SetActive(true);
            groundLight.color = Color.green;
            isLocked = false;
            if (playSound && stateChanged)
                am.PlaySound("Unlock");
        }
        else
        {
            redBottomLight.SetActive(true);
            greenBottomLight.SetActive(false);
            groundLight.color = Color.red;
            isLocked = true;
            if (playSound && stateChanged)
                am.PlaySound("Lock");
        }
    }

    // character tries to open
    private void openPoweredChest()
    {
        if(isLocked)
        {
            AudioMaster.instance.PlaySound("Kipling Error");
            WorldspaceCanvas.instance.popUpLockedEmote(transform.position + new Vector3(0f, 2f, 0f));
            return;
        }

        am.PlaySound("Cache Beep");
        am.PlaySound("Chest Gear Loop");
        isOpening = true;
        chestObject.GetComponent<Animator>().Play("Chest_Base_Unlock"); // animation end will call into kicking off the lid open
    }

    // called from base animator
    public void chestUnlocked()
    {
        am.StopSound("Chest Gear Loop");
        am.PlaySound("Chest Gear Stop");
        
        StartCoroutine(depressurize());
    }

    private IEnumerator depressurize()
    {
        depressurizeObj.SetActive(true);
        am.PlaySound("Chest Depressurize");
        yield return new WaitForSeconds(2f);
        am.PlaySound("Chest Open");
        lidObject.GetComponent<Animator>().Play("Chest_Top_Open");
    }

    // called from top animator
    public void chestOpened()
    {
        am.PlaySound("Chest Complete");

        ProCamera2DShake.Instance.Shake(SMALL_SHAKE);

        particleObj.SetActive(true);
        doneSprite.SetActive(true);

        DropManager.instance.generateDrop(dropCategories.classification.PoweredChest, transform.position + new Vector3(0f, 0.5f), true);

        this.gameObject.SetActive(false);
    }    

    // GENERAL FUNCTIONS ----------

    protected void previousSceneDataLoaded()
    {
        if (!isOpening)
            return;

        if(lootBoxType == 0)
        {
            openObject.SetActive(true);
            closedObject.SetActive(false);
        }     
        else if(lootBoxType == 1)
        {
            doneSprite.SetActive(true);
            particleObj.SetActive(true);

            foreach(Transform objToOffset in lidObject.GetComponentInChildren<Transform>())
                objToOffset.position += new Vector3(0f, 0.8f);

            this.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }
}
