using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/** 
    DESCRIPTION: Update the UI based on player health changes
**/
public class HealthUIBehavior : MonoBehaviour
{
    public GameObject player;
    public Sprite whiteHealth;
    public Sprite whiteShield;
    public Image shieldsVignette;
    public Sprite yellowShieldsVignette;
    public Sprite bleedingHealth;
    public Sprite crackedFrame;
    public float traceWaitTime = 0.5f;
    public float traceLerpSpeed = 5f;
    public float fadeDelayTime = 5f;
    public float fadeOutTime = 2f;
    public float fadeInTime = 0.25f;
    public float mostPossibleShields = 4f;

    [System.NonSerialized]
    public static HealthUIBehavior instance;

    private Sprite frame;
    private GameObject healthIndicator;
    private Image[] orbs;
    private Image[] frames;
    private Image[] trace;
    private Image[] bleedMeters;
    private int activeOrb;
    private float health;
    private float activeOrbHealth;
    private int maxOrbs;
    private Coroutine bloodCycleCoroutine;

    private GameObject shieldIndicator;
    private Image[] shieldOrbs;
    private Image[] expendedShieldOrbs;
    private Image[] shieldOrbFrames;
    private GameObject[] shieldEmphasisAnims;
    private static int maxShieldOrbs = 3;
    private static float fadedOutShieldAmount = 0.25f;
    private static float fadeShieldTime = 0.2f;
    private float shields;
    private float maxShields;

    private bool healthFlashing=false;
    private bool shieldsFlashing=false;
    private Sprite originalHealthFill;
    private bool shieldsRecharging;
    private Sprite originalShieldsVignette;
    private bool shieldVignetteOn = false;
    private bool fromBroken = false;
    private float ratio;
    private float oneOrbHealth;
    private Coroutine myCoroutine;
    private bool hasLoaded = false;
    private float activeFadeTimer = 0f;
    private bool isFaded = false;
    private Image[] children;

