using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class autoBehavior : MonoBehaviour
{
    public Vector3 endPos;
    public float duration = 1f;

    private Vector3 startPos;
    private bool isOn = false;
    protected AudioManager am;
    private bool isMoving = false;
    private ParticleSystem ps;
    // Start is called before the first frame update
    public virtual void Start()
    {
        startPos = transform.localPosition;
        am = GetComponent<AudioManager>();

        ps = GetComponent<ParticleSystem>();
        pace();
    }

    private void pace()
    {
        isOn = !isOn;

        if (isOn)
        {
            StartCoroutine(lerpTime(endPos));
        }
        else
        {
            StartCoroutine(lerpTime(startPos));
        }
    }

    private IEnumerator lerpTime(Vector3 endPosition)
    {
        float time = 0f;
        isMoving = true;
        Vector3 startPosition = transform.localPosition;

        // TO DO: Start squirting.
        ps.Play();
        am.PlaySound("Water");
        //StartCoroutine(handleAudio());

        while (time < duration)
        {
            float t = time / duration;
            t = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            time += Time.deltaTime;
            yield return null;
        }

        ps.Stop();
        am.StopSound("Water");

        isMoving = false;

        float randWait = Random.Range(5f, 25f);

        yield return new WaitForSeconds(randWait);

        pace();
    }

    private IEnumerator handleAudio()
    {
        am.PlaySound("Start");
        yield return new WaitForSeconds(0.5f);
        am.PlaySound("Loop");
    }

}
