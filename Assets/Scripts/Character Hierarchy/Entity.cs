using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
    DESCRIPTION: Abstraction of Character for dialogue options
    PRIMARY FUNCTIONALITY: Abstract dialogue, allow for paired down characters that don't use same animation struct
**/

public class Entity : MonoBehaviour
{
    [Header("Entity: Dialogue")]
    [Tooltip("0 = none, 1 = cyper, 2 = guardian, 3 = apostate, 4 = wolves, 5 = collective, 6 = legion, 7 = marauder, 8 = animal, 9 = recording")]
    public int faction = 0;
    public bool isCinematicTarget = true;
    [Tooltip("If set, this song plays on conversation start and returns to ambient on conversation end")]
    [ES3NonSerializable]
    public string themeSongName = "";
    // These will be auto-set on Characters if left blank
    public Sprite dialogueHair;
    public Sprite dialogueHead;
    public Sprite dialogueChest;
    public Sprite dialogueLegs;

    [System.NonSerialized]
    public GameObject speakingIndicator;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        if (transform.childCount == 0)
            return;

        if(transform.GetChild(0).childCount >= 1)
            speakingIndicator = transform.GetChild(0).GetChild(0).gameObject;
    }

    public virtual void updateSpeakerCanvasPortrait()
    {
        DialogueIndicatorManager.instance.updateSpritesheet(dialogueChest, dialogueLegs, dialogueHair, dialogueHead);
    }

    // dummy function for reference from dialogue manager
    public virtual void faceObject(Vector2 target, bool overrideAndReturnToOriginal)
    {
        return;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(themeSongName))
            return;

        AudioMaster audioMaster = FindObjectOfType<AudioMaster>();
        if (audioMaster == null || audioMaster.sounds == null)
            return;

        foreach (Sound s in audioMaster.sounds)
        {
            if (s != null && s.name == themeSongName)
                return;
        }

        Debug.LogWarning(gameObject.name + ": themeSongName \"" + themeSongName + "\" not found in AudioMaster sounds.");
    }
#endif
}
