using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sceneBeatManager : MonoBehaviour
{
    [ES3Serializable]
    [System.NonSerialized]
    public bool spawnedEquipmentThisScene = false;

    // Dev-only: signature of the GameDevTestOverrides beat override last baked into this save,
    // so the override applies once per save rather than once per play session.
    [ES3Serializable]
    [System.NonSerialized]
    public string appliedTestBeatOverride = "";

    [ES3Serializable]
    public int[] beatMatrix;

    [System.NonSerialized]
    public static sceneBeatManager instance;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(this.gameObject);
    }

    protected virtual void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    protected virtual void previousSceneDataLoaded()
    {
        if (beatMatrix.Length <= 0)
        {
            initializeData();

            for (int i = 0; i < beatMatrix.Length; i++)
            {
                beatMatrix[i] = 0;

                if(GameDevTestOverrides.instance != null && GameDevTestOverrides.instance.shouldSkipStoryBeats())
                {
                    beatMatrix[i] = 99;
                }
            }
        }

        if (GameDevTestOverrides.instance != null)
            GameDevTestOverrides.instance.TryApplyForcedBeatMatrix(this);

        loadBeats();

        if (GameDevTestOverrides.instance != null
            && GameDevTestOverrides.instance.shouldSpawnTestEquipment()
            && !spawnedEquipmentThisScene)
        {
            DropManager.instance.spawnTestItems();
            spawnedEquipmentThisScene = true;
        }
    }

    // Dev-only: whether story beats should fast-forward to their end state. Driven by
    // GameDevTestOverrides so the predicate lives in one place across all beat subclasses.
    protected bool shouldSkipBeats()
    {
        return GameDevTestOverrides.instance != null && GameDevTestOverrides.instance.shouldSkipStoryBeats();
    }

    public int getBeatProgress(int beatLine)
    {
        return beatMatrix[beatLine];
    }

    public virtual void debugProgressQuests()
    {

    }

    protected virtual void loadBeats()
    {

    }

    protected virtual void initializeData()
    {
        // Children clases can use this to initialize
    }

    public void progressBeat(int beatLine)
    {
        beatMatrix[beatLine]++;
        Debug.Log("Beat " + beatLine + " progressed to " + beatMatrix[beatLine]);
        if (DialogueAutosaver.instance != null) DialogueAutosaver.instance.flagImportant();
    }

    public void setBeat(int beatLine, int value)
    {
        beatMatrix[beatLine] = value;
        Debug.Log("Beat " + beatLine + " progressed to " + beatMatrix[beatLine]);
        if (DialogueAutosaver.instance != null) DialogueAutosaver.instance.flagImportant();
    }
}
