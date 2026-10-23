using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("item", "objsToEnableOnPickup")]
	public class ES3UserType_Loot : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Loot() : base(typeof(Loot)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Loot)obj;
			
			writer.WriteProperty("item", instance.item, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(InventoryItem)));
			writer.WriteProperty("objsToEnableOnPickup", instance.objsToEnableOnPickup, ES3Type_GameObjectArray.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Loot)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "item":
						instance.item = reader.Read<InventoryItem>();
						break;
					case "objsToEnableOnPickup":
						instance.objsToEnableOnPickup = reader.Read<UnityEngine.GameObject[]>(ES3Type_GameObjectArray.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_LootArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_LootArray() : base(typeof(Loot[]), ES3UserType_Loot.Instance)
		{
			Instance = this;
		}
	}
}