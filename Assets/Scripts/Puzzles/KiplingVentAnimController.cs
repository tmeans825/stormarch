using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KiplingVentAnimController : MonoBehaviour
{
    private Vent ventScript;

    private SpriteRenderer standInRenderer;
    // Cache the instance we registered with so OnDisable unregisters from that same
    // one, even if the player has been swapped out in the meantime.
    private KiplingTempestVisuals registeredVisuals;

    void Awake()
    {
        standInRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject parent = transform.parent.gameObject;
        ventScript = parent.GetComponent<Vent>();

        // Fallback: if we enabled before the player existed, retry once.
        if (registeredVisuals == null)
            registerWithTempestVisuals();
    }

    // This sprite stands in for Kipling during the run-through, but it lives on the
    // Vent prefab rather than under the player, so KiplingTempestVisuals can't find it
    // on its own. Hand it over while it is on screen so a tempest tints it too.
    void OnEnable()
    {
        registerWithTempestVisuals();
    }

    void OnDisable()
    {
        if (registeredVisuals == null)
            return;

        registeredVisuals.unregisterStandInRenderer(standInRenderer);
        registeredVisuals = null;
    }

    private void registerWithTempestVisuals()
    {
        if (PlayerController.instance == null)
            return;

        KiplingTempestVisuals visuals = PlayerController.instance.GetComponent<KiplingTempestVisuals>();
        if (visuals == null)
            return;

        registeredVisuals = visuals;
        registeredVisuals.registerStandInRenderer(standInRenderer);
    }

    // dir can be 0 (up) or 1 (down)
    public void endAnim(int dir)
    {
        bool fromBelow = false;
        if (dir == 1)
            fromBelow = true;

        ventScript.endAnim(fromBelow);
    }
}
