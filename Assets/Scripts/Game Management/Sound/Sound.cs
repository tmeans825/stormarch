using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound 
{
    public string name;

    public AudioClip clip;

    [Range(0f,1f)]
    public float volume;
    [Range(0.1f,3f)]
    public float pitch=1f;
    [Range(0f, 1f)]
    public float spatialBlend=1f;
    public float minDistance = 5f;
    public float maxDistance = 15f;

    public bool loop;

    [Header("AudioMaster Songs Only")]
    [Tooltip("3 = Background Music/SFX, 2 = Combat Music, 1 = Infestation Music, 0 = Story Music")]
    public int priority = 3;

    [HideInInspector]
    public AudioSource source;
    [HideInInspector]
    public bool isFadingIn = false;
    [HideInInspector]
    public bool isFadingOut = false;
    [HideInInspector]
    public float originalVolume;
}
