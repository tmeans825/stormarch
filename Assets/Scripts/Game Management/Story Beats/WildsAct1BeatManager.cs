using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using UnityEngine.Experimental.Rendering.Universal;
using Com.LuisPedroFonseca.ProCamera2D;

public class WildsAct1BeatManager : sceneBeatManager
{
    [ES3NonSerializable]
    public GameObject storyOwen;
    [ES3NonSerializable]
    public GameObject wolvoRock;
    [ES3NonSerializable]
    public NonCombatChar hermit;
    [ES3NonSerializable]
    public GameObject hermitWalkTrigger;
    [ES3NonSerializable]
    public CircleCollider2D[] cyperminiumColliders;
    [ES3NonSerializable]
    public GameObject fallenTree;
    [ES3NonSerializable]
    public NonCombatChar stranger;
    [ES3NonSerializable]
    public GameObject campFire;
    [ES3NonSerializable]
    public GameObject realBox;
    [ES3NonSerializable]
    public GameObject itriCorpse;

    private void OnEnable()
    {
        subscribeToPlayerDeath();
    }

    private void OnDisable()
    {
        unsubscribeFromPlayerDeath();
    }

    protected override void Start()
    {
        base.Start();
        subscribeToPlayerDeath();
    }

    protected override void initializeData()
    {
        beatMatrix = new int[7];

        // Beat line 0 - Owen & Camp
        // 1 => Player has radio
        // 2 => Player has called down the camp
        // 3 => Owen left

        // Beat line 1 - Thorvald and Ashland quest
        // 1 => Quest given, basin open
        // 2 => wolvos fight begin
        // 3 => wolvos defeated

        // Beat line 2 - The Hermit
        // 1 => Player has spoken to the Hermit
        // 2 => Hermit is going to tell the player about the storm caller
        // 3 => Hermit has given player the firebug

        // Beat line 3 - The box
        // 1 => Player has fake box
        // 2 => Player has the real box

        // Beat line 4 - Itri 
        // 1 => Itri is following the player 
        // 2 => Itri escaped
        // 3 => Itri is dead outside
        // 4 => Itri is dead inside

        // Beat line 5 - Itri exposure
        // 1 => Itri is exposed

        // Beat line 6 - the fallen tree
        // 1 => the tree has fallen
        // 2 => the tree has been cleared
    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();
        PlayerController.instance.hasBaton = true;

        // Beat line 2 stage 3 means the Hermit has handed over the firebug, so any path that
        // reaches it without the dialogue grant (dev skip => 99, forced matrix) gets one here.
        if(beatMatrix[2] >= 3 && PlayerController.instance.getMaxFirebugs() == 0)
        {
            PlayerController.instance.addMaxFirebugs();
        }
    }

    protected override void loadBeats()
    {
        if(beatMatrix[0] < 1)
        {
            foreach (CircleCollider2D col in cyperminiumColliders)
            {
                col.enabled = false;
            }
        }

        if(beatMatrix[0] >= 3)
        {
            storyOwen.SetActive(false);
        }

        if (beatMatrix[1] >= 1)
        {
            wolvoRock.SetActive(false);
        }

        if (beatMatrix[2] == 2)
        {
            deployHermitToTalkToPlayer();
        }

        if(beatMatrix[4] == 1)
        {
            stranger.startFollowingPlayer();
        }
        else if(beatMatrix[4] == 3)
        {
            killItriOutside();
            stranger.gameObject.SetActive(false);
        }
        else if(beatMatrix[4] == 4)
        {
            killItriInside();
        }
        
        if(beatMatrix[4] > 1)
        {
            stranger.gameObject.SetActive(false);
            campFire.SetActive(false);
        }

        if(beatMatrix[5] >= 1)
        {
            exposeItri();
        }

        if(beatMatrix[6] == 1)
        {
            fallenTree.SetActive(true);
        }
    }

    // EVENTS
    public void calledCamp()
    {
        if(beatMatrix[0] == 1)
        {
            progressBeat(0);
        }
    }

    public void owenDeparts()
    {
        progressBeat(0);
        storyOwen.GetComponent<NonCombatChar>().startMoving(new Vector3(79.3f, 0.1f));
    }

    public void beginWolvoFight()
    {
        progressBeat(1);
        QuestLog.SetQuestEntryState("Wildlife Mismanagement", 1, "success");
        QuestLog.SetQuestEntryState("Wildlife Mismanagement", 2, "active");
        wolvoRock.SetActive(true);
    }

    public void completeWolvoQuest()
    {         
        progressBeat(1);
        QuestLog.SetQuestEntryState("Wildlife Mismanagement", 2, "success");
        QuestLog.SetQuestEntryState("Wildlife Mismanagement", 3, "active");
        wolvoRock.SetActive(false);
    }

    public void interactWithStormCaller()
    {
        if(beatMatrix[2] == 1) // only after initial hermit convo
        {
            progressBeat(2);
            deployHermitToTalkToPlayer();
        }   
    }

