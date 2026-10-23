using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("critterType", "checkRadius", "nextWaypointDistance", "critterMoving", "health", "maxHealth", "bleedAmount", "bleedCooldown", "bloodPing", "blood", "healthPopup", "critPopup", "medPopup", "grenade", "firebomb", "huckSpeedMultiplier", "maxThrowMag", "activeHealthModule", "baseSpeed", "slowdownModifier", "slideSpeed", "slideSlowdownModifier", "slideCooldownIncrement", "chestSpriteSheetName", "legsSpriteSheetName", "skinSpriteSheetName", "hairSpriteSheetName", "heads", "startingDirection", "faction")]
	public class ES3UserType_Critter : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Critter() : base(typeof(Critter)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Critter)obj;
			
			writer.WriteProperty("critterType", instance.critterType, ES3Type_int.Instance);
			writer.WriteProperty("checkRadius", instance.checkRadius, ES3Type_float.Instance);
			writer.WriteProperty("nextWaypointDistance", instance.nextWaypointDistance, ES3Type_float.Instance);
			writer.WriteProperty("critterMoving", instance.critterMoving, ES3Type_bool.Instance);
			writer.WriteProperty("health", instance.health, ES3Type_float.Instance);
			writer.WriteProperty("maxHealth", instance.maxHealth, ES3Type_float.Instance);
			writer.WriteProperty("bleedAmount", instance.bleedAmount, ES3Type_float.Instance);
			writer.WriteProperty("bleedCooldown", instance.bleedCooldown, ES3Type_float.Instance);
			writer.WritePropertyByRef("bloodPing", instance.bloodPing);
			writer.WritePropertyByRef("blood", instance.blood);
			writer.WritePropertyByRef("healthPopup", instance.healthPopup);
			writer.WritePropertyByRef("critPopup", instance.critPopup);
			writer.WritePropertyByRef("medPopup", instance.medPopup);
			writer.WritePropertyByRef("grenade", instance.grenade);
			writer.WritePropertyByRef("firebomb", instance.firebomb);
			writer.WriteProperty("huckSpeedMultiplier", instance.huckSpeedMultiplier, ES3Type_float.Instance);
			writer.WriteProperty("maxThrowMag", instance.maxThrowMag, ES3Type_float.Instance);
			writer.WritePrivateField("activeHealthModule", instance);
			writer.WriteProperty("baseSpeed", instance.baseSpeed, ES3Type_float.Instance);
			writer.WriteProperty("slowdownModifier", instance.slowdownModifier, ES3Type_float.Instance);
			writer.WriteProperty("slideSpeed", instance.slideSpeed, ES3Type_float.Instance);
			writer.WriteProperty("slideSlowdownModifier", instance.slideSlowdownModifier, ES3Type_float.Instance);
			writer.WriteProperty("slideCooldownIncrement", instance.slideCooldownIncrement, ES3Type_float.Instance);
			writer.WriteProperty("chestSpriteSheetName", instance.chestSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("legsSpriteSheetName", instance.legsSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("skinSpriteSheetName", instance.skinSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("hairSpriteSheetName", instance.hairSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("heads", instance.heads, ES3Type_GameObjectArray.Instance);
			writer.WriteProperty("startingDirection", instance.startingDirection, ES3Type_int.Instance);
			writer.WriteProperty("faction", instance.faction, ES3Type_int.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Critter)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "critterType":
						instance.critterType = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "checkRadius":
						instance.checkRadius = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "nextWaypointDistance":
						instance.nextWaypointDistance = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "critterMoving":
						instance.critterMoving = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "health":
						instance.health = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "maxHealth":
						instance.maxHealth = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bleedAmount":
						instance.bleedAmount = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bleedCooldown":
						instance.bleedCooldown = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bloodPing":
						instance.bloodPing = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "blood":
						instance.blood = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "healthPopup":
						instance.healthPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "critPopup":
						instance.critPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "medPopup":
						instance.medPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "grenade":
						instance.grenade = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "firebomb":
						instance.firebomb = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "huckSpeedMultiplier":
						instance.huckSpeedMultiplier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "maxThrowMag":
						instance.maxThrowMag = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "activeHealthModule":
					instance = (Critter)reader.SetPrivateField("activeHealthModule", reader.Read<System.Int32>(), instance);
					break;
					case "baseSpeed":
						instance.baseSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slowdownModifier":
						instance.slowdownModifier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideSpeed":
						instance.slideSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideSlowdownModifier":
						instance.slideSlowdownModifier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideCooldownIncrement":
						instance.slideCooldownIncrement = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "chestSpriteSheetName":
						instance.chestSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "legsSpriteSheetName":
						instance.legsSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "skinSpriteSheetName":
						instance.skinSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "hairSpriteSheetName":
						instance.hairSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "heads":
						instance.heads = reader.Read<UnityEngine.GameObject[]>(ES3Type_GameObjectArray.Instance);
						break;
					case "startingDirection":
						instance.startingDirection = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "faction":
						instance.faction = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_CritterArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_CritterArray() : base(typeof(Critter[]), ES3UserType_Critter.Instance)
		{
			Instance = this;
		}
	}
}