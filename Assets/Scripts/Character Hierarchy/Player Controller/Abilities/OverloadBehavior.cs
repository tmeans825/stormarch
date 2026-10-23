using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverloadBehavior : MonoBehaviour
{
    public CircleCollider2D col;
    public GameObject[] groundStickers;

    private Explode explosion;
    private AudioManager audioManager;
    private float defaultStunTime = 1f;

    private void Awake()
    {
        explosion = GetComponent<Explode>();
        if (explosion == null)
        {
            Debug.LogError("OverloadBehavior requires an Explode component.");
        }

        audioManager = GetComponent<AudioManager>();
    }

    public void triggerOverload(float shieldUnitsUsed)
    {
        //Vector2 origin = PlayerController.instance.transform.position;
        //transform.position = origin;

        //StartCoroutine(followPlayer());

        float radius = col.radius;
        if (shieldUnitsUsed >= 2f)
        {
            radius *= (1f + (0.2f * (shieldUnitsUsed - 1f))); // increase radius by 20% for each shield unit above 1
        }
        
        foreach (var bullet in ProjectileScript.all)
        {
            if (Vector2.Distance(bullet.transform.position, transform.position) <= radius)
            {
                bullet.reflectEnemyBullets(transform.position);
            }
        }

        explosion.radius = radius;
        explosion.stunTime = defaultStunTime * shieldUnitsUsed;
        audioManager.PlaySound("Overload");
        explosion.manualExplosion();

        int sticker = Random.Range(0, groundStickers.Length);
        GameObject activeSticker = Instantiate(groundStickers[sticker], transform.position - new Vector3(0f, 1f, 0f), Quaternion.identity, GameObject.FindGameObjectWithTag("Pooler").transform);
        activeSticker.transform.localScale = new Vector3(10f, 10f, 10f);
    }

    private IEnumerator followPlayer()
    {
        float timer = 0f;

        while(timer < 5f)
        {
            transform.position = PlayerController.instance.transform.position;
            timer += Time.deltaTime;
            yield return null;
        }
    }

}
