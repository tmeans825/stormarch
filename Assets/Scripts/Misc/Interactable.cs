using System.Collections;
using System.Collections.Generic;
using HighlightPlus2D;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine.UI;

public class Interactable : MonoBehaviour
{
    [Tooltip("0 = Generator, 1 = Item, 2 = Person, 3 = Recording/Note, 4 = Pressure Plate, 5 = Vent, 6 = Computer, 7 = The Grasshopper, 8 = Workbench, 9 = Button, 10 = Non-Person Talkable, 11 = Loot Box, 12 = Clotberry")]
    [Range(0, 13)]
    public int interactableType = -1;

    public bool canKiplingUse;
    [Tooltip("Kipling carries this back to the player like loot. Item (1) and Recording/Note (3) are always carried; this enables carrying for other types (e.g. a talkable).")]
    public bool canKiplingCarry;
    public Color noColor = Color.HSVToRGB(25f, 100f, 89f);
    public Color yesColor = Color.HSVToRGB(185f,100f,89f);
    public bool isDeployedTo = false;

    public string nonPersonTalkableName;

    [System.NonSerialized]
    public string fieldName;

    private Color defaultColor;

    private HighlightEffect2D outlineBehavior;

    private bool isBeingCarried = false;
    private bool cancelCarryTrigger = false;
    
    // Start is called before the first frame update
    void Start()
    {
        outlineBehavior = GetComponent<HighlightEffect2D>();

        if (!canKiplingUse)
            defaultColor = noColor;
        else
            defaultColor = yesColor;

        outlineBehavior.outlineColor = defaultColor;

        if (interactableType == -1)
        {
            if ((gameObject.GetComponent("GeneratorBehavior") as GeneratorBehavior) != null)
                interactableType = 0;
        }

        fieldName = getFieldName();
    }

    public void toggleDeployed(bool b)
    {
        isDeployedTo = b;
        if (isDeployedTo)
            outlineBehavior.outlineColor = Color.white;
        else
            outlineBehavior.outlineColor = defaultColor;
        outlineBehavior.Refresh();
    }

    // This is for kipling's interactions
    public bool interact()
    {
        if (isCarryable())
        {
            StartCoroutine(carry());
            return true;
        }

        switch(interactableType)
        {
            case 0:
                gameObject.GetComponent<GeneratorBehavior>().toggle();
                break;
            case 4:
                gameObject.GetComponent<PressurePlate>().pressedByKipling();
                KiplingBehavior.instance.isWaitingOnPlate = true;
                //KiplingBehavior.instance.transform.position = transform.position + new Vector3(0.0f, 0.8f, 0.0f);
                //KiplingBehavior.instance.lerpToPos(transform.position + new Vector3(0.0f, 0.8f, 0.0f));
                break;
            case 9:
                gameObject.GetComponent<PuzzleButtonBehavior>().press();
                break;
            default:
                break;
        }
        return false;
    }

    public void signalReturn(bool c)
    {
        if (isCarryable())
        {
            cancelCarryTrigger = c;
            StartCoroutine(waitForSwimEnd());
        }
    }

    private bool isCarryable()
    {
        return interactableType == 1 || interactableType == 3 || canKiplingCarry;
    }

    private IEnumerator waitForSwimEnd()
    {
        bool wait = false;
        while (KiplingBehavior.instance.isSwimming)
        {
            if (!wait)
                wait = true;
            yield return null;
        }

        if (wait)
            yield return new WaitForSeconds(0.2f);

        isBeingCarried = false;
    }

    private IEnumerator carry()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>(); // talkables keep their visual on a child object
        string originalLayerName = sprite.sortingLayerName;
        sprite.sortingLayerName = "Overhead";
        GetComponent<BoxCollider2D>().enabled = false;
        isBeingCarried = true;
        while(isBeingCarried)
        {
            transform.position = KiplingBehavior.instance.transform.position;
            yield return null;
        }
        Vector3 pos;
        if (cancelCarryTrigger)
        {
            pos = transform.position;
            cancelCarryTrigger = false;
        }
        else
            pos = PlayerController.instance.transform.position;
        transform.position = pos;
        sprite.sortingLayerName = originalLayerName;
        GetComponent<BoxCollider2D>().enabled = true;
        LootMaster.instance.addItemDropForce(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            ObjectSelector.current.AddToTouchables(this.gameObject);
            if (!canKiplingUse)
            {
                outlineBehavior.outlineColor = yesColor;

                if (interactableType == 11)
                {
                    if (GetComponent<LootBox>().lootBoxType == 1 && GetComponent<LootBox>().isLocked)
                        outlineBehavior.outlineColor = noColor;
                }
               
                outlineBehavior.Refresh();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
            ObjectSelector.current.RemoveFromTouchables(this.gameObject);
        if (!canKiplingUse)
        {
            outlineBehavior.outlineColor = noColor;
            outlineBehavior.Refresh();
        }
    }

    private string getFieldName()
    {
        switch(interactableType)
        {
            case 0:
                return "Generator";
            case 1:
                return getItemName();
            case 2:
                return gameObject.GetComponent<DialogueActor>().GetActorName();
            case 3:
                if (gameObject.GetComponent<RecorderBehavior>().recording)
                    return "Recorder";
                else
                    return "Note"; 
            case 4:
                return "Pressure Plate";
            case 5:
                return "Vent";
            case 6:
                return "Computer";
            case 7:
                return "The Grasshopper";
            case 8:
                return "Workbench";
            case 9:
                return "Button";
            case 10:
                return nonPersonTalkableName;
            case 11:
                int boxType = gameObject.GetComponent<LootBox>().lootBoxType;
                if (boxType == 0)
                    return "Supply Cache";
                else if (boxType == 1)
                    return "Powered Chest";
                else
                    goto default;
            case 12:
                return "Clotberry";
            default:
                Debug.LogWarning("Couldn't find interactable field name on mouseover...");
                return "";
        }
    }

    private string getItemName()
    {
        InventoryItem item = gameObject.GetComponent<Loot>().item;
        return LootMaster.instance.getUIName(item); // source of truth for item names
    }

    public void mousedOver()
    {
        bool isChar = false;
        string retName = fieldName;

        if (interactableType == 2)
            retName = gameObject.GetComponent<DialogueActor>().GetActorName(); // live — actor can change (e.g. Stranger → Itri)

        if (interactableType == 2 || interactableType == 3)
            isChar = true; // forces title case + translation

        if (interactableType == 1)
        {
            float displayCount = gameObject.GetComponent<Loot>().item.count;

            if (gameObject.GetComponent<Loot>().item.iType == InventoryItem.itemType.Cloth || gameObject.GetComponent<Loot>().item.iType == InventoryItem.itemType.Chemical || gameObject.GetComponent<Loot>().item.iType == InventoryItem.itemType.Scrap)
                retName = retName + " (" + (displayCount/4f) + ")";
            else if ((gameObject.GetComponent<Loot>().item.count > 1) && (gameObject.GetComponent<Loot>().item.iType != InventoryItem.itemType.Lantern))
                retName = retName + " (" + displayCount + ")";
        }

        if (interactableType == 3)
            retName = gameObject.GetComponent<DialogueSystemTrigger>().conversation;

        HUDUIBehavior.instance.triggerPopup(retName, isChar);
    }


}
