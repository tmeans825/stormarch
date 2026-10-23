using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleButtonBehavior : MonoBehaviour
{
    public PuzzlePiece puzzlePieceToActivate;
    public GameObject gameObjectToActivate;
    public Sprite pressedSprite;
    private Sprite originalSprite;
    private SpriteRenderer sr;
    private AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        am = GetComponent<AudioManager>();
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        originalSprite = sr.sprite;
    }

    public void press()
    {
        int successActivateCount = 0;

        if (puzzlePieceToActivate != null)
            if (togglePuzzle()) successActivateCount++;

        if (gameObjectToActivate != null)
            if (toggleObj()) successActivateCount++;

        if(successActivateCount > 0)
        {
            StartCoroutine(pressAnim());
            am.PlaySound("Beep");
        }
        else
            am.PlaySound("Negative");
    }

    private bool togglePuzzle()
    {
        return puzzlePieceToActivate.togglePower(true);
    }

    private bool toggleObj()
    {
        if(!gameObjectToActivate.activeSelf)
        {
            gameObjectToActivate.SetActive(true);
            return true;
        }

        return false;
    }

    private IEnumerator pressAnim()
    {
        sr.sprite = pressedSprite;
        yield return new WaitForSeconds(0.5f);
        sr.sprite = originalSprite;
        am.PlaySound("Toggle");
    }
}
