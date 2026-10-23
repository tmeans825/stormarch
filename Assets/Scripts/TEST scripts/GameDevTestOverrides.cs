using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class GameDevTestOverrides : MonoBehaviour
{
    [System.NonSerialized]
    public static GameDevTestOverrides instance;

    [ES3NonSerializable]
    public bool enableGameDevOverrides = false;

    [Header("Test Guns")]
    [ES3NonSerializable]
    public bool forceTestGunCharms = false;
    [ES3NonSerializable]
    public int forcedTestGunRarity = 4;
    [ES3NonSerializable]
    public TestGunCharmSet[] testGunCharmSets = new TestGunCharmSet[]
    {
        new TestGunCharmSet { model = TestGunModel.All },
        new TestGunCharmSet { model = TestGunModel.Rifle },
        new TestGunCharmSet { model = TestGunModel.Pistol },
        new TestGunCharmSet { model = TestGunModel.Shotgun },
    };

    [Header("Test Armor (chest only — legs/helm never get charms)")]
    [ES3NonSerializable]
    public bool forceTestArmorCharms = false;
    [ES3NonSerializable]
    public int forcedTestArmorRarity = 4;
    [ES3NonSerializable]
    public Armor.armorWeight forcedTestArmorWeight = Armor.armorWeight.Heavy;
    [ES3NonSerializable]
    public string[] testArmorCharmIds = new string[0];

    [Header("Test Skill Tree")]
    [ES3NonSerializable]
    public bool forceSkillTreeUnlocks = false;
    [ES3NonSerializable]
    public TestSkillUnlock[] skillTreeUnlocks = new TestSkillUnlock[0];

    [Header("Test Story / Spawning")]
    [ES3NonSerializable]
    public bool forceSkipStoryBeats = false;
    [ES3NonSerializable]
    public bool forceSpawnTestEquipment = false;

    [Header("Test Map (read-time only; never written to the save)")]
    [ES3NonSerializable]
    public bool forceAllLocationsDiscovered = false;

    [Header("Test Beat Matrix (drag a beat manager + values; blank lines keep loaded values; applied once per save, editing values re-applies)")]
    [ES3NonSerializable]
    public bool forceBeatMatrixValues = false;
    [ES3NonSerializable]
    public TestBeatMatrixOverride[] beatMatrixOverrides = new TestBeatMatrixOverride[0];

    [Header("Test Quest States (applied once per save after dialogue data loads; editing values re-applies)")]
    [ES3NonSerializable]
    public bool forceQuestStates = false;
    [ES3NonSerializable]
    public TestQuestOverride[] questOverrides = new TestQuestOverride[0];

    [Header("Test Player Movement")]
    [ES3NonSerializable]
    public bool noclipAndTripleSpeed = false;
    private bool freeMovementApplied = false;

    private Dictionary<string, Charm> weaponCharmsById;
    private Dictionary<string, Charm> armorCharmsById;

    // "Already applied" markers live in the save next to the state they protect (beat matrix on
    // the beat manager, quest states in Dialogue System Lua), so a restarted play session doesn't
    // re-stomp progress made after the override was baked in.
    private const string BEAT_OVERRIDE_SIGNATURE_VERSION = "v1:";
    private const string QUEST_OVERRIDE_SIGNATURE_VARIABLE = "GameDevTestOverrides_QuestSignature_v1";

    private bool subscribedToSceneDataLoaded = false;

    // Master gate shared by every dev override. Cheats can never fire in a release build.
    private bool overridesActive => Debug.isDebugBuild && enableGameDevOverrides;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    private void OnEnable()
    {
        trySubscribeToSceneDataLoaded();
    }

    private void Start()
    {
        // OnEnable can run before GameEvents.current exists; Start is the guaranteed retry.
        trySubscribeToSceneDataLoaded();
    }

    private void OnDisable()
    {
        if (!subscribedToSceneDataLoaded || GameEvents.current == null)
            return;

        GameEvents.current.onPreviousSceneDataLoaded -= applyForcedQuestStates;
        subscribedToSceneDataLoaded = false;
    }

    private void trySubscribeToSceneDataLoaded()
    {
        if (subscribedToSceneDataLoaded || instance != this || GameEvents.current == null)
            return;

        GameEvents.current.onPreviousSceneDataLoaded += applyForcedQuestStates;
        subscribedToSceneDataLoaded = true;
    }

    // Centralized cheat-key polling. Previously these lived (ungated) in PlayerController.processInputs().
    private void Update()
    {
        if (!overridesActive)
            return;

        // Player-affecting cheats mirror the old "if (!isDead)" wrapper they ran inside.
        PlayerController player = PlayerController.instance;
        if (player != null && !player.isDead)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                player.damageCharacter(50f, new Vector2(-1f, 0f), false, false, true, true);

            if (Input.GetKeyDown(KeyCode.F1))
                player.damageCharacter(3f, new Vector2(1f, 0f), false, false, true, true);

            if (Input.GetKeyDown(KeyCode.Equals))
                player.devUpgradeBaton();

            if (Input.GetKeyDown(KeyCode.Alpha9))
                player.updateCurrencies(0, 10);

            if (Input.GetKeyDown(KeyCode.Alpha0))
                player.updateCurrencies(1, 0);

            if (Input.GetKeyDown(KeyCode.Alpha8))
                player.updateCurrencies(0, -10);

            if (Input.GetKeyDown(KeyCode.Alpha7))
                player.updateCurrencies(-10, 0);
            
            if(Input.GetKeyDown(KeyCode.Alpha1))
                player.updateAlignment(10, 0);

            if(Input.GetKeyDown(KeyCode.Alpha2))
                player.updateAlignment(-10, 0);
            
            if(Input.GetKeyDown(KeyCode.Alpha3))
                player.updateAlignment(0, 10);

            if(Input.GetKeyDown(KeyCode.Alpha4))
                player.updateAlignment(0, -10);

            if (Input.GetKeyDown(KeyCode.K))
                player.devToggleGodMode();

            if (noclipAndTripleSpeed != freeMovementApplied)
            {
                player.devSetFreeMovement(noclipAndTripleSpeed);
                freeMovementApplied = noclipAndTripleSpeed;
            }
        }

        if (Input.GetKeyDown(KeyCode.Minus) && HomesteadUIBehavior.instance != null)
            HomesteadUIBehavior.instance.tierUp();

        if (Input.GetKeyDown(KeyCode.P) && sceneBeatManager.instance != null)
            sceneBeatManager.instance.debugProgressQuests();
    }

    public bool shouldSkipStoryBeats()
    {
        // A forced beat matrix for the active scene's manager takes precedence over the blanket
        // skip — otherwise per-scene skip shortcuts (e.g. OwensAct1BeatManager.loadBeats jamming
        // beatMatrix[0] = 5 on every load) would stomp the forced values right after apply.
        return overridesActive && forceSkipStoryBeats && !hasForcedBeatMatrixFor(sceneBeatManager.instance);
    }

    private bool hasForcedBeatMatrixFor(sceneBeatManager manager)
    {
        return forceBeatMatrixValues && manager != null && hasAnyForcedBeatValue(findBeatMatrixOverride(manager.GetType()));
    }

    // A beat line is forced only when its entry has text — blank/null lines mean "keep the
    // loaded value", so an all-blank override forces nothing (and doesn't block Skip Story Beats).
    private bool hasAnyForcedBeatValue(TestBeatMatrixOverride matrixOverride)
    {
        if (matrixOverride == null || matrixOverride.beatValues == null)
            return false;

        for (int i = 0; i < matrixOverride.beatValues.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(matrixOverride.beatValues[i]))
                return true;
        }

        return false;
    }

    // Called by sceneBeatManager right before loadBeats(), after ES3 has restored beatMatrix.
    // Copies the configured values into the live (ES3-saved) matrix, once per save — the next
    // autosave bakes them (and the applied signature) into the save, which is the point of the
    // tool. Editing the values changes the signature and re-applies once. Blank beat lines are
    // left at their loaded values; only lines with an explicit number are forced.
    public bool TryApplyForcedBeatMatrix(sceneBeatManager manager)
    {
        if (!Debug.isDebugBuild || !enableGameDevOverrides || !forceBeatMatrixValues)
            return false;

        if (manager == null || manager.beatMatrix == null)
            return false;

        TestBeatMatrixOverride matrixOverride = findBeatMatrixOverride(manager.GetType());
        if (matrixOverride == null)
            return false;

        string managerName = manager.GetType().Name;

        if (matrixOverride.beatValues == null || matrixOverride.beatValues.Length != manager.beatMatrix.Length)
        {
            int configuredLength = matrixOverride.beatValues == null ? 0 : matrixOverride.beatValues.Length;
            Debug.LogError("GameDevTestOverrides beat matrix override for " + managerName + " has " + configuredLength + " values but its beatMatrix has " + manager.beatMatrix.Length + ". See the beat line comments in its initializeData().");
            return false;
        }

        // Parse every line before touching the matrix so a typo applies nothing, matching the
        // all-or-nothing length check above.
        int?[] forcedValues = new int?[matrixOverride.beatValues.Length];
        for (int i = 0; i < matrixOverride.beatValues.Length; i++)
        {
            string rawValue = matrixOverride.beatValues[i];
            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            if (!int.TryParse(rawValue.Trim(), out int parsedValue))
            {
                Debug.LogError("GameDevTestOverrides beat matrix override for " + managerName + " line " + i + " is '" + rawValue + "', which is not a whole number (leave it blank to keep the loaded value). No lines were applied.");
                return false;
            }

            forcedValues[i] = parsedValue;
        }

        // Built from parsed values so formatting-only edits ("01" vs "1") don't count as a new override.
        string signature = BEAT_OVERRIDE_SIGNATURE_VERSION + string.Join("|", forcedValues.Select(v => v.HasValue ? v.Value.ToString() : ""));
        if (manager.appliedTestBeatOverride == signature)
            return false;

        List<string> appliedLines = new List<string>();
        for (int i = 0; i < forcedValues.Length; i++)
        {
            if (forcedValues[i] == null)
                continue;

            manager.beatMatrix[i] = forcedValues[i].Value;
            appliedLines.Add(i + "->" + forcedValues[i].Value);
        }

        if (appliedLines.Count == 0)
            return false;

        manager.appliedTestBeatOverride = signature;

        if (forceSkipStoryBeats)
            Debug.LogWarning("GameDevTestOverrides: forced beat matrix takes precedence over Skip Story Beats for " + managerName + ".");

        int untouchedCount = forcedValues.Length - appliedLines.Count;
        Debug.Log("GameDevTestOverrides forced " + managerName + " beat lines [" + string.Join(", ", appliedLines) + "]" + (untouchedCount > 0 ? " (" + untouchedCount + " left at loaded values)." : "."));
        return true;
    }

    // Matched by concrete type, not reference: the inspector slot holds the beat manager
    // prefab asset, while the live manager is the scene's own (DontDestroyOnLoad) instance.
    private TestBeatMatrixOverride findBeatMatrixOverride(System.Type managerType)
    {
        if (beatMatrixOverrides == null)
            return null;

        for (int i = 0; i < beatMatrixOverrides.Length; i++)
        {
            TestBeatMatrixOverride candidate = beatMatrixOverrides[i];
            if (candidate != null && candidate.beatManager != null && candidate.beatManager.GetType() == managerType)
                return candidate;
        }

        return null;
    }

    // Fires on onPreviousSceneDataLoaded, after the Dialogue System's save data has been
    // restored. No autosave flagging here: DialogueAutosaver only listens during
    // conversations, so the forced states live in memory until the next normal save
    // (camp, cache, or scene transition) persists the dialogue singletons.
    private void applyForcedQuestStates()
    {
        if (!overridesActive || !forceQuestStates)
            return;

        if (questOverrides == null)
            return;

        string signature = buildQuestOverrideSignature();
        if (string.IsNullOrEmpty(signature) || DialogueLua.GetVariable(QUEST_OVERRIDE_SIGNATURE_VARIABLE).asString == signature)
            return;

        foreach (TestQuestOverride questOverride in questOverrides)
        {
            if (questOverride == null)
                continue;

            string questName = questOverride.questName == null ? "" : questOverride.questName.Trim();
            if (string.IsNullOrEmpty(questName))
                continue;

            if (DialogueManager.masterDatabase == null || DialogueManager.masterDatabase.GetItem(questName) == null)
            {
                Debug.LogError("GameDevTestOverrides could not find quest '" + questName + "' in the dialogue database.");
                continue;
            }

            if (questOverride.questState != TestQuestState.NoChange)
                QuestLog.SetQuestState(questName, toQuestState(questOverride.questState));

            if (questOverride.entries != null)
            {
                int entryCount = QuestLog.GetQuestEntryCount(questName);
                foreach (TestQuestEntryOverride entry in questOverride.entries)
                {
                    if (entry == null || entry.state == TestQuestState.NoChange)
                        continue;

                    if (entry.entryNumber < 1 || entry.entryNumber > entryCount)
                    {
                        Debug.LogError("GameDevTestOverrides quest '" + questName + "' entry " + entry.entryNumber + " is out of range (quest has " + entryCount + " entries).");
                        continue;
                    }

                    QuestLog.SetQuestEntryState(questName, entry.entryNumber, toQuestState(entry.state));
                }
            }

            Debug.Log("GameDevTestOverrides applied quest state overrides for '" + questName + "'.");
        }

        DialogueLua.SetVariable(QUEST_OVERRIDE_SIGNATURE_VARIABLE, signature);
    }

    // Canonical form of the configured quest overrides. Names are length-prefixed so delimiter
    // characters inside a quest title can't make two different configs collide.
    private string buildQuestOverrideSignature()
    {
        System.Text.StringBuilder signature = new System.Text.StringBuilder();

        foreach (TestQuestOverride questOverride in questOverrides)
        {
            if (questOverride == null)
                continue;

            string questName = questOverride.questName == null ? "" : questOverride.questName.Trim();
            if (string.IsNullOrEmpty(questName))
                continue;

            signature.Append(questName.Length).Append(':').Append(questName).Append('=').Append((int)questOverride.questState).Append(';');

            if (questOverride.entries != null)
            {
                foreach (TestQuestEntryOverride entry in questOverride.entries)
                {
                    if (entry == null || entry.state == TestQuestState.NoChange)
                        continue;

                    signature.Append(entry.entryNumber).Append('=').Append((int)entry.state).Append(',');
                }
            }

            signature.Append('/');
        }

        return signature.ToString();
    }

    private QuestState toQuestState(TestQuestState state)
    {
        switch (state)
        {
            case TestQuestState.Unassigned: return QuestState.Unassigned;
            case TestQuestState.Active: return QuestState.Active;
            case TestQuestState.Success: return QuestState.Success;
            case TestQuestState.Failure: return QuestState.Failure;
            default:
                Debug.LogError("GameDevTestOverrides received unexpected quest state " + state + ". Falling back to Active.");
                return QuestState.Active;
        }
    }

    public bool shouldSpawnTestEquipment()
    {
        return overridesActive && forceSpawnTestEquipment;
    }

    public bool shouldForceAllLocationsDiscovered()
    {
        return overridesActive && forceAllLocationsDiscovered;
    }

    public bool TryGetForcedTestGunCharms(int gunModel, out List<Charm> charms, out int? rarityOverride)
    {
        charms = null;
        rarityOverride = null;

        if (!Debug.isDebugBuild || !enableGameDevOverrides || !forceTestGunCharms)
            return false;

        TestGunCharmSet charmSet = getCharmSetForGunModel(gunModel);
        if (charmSet == null || charmSet.charmIds == null || charmSet.charmIds.Length == 0)
            return false;

        if (!ensureCharmLookup(Charm.appliesToWeaponString, ref weaponCharmsById))
            return false;

        if (!tryResolveForcedCharms(charmSet.charmIds, weaponCharmsById, out charms))
            return false;

        rarityOverride = forcedTestGunRarity;
        return true;
    }

    public bool TryGetForcedTestArmorCharms(out List<Charm> charms, out int? rarityOverride, out Armor.armorWeight? weightOverride)
    {
        charms = null;
        rarityOverride = null;
        weightOverride = null;

        if (!Debug.isDebugBuild || !enableGameDevOverrides || !forceTestArmorCharms)
            return false;

        if (testArmorCharmIds == null || testArmorCharmIds.Length == 0)
            return false;

        if (!ensureCharmLookup(Charm.appliesToArmorString, ref armorCharmsById))
            return false;

        if (!tryResolveForcedCharms(testArmorCharmIds, armorCharmsById, out charms))
            return false;

        rarityOverride = forcedTestArmorRarity;
        weightOverride = forcedTestArmorWeight;
        return true;
    }

    public bool TryApplyForcedSkillTree(PlayerController.playerSkill[,] skillTree)
    {
        if (!Debug.isDebugBuild || !enableGameDevOverrides || !forceSkillTreeUnlocks)
            return false;

        if (skillTree == null || skillTreeUnlocks == null)
            return false;

        bool appliedAny = false;

        foreach (TestSkillUnlock unlock in skillTreeUnlocks)
        {
            if (unlock == null)
                continue;

            getBuildingCoords(unlock.building, out int i, out int j);

            PlayerController.playerSkill skill = skillTree[i, j];
            if (skill == null)
            {
                Debug.LogError("GameDevTestOverrides could not find skill tree entry for building " + unlock.building + ".");
                continue;
            }

            skill.isPurchased = true;
            appliedAny = true;

            if (skill.upgrades == null)
                continue;

            if (unlock.unlockAllUpgrades)
            {
                for (int k = 0; k < skill.upgrades.Count; k++)
                    skill.upgrades[k] = true;
            }
            else if (unlock.upgradeIndices != null)
            {
                foreach (int upgradeIndex in unlock.upgradeIndices)
                {
                    if (upgradeIndex < 0 || upgradeIndex >= skill.upgrades.Count)
                    {
                        Debug.LogError("GameDevTestOverrides upgrade index " + upgradeIndex + " is out of range for building " + unlock.building + " (has " + skill.upgrades.Count + " upgrades).");
                        continue;
                    }

                    skill.upgrades[upgradeIndex] = true;
                }
            }
        }

        return appliedAny;
    }

    private void getBuildingCoords(SkillTreeBuilding building, out int i, out int j)
    {
        switch (building)
        {
            case SkillTreeBuilding.Armory: i = 0; j = 0; break;
            case SkillTreeBuilding.ExplosivesRange: i = 0; j = 1; break;
            case SkillTreeBuilding.TinkeringBench: i = 0; j = 2; break;
            case SkillTreeBuilding.Campsite: i = 0; j = 3; break;
            case SkillTreeBuilding.ComputerLab: i = 1; j = 0; break;
            case SkillTreeBuilding.FiringRange: i = 1; j = 1; break;
            case SkillTreeBuilding.MedicalTent: i = 1; j = 2; break;
            case SkillTreeBuilding.AmmoForge: i = 1; j = 3; break;
            case SkillTreeBuilding.AutoCultivator: i = 2; j = 0; break;
            case SkillTreeBuilding.ExperimentalFacility: i = 2; j = 1; break;
            case SkillTreeBuilding.GunResearchLab: i = 2; j = 2; break;
            case SkillTreeBuilding.BiotechWorkshop: i = 2; j = 3; break;
            default:
                Debug.LogError("GameDevTestOverrides received unknown skill tree building " + building + ". Falling back to Armory.");
                i = 0; j = 0; break;
        }
    }

    private bool tryResolveForcedCharms(string[] charmIds, Dictionary<string, Charm> lookup, out List<Charm> charms)
    {
        charms = null;

        List<Charm> resolvedCharms = new List<Charm>();
        HashSet<string> seenIds = new HashSet<string>();

        foreach (string rawId in charmIds)
        {
            string charmId = rawId == null ? "" : rawId.Trim();
            if (string.IsNullOrEmpty(charmId))
                continue;

            if (!seenIds.Add(charmId))
            {
                Debug.LogError("GameDevTestOverrides has duplicate forced charm id '" + charmId + "'.");
                return false;
            }

            if (!lookup.TryGetValue(charmId, out Charm charm))
            {
                Debug.LogError("GameDevTestOverrides could not find forced charm id '" + charmId + "' in charms.json.");
                return false;
            }

            if (!CharmCompatibility.isCompatibleWithSelection(charm, resolvedCharms))
            {
                Debug.LogError("GameDevTestOverrides forced charm id '" + charmId + "' is incompatible with the current forced charm set.");
                return false;
            }

            resolvedCharms.Add(charm);
        }

        if (resolvedCharms.Count == 0)
            return false;

        charms = resolvedCharms;
        return true;
    }

    private TestGunCharmSet getCharmSetForGunModel(int gunModel)
    {
        TestGunModel model = getTestGunModel(gunModel);
        TestGunCharmSet modelSpecificSet = findConfiguredSet(model);

        if (modelSpecificSet != null && modelSpecificSet.charmIds != null && modelSpecificSet.charmIds.Length > 0)
            return modelSpecificSet;

        return findConfiguredSet(TestGunModel.All);
    }

    private TestGunCharmSet findConfiguredSet(TestGunModel model)
    {
        if (testGunCharmSets == null)
            return null;

        for (int i = 0; i < testGunCharmSets.Length; i++)
        {
            if (testGunCharmSets[i] != null && testGunCharmSets[i].model == model)
                return testGunCharmSets[i];
        }

        return null;
    }

    private bool ensureCharmLookup(string appliesTo, ref Dictionary<string, Charm> lookup)
    {
        if (lookup != null)
            return true;

        CharmDatabase charmDatabase = CharmLoader.LoadCharmDatabase();
        if (charmDatabase == null || charmDatabase.charms == null)
        {
            Debug.LogError("GameDevTestOverrides could not load charms.json.");
            return false;
        }

        lookup = charmDatabase.charms
            .Where(c => c.appliesTo == appliesTo)
            .GroupBy(c => c.id)
            .ToDictionary(group => group.Key, group => group.First());

        return true;
    }

    private TestGunModel getTestGunModel(int gunModel)
    {
        switch (gunModel)
        {
            case 0:
                return TestGunModel.Rifle;
            case 1:
                return TestGunModel.Pistol;
            case 2:
                return TestGunModel.Shotgun;
            default:
                Debug.LogWarning("GameDevTestOverrides received unknown test gun model " + gunModel + ". Falling back to All.");
                return TestGunModel.All;
        }
    }
}

