using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GenericPlayerTrigger : MonoBehaviour
{
    public UnityEvent onEnter = new UnityEvent();
    public UnityEvent onExit = new UnityEvent();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
            onEnter.Invoke();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
            onExit.Invoke();
    }
}
