using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KiplingController : MonoBehaviour
{
    public bool isKipOnShoulders = true;
    public GameObject mobileKipling;

    // Ambient glow particles that should only emit while Kipling is visibly on the
    // shoulders. Wire explicitly in the Inspector; if left empty, Awake falls back to
    // the shoulder Kipling's *active* particle children only — inactive nested effects
    // (e.g. charging) are owned by other flows and must not be played/stopped here.
    [SerializeField] private ParticleSystem[] shoulderGlowParticles;

    private Vector3 onShouldersEmoteOffset = new Vector3(0f, 1.6f, 0f);
    private Vector3 deployedEmoteOffset = new Vector3(0f, 1f, 0f);

    private GameObject shoulderKipling;
    private SpriteRenderer kiplingSprite;
    private bool isFacingLeft = true;
    private int oldDirection = 3;
    private float originalX;
    private Animator kipAnim;
    private KiplingBehavior kb;
    private Interactable deployedTarget;

    private Coroutine currentTrackCoroutine;
    // Start is called before the first frame update
    void Awake()
    {
        shoulderKipling = transform.GetChild(1).GetChild(4).gameObject;
        kiplingSprite = shoulderKipling.GetComponent<SpriteRenderer>();
        kipAnim = shoulderKipling.GetComponent<Animator>();
        originalX = shoulderKipling.transform.localPosition.x;

        if (shoulderGlowParticles == null || shoulderGlowParticles.Length == 0)
            shoulderGlowParticles = shoulderKipling.GetComponentsInChildren<ParticleSystem>(false);

        if (!isKipOnShoulders)
            setShoulderKiplingVisible(false);

        kb = mobileKipling.GetComponent<KiplingBehavior>();
    }

    public void toggleKipOnShoulders(bool toggle)
    {
        setShoulderKiplingVisible(toggle);
        isKipOnShoulders = toggle;
    }

    // Sprite and glow particles must always toggle together — the shoulder Kipling's
    // GameObject stays active while deployed (its Animator keeps running), so hiding
    // only the sprite leaves the glow emitting on an empty shoulder.
    private void setShoulderKiplingVisible(bool visible)
    {
        kiplingSprite.enabled = visible;

        foreach (ParticleSystem ps in shoulderGlowParticles)
        {
            if (visible)
                ps.Play();
            else
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void sendKiplingOut(bool playerDead)
    {
        if (!PlayerController.instance.hasKipling)
            return;

        if (!playerDead)
        {
            bool transfer = false;
            if (KiplingBehavior.instance.isWaitingOnPlate)
            {
                transfer = true;
            }

            GameObject selectedObj = ObjectSelector.current.selectedWithPointer;
            if (selectedObj)
            {
                Interactable selectedInteractable = selectedObj.GetComponent<Interactable>();
                if (selectedInteractable.canKiplingUse)
                {
                    if (selectedInteractable == deployedTarget && KiplingBehavior.instance.isWaitingOnPlate)
                        return; // don't do shit if kip is already on this mother fucker

                    if (kb.IsActivelyTargeting(selectedInteractable))
                        return;

                    WorldspaceCanvas.instance.endHelpText(true); // turn off help text since player clearly knows what to do

                    Vector2 kiplingStartPosition = isKipOnShoulders ? (Vector2)transform.position : (Vector2)kb.transform.position;
                    if (!KiplingBehavior.instance.isAccessibleToKipling(selectedObj, kiplingStartPosition))
                    {
                        // TODO: Update offset for kipling on shoulders
                        AudioMaster.instance.PlaySound("Kipling Error");


                        float faceModifier = getFaceModifier();

                        WorldspaceCanvas.instance.popUpKiplingEmote(transform.position + onShouldersEmoteOffset + new Vector3(0.6f*faceModifier, 0f, 0f));

                        if (currentTrackCoroutine != null)
                            StopCoroutine(currentTrackCoroutine);

                        currentTrackCoroutine = StartCoroutine(trackEmoteOverHead());

                        return;
                    }

                    if (transfer)
                    {
                        KiplingBehavior.instance.isWaitingOnPlate = false;
                        kb.transferFromOne();
                    }

                    if (isKipOnShoulders)
                        deployMobileKip(transform.position);

                    kb.startMoving(selectedObj, true);
                    deployedTarget = selectedInteractable;
                }
            }
            else if(transfer)
            {
                if (!KiplingBehavior.instance.isAccessibleToKipling(PlayerController.instance.gameObject))
                {
                    // ADD QUESTION MARK ABOVE STANDING KIPLING
                    AudioMaster.instance.PlaySound("Kipling Error");
                    WorldspaceCanvas.instance.popUpKiplingEmote(kb.transform.position + deployedEmoteOffset);
                    return;
                }

                KiplingBehavior.instance.isWaitingOnPlate = false;
                deployedTarget = null;
            }
        }
        else if (isKipOnShoulders)
        {
            StartCoroutine(yieldDeath());
        }
    }

    public GameObject getKiplingObjReference()
    {
        return kb.gameObject;
    }

    // Called from PlayerController.prepareForRespawnSave. Cancels the death
    // sequence (yieldDeath coroutine + deployed kip pathing) and snaps Kipling
    // back to the on-shoulders baseline so the upcoming save captures a clean
    // "kip is with player" state. No-op if the player never had Kipling.
    public void recallForRespawn()
    {
        StopAllCoroutines();

        if (mobileKipling != null)
        {
            if (kb != null)
                kb.resetForRespawn();
            mobileKipling.SetActive(false);
            mobileKipling.transform.SetParent(transform); // restore on-shoulders parenting baseline
        }

        deployedTarget = null;

        bool kipPresent = PlayerController.instance.hasKipling;
        isKipOnShoulders = kipPresent;
        setShoulderKiplingVisible(kipPresent);
    }

    // Called from PlayerController.returningToTitle before the player is moved under the
    // persistent ObjectPooler. A deployed Kipling is scene-rooted, so without this it is
    // destroyed on the title scene unload, leaving mobileKipling a dangling reference on
    // the persisted player. This snaps Kipling to the clean on-shoulders baseline (stopping
    // any pending deploy/emote coroutine and reparenting it under the player) so it rides
    // along — identical teardown to the respawn recall.
    public void recallForTitle()
    {
        recallForRespawn();
    }

    private IEnumerator trackEmoteOverHead()
    {
        float timeElapsed = 0f;
        while (timeElapsed < 1f)
        {
            float faceModifier = getFaceModifier();
            WorldspaceCanvas.instance.updateEmoteLocation(transform.position + onShouldersEmoteOffset + new Vector3(0.6f * faceModifier, 0f, 0f));

            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private float getFaceModifier()
    {
        if (isFacingLeft)
            return -1f;
        else
            return 1f;
    }

    private IEnumerator yieldDeath()
    {
        yield return new WaitForSeconds(0.55f);
        deployMobileKip(transform.position + new Vector3(2f, 0f));
        kb.playerDied();
    }

    private void deployMobileKip(Vector3 startingPos)
    {
        setShoulderKiplingVisible(toggleOnShoulders());
        // Detach to scene root so transform-driven player motion (bridge, slide)
        // doesn't drag the deployed Kipling. Reattached on return (returnKipling).
        mobileKipling.transform.SetParent(null);
        mobileKipling.transform.position = startingPos;
        mobileKipling.SetActive(true);
        isKipOnShoulders = false;
    }

    public void returnKipling()
    {
        // Reparent under the player so the idle/on-shoulders Kipling shares the
        // player's lifetime again (e.g. follows it into ObjectPooler on return to title).
        mobileKipling.transform.SetParent(transform);
        setShoulderKiplingVisible(toggleOnShoulders());
    }

    public bool toggleOnShoulders()
    {
        isKipOnShoulders = !isKipOnShoulders;
        return isKipOnShoulders;
    }

    public void changeDirection(int newDirection)
    {
        if (newDirection == oldDirection)
            return;

        if(newDirection == 1)
        {
            if(isFacingLeft)
                flipKip();
            if (oldDirection != 3)
                kipAnim.Play("Shoulder_Kipling_Side");
        }
        else if(newDirection == 3)
        {
            if(!isFacingLeft)
                flipKip();
            if (oldDirection != 1)
                kipAnim.Play("Shoulder_Kipling_Side");
        }
        else if(newDirection == 2)
        {
            if(oldDirection == 0)
                flipKip();
            kipAnim.Play("Shoulder_Kipling_Down");
        }
        else if(newDirection == 0)
        {
            flipKip();
            //kiplingSprite.sortingOrder = 2;
            kiplingSprite.sortingOrder += 4;
            shoulderKipling.transform.localPosition += new Vector3(0.0f, -0.01f, 0.0f);
            kipAnim.Play("Shoulder_Kipling_Up");
        }

        if(oldDirection == 0)
        {
            //kiplingSprite.sortingOrder = -1;
            kiplingSprite.sortingOrder -= 4;
            shoulderKipling.transform.localPosition += new Vector3(0.0f, 0.01f, 0.0f);
        }

        oldDirection = newDirection;
    }

    private void flipKip()
    {
        float xFactor = 1.0f;
        if (isFacingLeft) // flip right
        {
            xFactor = -1.0f;
            shoulderKipling.transform.localPosition = new Vector3(0.01f, shoulderKipling.transform.localPosition.y, shoulderKipling.transform.localPosition.z);
        }
        else
        {
            shoulderKipling.transform.localPosition = new Vector3(originalX, shoulderKipling.transform.localPosition.y, shoulderKipling.transform.localPosition.z);
        }

        shoulderKipling.transform.localScale = new Vector3(xFactor, 1.0f, 1.0f);

        isFacingLeft = !isFacingLeft;
    }
}
