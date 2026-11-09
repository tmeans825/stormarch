using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// Sends a group of passive NPCs from this object's position to a rally point. The group is cloned
// at runtime and never saved, so story code re-creates it with spawnAtRally() on later loads.
public class NpcResponderBurst : MonoBehaviour
{
    [Tooltip("An INACTIVE scene NPC to clone. It has to be inactive so its ES3 components can be removed before they register.")]
    public GameObject npcTemplate;
    public Transform rallyPoint;
    public float rallyRadius = 7f;
    public int count = 6;
    public float runSpeed = 5f;
    public float minInterval = 0.15f;
    public float maxInterval = 0.4f;
    public CharacterAppearanceReferences.TemplateLook[] looks =
    {
        CharacterAppearanceReferences.TemplateLook.Guardian,
        CharacterAppearanceReferences.TemplateLook.Sentinel
    };

    private const float SPAWN_SCATTER_RADIUS = 1.5f;

    public void spawnAndRun()
    {
        StartCoroutine(runToRally());
    }

    public void spawnAtRally()
    {
        for (int i = 0; i < count; i++)
        {
            createNpc(getRallySpot());
        }
    }

    private IEnumerator runToRally()
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = getWalkableSpot(transform.position, SPAWN_SCATTER_RADIUS);
            NonCombatChar npc = createNpc(spawnPos);

            yield return null; // startMoving needs the seeker that NonCombatChar.Start assigns
            npc.startMoving(getRallySpot());

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    private NonCombatChar createNpc(Vector2 position)
    {
        GameObject obj = Instantiate(npcTemplate, position, Quaternion.identity, transform);

        // Awake hasn't run on an inactive clone, so these never reach the save manager.
        DestroyImmediate(obj.GetComponent<ES3AutoSave>());
        DestroyImmediate(obj.GetComponent<ES3Internal.ES3Prefab>());

        NonCombatChar npc = obj.GetComponent<NonCombatChar>();
        npc.templateLook = looks[Random.Range(0, looks.Length)];
        npc.baseSpeed = runSpeed;

        obj.SetActive(true);
        return npc;
    }

    private Vector2 getRallySpot()
    {
        return getWalkableSpot(rallyPoint.position, rallyRadius);
    }

    private Vector2 getWalkableSpot(Vector2 center, float radius)
    {
        Vector2 spot = center + Random.insideUnitCircle * radius;

        if (AstarPath.active != null)
        {
            NNInfo nearest = AstarPath.active.GetNearest(spot, NNConstraint.Default);
            if (nearest.node != null)
                return nearest.position;
        }

        return spot;
    }
}
