using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class baseAnimHelper : MonoBehaviour
{
    public void locksDone()
    {
        GetComponentInParent<LootBox>().chestUnlocked();
    }
}
