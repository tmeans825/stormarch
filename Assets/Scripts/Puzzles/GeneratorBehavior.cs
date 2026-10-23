using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorBehavior : MonoBehaviour
{
    public GameObject[] prefabArray;

    [SerializeField] private GameObject[] indicatorLights; // light1, light2, light3 - selectable markers
    [SerializeField] private GameObject activeLight;       // green "active" marker that moves to the selected slot

    private int attachedObjectCount;
    private int activeIndex = 0;

    private AudioManager am;


    // Start is called before the first frame update
    IEnumerator Start()
    {
        am = GetComponent<AudioManager>();

        resolveReferences();

        am.PlaySound("Engine");

        yield return 0;
        yield return 0;
        yield return 0; // SAVE TAKES PLACE IN FRAME T+1, SO WAIT UNTIL T+2

        // Count-dependent work happens here, not above: PuzzleAutomator.Start() sets prefabArray
        // at Start() with undefined order vs. this coroutine's synchronous block. By T+2 it has run.
        attachedObjectCount = countValidDoors();
        if (attachedObjectCount == 0)
        {
            Debug.LogError("GeneratorBehavior (" + name + "): no valid powered objects in prefabArray. Generator inert.");
            yield break;
        }

        // Disable indicator lights with no door behind them; enable the rest.
        for (int i = 0; i < indicatorLights.Length; i++)
            if (indicatorLights[i] != null)
                indicatorLights[i].SetActive(i < attachedObjectCount);

        if (activeIndex >= attachedObjectCount || activeIndex < 0)
            activeIndex = 0; // clamp a stale saved index into the current door range

        if (activeLight != null)
            activeLight.SetActive(true);
        else
            Debug.LogError("GeneratorBehavior (" + name + "): no activeLight marker found.");

        //Debug.Log("Active index for generator: " + activeIndex);

        toggle(activeIndex);
    }

    private void resolveReferences()
    {
        if (activeLight == null)
        {
            Transform t = transform.Find("activeLight");
            if (t != null) activeLight = t.gameObject;
        }

        if (indicatorLights == null || indicatorLights.Length == 0)
        {
            indicatorLights = new GameObject[]
            {
                transform.Find("light1")?.gameObject,
                transform.Find("light2")?.gameObject,
                transform.Find("light3")?.gameObject,
            };
        }
    }

    private int countValidDoors()
    {
        if (prefabArray == null) return 0;

        int count = 0;
        for (int i = 0; i < prefabArray.Length; i++)
            if (prefabArray[i] != null)
                count++;
        return count;
    }

    // Moves the active marker onto the indicator light for the given slot.
    private void moveActiveLightTo(int index)
    {
        if (activeLight == null) return;
        if (index < 0 || index >= indicatorLights.Length || indicatorLights[index] == null)
        {
            Debug.LogError("GeneratorBehavior (" + name + "): missing indicator light for slot " + index + ".");
            return;
        }

        activeLight.transform.position = indicatorLights[index].transform.position + new Vector3(0.0f, 0.0f, -0.1f); // move green light
    }

    private PuzzlePiece getPuzzlePiece(int index)
    {
        if (prefabArray == null || index < 0 || index >= prefabArray.Length || prefabArray[index] == null)
            return null;
        return prefabArray[index].GetComponent<PuzzlePiece>();
    }


    public void toggle()
    {
        PuzzlePiece current = getPuzzlePiece(activeIndex);
        if (current == null)
        {
            Debug.LogError("GeneratorBehavior (" + name + "): missing PuzzlePiece on powered object " + activeIndex + ".");
            return;
        }

        if (current.togglePower(false))
        {
            activeIndex++;
            if (activeIndex >= attachedObjectCount)
                activeIndex = 0;

            moveActiveLightTo(activeIndex);

            am.PlaySound("Beep");

            PuzzlePiece next = getPuzzlePiece(activeIndex);
            if (next != null)
                next.togglePower(true);
            else
                Debug.LogError("GeneratorBehavior (" + name + "): missing PuzzlePiece on powered object " + activeIndex + ".");
        }
        else
        {
            // play negative SFX
        }
    }

    private void toggle(int index)
    {
        //Debug.Log("index: " + index + ". attached obj: " + attachedObjectCount + ".");
        if (index >= attachedObjectCount)
            index = 0;

        moveActiveLightTo(index);

        //am.PlaySound("Beep");

        PuzzlePiece piece = getPuzzlePiece(index);
        if (piece != null)
            piece.togglePower(true);
        else
            Debug.LogError("GeneratorBehavior (" + name + "): missing PuzzlePiece on powered object " + index + ".");
    }
}
