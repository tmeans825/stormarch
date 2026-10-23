using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fadeSoundByLifetime : MonoBehaviour
{
    public float delay;
    public float fadeSpeed;
    public bool useAudioManager = false;
    public string soundName;
    private AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        if (useAudioManager)
        {
            am = GetComponent<AudioManager>();
            StartCoroutine(fadeSoundAM());
        }
        else
            StartCoroutine(fadeSound());
    }

    private IEnumerator fadeSoundAM()
    {
        yield return new WaitForSeconds(delay);
        am.fadeOutSound(soundName, fadeSpeed);
    }

    private IEnumerator fadeSound()
    {
        yield return new WaitForSeconds(delay);
        AudioSource s = transform.GetComponent<AudioSource>();
        

        if (s == null)
            yield break;

        float originalVolume = s.volume;

        while (s.volume > 0)
        {
            s.volume -= originalVolume * (fadeSpeed / 10f);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
