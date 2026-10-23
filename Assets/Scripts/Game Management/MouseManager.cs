using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseManager : MonoBehaviour
{
    [System.NonSerialized]
    public static MouseManager instance;

    public float zAxis = 0.0f;
    public Texture2D mouseSprite;
    public Texture2D reticleSprite;
    public Texture2D blankSprite;
    public Animator cursorAnimator;
    public float delayToMorphTime = 3f;

    public GameObject swayIndicator;
    public SpriteRenderer cursorRenderer;

    [System.NonSerialized]
    public Vector3 mousePosition;
    //private Vector2 reticleHotspot;

    private bool isReticle = false;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        //reticleHotspot = new Vector2(reticleSprite.width / 2, reticleSprite.height / 2);       
    }

    private void Start()
    {
        //swapReticle(false);
        //menusOff();
    }
    void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = zAxis;

        transform.position = mousePosition;

        // test code for seeing what mouse is hitting
        /*
        Vector2 point = new Vector2(mousePosition.x, mousePosition.y);
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        Collider2D[] results = new Collider2D[10];
        int colliderCount = Physics2D.OverlapPoint(point, contactFilter, results);

        if (results[0] != null)
        {
            Debug.Log("Mouseover hit: " + results[0].name + " at position: " + point);
        }
        */
        // end test code
    }

    public void menusOn()
    {
        Cursor.SetCursor(mouseSprite, Vector2.zero, CursorMode.Auto);
        cursorRenderer.enabled = false;
    }

    public void menusOff()
    {
        Cursor.SetCursor(blankSprite, Vector2.zero, CursorMode.Auto);
        if(cursorRenderer != null)
            cursorRenderer.enabled = true;
    }

    public void swapReticle(bool inCombat)
    {
        if (inCombat)
            StopAllCoroutines();
        swapReticle(inCombat, false);
    }

    public void swapReticle(bool changetoReticule, bool fromShot)
    {
        if(fromShot && PlayerController.instance.isInCombat)
        {
            return;
        }

        if(fromShot && isReticle)
        {
            StopAllCoroutines();
            StartCoroutine(beginMorphBackCounter(delayToMorphTime));
            return;
        }

        if (changetoReticule && !isReticle)
        {
            if (fromShot)
            {
                StopAllCoroutines();
                StartCoroutine(beginMorphBackCounter(delayToMorphTime));
            }

            isReticle = true;
            cursorAnimator.Play("MousePointerToReticule");
            swayIndicator.SetActive(true);

            
            if (PlayerController.instance.isInCombat)
                StopAllCoroutines();           
        }
        else if (isReticle && !changetoReticule)
        {
            isReticle = false;
            cursorAnimator.Play("MouseReticuleToPointer");
            swayIndicator.SetActive(false);
        }
    }

    private IEnumerator beginMorphBackCounter(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        swapReticle(false);
    }

    public void hideMouse()
    {
        cursorRenderer.enabled = false;
    }

    public void showMouse()
    {
        cursorRenderer.enabled = true;
    }
}
