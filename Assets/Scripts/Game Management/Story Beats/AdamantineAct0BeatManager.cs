using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Beat manager for the Act0_Adamantine prologue (you play Thomas Winguard).
/// Rough draft: every public "EVENT" method is meant to be wired from a Beat Trigger / dialogue in the scene.
///
/// Replay rule: loadBeats() only RESTORES end-state (activate/deactivate objects, reposition). The one-shot
/// transitions (cinematics, quest changes, sounds, fades) live in the EVENT methods and are guarded by the
/// current beat, so a save/load never re-plays them.
/// </summary>
public class AdamantineAct0BeatManager : sceneBeatManager
{
    // ---- Beat lines ----
    // Beat line 0 - main arc
    //   1 => woke from daydream (scene start)
    //   2 => all crates in the train
    //   3 => met Joni
    //   4 => has the pistol
    //   5 => assembly / closure announcement done
    //   6 => left work
    //   7 => hawker (ration choice) done
    //   8 => home
    //   9 => left home
    //   10 => in the woods
    //   11 => wolvolin freeze moment done
    //   12 => Joni healed with the clotberry
    //   13 => reached the clearing
    //   14 => ambush
    //   15 => execution (end)
    // Beat line 1 - crates currently delivered to the train (0..CRATES_REQUIRED)
    // Beat line 2 - ration choice made (1 once the hawker conversation recorded "rationChoice")
    // Beat line 3 - bread line (1 => stood in line)
    private const int LINE_MAIN = 0;
    private const int LINE_CRATES = 1;
    private const int LINE_RATIONS = 2;
    private const int LINE_BREADLINE = 3;

    public const int CRATES_REQUIRED = 6;

    // Dialogue outcome ids (strings, write-once). Values are decided in the Hans/Bread Line conversations.
    public const string OUTCOME_RATION_CHOICE = "rationChoice"; // "gaveAll" | "bartered" | "boughtNormal"
    public const string OUTCOME_BREAD_LINE = "breadLineDone"; // "yes"

    private const string QUEST_FEED_FAMILY = "Keep the Family Fed";
    private const string QUEST_MEET_JONI = "Meet Joni in the Woods";

    [Header("Factory")]
    [ES3NonSerializable] public GameObject trainDoorBlocker; // optional: removed once the crates are loaded
    [ES3NonSerializable] public GameObject assemblyGuardians; // optional: shown during the assembly

    [Header("Joni")]
    [ES3NonSerializable] public NonCombatChar joni;
    [ES3NonSerializable] public Transform joniFactoryCornerPoint;
    [ES3NonSerializable] public Transform joniWoodsStartPoint;

    [Header("Woods")]
    [ES3NonSerializable] public GameObject woodsDarkness; // optional night-lighting object toggled on at the woods
    [ES3NonSerializable] public Enemy wolvolin;
    [ES3NonSerializable] public Clotberry clotberry;
    [Tooltip("Wolvolin counts as 'at Joni' inside this distance and triggers the time freeze")]
    public float freezeDistance = 6f;
    [Tooltip("Real-time seconds the player has to take the shot")]
    public float freezeTimeout = 6f;
    [Tooltip("Real-time seconds to wait for the wolvolin to reach Joni before freezing anyway")]
    public float rushTimeout = 8f;

    [Header("Clearing / ambush")]
    [ES3NonSerializable] public GameObject clearingCitizens;
    [ES3NonSerializable] public GameObject[] ambushers; // Guardians/Sentinels, start inactive
    [ES3NonSerializable] public GameObject littleSisterHostage;

    [Header("Ending")]
    [Tooltip("Sound names (AudioMaster) played, in order, in the dark after the cut to black")]
    public string[] executionSounds;
    public float executionSoundInterval = 0.35f;
    public float fadeToBlackDuration = 1.5f;
    public float blackHoldBeforeShots = 2.5f;
    public float blackHoldAfterShots = 3f;

    [Header("Bread line")]
    public float breadLineWaitSeconds = 8f;
    public float breadLineFadeSeconds = 1.25f;

    private bool breadLineRunning = false;
    private bool endingRunning = false;
    private bool wolvolinSequenceRunning = false;

    // Crates currently inside a drop zone, so reload/respawn can't double count (see crateCountChanged)
    [System.NonSerialized] private bool subscribedToTitle = false;

