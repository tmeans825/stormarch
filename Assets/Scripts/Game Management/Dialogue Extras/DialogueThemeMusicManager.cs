using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DialogueThemeMusicManager : MonoBehaviour
{
    private string activeThemeSong = "";

    void OnConversationStart(Transform actor)
    {
        // actor is the player; the NPC is the conversant. Null conversant = note/recording.
        Transform conversant = DialogueManager.currentConversant;
        if (conversant == null)
            return;

        Entity entity = conversant.GetComponent<Entity>();
        if (entity == null || string.IsNullOrEmpty(entity.themeSongName))
            return;

        AudioMaster.instance.PlayMusic(entity.themeSongName, 1f);
        activeThemeSong = entity.themeSongName;
    }

    void OnConversationEnd(Transform actor)
    {
        if (string.IsNullOrEmpty(activeThemeSong))
            return;

        AudioMaster.instance.stopMusicAndResumeAmbientIfActive(activeThemeSong);
        activeThemeSong = "";
    }
}
