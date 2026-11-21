using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Per-scene override of who the player character is (name + optional look). Placed only in scenes where the
/// protagonist isn't the default (e.g. the Act0_Adamantine prologue, where you play Thomas Winguard).
/// Applies after saved data loads, and restores the default protagonist when the scene is left so the
/// override can't leak into later scenes or new games.
/// </summary>
public class PlayerProfileApplier : MonoBehaviour
{
    [Header("Profile (dialogue FirstName/LastName variables + Player actor display name)")]
    public string firstName = "Thomas";
    public string lastName = "Winguard";

    [Header("Optional look (leave blank to keep the current look)")]
    public string hairSpritesheet = "";
    public string skinSpritesheet = "";

    [Header("Default protagonist restored on exit")]
    public string defaultFirstName = "Jaden";
    public string defaultLastName = "Hayes";

    private const string PLAYER_ACTOR = "Player";
    private const string DISPLAY_NAME_FIELD = "Display Name";

    private bool applied = false;

    private bool subscribed = false;

    void OnEnable()
    {
        subscribe();
    }

    // GameEvents.current may not exist yet during scene load (Awake order isn't guaranteed), so retry in Start.
    void Start()
    {
        subscribe();
    }

    private void subscribe()
    {
        if (subscribed || GameEvents.current == null)
            return;

        GameEvents.current.onPreviousSceneDataLoaded += apply;
        GameEvents.current.onReturnToTitle += restoreDefault;
        subscribed = true;
    }

    void OnDisable()
    {
        if (subscribed && GameEvents.current != null)
        {
            GameEvents.current.onPreviousSceneDataLoaded -= apply;
            GameEvents.current.onReturnToTitle -= restoreDefault;
        }
        subscribed = false;
    }

    void OnDestroy()
    {
        // Also covers leaving the scene by any route other than the title screen.
        restoreDefault();
    }

    private void apply()
    {
        // previousSceneDataLoaded can fire more than once per scene load path; the writes are idempotent
        setNames(firstName, lastName);

        PlayerController player = PlayerController.instance;
        if (player != null)
        {
            if (!string.IsNullOrEmpty(hairSpritesheet))
            {
                player.hairSpriteSheetName = hairSpritesheet;
                player.changeHairSprites(hairSpritesheet);
            }

            if (!string.IsNullOrEmpty(skinSpritesheet))
            {
                player.skinSpriteSheetName = skinSpritesheet;
                player.changeSkinColor(skinSpritesheet);
            }
        }

        applied = true;
    }

    private void restoreDefault()
    {
        if (!applied)
            return;

        applied = false;

        // Quitting play mode tears down the Dialogue System before scene objects
        if (!DialogueManager.hasInstance)
            return;

        setNames(defaultFirstName, defaultLastName);
    }

    private void setNames(string first, string last)
    {
        // The Dialogue Database's FirstName ends in a space ("Jaden "); dialogue text concatenates the two variables.
        first = first.Trim();
        last = last.Trim();
        DialogueLua.SetVariable("FirstName", first + " ");
        DialogueLua.SetVariable("LastName", last);
        DialogueLua.SetActorField(PLAYER_ACTOR, DISPLAY_NAME_FIELD, first + " " + last);
    }
}
