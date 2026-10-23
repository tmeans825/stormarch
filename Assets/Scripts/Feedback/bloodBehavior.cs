using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class bloodBehavior : MonoBehaviour
{
    public float speed = 1.0f;
    public float lifeTime = 1.0f;
    public GameObject bloodSpatter;
    public Sprite[] bloodSpatters;

    public float[] angles = new float[4] { 0f, 90f, 180f, 270f };
    private Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        float directionX = Random.Range(-1f,1f);
        float directionY = 1f;
        Vector2 dVector = new Vector2(directionX, directionY).normalized;
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(dVector.x * speed, dVector.y * speed);
        StartCoroutine("fallingBlood");
    }

    private IEnumerator fallingBlood()
    {
        yield return new WaitForSeconds(lifeTime);
        Instantiate(bloodSpatter, transform.position, new Quaternion(0f, 0f, angles[Random.Range(0, 4)], 0f), this.transform.parent.transform);
        bloodSpatter.GetComponent<SpriteRenderer>().sprite = bloodSpatters[Random.Range(0, bloodSpatters.Length)];
        Destroy(this.gameObject);
    }
}
