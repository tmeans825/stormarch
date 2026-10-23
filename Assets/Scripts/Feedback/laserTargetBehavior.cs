using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserTargetBehavior : MonoBehaviour
{
    public LineRenderer line;
    public Vector3 direction;

    private int layers;


    private void Awake()
    {
        line.positionCount = 2;
    }

    private void Start()
    {
        layers = (1 << LayerMask.NameToLayer("Obstacle")) | (1 << LayerMask.NameToLayer("BlockingLayer")) | (1 << LayerMask.NameToLayer("EnemyBlockingLayer"));
        
    }

    void OnEnable()
    {
        line.enabled = true;
        line.SetPosition(0, transform.position);

        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, direction, 100f, layers);
        if (raycastHit)
        {
            line.SetPosition(1, raycastHit.point);
        }

        else
        {
            line.SetPosition(1, direction * 100f);
        }
    }

    private void OnDisable()
    {
        line.enabled = false;
    }

    void Update()
    {
        line.SetPosition(0, transform.position);

        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, direction, 100f, layers);
        if (raycastHit)
        {
            line.SetPosition(1, raycastHit.point);
        }

        else
        {
            line.SetPosition(1, direction * 100f);
        }

    }
}
