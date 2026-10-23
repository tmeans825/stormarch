using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    [System.NonSerialized]
    public bool muted = false;

    private static readonly string[] suppressErrorForSoundName = { "Start", "ShieldsOn", "ShieldStatic" };

    protected virtual void Awake()
    {       
        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.originalVolume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.priority = s.priority;
            s.source.playOnAwake = false;
            s.source.outputAudioMixerGroup = AudioMaster.instance.amGroup;
            s.source.dopplerLevel = 0f;
            if (gameObject.tag != "Player")
            {
                s.source.spatialBlend = s.spatialBlend;
                s.source.minDistance = s.minDistance;
                s.source.maxDistance = s.maxDistance;
                s.source.rolloffMode = AudioRolloffMode.Linear;
            }
        }
    }

    /// <summary>
    /// Plays a one-off sound with no fade-in or fade-out. Returns true if sound exists, false otherwise.
    /// Passing "" will suppress errors, as will adding a name to the static readonly variable on this script.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <returns></returns>
    public bool PlaySound(string name)
    {           
        return PlaySound(name, false);       
    }

    /// <summary>
    /// Plays a one-off sound with no fade-in or fade-out. Returns true if sound exists, false otherwise.
    /// Passing "" will suppress errors, as will adding a name to the static readonly variable on this script.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <param name="pitchShift">If set to true, the sound will have a slight randomized pitch shift.</param>
    /// <returns></returns>
    public bool PlaySound(string name, bool pitchShift)
    {
        if (name == "" || muted)
            return false;

        Sound s = getSound(name);
        if (s == null)
            return false;

        if (pitchShift)
        {
            float pitchChange = UnityEngine.Random.Range(-0.15f, 0.15f);
            s.source.pitch = 1f + pitchChange;
        }

        s.source.Play();
        return true;
    }

    public void PlaySoundAtPitch(string name, float pitch)
    {
        if (name == "" || muted)
            return;

        Sound s = getSound(name);
        if (s == null)
            return;

        s.source.pitch = pitch;       
        s.source.Play();
    }

    /// <summary>
    /// Immediately stop a sound.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <returns></returns>
    public void StopSound(string name)
    {
        Sound s = getSound(name);
        if (s == null)
            return;

        s.source.Stop();
    }

    /// <summary>
    /// Fade a sound.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <param name="speed">Percentage of the song to fade out per second, until it is at zero volume.</param>
    /// <returns></returns>
    public IEnumerator fadeOutSound(string name, float speed)
    {
        Sound s = getSound(name);       

        if (s == null)
            yield break;

        float originalVolume = s.volume;

        while (s.source.volume > 0)
        {
            s.source.volume -= originalVolume * (speed / 10f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Helper Functions -----------------------------------------
    public Sound getSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            foreach(string str in suppressErrorForSoundName)
            {
                if (str == name)
                    return null; // do not log error
            }

            Debug.LogWarning("Sound: " + name + " not found!");
            return null;
        }
        return s;
    }
}
