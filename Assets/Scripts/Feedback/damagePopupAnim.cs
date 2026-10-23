using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class damagePopupAnim : MonoBehaviour
{
    public float floatSpeed = 1.0f;
    public float alphaSpeed = 0.1f;
    private TextMeshPro text;
    
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up*floatSpeed*Time.deltaTime);
        ChangeAlpha(alphaSpeed*Time.deltaTime);
    }

    public void ChangeAlpha(float newAlpha)
    {
        var color = text.color;
        color.a -= newAlpha;
        if (color.a <= 0f)
            Destroy(this.gameObject);
        text.color = color;
    }
}