    public void hermitCircle()
    {
        hermit.startMoving(new Vector3(-42.75f, -101.95f));
    }

    public void hermitHideFromStorm()
    {
        progressBeat(2);
        hermit.startMoving(new Vector3(-71.1f, -139.19f));
    }

    public void strangerGoesOutside()
    {
        stranger.baseSpeed = 12f;
        stranger.startMoving(new Vector3(-27f, -50.8f));
        StartCoroutine(deactivateStrangerAfterDelay(5f));
    }

    public void strangerFollowsPlayer()
    {
        stranger.startFollowingPlayer();
    }

    public void itriDiesOutside(bool makeCorpse = true)
    {
        setBeat(4,3); // Itri is dead outside
        NotableOutcomesTracker.instance.recordOutcome("Itri Act 1","Dead");
        killItriOutside(makeCorpse);
    }

    public void itriDiesInside()
    {
        setBeat(4,4); // Itri is dead inside
        NotableOutcomesTracker.instance.recordOutcome("Itri Act 1","Dead");
        killItriInside();
    }

    public void itriLives()
    {
        setBeat(4,2); // Itri escaped
        NotableOutcomesTracker.instance.recordOutcome("Itri Act 1","Alive");
        stranger.stopFollowingPlayer();
        stranger.baseSpeed = 12f;
        stranger.startMoving(new Vector3(56.1f, -296.3f));
        StartCoroutine(deactivateStrangerAfterDelay(10f));
    }

    public void exposeItri()
    {
        DialogueActor dialogueActor = stranger.GetComponent<DialogueActor>();
        string previousActor = dialogueActor.actor;
        if (previousActor == "Itri")
            return; // already exposed (loadBeats replay)

        // Toggle enabled so OnDisable/OnEnable swap the actor-transform registration
        // (the registry would otherwise keep the stale "Stranger" entry).
        dialogueActor.enabled = false;
        dialogueActor.actor = "Itri";
        dialogueActor.enabled = true;

        // The active conversation cached CharacterInfo at conversation start — patch it in
        // place so the remaining lines of the exposure conversation show the new name.
        if (DialogueManager.isConversationActive && DialogueManager.conversationModel != null)
        {
            Actor previousDbActor = DialogueManager.masterDatabase.GetActor(previousActor);
            if (previousDbActor != null)
            {
                PixelCrushers.DialogueSystem.CharacterInfo info = DialogueManager.conversationModel.GetCharacterInfo(previousDbActor.id);
                info.Name = PixelCrushers.DialogueSystem.CharacterInfo.GetLocalizedDisplayNameInDatabase("Itri");
                Actor itriDbActor = DialogueManager.masterDatabase.GetActor("Itri");
                if (itriDbActor != null && itriDbActor.GetPortraitSprite() != null)
                    info.portrait = itriDbActor.GetPortraitSprite();
            }
        }
    }

    public void treeFalls()
    {
        setBeat(6,1); // the tree has fallen
        fallenTree.SetActive(true);
    }

    public void treeCleared()
    {
        setBeat(6,2); // the tree has been cleared
        fallenTree.SetActive(false);
    }

    // Helpers
    private void deployHermitToTalkToPlayer()
    {
        hermitWalkTrigger.gameObject.SetActive(true);
        hermit.startMoving(new Vector3(-37.25f, -105.98f));
    }

    private void killItriInside()
    {
        campFire.SetActive(false);
        stranger.gameObject.SetActive(false);
        itriCorpse.SetActive(true);

        if(beatMatrix[3] < 2 && !realBox.activeSelf)
        {
            realBox.SetActive(true);
        }
    }

    private void killItriOutside(bool makeCorpse = true)
    {
        if (makeCorpse)
        {
            itriCorpse.SetActive(true);
            itriCorpse.transform.localPosition = new Vector3(-28.08f, -66.39f);
        }

        if(beatMatrix[3] < 2 && !realBox.activeSelf)
        {
            realBox.SetActive(true);
            realBox.transform.localPosition = new Vector3(-27.21f, -63.21f);
        }
    }

    private IEnumerator deactivateStrangerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        stranger.gameObject.SetActive(false);
    }

    private void subscribeToPlayerDeath()
    {
        if (GameEvents.current == null)
            return;

        GameEvents.current.onPlayerDeath -= monitorItriPlayerDeath;
        GameEvents.current.onPlayerDeath += monitorItriPlayerDeath;
    }

    private void unsubscribeFromPlayerDeath()
    {
        if (GameEvents.current != null)
            GameEvents.current.onPlayerDeath -= monitorItriPlayerDeath;
    }

    private void monitorItriPlayerDeath()
    {
        if (beatMatrix != null && beatMatrix.Length > 4 && beatMatrix[4] == 1)
        {
            itriDiesOutside(false);
        }
    }

}
