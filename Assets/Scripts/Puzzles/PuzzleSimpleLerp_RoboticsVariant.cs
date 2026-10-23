using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleSimpleLerp_RoboticsVariant : PuzzleSimpleLerp
{
    public SpriteRenderer swapSprite1; // starts on
    public SpriteRenderer swapSprite2; // starts off
    public Sprite onSprite;
    public Sprite offSprite;

    public GameObject toggleObj1; // starts on
    public GameObject toggleObj2; // starts off

    public string additionalOnSound;
    public string additionalOffSound;

    private bool isObj1 = true; // starts on

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    protected override void signalStart()
    {
        am.PlaySound(additionalOnSound);

        if (isObj1)
        {
            swapSprite1.sprite = offSprite;
            toggleObj1.SetActive(false);
        }
        else
        {
            swapSprite2.sprite = offSprite;
            toggleObj2.SetActive(false);
        }
    }

    protected override void signalEnd()
    {
        am.PlaySound(additionalOffSound);

        if(isObj1)
        {
            swapSprite2.sprite = onSprite;
            toggleObj2.SetActive(true);
        }
        else
        {
            swapSprite1.sprite = onSprite;
            toggleObj1.SetActive(true);
        }

        isObj1 = !isObj1;
    }

  
}