    // ---------- lifecycle ----------

    private void OnEnable()
    {
        subscribeToTitle();
    }

    protected override void Start()
    {
        base.Start();
        subscribeToTitle();
    }

    private void subscribeToTitle()
    {
        if (subscribedToTitle || GameEvents.current == null)
            return;

        GameEvents.current.onReturnToTitle += handleReturnToTitle;
        subscribedToTitle = true;
    }

    private void OnDisable()
    {
        unsubscribeFromTitle();
    }

    private void unsubscribeFromTitle()
    {
        if (!subscribedToTitle || GameEvents.current == null)
            return;

        GameEvents.current.onReturnToTitle -= handleReturnToTitle;
        subscribedToTitle = false;
    }

    // sceneBeatManager is a DontDestroyOnLoad singleton that never tears itself down, so if this one
    // survived to the title screen it would destroy the next scene's beat manager. Remove it explicitly.
    private void handleReturnToTitle()
    {
        unsubscribeFromTitle();
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;

        if (instance == this)
            instance = null;

        Destroy(gameObject);
    }

    protected override void initializeData()
    {
        beatMatrix = new int[4];
    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();

        // First entry only; replays on load skip this because the beat is already past 0.
        if (beatMatrix[LINE_MAIN] == 0)
            setBeat(LINE_MAIN, 1);
    }

    // Restore end-state only. NO cinematics, quest changes, sounds, fades or progressBeat in here.
    protected override void loadBeats()
    {
        int main = beatMatrix[LINE_MAIN];

        if (trainDoorBlocker != null)
            trainDoorBlocker.SetActive(main < 2);

        if (assemblyGuardians != null)
            assemblyGuardians.SetActive(main >= 5 && main < 6);

        if (woodsDarkness != null)
            woodsDarkness.SetActive(main >= 10);

        if (main >= 10 && joni != null)
        {
            // Joni comes with the player from the woods onward
            if (main < 14)
                joni.startFollowingPlayer();
        }
        else if (main >= 3 && main < 10 && joni != null)
        {
            // Joni is not with the player in the city/home; parked at the factory corner until the woods
            if (joniFactoryCornerPoint != null)
                joni.transform.position = joniFactoryCornerPoint.position;
        }

        if (main >= 11 && wolvolin != null)
            wolvolin.gameObject.SetActive(false);

        if (main >= 12 && clotberry != null && !clotberry.used)
        {
            // Clotberry state is saved by Clotberry itself; nothing to force here.
        }

        if (clearingCitizens != null)
            clearingCitizens.SetActive(main >= 13);

        if (main >= 14 && ambushers != null)
        {
            for (int i = 0; i < ambushers.Length; i++)
                if (ambushers[i] != null) ambushers[i].SetActive(true);
        }

        if (littleSisterHostage != null)
            littleSisterHostage.SetActive(main >= 14);
    }

    // ---------- EVENTS: factory ----------

    // Called by CrateDropZone whenever the number of crates inside the train changes.
    // Overwrites the count rather than incrementing it, so respawns/reloads can't double count.
    public void crateCountChanged(int crateCount)
    {
        crateCount = Mathf.Clamp(crateCount, 0, CRATES_REQUIRED);

        if (beatMatrix[LINE_CRATES] != crateCount)
            setBeat(LINE_CRATES, crateCount);

        if (crateCount >= CRATES_REQUIRED && beatMatrix[LINE_MAIN] < 2)
        {
            setBeat(LINE_MAIN, 2);
            if (trainDoorBlocker != null)
                trainDoorBlocker.SetActive(false);
        }
    }

    public void metJoni()
    {
        if (beatMatrix[LINE_MAIN] >= 3)
            return;

        setBeat(LINE_MAIN, 3);
        startQuestIfNew(QUEST_MEET_JONI);
    }

    // Wire to a Beat Trigger placed after the pistol room (or the pistol pickup).
    public void grabbedPistol()
    {
        if (beatMatrix[LINE_MAIN] >= 4)
            return;

        setBeat(LINE_MAIN, 4);
    }

    // The assembly hall announcement. Conversation title is a placeholder you can rewrite.
    public void startAssembly()
    {
        if (beatMatrix[LINE_MAIN] != 4)
            return; // needs the pistol first; also keeps the one-shot from replaying

        setBeat(LINE_MAIN, 5);

        if (assemblyGuardians != null)
            assemblyGuardians.SetActive(true);

        DialogueManager.StartConversation("Adamantine/Act 0/Assembly/Announcement");
    }

