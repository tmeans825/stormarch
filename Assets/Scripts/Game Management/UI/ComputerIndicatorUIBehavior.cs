using System.Collections.Generic;
using UnityEngine;

public class ComputerIndicatorUIBehavior : MonoBehaviour
{
    public static ComputerIndicatorUIBehavior instance;

    public GameObject indicatorPrefab;
    public float edgePadding = 30f;
    public float showMargin = 0.03f;

    private List<ComputerBehavior> targets = new List<ComputerBehavior>();
    private List<ComputerIndicatorElement> elements = new List<ComputerIndicatorElement>();
    private List<ComputerIndicatorElement> pooledElements = new List<ComputerIndicatorElement>();
    private RectTransform rt;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        rt = (RectTransform)transform;
    }

    public void registerComputer(ComputerBehavior computer)
    {
        if (computer == null || targets.Contains(computer))
            return;

        ComputerIndicatorElement element;
        if (pooledElements.Count > 0)
        {
            element = pooledElements[pooledElements.Count - 1];
            pooledElements.RemoveAt(pooledElements.Count - 1);
        }
        else
        {
            element = Instantiate(indicatorPrefab, transform).GetComponent<ComputerIndicatorElement>();
        }

        element.gameObject.SetActive(false); // stays hidden until LateUpdate confirms off-screen
        targets.Add(computer);
        elements.Add(element);
    }

    public void unregisterComputer(ComputerBehavior computer)
    {
        int index = targets.IndexOf(computer);
        if (index >= 0)
            removeAt(index);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            for (int i = 0; i < elements.Count; i++)
                elements[i].gameObject.SetActive(false);
            return;
        }

        Vector2 size = rt.rect.size;
        Vector2 halfExtents = size * 0.5f - new Vector2(edgePadding, edgePadding);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            ComputerBehavior computer = targets[i];
            if (computer == null)
            {
                removeAt(i);
                continue;
            }

            ComputerIndicatorElement element = elements[i];
            Vector3 viewportPos = cam.WorldToViewportPoint(computer.transform.position);

            // hysteresis band between the two thresholds prevents flicker at the screen edge
            bool clearlyOnScreen = viewportPos.x > 0f && viewportPos.x < 1f
                                && viewportPos.y > 0f && viewportPos.y < 1f;
            bool clearlyOffScreen = viewportPos.x < -showMargin || viewportPos.x > 1f + showMargin
                                 || viewportPos.y < -showMargin || viewportPos.y > 1f + showMargin;
            bool show = element.gameObject.activeSelf ? !clearlyOnScreen : clearlyOffScreen;
            element.gameObject.SetActive(show);
            if (!show)
                continue;

            Vector2 targetPos = new Vector2((viewportPos.x - 0.5f) * size.x, (viewportPos.y - 0.5f) * size.y);
            Vector2 clampedPos = new Vector2(Mathf.Clamp(targetPos.x, -halfExtents.x, halfExtents.x),
                                             Mathf.Clamp(targetPos.y, -halfExtents.y, halfExtents.y));
            ((RectTransform)element.transform).anchoredPosition = clampedPos;

            Vector2 pointDirection = targetPos - clampedPos;
            if (pointDirection.sqrMagnitude > 0.0001f) // keep last rotation while inside the hysteresis band
                element.arrowPivot.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(pointDirection.y, pointDirection.x) * Mathf.Rad2Deg + 90f); // arrow art points down

            element.fillImage.fillAmount = computer.getRemainingPowerFraction();
        }
    }

    private void removeAt(int index)
    {
        ComputerIndicatorElement element = elements[index];
        element.gameObject.SetActive(false);
        pooledElements.Add(element);
        targets.RemoveAt(index);
        elements.RemoveAt(index);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (indicatorPrefab == null)
        {
            Debug.LogWarning("ComputerIndicatorUIBehavior: indicatorPrefab is not assigned", this);
            return;
        }

        ComputerIndicatorElement element = indicatorPrefab.GetComponent<ComputerIndicatorElement>();
        if (element == null)
            Debug.LogWarning("ComputerIndicatorUIBehavior: indicatorPrefab has no ComputerIndicatorElement", this);
        else if (element.arrowPivot == null || element.fillImage == null)
            Debug.LogWarning("ComputerIndicatorUIBehavior: indicatorPrefab element is missing arrowPivot or fillImage", this);
    }
#endif
}
