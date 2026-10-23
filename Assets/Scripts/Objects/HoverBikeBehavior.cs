using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverBikeBehavior : MonoBehaviour
{
    public Sprite greenLight;
    public Sprite redLight;

    private Animator spriteAnimator;
    private SpriteRenderer lightRenderer;
    private GameObject hoverLight;

    private GameObject trailLight;
    private ParticleSystem trailPS;

    private AudioManager am;


    // Start is called before the first frame update
    private void Start()
    {
        spriteAnimator = GetComponentInChildren<Animator>();
        lightRenderer = transform.GetChild(0).transform.GetChild(0).GetComponent<SpriteRenderer>();
        hoverLight = transform.GetChild(0).transform.GetChild(2).gameObject;

        trailLight = transform.GetChild(0).GetChild(3).gameObject;
        trailPS = transform.GetChild(0).GetChild(4).GetComponent<ParticleSystem>();

        am = GetComponent<AudioManager>();
    }

    public void activate()
    {
        spriteAnimator.Play("HoverBike_Activate_Anim");

        // lights
        lightRenderer.sprite = greenLight;
        trailPS.Play();
        trailLight.SetActive(true);
        am.PlaySound("Start");
        StartCoroutine(delayEngine());
    }

    private IEnumerator delayEngine()
    {
        yield return new WaitForSeconds(0.3f);
        am.PlaySound("EngineOn");
    }

    public void deActivate()
    {
        StopAllCoroutines();
        spriteAnimator.Play("HoverBike_Off_Anim");
        lightRenderer.sprite = redLight;
        hoverLight.SetActive(false);
        trailPS.Stop();
        trailLight.SetActive(false);
        am.StopSound("EngineOn");
        am.PlaySound("EngineOff");
    }
}
