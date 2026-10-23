using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vent : MonoBehaviour
{
    public GameObject kiplingCollider;
    public GameObject kiplingTrigger;
    public BoxCollider2D personCollider;
    public Transform spriteTransform;
    public GameObject kiplingAnimObj;
    //public GameObject kiplingGuardRail;
    public float rotSpeed = 1.0f;

    private Animator kiplingAnimator;
    private GameObject kipling;

    [ES3Serializable]
    private bool isOpen = false;

    public IEnumerator Start()
    {
        kiplingAnimator = kiplingAnimObj.GetComponent<Animator>();

        yield return 0;
        yield return 0;
        yield return 0;

        if (isOpen)
            use(true);
    }

    public void use()
    {
        use(false);
    }

    public void use(bool ignoreSound)
    {
        kiplingCollider.SetActive(false);
        GetComponent<CircleCollider2D>().enabled = false;
        spriteTransform.gameObject.SetActive(false);
        kiplingCollider.SetActive(false);

        kiplingTrigger.SetActive(true);
        //kiplingGuardRail.SetActive(true);

        AstarPath.active.UpdateGraphs(personCollider.bounds);

        //BoxCollider2D[] boxes = kiplingGuardRail.GetComponents<BoxCollider2D>();

        AstarPath.active.UpdateGraphs(personCollider.bounds);
        //AstarPath.active.UpdateGraphs(boxes[0].bounds);
        //AstarPath.active.UpdateGraphs(boxes[1].bounds);

        if(!ignoreSound)
            GetComponent<AudioManager>().PlaySound("Vent");

        isOpen = true;
    }

    public void startAnim(bool fromBelow, GameObject kipObj)
    {
        if (kipling == null)
            kipling = kipObj;


        string animName = "Vent_Kipling_Up";
        if (fromBelow)
            animName = "Vent_Kipling_Down";

        kipling.SetActive(false);
        kiplingAnimObj.SetActive(true);
        kiplingAnimator.Play(animName);
    }

    public void endAnim(bool fromBelow)
    {
        kiplingAnimObj.SetActive(false);
        kipling.SetActive(true);
        float modifier = 2f;
        if (fromBelow)
            modifier = -2f;

        // jump kipling either above or below the vent when the animation is complete.
        //Debug.Log("before: " + kipling.transform.position);
        //kipling.transform.position += modifier;
        kipling.transform.position = new Vector3(transform.position.x, transform.position.y + modifier);
        //Debug.Log("after: " + kipling.transform.position);
        kipling.GetComponent<KiplingBehavior>().refreshPath();

        kipling.GetComponent<KiplingBehavior>().exitingVent = true;
    }

    private IEnumerator swingVent()
    {
        float totRot = 0.0f;
        while(totRot <= 90.0f)
        {
            totRot += rotSpeed * Time.deltaTime;
            spriteTransform.Rotate(spriteTransform.rotation.x + (rotSpeed * Time.deltaTime), 0, 0);
            yield return null;
        }
        spriteTransform.gameObject.SetActive(false);
    }
}
