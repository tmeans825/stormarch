using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoweredChestPuzzleHandler : PuzzlePiece
{
    public int totalLights;
    public GameObject[] offLights;
    public GameObject[] onLights;

    private int powerLevel;
    private LootBox lb;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < totalLights; i++)
        {
            offLights[i].SetActive(true);
        }

        lb = GetComponent<LootBox>();
    }

    public override bool togglePower(bool on)
    {
        if (on)
        {
            powerLevel++;
            if (powerLevel == totalLights)
                lb.powerChest(true);
        }
        else
        {
            if (powerLevel == totalLights)
                lb.powerChest(false);
            powerLevel--;
        }

        for (int i = 0; i < totalLights; i++)
        {
            bool toggle = false;
            if (i < powerLevel)
                toggle = true;

            onLights[i].SetActive(toggle);
        }

        return true;
    }
}
