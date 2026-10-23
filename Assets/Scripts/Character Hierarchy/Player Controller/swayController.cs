using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swayController : MonoBehaviour
{
    public float movementSwayIncrement = 0.01f;
    public float swayDecrement = 0.01f;

    [System.NonSerialized]
    public static swayController instance;
    [System.NonSerialized]
    public float recoilCooldown = 0f;

    [System.NonSerialized]
    public float swayModifier = 1f;

    [System.NonSerialized]
    public float[] swayByGunType;

    private float maxMovementSway = 1.18f;
    private float maxShootingSway = 1.36f;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        swayByGunType = new float[3];
        swayByGunType[0] = 0.5f; // rifle
        swayByGunType[1] = 0.85f; // pistol
        swayByGunType[2] = 0f; // shotgun
    }

    // Update is called once per frame
    void Update()
    {
        // check 1: moving will lerp up sway mod to 1.1 max
        if(PlayerController.instance.isMoving)
        {
            if(swayModifier < maxMovementSway)
            {
                // increase sway by a lil
                swayModifier += movementSwayIncrement * Time.deltaTime;
                pushSwayChangeToUI();
            }
            else if(swayModifier > maxMovementSway && recoilCooldown == 0)
            {
                swayModifier -= swayDecrement * Time.deltaTime;
                if (swayModifier < maxMovementSway)
                    swayModifier = maxMovementSway;

                pushSwayChangeToUI();
            }
        }
        else
        {
            // check 2: not moving, cooldown is done, and swayMod > start, start decreasing it
            if(recoilCooldown == 0f && swayModifier > 1f)
            {
                swayModifier -= swayDecrement * Time.deltaTime;
                if (swayModifier < 1f)
                    swayModifier = 1f;

                pushSwayChangeToUI();               
            }
            // check 3: not moving, cooldown isn't done, decrement cooldown           
        }

        if(recoilCooldown != 0f)
        {
            recoilCooldown -= Time.deltaTime;

            if (recoilCooldown < 0f)
                recoilCooldown = 0f;
        }
    }

    // TO DO: Push changes to UI, so UI doesn't have to 'listen' every frame?

    public void recoil(float cooldown)
    {
        // increase swayMod by amount, reset wait to new cooldown
        float amount = cooldown * (maxShootingSway - 1); // ' if we shoot this gun for 1 straight second, we hit max recoil'

        recoilCooldown = cooldown;
        swayModifier += amount;

        if (swayModifier > maxShootingSway)
            swayModifier = maxShootingSway;

        pushSwayChangeToUI();
    }

    private void pushSwayChangeToUI()
    {
        swayUIBehvaior.instance.updateSway(swayModifier);
    }

}