    public void leaveWork()
    {
        if (beatMatrix[LINE_MAIN] != 5)
            return;

        setBeat(LINE_MAIN, 6);

        if (assemblyGuardians != null)
            assemblyGuardians.SetActive(false);

        startQuestIfNew(QUEST_FEED_FAMILY);
    }

    // ---------- EVENTS: city ----------

    // Wire to a Beat Trigger as the player leaves the hawker. Does nothing until the choice was recorded.
    public void finishHawker()
    {
        if (beatMatrix[LINE_MAIN] != 6 || !NotableOutcomesTracker.instance.hasOutcome(OUTCOME_RATION_CHOICE))
            return;

        setBeat(LINE_RATIONS, 1);
        setBeat(LINE_MAIN, 7);
    }

    // Bread line: long real-time wait. Only offered if the player still holds a ration card.
    // Wire to the bread line NPC's dialogue end or an Interact trigger.
    public void standInBreadLine()
    {
        if (breadLineRunning
            || NotableOutcomesTracker.instance.hasOutcome(OUTCOME_BREAD_LINE)
            || NotableOutcomesTracker.instance.checkOutcome(OUTCOME_RATION_CHOICE, "gaveAll"))
            return;

        StartCoroutine(breadLineRoutine());
    }

    private IEnumerator breadLineRoutine()
    {
        breadLineRunning = true;
        PlayerController.instance.freezeChar(true);

        // try/finally so the reference-counted freeze is released exactly once whatever happens
        try
        {
            yield return CanvasMaster.instance.fadeBlack(true, breadLineFadeSeconds);
            yield return new WaitForSecondsRealtime(breadLineWaitSeconds);

            NotableOutcomesTracker.instance.recordOutcome(OUTCOME_BREAD_LINE, "yes");
            setBeat(LINE_BREADLINE, 1);

            yield return CanvasMaster.instance.fadeBlack(false, breadLineFadeSeconds);
        }
        finally
        {
            if (PlayerController.instance != null)
                PlayerController.instance.freezeChar(false);
            breadLineRunning = false;
        }
    }

    // Home reached (door trigger)
    public void arriveHome()
    {
        if (beatMatrix[LINE_MAIN] != 7)
            return;

        setBeat(LINE_MAIN, 8);
    }

    // Player heads out through the fence gap (after the family conversation)
    public void leaveHome()
    {
        if (beatMatrix[LINE_MAIN] != 8)
            return;

        setBeat(LINE_MAIN, 9);
    }

    // ---------- EVENTS: woods ----------

    public void enterWoods()
    {
        if (beatMatrix[LINE_MAIN] != 9)
            return;

        setBeat(LINE_MAIN, 10);

        if (woodsDarkness != null)
            woodsDarkness.SetActive(true);

        if (joni != null)
        {
            if (joniWoodsStartPoint != null)
                joni.transform.position = joniWoodsStartPoint.position;
            joni.startFollowingPlayer();
        }
    }

    // Wolvolin charges Joni; time freezes; the player gets one shot.
    public void startWolvolinRush()
    {
        if (beatMatrix[LINE_MAIN] != 10 || wolvolinSequenceRunning || wolvolin == null)
            return;

        StartCoroutine(wolvolinRoutine());
    }

