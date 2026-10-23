using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Opt-in: NPCs parented under this GameObject leave the scene while an infestation is active and
 * come back when it ends. Only this group's own active state is toggled, so each NPC's activeSelf
 * stays owned by what already owns it - ES3AutoSave's saved "active" and StoryBeatManager's toggles -
 * and there's no "who hid this?" state to persist. The group isn't autosaved, so it loads active
 * and EventManager.loadInEvents' replayed beginInfestation hides it again.
 *
 * Authoring rules:
 *  - Keep the group at the origin with identity rotation/scale so parenting doesn't move anyone.
 *  - NPCs only. Enemies lose their pacing when deactivated and autosave their Transform parent,
 *    so older saves would pull them back out of the group.
 *  - Don't toggle this GameObject from story beats or UnityEvents; toggle the NPCs inside it.
 *
 */

public class HideDuringInfestation : MonoBehaviour
{
    private Animator[] animators;
    private bool[] authoredKeepAnimatorState;
    private Interactable[] interactables;

    // Cache the dispatcher we subscribed to so OnDestroy unbinds the same instance even if
    // GameEvents.current is replaced/nulled during teardown (mirrors GameEventListener).
    private GameEvents subscribedEvents;

    // Subscribed for the object's lifetime rather than OnEnable/OnDisable: hiding deactivates this
    // GameObject, and it still has to hear endInfestation to come back.
    void Awake()
    {
        animators = GetComponentsInChildren<Animator>(true);
        authoredKeepAnimatorState = new bool[animators.Length];
        for (int i = 0; i < animators.Length; i++)
            authoredKeepAnimatorState[i] = animators[i].keepAnimatorControllerStateOnDisable;

        interactables = GetComponentsInChildren<Interactable>(true);

        if (GetComponentInChildren<CombatChar>(true) != null)
            Debug.LogWarning("HideDuringInfestation on '" + name + "' contains a CombatChar - only NPCs are supported.", this);

        trySubscribe();
    }

    void Start()
    {
        // Fallback: if we woke before GameEvents.Awake ran, retry once now.
        if (subscribedEvents == null)
            trySubscribe();
    }

    void OnDestroy()
    {
        if (subscribedEvents == null)
            return;
        subscribedEvents.beginInfestation -= hide;
        subscribedEvents.endInfestation -= show;
        subscribedEvents = null;
    }

    private void trySubscribe()
    {
        if (subscribedEvents != null) return; // already bound
        GameEvents ge = GameEvents.current;
        if (ge == null) return;
        subscribedEvents = ge;
        ge.beginInfestation += hide;
        ge.endInfestation += show;
    }

    // Set state, never toggle: beginInfestation is replayed on load and endInfestation isn't
    // idempotent, so repeat fires must be no-ops.
    private void hide()
    {
        if (!gameObject.activeSelf)
            return;

        // Without this the round trip resets chest/legs animators to their default state, out of
        // step with the swapped head objects and Character's last-sent-trigger cache.
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].keepAnimatorControllerStateOnDisable = true;
        }

        gameObject.SetActive(false);

        // OnTriggerExit2D/OnMouseExit aren't guaranteed for an object being deactivated, which would
        // leave a hidden NPC selected for interaction or Kipling. Runs after deactivation so a
        // delivered exit makes this a no-op.
        if (ObjectSelector.current != null)
        {
            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i] != null)
                    ObjectSelector.current.forgetInteractable(interactables[i].gameObject);
            }
        }
    }

    private void show()
    {
        if (gameObject.activeSelf)
            return;

        gameObject.SetActive(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].keepAnimatorControllerStateOnDisable = authoredKeepAnimatorState[i];
        }
    }
}
