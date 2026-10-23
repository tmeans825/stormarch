using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenUIBehavior : MonoBehaviour
{
    public GameObject pressAnyKey;
    public GameObject titleMenu;
    public GameObject gameMaster;
    public GameObject levelLoader;
    public GameObject executionBeams;


    private bool anyKeyPressed = false;
    private LightWeatherManager weather;
    // Start is called before the first frame update
    void Start()
    {
        weather = gameMaster.GetComponent<LightWeatherManager>();
        MouseManager.instance.menusOn();

        //if (PlayerPrefs.GetInt("Beams On?", 0) > 0)
            //executionBeams.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(!anyKeyPressed && Input.anyKeyDown)
        {
            weather.triggerLightning();
            pressAnyKey.GetComponent<Animator>().Play("AnyKeyFadeOutAnim");
            //pressAnyKey.SetActive(false);
            titleMenu.SetActive(true);
            anyKeyPressed = true;
            StartCoroutine(kickOffMusic());
        }
    }
    public void playSelectNoise()
    {
        AudioMaster.instance.PlaySound("Deep Select");
    }

    public void onExitClick()
    {
        Application.Quit();
    }

    IEnumerator kickOffMusic()
    {
        yield return new WaitForSeconds(2f);
        AudioMaster.instance.PlayMusic("Under the Shadow", 100f);
    }
}