    private IEnumerator wolvolinRoutine()
    {
        wolvolinSequenceRunning = true;

        wolvolin.gameObject.SetActive(true);

        // Wait for the wolvolin to close on Joni (real time, so it isn't affected by any slow-mo)
        float waited = 0f;
        while (waited < rushTimeout && wolvolin != null && !wolvolin.isDead)
        {
            Vector2 target = joni != null ? (Vector2)joni.transform.position : (Vector2)PlayerController.instance.transform.position;
            if (Vector2.Distance(wolvolin.transform.position, target) <= freezeDistance)
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (wolvolin != null && !wolvolin.isDead && TimeFreezeController.instance != null)
        {
            TimeFreezeController.instance.freeze(freezeTimeout);

            // freeze() ends itself on the player's bullet, a timeout, death or title return
            while (TimeFreezeController.instance != null && TimeFreezeController.instance.isFrozen)
                yield return null;
        }

        // Give the shot a moment to land; don't soft-lock if the player missed.
        float settle = 0f;
        while (settle < 4f && wolvolin != null && !wolvolin.isDead)
        {
            settle += Time.deltaTime;
            yield return null;
        }

        setBeat(LINE_MAIN, 11);
        wolvolinSequenceRunning = false;
    }

    // Joni walks to the clotberry bush and uses it to stop the bleeding. Wire after the wolvolin conversation.
    public void joniUsesClotberry()
    {
        if (beatMatrix[LINE_MAIN] != 11 || joni == null)
            return;

        setBeat(LINE_MAIN, 12);
        StartCoroutine(joniClotberryRoutine());
    }

    private IEnumerator joniClotberryRoutine()
    {
        if (clotberry != null)
        {
            joni.stopFollowingPlayer();
            joni.startMoving(clotberry.transform.position);

            float waited = 0f;
            while (waited < 6f && Vector2.Distance(joni.transform.position, clotberry.transform.position) > 1.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!clotberry.used)
                clotberry.useClotberry();
        }

        joni.startFollowingPlayer();
    }

    // ---------- EVENTS: clearing / ending ----------

    public void reachClearing()
    {
        if (beatMatrix[LINE_MAIN] != 12)
            return;

        setBeat(LINE_MAIN, 13);

        if (clearingCitizens != null)
            clearingCitizens.SetActive(true);

        if (joni != null)
            joni.stopFollowingPlayer();
    }

    // Guardians and Sentinels emerge and encircle the group, holding the little sister
    public void ambushStart()
    {
        if (beatMatrix[LINE_MAIN] != 13)
            return;

        setBeat(LINE_MAIN, 14);

        if (ambushers != null)
        {
            for (int i = 0; i < ambushers.Length; i++)
                if (ambushers[i] != null) ambushers[i].SetActive(true);
        }

        if (littleSisterHostage != null)
            littleSisterHostage.SetActive(true);

        StartCoroutine(ambushConversationRoutine());
    }

    private IEnumerator ambushConversationRoutine()
    {
        DialogueManager.StartConversation("Adamantine/Act 0/Ambush/Dont Shoot");

        // StartConversation is not instant; wait for it to begin, then to end, before the execution
        float waited = 0f;
        while (!DialogueManager.isConversationActive && waited < 2f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        while (DialogueManager.isConversationActive)
            yield return null;

        executionSequence();
    }

    // Runs after the ambush conversation ends (see ambushConversationRoutine). Also safe to wire manually.
    public void executionSequence()
    {
        if (beatMatrix[LINE_MAIN] != 14 || endingRunning)
            return;

        StartCoroutine(executionRoutine());
    }

    private IEnumerator executionRoutine()
    {
        endingRunning = true;

        // Time must be normal before any transition, since LevelLoader fades run on scaled time
        if (TimeFreezeController.instance != null)
            TimeFreezeController.instance.endFreeze(false);

        PlayerController.instance.freezeChar(true);
        setBeat(LINE_MAIN, 15);

        yield return CanvasMaster.instance.fadeBlack(true, fadeToBlackDuration);
        yield return new WaitForSecondsRealtime(blackHoldBeforeShots);

        if (executionSounds != null)
        {
            for (int i = 0; i < executionSounds.Length; i++)
            {
                if (!string.IsNullOrEmpty(executionSounds[i]) && AudioMaster.instance != null)
                    AudioMaster.instance.PlaySound(executionSounds[i]);
                yield return new WaitForSecondsRealtime(executionSoundInterval);
            }
        }

        yield return new WaitForSecondsRealtime(blackHoldAfterShots);

        // Standalone prologue: finish on the title screen. Same teardown path as the pause menu's exit.
        if (SaveManager.instance != null)
            SaveManager.instance.WaitForActiveSave();

        GameEvents.current.returnToTitle(); // this manager destroys itself here; nothing after the next line needs it
        LevelLoader.instance.loadLevel("TitleScreen");
    }

    // ---------- helpers ----------

    // Alerts are not automatic: they only fire when a quest state is explicitly set.
    private void startQuestIfNew(string questName)
    {
        if (QuestLog.GetQuestState(questName) == QuestState.Unassigned)
            QuestLog.StartQuest(questName);
    }
}
