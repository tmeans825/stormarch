using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletShellBehavior : MonoBehaviour
{
    public float speed = 1.0f;
    public float lifeTime = 1.0f;
    public bool isDebris = false;
    public bool isWormHead = false;
    public Sprite[] debrisSprites;
    public GameObject finalHead;

    private float[] angles = new float[4] { 0f, 90f, 180f, 270f };
    private Rigidbody2D rb;
    private float calculatedSpeed;

    private float directionX;
    private float directionY;
    private bool alteredCourse = false;
    private Vector2 dVector;

    // Start is called before the first frame update
    void Start()
    {
        if (!alteredCourse)
        {
            directionX = Random.Range(-1f, 1f);
            directionY = 0f;
        }

        if (!isDebris)
        {
            directionY = 1f;
            dVector = new Vector2(directionX, directionY).normalized;
        }
        else
        {
            int randInt = Random.Range(0, debrisSprites.Length);
            GetComponentInChildren<SpriteRenderer>().sprite = debrisSprites[randInt];
            dVector = new Vector2(directionX, directionY);
        }
      
        rb = GetComponent<Rigidbody2D>();
        calculatedSpeed = speed + Random.Range(-speed * .2f, speed * .2f);
        rb.velocity = new Vector2(dVector.x * calculatedSpeed, dVector.y * calculatedSpeed);

        if (isWormHead)
        {
            float rotationSpeed = Random.Range(-200f, 200f);
            rb.angularVelocity = rotationSpeed;
        }

        StartCoroutine("fallingShell");
    }

    public void alterCourse(float newX, float newY)
    {
        alteredCourse = true;
        directionX = newX;
        directionY = newY;
    }

    private IEnumerator fallingShell()
    {
        yield return new WaitForSeconds(lifeTime);
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        if (!isDebris)
        {
            int randSound = Random.Range(0, 13);

            if(!isWormHead)
                GetComponent<AudioManager>().PlaySound("Casing"+randSound);

            done();
            rb.drag = 2f;
            rb.velocity = new Vector2(dVector.x * calculatedSpeed, 0f);
            yield return new WaitForSeconds(0.75f);
        }
        else
            done();

        rb.velocity = Vector2.zero;      
    }

    private void done()
    {
        rb.gravityScale = 0f;
        
        if (!isWormHead)
        {
            transform.GetChild(0).GetComponent<SpriteRenderer>().sortingLayerName = "Floor";
            transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = -2;
        }
        else
        {
            finalHead.SetActive(true);
            finalHead.transform.position += new Vector3(0f, 0.82f, 0f);
            transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
            finalHead.transform.parent = ObjectPooler.instance.transform;
        }
    }
}
