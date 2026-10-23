using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
    DESCRIPTION: Destroy object after a set number of seconds
    MAJOR CHANGE HISTORY:
        *11/8 | a.03.3 - Created
**/
public class destroyAfterSeconds : MonoBehaviour
{
    public float time = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(this.gameObject, time);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
