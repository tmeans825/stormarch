using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PixelCrushers;
using Com.LuisPedroFonseca.ProCamera2D;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown graphicsDropdown;
    public Slider volumeSlider;
    public Slider screenshakeSlider;
    public GameObject keybindHelperText;
    public GameObject returnToMainSettingsButton;
    public float volumeChangeTime;
    public Toggle fullscreenToggle;

    // Merged source of truth for keybinds: a fresh copy of DEFAULT_BUTTON_KEYS with any saved
    // overrides from Settings.es3 layered on top. Static + lazily loaded so it survives scene
    // loads and is queryable (e.g. by the getKey Lua function) even when no SettingsMenu
    // instance is currently active.
    private static Dictionary<string, KeyCode> keybindCache;

    private static readonly Dictionary<string, KeyCode> DEFAULT_BUTTON_KEYS = new Dictionary<string, KeyCode>()
    {
        { "Quick Use 1", KeyCode.X },
        { "Quick Use 2", KeyCode.Z },
        { "Slide", KeyCode.LeftShift },
        { "Interact", KeyCode.E },
        { "Deploy Kipling", KeyCode.F },
        { "Menu", KeyCode.Tab },
        { "Crafting", KeyCode.LeftControl },
        { "Shock Baton", KeyCode.Mouse1 },
        { "No Conductor", KeyCode.Q },
        // Migrated from PlayerController's formerly hardcoded ability fields. Defaults MUST
        // match those field initializers so fresh games and this table agree.
        { "Reload", KeyCode.R },
        { "Shield Wall", KeyCode.Space },
        { "Overload", KeyCode.C },
        { "Turret", KeyCode.T },
        { "Deploy Firebug", KeyCode.G },
        { "Recall Firebug", KeyCode.H }
    };

    public GameObject saveObj;
    public GameObject exitObj;
    public GameObject confirmExitObj;

    [System.NonSerialized]
    public static SettingsMenu instance;

    private string activeControlToRebind;
    private TextMeshProUGUI activeButtonText;

    private Resolution[] resolutions;
    private bool isRebinding = false;
    private GameObject myEventSystem;
    

    private Button saveButtonObj;
    private TextMeshProUGUI saveButtonText;
    private TextMeshProUGUI menuPopupText;

    private int savedResolutionIndex;
    private bool savedFullscreen = true;
    private int savedQuality = 0;
    private float savedVolume = 1.0f; 
    private float savedScreenshake = 1.0f;

    private bool saveFlag = false;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        saveButtonObj = transform.GetChild(1).GetComponent<Button>();
        saveButtonText = saveButtonObj.GetComponentInChildren<TextMeshProUGUI>();

        if(Menus.instance != null)
            menuPopupText = Menus.instance.menuPopupText;
    }
    private void Start()
    {
        myEventSystem = GameObject.Find("@EventSystem");
        
        // Resolutions dropdown
        initializeResolutions();

        if(ES3.FileExists("Settings.es3"))
        {
            savedResolutionIndex = ES3.Load<int>("SavedResolution", "Settings.es3");
            SetResolution(savedResolutionIndex);

            savedFullscreen = ES3.Load<bool>("Fullscreen", "Settings.es3");
            fullscreenToggle.isOn = savedFullscreen;
            SetFullScreen(savedFullscreen);

            savedQuality = ES3.Load<int>("Quality", "Settings.es3");
            SetQuality(savedQuality);

            savedVolume = ES3.Load<float>("Volume", "Settings.es3");
            SetVolume(savedVolume);

            savedScreenshake = ES3.Load<float>("Screenshake", "Settings.es3");
            SetScreenshake(savedScreenshake);

        }

        // Push every known binding (defaults merged with any saved overrides) into
        // PlayerController so its live KeyCode fields match the shared keybind store —
        // including the 6 migrated abilities and any future default-only entries. Done
        // unconditionally (not gated on the save file) so defaults are always applied.
        loadInKeybindings();


        // Graphics dropdown
        graphicsDropdown.value = QualitySettings.GetQualityLevel();
        graphicsDropdown.RefreshShownValue();


        // Volume slider
        volumeSlider.value = savedVolume;
        screenshakeSlider.value = savedScreenshake;

        if (ProCamera2DShake._instance != null)
            ProCamera2DShake._instance.shakeStrengthModifier = savedScreenshake;
        /*float volumeVal;
        bool result = audioMixer.GetFloat("volume", out volumeVal);
        if (result)
            volumeSlider.value = 1f - (volumeVal / -60.0f);
        else
            volumeSlider.value = 1f;*/
        
        gameObject.SetActive(false);

        // Screen shake slider
        // TODO: SAVE FOR AFTER SAVING and figuring out where we gon' store this mother fucker
    }

    private void Update()
    {
        if (isRebinding)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                leaveKeybinding();
                return; // cancel
            }

            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        activeButtonText.text = keyCode.ToString();
                        HandleKeyBindChangefor(activeControlToRebind, keyCode);
                        keybindCache[activeControlToRebind] = keyCode;
                        saveFlag = true;                                               
                        break;
                    }
                }
                leaveKeybinding();
            }
        }
    }

    private void OnEnable()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != "TitleScreen")
        {
            saveObj.SetActive(true);
            exitObj.SetActive(true);
            confirmExitObj.SetActive(false);
            // Exit no longer triggers a save, so the combat/bleeding interlock that
            // used to gray this button out is gone — it's always interactable.
            saveButtonObj.interactable = true;
            saveButtonText.color = Color.white;
        }
    }

    private void OnDisable()
    {
        if(menuPopupText != null)
            menuPopupText.gameObject.SetActive(false);

        if(saveFlag)
        {
            saveFlag = false;

            ES3.Save("SavedResolution", savedResolutionIndex, "Settings.es3");
            ES3.Save("Fullscreen", savedFullscreen, "Settings.es3");
            ES3.Save("Quality", savedQuality, "Settings.es3");
            ES3.Save("Volume", savedVolume, "Settings.es3");
            ES3.Save("Screenshake", savedScreenshake, "Settings.es3");
            ES3.Save("ButtonKeys", keybindCache, "Settings.es3");
        }
    }

    private void loadInKeybindings()
    {
        ensureKeybindsLoaded();
        foreach(KeyValuePair<string, KeyCode> binding in keybindCache)
        {
            HandleKeyBindChangefor(binding.Key, binding.Value);
        }
    }

    // Lazily builds the merged keybind cache: a fresh copy of the defaults with any saved
    // ButtonKeys overrides layered on top. Loaded once per session and reused thereafter.
    private static void ensureKeybindsLoaded()
    {
        if (keybindCache != null)
            return;

        keybindCache = new Dictionary<string, KeyCode>(DEFAULT_BUTTON_KEYS);

        // KeyExists (not just FileExists): an older/partial Settings.es3 may lack the
        // ButtonKeys key, in which case we cleanly keep the defaults rather than throwing.
        if (ES3.KeyExists("ButtonKeys", "Settings.es3"))
        {
            Dictionary<string, KeyCode> saved = ES3.Load<Dictionary<string, KeyCode>>("ButtonKeys", "Settings.es3");
            foreach (KeyValuePair<string, KeyCode> binding in saved)
                keybindCache[binding.Key] = binding.Value;
        }
    }

    public static bool tryGetDisplayKeyBinding(string controlName, out KeyCode keyCode)
    {
        ensureKeybindsLoaded();
        return keybindCache.TryGetValue(controlName, out keyCode);
    }

    private IEnumerator fadeTextInAndOut(TextMeshProUGUI text, float timeToHold)
    {
        /*
        Color tempCol = text.color;
        tempCol.a = 0.01f;
        text.color = tempCol;*/
        text.gameObject.SetActive(true);
        text.CrossFadeAlpha(0f, 0.01f, false);
        text.CrossFadeAlpha(1f, 0.5f, false);
        yield return new WaitForSeconds(timeToHold + 0.5f);
        text.CrossFadeAlpha(0f, 0.5f, false);
        yield return new WaitForSeconds(0.5f);
        text.gameObject.SetActive(false);
    }

    public void backButton()
    {
        playButtonClickSound();
        menuPopupText.gameObject.SetActive(false);
    }

    public void warnLostProgress()
    {
        playButtonClickSound();
        StopAllCoroutines();
        menuPopupText.CrossFadeAlpha(1f, 0.5f, false);
        menuPopupText.gameObject.SetActive(true);
        menuPopupText.GetComponent<LocalizeUI>().fieldName = "Progress warning";
        menuPopupText.GetComponent<LocalizeUI>().UpdateText();

        if (SaveManager.instance.tryGetLastSaveTime(out System.DateTime saved))
            menuPopupText.text = menuPopupText.text + "\n" + saved.ToString("g");
    }


    public void exitButton()
    {
        AudioMaster.instance.PlaySound("Deep Select");
        // Drain pending background save flushes so an autosave that triggered just
        // before the player hit Exit is on disk before we tear down the scene.
        SaveManager.instance.WaitForActiveSave();
        GameEvents.current.returnToTitle();
        LevelLoader.instance.loadLevel("TitleScreen");
    }

    // Mirrors title-screen Continue: loads the scene named in the save file (which
    // may differ from the player's current scene) so the result is identical to
    // re-entering the game from the main menu.
    public void reloadLastSave()
    {
        string slotName = SaveManager.instance.activeSlotName;
        if (string.IsNullOrEmpty(slotName)) return;
        // Drain pending background save flushes before reading the slot file —
        // an autosave/camp save still mid-flush would otherwise let us read stale
        // bytes (or race the background writer outright).
        SaveManager.instance.WaitForActiveSave();
        if (!ES3.FileExists(slotName + ".es3")) return;
        string sceneToLoad = ES3.Load<string>("Scene", slotName + ".es3");
        LevelLoader.instance.loadLevel(sceneToLoad);
    }

    public void playButtonClickSound()
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
    }

    private void HandleKeyBindChangefor(string controlName, KeyCode keyCode)
    {        
        if(PlayerController.instance != null)
        {
            PlayerController.instance.updateKeyBindings(controlName, keyCode);
        }
    }

    private void leaveKeybinding()
    {
        keybindHelperText.SetActive(false);
        returnToMainSettingsButton.SetActive(true);
        myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);

        isRebinding = false;
        if (Menus.instance != null)
            Menus.instance.isRebinding = false;
    }

    public void rebindControl(string controlToRebindName, TextMeshProUGUI buttonText)
    {
        activeControlToRebind = controlToRebindName;
        activeButtonText = buttonText;
        isRebinding = true;
        if (Menus.instance != null)
            Menus.instance.isRebinding = true;

        keybindHelperText.SetActive(true);
        returnToMainSettingsButton.SetActive(false);
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", Mathf.Log10(volume) * 20);

        if (volume != savedVolume)
        {
            savedVolume = volume;
            saveFlag = true;
        }
    }

    public void SetScreenshake(float screenshake)
    {
        // TO DO - screenshake functionality
        if (ProCamera2DShake._instance != null)
            ProCamera2DShake._instance.shakeStrengthModifier = screenshake;

        if (screenshake != savedScreenshake)
        {
            savedScreenshake = screenshake; 
            saveFlag = true;
        }
    }

    public float getVolume()
    {
        return savedVolume;
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);

        if(index != savedQuality)
        {
            savedQuality = index;
            saveFlag = true;
        }       
    }

    public void SetFullScreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if(isFullscreen != savedFullscreen)
        {
            savedFullscreen = isFullscreen;
            saveFlag = true;
        }       
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= resolutions.Length)
            resolutionIndex = 0;

        if (savedResolutionIndex != resolutionIndex)
        {
            savedResolutionIndex = resolutionIndex;
            saveFlag = true;
        }

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    private void initializeResolutions()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string opt = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(opt);

            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        if (savedResolutionIndex.ToString() == "")
        {
            savedResolutionIndex = currentResolutionIndex;
            ES3.Save("ResolutionIndex", savedResolutionIndex, "Settings.es3");
        }
    }
}
