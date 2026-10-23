using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SaveSlotMenu : MonoBehaviour
{
    public string[] allSceneNames;
    public string activeSaveName;
    public LevelLoader levelLoader;
    public SaveSlot[] slots;
    public GameObject deleteButton;
    public Sprite defaultHood;

    public void slotSelected()
    {
        SaveManager.instance.activeSlotName = activeSaveName;

        string slotToLoad = SaveManager.instance.returnSlotName(activeSaveName);

        string sceneToLoad = "Act1_Zephyr";

        if (ES3.FileExists(slotToLoad + ".es3"))
        {
            sceneToLoad = ES3.Load<string>("Scene", slotToLoad + ".es3");
        }

        AudioMaster.instance.PlaySound("Deep Select");
        levelLoader.GetComponent<LevelLoader>().loadLevel(sceneToLoad);
    }

    public void deleteSlot()
    {
        ES3.DeleteFile(activeSaveName + ".es3");

        foreach (string sceneName in allSceneNames)
        {
            if (ES3.FileExists(activeSaveName + "_" + sceneName + ".es3"))
            {
                ES3.DeleteFile(activeSaveName + "_" + sceneName + ".es3");
            }
        }

        foreach (SaveSlot slot in slots)
        {
            slot.reinitialize();
        }

        AudioMaster.instance.PlaySound("Deep Select");
    }
}
