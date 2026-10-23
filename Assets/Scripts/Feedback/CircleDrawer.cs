using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleDrawer : MonoBehaviour
{
    [Header("Circle Settings")]
    public float radius = 1f;
    public int segments = 64;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = 0.05f;
        DrawCircle();
    }

    void OnValidate()
    {
        if (lineRenderer != null)
            DrawCircle();
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        DrawCircle();
    }

    public void EnableCircle(bool isOn)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = isOn;
    }

    private void DrawCircle()
    {
        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            lineRenderer.SetPosition(i, point);
        }
    }
}
