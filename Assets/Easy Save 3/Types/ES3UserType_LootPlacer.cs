using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("itemType", "rarity", "tier", "gunModel", "isBaton", "throwableType", "healableType", "armorType", "conductorCharges", "enabled")]
	public class ES3UserType_LootPlacer : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_LootPlacer() : base(typeof(LootPlacer)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (LootPlacer)obj;
			
			writer.WriteProperty("itemType", instance.itemType, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(InventoryItem.itemType)));
			writer.WriteProperty("rarity", instance.rarity, ES3Type_int.Instance);
			writer.WriteProperty("gunModel", instance.gunModel, ES3Type_int.Instance);
            writer.WriteProperty("isBaton", instance.isShockBaton, ES3Type_bool.Instance);
            writer.WriteProperty("throwableType", instance.throwableType, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(Throwable.throwableType)));
			writer.WriteProperty("healableType", instance.healableType, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(Healable.healableType)));
			writer.WriteProperty("armorType", instance.armorType, ES3Type_int.Instance);
			writer.WriteProperty("conductorCharges", instance.conductorCharges, ES3Type_int.Instance);
			writer.WriteProperty("enabled", instance.enabled, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (LootPlacer)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "itemType":
						instance.itemType = reader.Read<InventoryItem.itemType>();
						break;
					case "rarity":
						instance.rarity = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "gunModel":
						instance.gunModel = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "isBaton":
						instance.isShockBaton = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "throwableType":
						instance.throwableType = reader.Read<Throwable.throwableType>();
						break;
					case "healableType":
						instance.healableType = reader.Read<Healable.healableType>();
						break;
					case "armorType":
						instance.armorType = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "conductorCharges":
						instance.conductorCharges = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "enabled":
						instance.enabled = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_LootPlacerArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_LootPlacerArray() : base(typeof(LootPlacer[]), ES3UserType_LootPlacer.Instance)
		{
			Instance = this;
		}
	}
}