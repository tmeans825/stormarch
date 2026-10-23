using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class beatTrigger : MonoBehaviour
{
    [Tooltip("This should usually be yes so the state gets saved.")]
    public bool autoDeactivate = true;
    [Tooltip("0 = trigger, 1 = unaggro, 2 = on enable")]
    [Range(0, 2)]
    public int type = 0;
    [Tooltip("Null will assume only player can trigger")]
    public List<GameObject> objsThatCanTrigger = new List<GameObject>();
    public float delayForInvoke = 0f;
    public UnityEvent eventToTrigger;
    private Collider2D coll;

    private bool triggered = false;
    // Start is called before the first frame update
    void Start()
    {
        if(type == 0)
            coll = GetComponent<Collider2D>();
        else if(type == 1)
        {
            GameEvents.current.onUnaggro += OnUnaggro;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type != 0)
            return;
        else if(objsThatCanTrigger.Count > 0)
        {
            if (objsThatCanTrigger.Contains(collision.gameObject))
                StartCoroutine(triggerEvent());
        }
        else if(collision.CompareTag("Player"))
        {
            StartCoroutine(triggerEvent());
        }    
    }

    private void OnUnaggro()
    {
        StartCoroutine(triggerEvent());
    }

    private void OnEnable()
    {
        if(type == 2)
            StartCoroutine(triggerEvent());
    }

    private IEnumerator triggerEvent()
    {
        if (triggered) // only allow a trigger to fire off once
            yield break;

        triggered = true;
        yield return new WaitForSeconds(delayForInvoke);
        eventToTrigger.Invoke();

        if (autoDeactivate)
        {
            yield return null; // wait one frame to allow previous stuff to activate
            gameObject.SetActive(false);
            GameEvents.current.onUnaggro -= OnUnaggro;
        }
        else
        {
            triggered = false;
        }
    }
}
