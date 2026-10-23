using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class GrenadeExplode : Explode
{
    public float timeInSky = 1.5f;
    public GameObject[] groundStickers;
    protected Rigidbody2D rb;
    protected BoxCollider2D coll;
    protected GameObject shadow;
    protected GameObject grenade;
    protected AudioManager am;
    protected GameObject activeSticker;
    protected Collider2D thrower;
    protected bool isGrenade = true;

    private GameObject fleeRadius;

    private bool fleeActivated = false;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shadow = transform.GetChild(1).gameObject;
        grenade = transform.GetChild(0).gameObject;
        coll = GetComponent<BoxCollider2D>();
        am = GetComponent<AudioManager>();

        fleeRadius = transform.GetChild(2).gameObject;
        fleeRadius.GetComponent<CircleCollider2D>().radius = radius;
        StartCoroutine(startLife());
    }

    protected override void Update()
    {
        base.Update();

        if (countdown <= 0.8f && !fleeActivated && isGrenade)
        {
            fleeRadius.SetActive(true);
            fleeActivated = true;
        }
    }

    public void initializeGrenade(float r, float d, Collider2D charColl)
    {
        base.initialize(r, d);
        thrower = charColl;
    }

    protected override void blowUp(Transform t, Vector3 offset)
    {
        StopAllCoroutines();     
        rb.velocity = Vector2.zero;
        Destroy(rb);
        Destroy(GetComponent<Collider2D>());
        transform.GetChild(0).gameObject.SetActive(false);
        shadow.SetActive(false);
        if (isGrenade)
        {
            AstarPath.active.UpdateGraphs(fleeRadius.GetComponent<CircleCollider2D>().bounds);
            fleeRadius.SetActive(false);
        }

        int randomRot = Random.Range(0, 4);
        float rotZ = randomRot * 90f;
        Quaternion groundQuat = Quaternion.Euler(0.0f, 0.0f, rotZ);

        offset = new Vector3(0.0f, -1.2f, 0.0f);

        int sticker = Random.Range(0, groundStickers.Length);
        activeSticker = Instantiate(groundStickers[sticker], transform.position+offset, groundQuat, GameObject.FindGameObjectWithTag("Pooler").transform);
        activeSticker.transform.localScale = new Vector3(15f, 15f, 15f);

        base.blowUp(t, offset);
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        am.PlaySound("Bounce");
    }

    public override void overrideCountdown()
    {
        StopAllCoroutines();
        base.overrideCountdown();
    }

    private IEnumerator startLife()
    {
        float timeLapsed = 0f;
        yield return new WaitForSeconds(0.1f);
        gameObject.layer = 18;
        timeInSky -= 0.1f;
        while(timeLapsed < (timeInSky))
        {
            timeLapsed += Time.deltaTime;
            grenade.transform.localPosition = new Vector3(grenade.transform.localPosition.x, (-1f*((timeLapsed / timeInSky) * 1.2f)), grenade.transform.localPosition.z);
            coll.offset = new Vector2(0.0f,(-1f * ((timeLapsed / timeInSky) * 1.2f)));
            yield return 0;
        }
        am.PlaySound("Plink");

        

        if (rb != null)
            rb.drag = 3f;

        Collider2D tempCol = GetComponent<Collider2D>();
        if(tempCol!=null)
            tempCol.enabled = true;
    }
}
