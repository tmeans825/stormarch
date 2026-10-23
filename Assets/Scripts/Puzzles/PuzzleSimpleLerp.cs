using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script moves a puzzle piece between two waypoints
public class PuzzleSimpleLerp : PuzzlePiece
{    
    public Vector3 endPos;
    public float duration = 1f;

    private Vector3 startPos;
    protected bool isOn = false;
    protected AudioManager am;
    private bool isMoving = false;
    // Start is called before the first frame update
    public virtual void Start()
    {
        startPos = transform.localPosition;
        am = GetComponent<AudioManager>();
    }

    public override bool togglePower(bool on)
    {
        return togglePower(on, true);
    }

    public bool togglePower(bool on, bool playAudio)
    {
        if (isMoving)
            return false;

        isOn = !isOn;
     
        if(isOn)
        {
            StartCoroutine(lerpTime(endPos, playAudio));
        }
        else
        {
            StartCoroutine(lerpTime(startPos, playAudio));
        }

        signalStart();
        return true;
    }

    private IEnumerator lerpTime(Vector3 endPosition)
    {
        StartCoroutine(lerpTime(endPosition, true));
        yield return null;
    }

    private IEnumerator lerpTime(Vector3 endPosition, bool playAudio)
    {
        float time = 0f;
        isMoving = true;
        Vector3 startPosition = transform.localPosition;

        if(playAudio)
            StartCoroutine(handleAudio());

        while (time < duration)
        {
            float t = time / duration;
            t = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            time += Time.deltaTime;
            yield return null;
        }

        // To do: stop moving SFX
        if (playAudio)
        {
            am.StopSound("Loop");
            am.PlaySound("Stop");
        }

        isMoving = false;
        signalEnd();
    }

    private IEnumerator handleAudio()
    {
        if(am.PlaySound("Start"))
            yield return new WaitForSeconds(0.5f);

        am.PlaySound("Loop");
    }

    protected virtual void signalStart()
    {
        return;
    }

    protected virtual void signalEnd()
    {
        return;
    }

}
