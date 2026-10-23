using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HighlightPlus2D;

public class ObjectSelector : MonoBehaviour
{
    public static ObjectSelector current;
    public GameObject selectedTouchable;
    public GameObject selectedWithPointer;
    public float glowIntensity = 0.4f;
    public bool isTouchableSelected = false;
    public bool isPointerSelected = false;
    private List<GameObject> touchables = new List<GameObject>();

    private void Awake()
    {
        if (current == null && this != current)
            current = this;
        else
            Destroy(this);

        GameEvents.current.onSceneTransitionComplete += transitionComplete;
    }

    private void transitionComplete()
    {
        touchables.Clear();
    }

    private void OnDestroy()
    {
        GameEvents.current.onSceneTransitionComplete -= transitionComplete;
    }

    public void PointerSelect(GameObject selectedObj)
    {
        selectedWithPointer = selectedObj;
        isPointerSelected = true;
        // check for overriding touchable item
        if (isTouchableSelected) // if a touchable item is selected....
        {
            if (touchables.Contains(selectedWithPointer) && (selectedTouchable != selectedWithPointer))
            {
                UnselectTouchable();
                SelectTouchable(selectedWithPointer);
                return;
            }
            else
                spawnCardCheck();
        }

        WorldspaceCanvas.instance.popUpHelpText(selectedObj, true);
    }

    public void PointerUnselect()
    {
        unSpawnCardCheck();
        selectedWithPointer = null;
        isPointerSelected = false;

        WorldspaceCanvas.instance.endHelpText(true);
    }

    public void AddToTouchables(GameObject obj)
    {
        touchables.Add(obj);
        if (touchables.Count == 1)
        {
            SelectTouchable(obj);
        }
        else
        {
            checkTouchables();
        }
    }

    public void RemoveFromTouchables(GameObject obj)
    {
        unSpawnCardCheck();
        touchables.Remove(obj);
        UnselectTouchable();
        if (touchables.Count != 0)
            checkTouchables();

    }

    // For callers that deactivate an interactable in place, where OnTriggerExit2D and OnMouseExit
    // aren't guaranteed. Each half is guarded because RemoveFromTouchables and PointerUnselect clear
    // whatever is currently selected, not just obj.
    public void forgetInteractable(GameObject obj)
    {
        if (touchables.Contains(obj))
            RemoveFromTouchables(obj);

        if (selectedWithPointer == obj)
        {
            // The normal mouse-exit path: also closes the HUD popup and drops the highlight.
            HighlightTrigger2D trigger = obj.GetComponent<HighlightTrigger2D>();
            if (trigger != null)
                trigger.manualOverrideMouseExit();

            // manualOverrideMouseExit skips everything for timed highlights (highlightDuration > 0).
            if (selectedWithPointer == obj)
                PointerUnselect();
        }
    }

    public void checkTouchables()
    {
        if (touchables.Count == 0)
            return;

        int closestIndex = 0;
        float currentClosest = float.MaxValue;
        for(int i = 0; i < touchables.Count; i++)
        {
            if (Vector2.Distance(transform.position, touchables[i].transform.position) < currentClosest)
                closestIndex = i;
        }

        if (touchables[closestIndex] != selectedTouchable)
        {
            UnselectTouchable();
            SelectTouchable(touchables[closestIndex]);
        }
    }

    public void UnselectTouchable()
    {
        if (isTouchableSelected)
        {
            HighlightEffect2D he = selectedTouchable.GetComponent<HighlightEffect2D>();
            he.glow = 0f;
            he.Refresh();
            isTouchableSelected = false;
            selectedTouchable = null;

            // turn off popup for key?
        }

        WorldspaceCanvas.instance.endHelpText(false);
    } 
    
    public void SelectTouchable(GameObject obj)
    {        
        selectedTouchable = obj;
        spawnCardCheck();
        HighlightEffect2D he = selectedTouchable.GetComponent<HighlightEffect2D>();
        he.glow = glowIntensity;
        he.Refresh();
        isTouchableSelected = true;

        WorldspaceCanvas.instance.popUpHelpText(obj, false);
    }

    private void spawnCardCheck()
    {
        if (selectedTouchable == selectedWithPointer && !PlayerController.instance.frozen)
        {
            WorldspaceCanvas.instance.activateCard(selectedTouchable);
        }
    }

    private void unSpawnCardCheck()
    {
        if (selectedTouchable == selectedWithPointer && !PlayerController.instance.frozen)
        {
            WorldspaceCanvas.instance.deActivateCard();
            checkTouchables();
        }
    }
}
