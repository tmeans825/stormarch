using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlignmentCompassFace : MonoBehaviour
{
    public Image face;
    public Image hair;

    private void OnEnable()
    {
        face.sprite = PlayerController.instance.heads[3].GetComponent<SpriteRenderer>().sprite; // skin
        hair.sprite = PlayerController.instance.heads[3].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
    }
}
