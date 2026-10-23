using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swayUIBehvaior : MonoBehaviour
{
    public GameObject[] swayIndicators;
    public Vector2[] startingLocations;
    public Vector2[] finalLocations;

    [System.NonSerialized]
    public static swayUIBehvaior instance;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        for(int i = 0; i < swayIndicators.Length; i++)
        {
            swayIndicators[i].transform.localPosition = startingLocations[i];
        }
        transform.gameObject.SetActive(false);
    }

    public void updateSway(float swayMod)
    {
        swayMod--; // swayMod is base 1 - so move to 0
        for (int i = 0; i < swayIndicators.Length; i++)
        {
            swayIndicators[i].transform.localPosition = Vector2.Lerp(startingLocations[i], finalLocations[i], swayMod);
        }
    }
}
