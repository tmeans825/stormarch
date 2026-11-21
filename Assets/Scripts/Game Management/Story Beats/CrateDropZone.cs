using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger volume (the train car) that counts pushable crates currently inside it and reports the count to
/// the Adamantine beat manager. The count is recomputed from what is physically inside, not incremented,
/// so a save/load (Pushable restores its position, re-firing OnTriggerEnter) or a crate respawn can't
/// double count.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrateDropZone : MonoBehaviour
{
    [System.NonSerialized]
    private readonly HashSet<Pushable> cratesInside = new HashSet<Pushable>();

    void OnTriggerEnter2D(Collider2D other)
    {
        Pushable crate = other.GetComponentInParent<Pushable>();
        if (crate != null && cratesInside.Add(crate))
            report();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Pushable crate = other.GetComponentInParent<Pushable>();
        if (crate != null && cratesInside.Remove(crate))
            report();
    }

    private void report()
    {
        AdamantineAct0BeatManager manager = sceneBeatManager.instance as AdamantineAct0BeatManager;
        if (manager != null)
            manager.crateCountChanged(cratesInside.Count);
    }
}
