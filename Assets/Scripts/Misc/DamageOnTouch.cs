using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnTouch : MonoBehaviour
{
    public float damage = 20f;

    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.gameObject.tag == "Shield Wall")
        {
            PlayerController.instance.damageCharacter(damage, new Vector2(transform.position.x, transform.position.y), false, false, true, false);

            gameObject.SetActive(false); // only damage once per charge
        }

        if (col.gameObject.tag == "Player")
        {
            CombatChar colChar = col.gameObject.GetComponent<CombatChar>();
            colChar.damageCharacter(damage, new Vector2(transform.position.x, transform.position.y), false, false, true, false);

            gameObject.SetActive(false); // only damage once per charge
        }
    }
}
