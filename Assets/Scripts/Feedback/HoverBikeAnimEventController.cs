using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

public class HoverBikeAnimEventController : MonoBehaviour
{
    private ParticleSystem ps;
    private bool isOpen;
    private GameObject hoverLight;
    private HoverBikeBehavior bike;
    private void Start()
    {
        ps = transform.GetChild(1).GetComponent<ParticleSystem>();
        hoverLight = transform.GetChild(2).gameObject;
        bike = GetComponentInParent<HoverBikeBehavior>();
    }

    public void openMap()
    {
        hoverLight.SetActive(true);
        PlayerController.instance.openMap(bike);
        isOpen = true;
    }

    public void hitGround()
    {
        if (isOpen)
        {
            ProCamera2DShake.Instance.Shake(PlayerController.SMALL_SHAKE);
            ps.Play();
            transform.GetComponentInParent<AudioManager>().PlaySound("Slam");
        }
    }
}
