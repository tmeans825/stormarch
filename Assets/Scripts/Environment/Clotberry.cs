using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clotberry : MonoBehaviour
{
    public GameObject aliveSprite;
    public GameObject deadSprite;
    public GameObject shinySparkles;
    public GameObject dropParticles;

    public AudioManager am;

    [System.NonSerialized]
    [ES3Serializable]
    public bool used = false;

    private void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    public void useClotberry()
    {
        used = true;
        am.PlaySound("CollectBerry",true);
        dropParticles.SetActive(true);
        removeInteractableStuff();
    }

    private void removeInteractableStuff()
    {
        shinySparkles.SetActive(false);
        deadSprite.SetActive(true);   
        aliveSprite.SetActive(false);
    }

    private void previousSceneDataLoaded()
    {
        if (used)
        {
            removeInteractableStuff();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Fire")
        {
            useClotberry();
        }
    }

    public void respawnFromHarvested()
    {
        used = false;
        aliveSprite.SetActive(true);
        shinySparkles.SetActive(true);
        deadSprite.SetActive(false);
        dropParticles.SetActive(false);
    }

    // Filter on `used` so story-disabled untouched berries are left alone.
    public static void respawnAllUsedInCurrentScene()
    {
        var all = GameObject.FindObjectsOfType<Clotberry>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Clotberry c = all[i];
            if (c == null) continue;
            if (!c.used) continue;
            c.respawnFromHarvested();
        }
    }
}
