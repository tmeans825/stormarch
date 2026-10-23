using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FirebugUIBehavior : MonoBehaviour
{
    [System.NonSerialized]
    public static FirebugUIBehavior instance;

    public GameObject iconRow;
    public int maxFirebugs = 3;
    public Sprite fullSprite;
    public Sprite emptySprite;

    private GameObject[] iconArray;
    private Image[] iconImgArray;

    private bool isInfestationActive = false;
    private int deployedCount = 0;
    private bool isInPlacerMode = false;
    private bool isPulsing = false;

    private bool shouldRestAtDim => isInfestationActive || deployedCount > 0 || isInPlacerMode;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        int childCount = iconRow.transform.childCount;
        iconArray = new GameObject[childCount];
        iconImgArray = new Image[childCount];

        for (int i = 0; i < childCount; i++)
        {
            GameObject icon = iconRow.transform.GetChild(i).gameObject;
            iconArray[i] = icon;
            iconImgArray[i] = icon.GetComponent<Image>();
            icon.SetActive(false);
        }
    }

    void OnEnable()
    {
        GameEvents.current.beginInfestation += onInfestationBegin;
        GameEvents.current.endInfestation += onInfestationEnd;
    }

    void OnDisable()
    {
        GameEvents.current.beginInfestation -= onInfestationBegin;
        GameEvents.current.endInfestation -= onInfestationEnd;
    }

    public void setFirebugCount(int count)
    {
        if (count > iconArray.Length)
            count = iconArray.Length;

        deployedCount = 0;

        for (int i = 0; i < iconArray.Length; i++)
        {
            if (i < count)
            {
                iconArray[i].SetActive(true);
                iconImgArray[i].sprite = fullSprite;
            }
            else
            {
                iconArray[i].SetActive(false);
            }
        }

        pulseFade();
    }

    public void notifyDeployed(int index)
    {
        if (index < 0 || index >= iconImgArray.Length)
            return;

        iconImgArray[index].sprite = emptySprite;
        deployedCount++;
        pulseFade();
    }

    public void notifyReturned(int index)
    {
        if (index < 0 || index >= iconImgArray.Length)
            return;

        iconImgArray[index].sprite = fullSprite;
        deployedCount = Mathf.Max(0, deployedCount - 1);
        pulseFade();
    }

    private void onInfestationBegin()
    {
        isInfestationActive = true;
        settleToResting();
    }

    private void onInfestationEnd()
    {
        isInfestationActive = false;
        settleToResting();
    }

    public void notifyPlacerModeStart()
    {
        isInPlacerMode = true;
        settleToResting();
    }

    public void notifyPlacerModeEnd()
    {
        isInPlacerMode = false;
        settleToResting();
    }

    private void settleToResting()
    {
        if (isPulsing) return;
        StopAllCoroutines();
        float target = shouldRestAtDim ? 0.3f : 0f;
        changeAlphaOfIcons(target, 1f);
    }

    private void pulseFade()
    {
        StopAllCoroutines();
        isPulsing = false;
        StartCoroutine(fadePulse());
    }

    private IEnumerator fadePulse()
    {
        isPulsing = true;
        changeAlphaOfIcons(1.0f, 0.01f);
        yield return new WaitForSeconds(1f);
        changeAlphaOfIcons(0.3f, 1.5f);
        yield return new WaitForSeconds(1.5f);

        if (!shouldRestAtDim)
        {
            yield return new WaitForSeconds(1f);
            changeAlphaOfIcons(0f, 1.5f);
        }

        isPulsing = false;
    }

    private void changeAlphaOfIcons(float alpha, float time)
    {
        foreach (Image icon in iconImgArray)
        {
            if (icon.gameObject.activeSelf)
                icon.CrossFadeAlpha(alpha, time, true);
        }
    }
}
