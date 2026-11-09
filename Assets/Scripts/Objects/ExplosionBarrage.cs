using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns a staggered string of explosion prefabs, either across an authored area or around a point.
public class ExplosionBarrage : MonoBehaviour
{
    [Tooltip("Should clean itself up - e.g. Explode + destroyAfterSeconds.")]
    public GameObject explosionPrefab;
    [Tooltip("Width and height of the area playInArea() covers, centered on this object.")]
    public Vector2 areaSize = new Vector2(10f, 10f);
    public int count = 20;
    public float minInterval = 0.1f;
    public float maxInterval = 0.2f;

    public void playInArea()
    {
        playInArea(null);
    }

    public void playInArea(Action onFinished)
    {
        StartCoroutine(spawnExplosions(count, randomPointInArea, onFinished));
    }

    public void playAround(Vector3 center, float radius, int explosionCount)
    {
        StartCoroutine(spawnExplosions(explosionCount, () => center + (Vector3)(UnityEngine.Random.insideUnitCircle * radius), null));
    }

    private Vector3 randomPointInArea()
    {
        float x = UnityEngine.Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float y = UnityEngine.Random.Range(-areaSize.y / 2f, areaSize.y / 2f);
        return transform.position + new Vector3(x, y, 0f);
    }

    private IEnumerator spawnExplosions(int explosionCount, Func<Vector3> getPosition, Action onFinished)
    {
        for (int i = 0; i < explosionCount; i++)
        {
            Instantiate(explosionPrefab, getPosition(), Quaternion.identity, ObjectPooler.instance.transform);
            yield return new WaitForSeconds(UnityEngine.Random.Range(minInterval, maxInterval));
        }

        onFinished?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
    }
}