    private CanvasGroup canvasGroup;
    private Coroutine currentFade;


    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    { 
        shieldsVignette.canvasRenderer.SetAlpha(0.0f);
        originalShieldsVignette = shieldsVignette.sprite;

        // cache UI elements
        healthIndicator = transform.GetChild(0).gameObject;
        maxOrbs = healthIndicator.transform.childCount;
        orbs = new Image[maxOrbs];
        frames = new Image[maxOrbs];
        trace = new Image[maxOrbs];
        bleedMeters = new Image[maxOrbs];

        // health
        for (int i = 0; i < maxOrbs; i++)
        {
            trace[i] = healthIndicator.transform.GetChild(i).GetChild(2).GetComponent<Image>();
            orbs[i] = healthIndicator.transform.GetChild(i).GetChild(3).GetComponent<Image>();
            frames[i] = healthIndicator.transform.GetChild(i).GetChild(1).GetComponent<Image>();
            bleedMeters[i] = healthIndicator.transform.GetChild(i).GetChild(4).GetComponent<Image>();
        }

        oneOrbHealth = PlayerController.instance.maxHealth / 3.0f;

        // shields
        shields = PlayerController.instance.shields;
        maxShields = PlayerController.instance.maxShields;
        shieldIndicator = transform.GetChild(1).gameObject;

        shieldOrbs = new Image[maxShieldOrbs];
        shieldOrbFrames = new Image[maxShieldOrbs];
        expendedShieldOrbs = new Image[maxShieldOrbs];
        shieldEmphasisAnims = new GameObject[maxShieldOrbs];

        for (int i = 0; i < maxShieldOrbs; i++) // may need to update if they can have more than 3 shields
        {
            shieldOrbs[i] = shieldIndicator.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            expendedShieldOrbs[i] = shieldIndicator.transform.GetChild(i).GetChild(1).GetComponent<Image>();
            shieldOrbFrames[i] = shieldIndicator.transform.GetChild(i).GetChild(2).GetComponent<Image>();
            shieldEmphasisAnims[i] = shieldIndicator.transform.GetChild(i).GetChild(3).gameObject;
        }

        frame = frames[0].sprite;

        children = GetComponentsInChildren<Image>(true); // maybe not needed
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void initalizeLoadedUI()
    {
        // initialize UI elements
        // health
        health = PlayerController.instance.health;
        int pool = (int)(health / oneOrbHealth);
        //Debug.Log(pool);

        activeOrbHealth = health % oneOrbHealth; // how much health is in the active orb?

        if (activeOrbHealth == 0)
        {
            pool--;
            activeOrbHealth = oneOrbHealth;
        }
        activeOrb = pool;

        if (pool < (maxOrbs - 1)) // need to empty some orbs
        {
            for (int i = pool + 1; i < maxOrbs; i++)
            {
                orbs[i].fillAmount = 0.0f;
                frames[i].gameObject.SetActive(false);
                quietlyBreakOrb(orbs[i].transform.parent.gameObject);  // to do: instead, make each child shatter off by default           
            }
        }

        orbs[activeOrb].fillAmount = activeOrbHealth / oneOrbHealth;
        originalHealthFill = orbs[activeOrb].GetComponent<Image>().sprite;

        //shields
        updateMaxShields();
       
        hasLoaded = true;
    }

    private void quietlyBreakOrb(GameObject orb)
    {
        GameObject crack = orb.transform.GetChild(0).gameObject;

        crack.SetActive(true);

        for (int i = 0; i < 4; i++)
        {
            crack.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void explodeOrb(GameObject orb)
    {
        GameObject crack = orb.transform.GetChild(0).gameObject;

        crack.SetActive(false);
        crack.SetActive(true);

        for(int i = 0; i < 4; i ++)
        {
            pushShard(crack.transform.GetChild(i).gameObject);
        }
    }

    private void pushShard(GameObject shard)
    {
        shard.transform.localPosition = Vector3.zero;
        float forceMagnitude = Random.Range(100, 150);

        // Calculate a downward angle (for example, between -45 and -135 degrees)
        float angleInDegrees = Random.Range(-180, 0);
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // Convert the angle and magnitude into a Vector2 force
        Vector2 force = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)) * forceMagnitude;

        // Apply the force to the Rigidbody2D
        shard.GetComponent<Rigidbody2D>().AddForce(force, ForceMode2D.Impulse);

        // Apply a random torque
        float torque = Random.Range(-3f, 3f); // Adjust the range as needed
        shard.GetComponent<Rigidbody2D>().AddTorque(torque, ForceMode2D.Impulse);

    }

    void Update()
    {
        if (activeFadeTimer == 0f && !isFaded && !PlayerController.instance.inCombatOrBleeding())
        {
            activeFadeTimer = fadeDelayTime;
        }

        else if(activeFadeTimer > 0f)
        {
            activeFadeTimer -= Time.deltaTime;
            if(activeFadeTimer <= 0f)
            {
                isFaded = true;
                activeFadeTimer = 0f;
                for (int i = 0; i < maxOrbs; i++)
                {
                    trace[i].enabled = false;
                }
                /*foreach (Image child in children)
                {
                    if (child.enabled)
                        child.CrossFadeAlpha(0f, fadeOutTime, true);                   
                }*/
                Fade(canvasGroup, 0f, fadeOutTime); 
            }
        }

        if (PlayerController.instance.shields != shields)
        {
            resetEventHappened();
            if (PlayerController.instance.shields < shields)
            {
                if(!shieldsFlashing)
                    StartCoroutine("flashShields");
                fromBroken = false;
                if (!shieldVignetteOn)
                    shieldVignetteOn = true;
                if (shieldsVignette.sprite != originalShieldsVignette)
                    shieldsVignette.sprite = originalShieldsVignette;
                if (PlayerController.instance.shields == 0) // shields have been broken
                {
                    shieldsVignette.sprite = yellowShieldsVignette;
                    shieldsVignette.canvasRenderer.SetAlpha(1.0f);
                    shieldsVignette.CrossFadeAlpha(0, 0.5f, false);
                    shieldVignetteOn = false;
                }
                else if (shieldsVignette.sprite != originalShieldsVignette)
                    shieldsVignette.sprite = originalShieldsVignette;
            }
            else if(PlayerController.instance.shields > shields)
            {
                if (shields == 0) // shields now recharging
                {
                    fromBroken = true;
                }
                if (!shieldVignetteOn)
                    shieldVignetteOn = true;
                if (shieldsVignette.sprite != originalShieldsVignette)
                    shieldsVignette.sprite = originalShieldsVignette;
                
                shieldsRecharging = true;
            }

            updateShields();

            if (shieldVignetteOn)
            {
                if (!fromBroken)
                    ratio = ((maxShields - shields) / maxShields);
                else
                {
                    ratio = (shields / maxShields);
                    if (ratio >= 0.5f)
                        fromBroken = false;
                }

                shieldsVignette.canvasRenderer.SetAlpha(ratio);
                //Debug.Log("Changed alpha of shields");
            }
        }
        else if(shieldsRecharging)
        {
            shieldsRecharging = false;
            shieldsVignette.CrossFadeAlpha(0, 0.5f, false);
        }

        if(maxShields==shields)
        {
            shieldVignetteOn = false;
        }

        if (PlayerController.instance.health != health) // don't try to flash or update before we load
        {
            if (!trace[0].enabled)
            {
                for (int i = 0; i < maxOrbs; i++)
                {
                    trace[i].enabled = true;
                }
            }

            resetEventHappened();
            bool needToFlash = false;
            if (!healthFlashing && PlayerController.instance.health != health)
                needToFlash = true;
            healthUpdate();

            if (!PlayerController.instance.isBleeding) // don't flash the next orb if bleeding took it out
                needToFlash = false;

            if (needToFlash && hasLoaded) // all this bullshit with this bool is so the proper orb flashes
            {
                StartCoroutine("flashHealth");
            }
        }
    }

    public void resetEventHappened() // reset timer event = health or shield change, or equipped slot changed, or fired bullet or menu opened
    {
        if (!isFaded && activeFadeTimer == 0f)
            return;

        if(isFaded)
        {
            fadeInUI();
        }
        else
        {
            activeFadeTimer = fadeDelayTime; // reset wait time
        }
    }

    public void cancelEventHappened() // cancel event = combat entered or is now bleeding
    {
        if (!isFaded && activeFadeTimer == 0f)
            return;

        if(isFaded)
        {
            fadeInUI();
        }
        else
        {
            activeFadeTimer = 0f; // stop countdown from happening
        }
    }

    private void fadeInUI()
    {
        isFaded = false;

        Fade(canvasGroup, 1f, fadeInTime);
        
        /*foreach (Image child in children)
        {
            child.CrossFadeAlpha(1f, fadeInTime, true);
        }*/
    }

    private IEnumerator flashShields()
    {
        //shieldFill.GetComponent<Image>().sprite = whiteShield;
        shieldsFlashing = true;
        yield return new WaitForSeconds(0.15f);
        //shieldFill.GetComponent<Image>().sprite = originalShieldFill;
        shieldsFlashing = false;
    }

    private IEnumerator flashHealth()
    {
        Sprite returnSprite = orbs[activeOrb].sprite; // Changed for new bleeding health
        orbs[activeOrb].sprite = whiteHealth;
        healthFlashing = true;
        yield return new WaitForSeconds(0.15f);
        orbs[activeOrb].sprite = returnSprite;
        healthFlashing = false;
    }

    public void healthUpdate()
    {
        float difference = health - PlayerController.instance.health;
        activeOrbHealth -= difference;
        if(activeOrbHealth <= 0f)
        {
            orbs[activeOrb].fillAmount = 0f;
            trace[activeOrb].fillAmount = 0f;

            frames[activeOrb].gameObject.SetActive(false);
            explodeOrb(frames[activeOrb].transform.parent.gameObject);
            AudioMaster.instance.PlaySound("Health Shatter");

            // move down orbs
            if (activeOrb > 0)
            {
                activeOrb--;
                activeOrbHealth += oneOrbHealth;

                if (PlayerController.instance.isBleeding)
                    transferBleedingToNext();
            }
            else
                ceaseOrbBleeding(activeOrb);
        }
        else if(activeOrbHealth > oneOrbHealth)
        {
            orbs[activeOrb].fillAmount = oneOrbHealth;           
            if(activeOrb < (maxOrbs-1))
            {
                activeOrb++;
                frames[activeOrb].gameObject.SetActive(true);
                frames[activeOrb].transform.parent.GetChild(0).gameObject.SetActive(false); // turn off crack
                activeOrbHealth -= oneOrbHealth;
            }
        }

        orbs[activeOrb].fillAmount = activeOrbHealth / oneOrbHealth;
        health = PlayerController.instance.health;

        if (myCoroutine != null)
        {
            StopCoroutine(myCoroutine); // reset our wait for the trace to go down
            myCoroutine = null;
        }
        myCoroutine = StartCoroutine(signalTrace());
    }

    private IEnumerator signalTrace()
    {
        float cooldownTime = traceWaitTime;
        while (cooldownTime > 0f)
        {
            yield return null;
            cooldownTime -= Time.deltaTime;
        }

        bool[] doneFlags = new bool[trace.Length];
        //float[] originalFills = new float[trace.Length];
        

        for (int i = 0; i < trace.Length; i++)
        {
            //originalFills[i] = trace[i].fillAmount;

            if (trace[i].fillAmount != orbs[i].fillAmount)
                doneFlags[i] = false;
            else
                doneFlags[i] = true;
        }

        while (!checkAllArrayTrue(doneFlags))
        {
            for (int i = 0; i < trace.Length; i++)
            {
                if (doneFlags[i])
                    continue;

                if (trace[i].fillAmount <= orbs[i].fillAmount + 0.05f) // need to add tolerance value due to deviations in lerp output
                {
                    trace[i].fillAmount = orbs[i].fillAmount;
                    doneFlags[i] = true;
                    
                    continue;
                }

                trace[i].fillAmount = Mathf.Lerp(trace[i].fillAmount, orbs[i].fillAmount, traceLerpSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    private bool checkAllArrayTrue(bool[] toCheck)
    {
        for(int i = 0; i < toCheck.Length; i++)
        {
            if (!toCheck[i])
                return false;
        }

        return true;
    }
    
    private void updateShields()
    {
        shields = PlayerController.instance.shields;

        bool restOfShieldsAreEmpty = false;
        for (int i = 0; i < Mathf.CeilToInt(PlayerController.instance.maxShields); i++)
        {
            if (restOfShieldsAreEmpty)
            {
                shieldOrbFrames[i].fillAmount = 0;

                if (!expendedShieldOrbs[i].gameObject.activeSelf)
                {
                    StartCoroutine(playShieldAnimation(shieldEmphasisAnims[i]));
                    shieldOrbs[i].gameObject.SetActive(false);
                    expendedShieldOrbs[i].gameObject.SetActive(true);
                }
            }
            else if (shields >= i + 1) // shields are beyond current orb, or current orb is max orb and filled
            {
                shieldOrbFrames[i].fillAmount = 1f;

                if (expendedShieldOrbs[i].gameObject.activeSelf)
                {
                    StartCoroutine(playShieldAnimation(shieldEmphasisAnims[i]));
                    AudioMaster.instance.PlaySound("Shield Unit");
                    shieldOrbs[i].gameObject.SetActive(true);
                    expendedShieldOrbs[i].gameObject.SetActive(false);
                }
            }
            else if (shields < i + 1) // shields are partially full
            {
                shieldOrbFrames[i].fillAmount = shields % 1f;

                if (!expendedShieldOrbs[i].gameObject.activeSelf)
                {
                    StartCoroutine(playShieldAnimation(shieldEmphasisAnims[i]));
                    shieldOrbs[i].gameObject.SetActive(false);
                    expendedShieldOrbs[i].gameObject.SetActive(true);
                }

                restOfShieldsAreEmpty = true;
            }
        }
    } 
    
    private IEnumerator playShieldAnimation(GameObject shieldToAnimate)
    {
        shieldToAnimate.SetActive(false);
        yield return null;
        shieldToAnimate.SetActive(true);
    }

    public void toggleBleed(bool isBleeding)
    {
        if (isBleeding)
        {
            makeActiveOrbBleed();
        }
        else
        {
            ceaseOrbBleeding(activeOrb);
        }
    }

    private void transferBleedingToNext() // call after updating active orb
    {
        if (bleedMeters[activeOrb + 1] != null)
            ceaseOrbBleeding(activeOrb + 1);

        makeActiveOrbBleed();
    }

    private void ceaseOrbBleeding(int orbIndex)
    {
        orbs[orbIndex].GetComponent<Image>().sprite = originalHealthFill;
        bleedMeters[orbIndex].gameObject.SetActive(false);

        if (bloodCycleCoroutine != null)
            StopCoroutine(bloodCycleCoroutine);
    }

    private void makeActiveOrbBleed()
    {
        resetEventHappened();
        orbs[activeOrb].GetComponent<Image>().sprite = bleedingHealth;
        bleedMeters[activeOrb].gameObject.SetActive(true);
        bloodCycleCoroutine = StartCoroutine(cycleBleedMeter(bleedMeters[activeOrb]));
    }

    private IEnumerator cycleBleedMeter(Image imageToCycle)
    {
        imageToCycle.fillAmount = 1f;

        while (PlayerController.instance.isBleeding)
        {
            yield return null;
            imageToCycle.fillAmount = (PlayerController.instance.getNextBleed() - Time.time) / PlayerController.instance.bleedCooldown;
        }

        imageToCycle.gameObject.SetActive(false);
        bloodCycleCoroutine = null;
    }

    public void updateMaxShields()
    {
        maxShields = PlayerController.instance.maxShields;
        for (int i = 0; i < maxShieldOrbs; i++)
        {
            bool shieldOn;
            if (i < Mathf.CeilToInt(PlayerController.instance.maxShields))
            {
                shieldOn = true;
            }
            else
                shieldOn = false;

            shieldIndicator.transform.GetChild(i).gameObject.SetActive(shieldOn);
        }      
    }

    public void Fade(CanvasGroup group, float targetAlpha, float duration)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeCoroutine(group, targetAlpha, duration));
    }

    private IEnumerator FadeCoroutine(CanvasGroup group, float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        group.alpha = targetAlpha;
        currentFade = null;
    }

}
