using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class MouseOverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float addMargin = 10.0f;
    private TextMeshProUGUI text;
    private Vector4 originalMargin;
    private Button button;

    public void Awake()
    {
        text = transform.GetComponentInChildren<TextMeshProUGUI>();
        originalMargin = text.margin;
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(button.interactable)
            text.margin = originalMargin + new Vector4(addMargin, 0.0f, 0.0f, 0.0f);
        //AudioMaster.instance.PlaySound("UI Hotbar Ping");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        text.margin = originalMargin;
    }
}

