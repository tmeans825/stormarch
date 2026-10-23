using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HighlightPlus2D;

public class Loot : MonoBehaviour
{
    public InventoryItem item;
    public int rarity = 0;

    [ES3Serializable]
    private string spriteSheetName;

    private Dictionary<string, Sprite> spriteSheet;  
    private string loadedSpriteSheetName;
    private SpriteRenderer itemModelSpriteRenderer;
    private SpriteRenderer raritySpriteRenderer;
    [System.NonSerialized]
    public BoxCollider2D terrainCollider;

    public GameObject[] objsToEnableOnPickup;

    private void Start()
    {
        Sprite itemSprite;
        if (item.iType == InventoryItem.itemType.Armor)
        {
            Armor tempArmor = item as Armor;
            itemSprite = LootMaster.instance.getArmorSprite(tempArmor.aType, tempArmor.spritesheetName);
        }
        else
            itemSprite = item.itemModel;

        GetComponent<SpriteRenderer>().sprite = itemSprite;
        raritySpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        terrainCollider = GetComponent<BoxCollider2D>();

        rarity = item.rarity;

        this.LoadSpriteSheet();
    }
    public void pickup()
    {
        //
        if (item.maxCount == 1 || item.count < 1)
        {
            if (objsToEnableOnPickup != null)
            {
                foreach (GameObject obj in objsToEnableOnPickup)
                    obj.SetActive(true);
            }

            gameObject.SetActive(false);
            /*
            gameObject.GetComponent<CircleCollider2D>().enabled = false;
            gameObject.GetComponent<BoxCollider2D>().enabled = false;
            StartCoroutine(remove());*/
        }
    }

    private IEnumerator remove()
    {
        transform.position += new Vector3(100f, 100f);
        ObjectSelector.current.RemoveFromTouchables(this.gameObject);
        yield return new WaitForSeconds(0.1f);      
        Destroy(gameObject);
    }

    public void initializeChestDrop()
    {
        if(terrainCollider == null)
            terrainCollider = GetComponent<BoxCollider2D>();
        terrainCollider.enabled = false;
        StartCoroutine(turnOnCollider());
    }

    private IEnumerator turnOnCollider()
    {
        yield return new WaitForSeconds(0.5f);
        terrainCollider.enabled = true;
    }

    private void LateUpdate()
    {
        if (this.loadedSpriteSheetName != this.spriteSheetName)
        {
            this.LoadSpriteSheet();
        }

        Sprite data;
        if (!spriteSheet.TryGetValue(this.raritySpriteRenderer.sprite.name, out data))
        {
            Debug.LogError("Dictionary doesn't have the value " + this.raritySpriteRenderer.sprite.name);
        }
        else
        {
            this.raritySpriteRenderer.sprite = this.spriteSheet[this.raritySpriteRenderer.sprite.name];
        }

        
    }

    private void LoadSpriteSheet()
    {
        setRarity();

        var sprites = Resources.LoadAll<Sprite>("Rarities/" + this.spriteSheetName);
        this.spriteSheet = sprites.ToDictionary(x => x.name, x => x);

        this.loadedSpriteSheetName = this.spriteSheetName;
    }

    private void setRarity()
    {
        switch (rarity)
        {
            case 0:
                spriteSheetName = "craftable";
                break;
            case 1:
                spriteSheetName = "common";
                break;
            case 2:
                spriteSheetName = "uncommon";
                break;
            case 3:
                spriteSheetName = "rare";
                break;
            case 4:
                spriteSheetName = "exceptional";
                break;
            case 5:
                spriteSheetName = "unique";
                break;
        }
    }
}