using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ComputerNetwork : MonoBehaviour
{
    public GameObject[] computers;
    private int onComputers = 0;
    private int maxComputers;
    // Start is called before the first frame update
    void Start()
    {
        maxComputers = computers.Length;
    }

    public void signalOn()
    {
        onComputers++;
        if (onComputers == maxComputers)
            signalSuccess();
    }

    public void signalOff()
    {
        onComputers--;
    }

    public void overridePermanentOn()
    {
        signalSuccess();
    }

    public void reversePermanentOn()
    {
        signalFailure();
    }

    private void signalSuccess()
    {
        foreach (GameObject computer in computers)
        {
            computer.GetComponent<ComputerBehavior>().permanentlyPower();
        }
    }

    private void signalFailure()
    {
        foreach (GameObject computer in computers)
        {
            computer.GetComponent<ComputerBehavior>().reversePermanentPower();
        }
    }
}
