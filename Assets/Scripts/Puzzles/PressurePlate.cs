using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HighlightPlus2D;

public class PressurePlate : MonoBehaviour
{
    public bool isToggleLightPuzzle = false;

    public GameObject[] objectsToPower;
    public Sprite onLight;
    public Sprite offLight;

    private bool depressed = false; // unlike me, the pressure plate is not depressed
    private int bodyCount = 0; // me lol
    private Animator anim;
    private SpriteRenderer lightSprite;
    private GameObject currentActor;

    private bool kiplingActive = false;

    private AudioManager am;

    private HighlightTrigger2D highlightTrigger;
    private BoxCollider2D triggerCollider;
    private bool isInsideTrigger = false;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        lightSprite = transform.GetChild(1).GetComponent<SpriteRenderer>();
        lightSprite.sprite = offLight;
        am = GetComponent<AudioManager>();
        highlightTrigger = GetComponent<HighlightTrigger2D>();
        triggerCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (!isInsideTrigger && triggerCollider.bounds.Contains(MouseManager.instance.transform.position))
        {
            isInsideTrigger = true;
            highlightTrigger.manualOverrideMouseEnter();
        }
        else if (isInsideTrigger && !triggerCollider.bounds.Contains(MouseManager.instance.transform.position))
        {
            isInsideTrigger = false;
            highlightTrigger.manualOverrideMouseExit();
        }
    }

    public void activate(GameObject actor)
    {
        if (!depressed)
        {
            depressed = true;
            anim.Play("Pressure_Plate_Down");
            lightSprite.sprite = onLight;
            am.PlaySound("On");
            currentActor = actor;
            StartCoroutine(moveActorDown());

            foreach (GameObject objectToPower in objectsToPower)
            {
                objectToPower.GetComponent<PuzzlePiece>().togglePower(true);

                if (kiplingActive && (objectToPower.GetComponent<PuzzlePiece>().returnType() == 2)) // kipling on it + is a door getting powered
                    objectToPower.GetComponent<PoweredDoor>().addKiplingBlocker(true);
            }
        }
        bodyCount++;
    }

    private IEnumerator moveActorDown()
    {
        yield return new WaitForSeconds(0.1f);
        if (currentActor == null)
            yield break;

        currentActor.transform.localPosition = new Vector3(currentActor.transform.localPosition.x, currentActor.transform.localPosition.y - 0.1f, 0f);

        yield return new WaitForSeconds(0.1f);
        if (currentActor == null)
            yield break;

        currentActor.transform.localPosition = new Vector3(currentActor.transform.localPosition.x, currentActor.transform.localPosition.y - 0.1f, 0f);
    }

    public void deActivate()
    {
        bodyCount--;
        if(depressed && bodyCount <= 0)
        {
            bodyCount = 0;
            depressed = false;
            anim.Play("Pressure_Plate_Up");
            
            lightSprite.sprite = offLight;

            am.PlaySound("Off");

            currentActor = null;

            foreach (GameObject objectToPower in objectsToPower)
            {
                objectToPower.GetComponent<PuzzlePiece>().togglePower(isToggleLightPuzzle); // if isToggleLightPuzzle - pass true always. Otherwise, we want false here
                if (kiplingActive && (objectToPower.GetComponent<PuzzlePiece>().returnType() == 2))
                {
                    kiplingActive = false;
                    objectToPower.GetComponent<PoweredDoor>().addKiplingBlocker(false);
                }
            }
        }
    }

    public void pressedByKipling()
    {
        kiplingActive = true;
    }

    public bool isDepressed()
    {
        return depressed;
    }
}