[System.Serializable]
public class TestGunCharmSet
{
    public TestGunModel model;
    public string[] charmIds;
}

public enum TestGunModel
{
    All,
    Rifle,
    Pistol,
    Shotgun
}

[System.Serializable]
public class TestSkillUnlock
{
    public SkillTreeBuilding building;
    public bool unlockAllUpgrades = true;   // when true, every upgrade for this building is purchased
    public int[] upgradeIndices;            // used only when unlockAllUpgrades == false
}

[System.Serializable]
public class TestBeatMatrixOverride
{
    // Drag the scene's beat manager here — the prefabs under Prefabs/Game Management/Beat
    // Managers work. Matched by type at runtime.
    public sceneBeatManager beatManager;
    public string[] beatValues; // must match that manager's beatMatrix length (see its initializeData
                                // comments). Blank = no override for that line; a number forces it.
}

[System.Serializable]
public class TestQuestOverride
{
    public string questName;                                   // quest title in the dialogue database
    public TestQuestState questState = TestQuestState.Active;  // NoChange leaves the quest-level state alone
    public TestQuestEntryOverride[] entries;
}

[System.Serializable]
public class TestQuestEntryOverride
{
    public int entryNumber = 1; // 1-based, matching QuestLog
    public TestQuestState state = TestQuestState.Active;
}

public enum TestQuestState
{
    NoChange,
    Unassigned,
    Active,
    Success,
    Failure
}

public enum SkillTreeBuilding
{
    // Tier 0 (4 upgrades each)
    Armory,
    ExplosivesRange,
    TinkeringBench,
    Campsite,
    // Tier 1 (5 upgrades each)
    ComputerLab,
    FiringRange,
    MedicalTent,
    AmmoForge,
    // Tier 2 (3 upgrades each)
    AutoCultivator,
    ExperimentalFacility,
    GunResearchLab,
    BiotechWorkshop
}
