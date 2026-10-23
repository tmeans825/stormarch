using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class FirebombExplode : GrenadeExplode
{
    public float duration = 10.0f;
    private SpriteRenderer sprite;
    private GameObject fireRadius;

    protected override void Start()
    {
        base.Start();
        handleDamage = false;
        fireRadius = transform.GetChild(2).gameObject;
        fireRadius.GetComponent<CircleCollider2D>().radius = radius;
        isGrenade = false;
    }

    protected override void Update()
    {
        base.Update();
        grenade.transform.Rotate(new Vector3(0,0,1), Time.deltaTime*300f, Space.World);
    }

    protected override void blowUp(Transform t, Vector3 offset)
    {
        base.blowUp(t,offset);
        fireRadius.SetActive(true);
        activeSticker.transform.localScale = new Vector3(20f, 20f, 20f);
        sprite = activeSticker.GetComponent<SpriteRenderer>();
        StartCoroutine(fadeInBurn());
    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider != thrower)
        {
            StopAllCoroutines();
            base.overrideCountdown();
        }
    }

    private IEnumerator fadeInBurn()
    {
        Color tmp = sprite.color;
        tmp.a = 0.1f;
        sprite.color = tmp;
        float timePassed = 0.0f;
        while(timePassed < duration)
        {
            yield return 0;
            timePassed += Time.deltaTime;
            tmp.a += (0.4f * (Time.deltaTime / duration));
            sprite.color = tmp;
        }
        
        AstarPath.active.UpdateGraphs(fireRadius.GetComponent<CircleCollider2D>().bounds);
        fireRadius.SetActive(false);
    }
}
