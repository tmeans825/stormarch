using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMaster : AudioManager
{
    public AudioMixer masterMixer;
    public GameObject rainPrefab;
    
    public bool isPlayingSong = false;
    private Sound activeSong;

    public string sceneAmbientSongName;

    [System.NonSerialized]
    public static AudioMaster instance;
    [System.NonSerialized]
    public AudioMixerGroup amGroup;

    private Sound fadingOutSong;

    private int activeSongPriority;

    private Coroutine returnToAmbientHandle;

    private Sound activeBed;
    private Dictionary<Sound, Coroutine> bedFades = new Dictionary<Sound, Coroutine>();

    protected override void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        amGroup = masterMixer.FindMatchingGroups(string.Empty)[0];

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.originalVolume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.outputAudioMixerGroup = amGroup;
            s.source.priority = s.priority;
            s.source.playOnAwake = false;
        }

        if (rainPrefab == null)
            rainPrefab = GameObject.Find("RainPrefab2D");

        
    }

    private void Start()
    {
        playAmbient();
    }

    public void playAmbient()
    {
        if (string.IsNullOrEmpty(sceneAmbientSongName))
            return;

        PlayMusic(sceneAmbientSongName, 0.25f, 0.33f);
    }

    /// <summary>
    /// Changes the ambient song for the current region. Crossfades only if the ambient is what's
    /// playing now; during combat/dialogue/infestation music it just updates what they return to.
    /// </summary>
    public void setSceneAmbient(string songName)
    {
        if (sceneAmbientSongName == songName)
            return;

        bool ambientIsPlaying = activeSong == null || activeSong.name == sceneAmbientSongName;
        sceneAmbientSongName = songName;

        if (ambientIsPlaying)
            playAmbient();
    }

    /// <summary>
    /// Crossfades the looping ambient SFX bed (e.g. city noise) that sits under the music.
    /// Independent of the music channel. Pass "" for no bed.
    /// </summary>
    public void setAmbientBed(string name)
    {
        setAmbientBed(name, 0.33f);
    }

    public void setAmbientBed(string name, float fadeSpeed)
    {
        Sound next = string.IsNullOrEmpty(name) ? null : getSound(name);
        if (next == activeBed)
            return;

        if (activeBed != null)
            startBedFade(activeBed, 0f, fadeSpeed);

        activeBed = next;

        if (activeBed != null && !muted)
        {
            if (!activeBed.source.isPlaying)
            {
                activeBed.source.volume = 0f;
                activeBed.source.Play();
            }
            startBedFade(activeBed, activeBed.originalVolume, fadeSpeed);
        }
    }

    public void stopMusicAndResumeAmbient()
    {
        cancelPendingReturnToAmbient();
        returnToAmbientHandle = StartCoroutine(returnToAmbientCoroutine());
    }

    public void stopMusicAndResumeAmbientIfActive(string songName)
    {
        if (activeSong == null || activeSong.name != songName)
            return;

        stopMusicAndResumeAmbient();
    }

    private void cancelPendingReturnToAmbient()
    {
        if (returnToAmbientHandle != null)
        {
            StopCoroutine(returnToAmbientHandle);
            returnToAmbientHandle = null;
        }
    }


    public void configureRain()
    {
        AudioSource[] sources = rainPrefab.GetComponents<AudioSource>();

        foreach (AudioSource source in sources)
        {
            source.outputAudioMixerGroup = amGroup;
            //Debug.Log("Found an audio source");
        }
    }

    public void playMouseOver()
    {
        PlaySound("UI Hotbar Ping");
    }

    public void playMouseClick()
    {
        PlaySound("Deep Select");
    }

    /// <summary>
    /// Fades in a looping song. Will rapidly fade out any currently playing looping song.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <param name="fadeInSpeed">Percentage of the song to fade in per second, until it is at original volume.</param>
    /// <returns></returns>
    public void PlayMusic(string name, float fadeInSpeed)
    {
        PlayMusic(name, fadeInSpeed, 2.0f);
    }

    /// <summary>
    /// Fades in a looping song. Will rapidly fade out any currently playing looping song.
    /// </summary>
    /// <param name="name">Name of the song in the AudioManager array.</param>
    /// <param name="fadeInSpeed">Percentage of the song to fade in per second, until it is at original volume.</param>
    /// <param name="fadeOutOpposingMusicSpeed">Percentage of the currently playing song to fade out per second (if one exists), until it is at zero volume..</param>
    /// <returns></returns>
    public void PlayMusic(string name, float fadeInSpeed, float fadeOutOpposingMusicSpeed)
    {
        Sound s = getSound(name);

        if (s == null)
            return;

        // A pending return-to-ambient must not preempt whatever song is requested now,
        // including a re-request of the song it is currently fading out.
        cancelPendingReturnToAmbient();

        /*StopAllCoroutines();

        if (fadingOutSong != null) // we have a song actively fading out
        {
            fadingOutSong.source.Stop(); // cut out the fading song
            fadingOutSong = null;
        }*/

        if (activeSong != null)
        {
            if(activeSong.name == s.name && !activeSong.isFadingOut)
                return; // already playing homie

            if (activeSongPriority >= s.priority)
            {
                if(!activeSong.isFadingOut)
                {
                    activeSong.source.volume = activeSong.originalVolume; 
                    activeSong.isFadingIn = false;
                    StopMusic(fadeOutOpposingMusicSpeed);
                }
            }
            else
                return;
        }
      
        StartCoroutine(fadeInSong(fadeInSpeed, s));
        activeSong = s;
        activeSongPriority = s.priority;

        isPlayingSong = true;
    }

    /// <summary>
    /// Given a song name, checks if it is actively playing - if so, stop the song.
    /// </summary>
    /// <param name="songName">String name of the song to end</param>
    /// <param name="fadeOutSpeed">Percentage of the song to fade out per second, until it is at zero volume.</param>
    /// <returns></returns>
    public void StopMusic(string songName, float fadeOutSpeed)
    {
        if (activeSong == getSound(songName))
            StopMusic(fadeOutSpeed);
    }

    /// <summary>
    /// Fades out a currently playing looping song. 
    /// </summary>
    /// <param name="fadeOutSpeed">Percentage of the song to fade out per second, until it is at zero volume.</param>
    /// <returns></returns>
    public void StopMusic(float fadeOutSpeed)
    {
        activeSongPriority = 10000;

        if (activeSong != null)
            StartCoroutine(fadeOutSong(fadeOutSpeed));

        isPlayingSong = false;
    }


    // Helper Functions -----------------------------------------
    protected IEnumerator fadeInSong(float speed, Sound song)
    {
        //StopCoroutine("fadeOutSong");
        song.isFadingOut = false; // rescue: any in-flight fadeOutSong on this song stands down
        song.source.volume = 0;
        song.source.Play();
        song.isFadingIn = true;
        while (song.source.volume < song.originalVolume)
        {
            //Debug.Log("Increasing volume.. Current: " + song.source.volume + ". Original: " + originalVolume);
            song.source.volume += song.originalVolume * (speed / 10f);
            yield return new WaitForSeconds(0.1f);
        }
        song.isFadingIn = false;
    }

    protected IEnumerator fadeOutSong(float speed)
    {
        //fadingOutSong = activeSong;

        Sound song = activeSong;
        while (song.source.volume > 0 && !song.isFadingIn)
        {
            song.isFadingOut = true;
            song.source.volume -= song.originalVolume * (speed / 10f);
            yield return new WaitForSeconds(0.1f);
        }

        song.isFadingOut = false;

        if (!song.isFadingIn)
        {
            song.source.Stop();
            song.source.volume = song.source.volume;
        }

        // A rescued song is still the active song (same Sound object), so the
        // !isPlayingSong check must stay AND'd in — activeSong == song alone would
        // null the song we just rescued.
        if (activeSong == song && !isPlayingSong)
            activeSong = null;

        //fadingOutSong = null;
    }

    // One fade per bed; a new fade on the same bed replaces the old one from its current volume.
    private void startBedFade(Sound bed, float target, float speed)
    {
        Coroutine running;
        if (bedFades.TryGetValue(bed, out running) && running != null)
            StopCoroutine(running);

        bedFades[bed] = StartCoroutine(fadeBedTo(bed, target, speed));
    }

    private IEnumerator fadeBedTo(Sound bed, float target, float speed)
    {
        float step = bed.originalVolume * (speed / 10f);
        while (!Mathf.Approximately(bed.source.volume, target))
        {
            bed.source.volume = Mathf.MoveTowards(bed.source.volume, target, step);
            yield return new WaitForSeconds(0.1f);
        }

        if (target <= 0f)
            bed.source.Stop();

        bedFades.Remove(bed);
    }

    private IEnumerator returnToAmbientCoroutine()
    {
        StopMusic(0.5f);
        yield return new WaitForSeconds(2f);
        returnToAmbientHandle = null; // clear before playAmbient so PlayMusic doesn't cancel this coroutine mid-run
        playAmbient();
    }
}
