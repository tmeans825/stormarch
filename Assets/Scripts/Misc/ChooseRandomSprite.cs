using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseRandomSprite : MonoBehaviour
{
    public Sprite[] sprites;
    // Start is called before the first frame update
    void Start()
    {
        int randInt = Random.Range(0, sprites.Length);
        GetComponent<SpriteRenderer>().sprite = sprites[randInt];
    }
}
