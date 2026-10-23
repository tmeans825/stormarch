using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LerpUI : MonoBehaviour
{
    public float duration;
    public Vector3 endPosition;

    private RectTransform rectTransform;
    private Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.localPosition;

        Animator animator = GetComponent<Animator>();

        if(animator != null) // safety check - override animations
        {
            animator.enabled = false;
        }

        StartCoroutine(lerp());
    }

    // Update is called once per frame
    private IEnumerator lerp()
    {
        float time = 0.0f;
        while (time < duration)
        {
            float t = time / duration;
            rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            time += Time.deltaTime;
            yield return null;
        }

        rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, 1);
    }
}
