using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("ammoArray", "equippedItemIndex", "craftMatArray", "inventory", "equippedArmor")]
	public class ES3UserType_InventoryManager : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_InventoryManager() : base(typeof(InventoryManager)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (InventoryManager)obj;
			
			writer.WriteProperty("ammoArray", instance.ammoArray, ES3Type_intArray.Instance);
			writer.WriteProperty("equippedItemIndex", instance.equippedItemIndex, ES3Type_int.Instance);
			writer.WriteProperty("craftMatArray", instance.craftMatArray, ES3Type_intArray.Instance);
			writer.WritePrivateField("inventory", instance);
			writer.WritePrivateField("equippedArmor", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (InventoryManager)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "ammoArray":
						instance.ammoArray = reader.Read<System.Int32[]>(ES3Type_intArray.Instance);
						break;
					case "equippedItemIndex":
						instance.equippedItemIndex = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "craftMatArray":
						instance.craftMatArray = reader.Read<System.Int32[]>(ES3Type_intArray.Instance);
						break;
					case "inventory":
					instance = (InventoryManager)reader.SetPrivateField("inventory", reader.Read<InventoryItem[]>(), instance);
					break;
					case "equippedArmor":
					instance = (InventoryManager)reader.SetPrivateField("equippedArmor", reader.Read<Armor[]>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_InventoryManagerArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_InventoryManagerArray() : base(typeof(InventoryManager[]), ES3UserType_InventoryManager.Instance)
		{
			Instance = this;
		}
	}
}