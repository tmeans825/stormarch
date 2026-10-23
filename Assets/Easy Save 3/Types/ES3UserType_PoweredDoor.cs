using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("totalLights", "offLights", "onLights", "doorAnim", "kiplingBlocker", "isCurrentlyOpen")]
	public class ES3UserType_PoweredDoor : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_PoweredDoor() : base(typeof(PoweredDoor)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (PoweredDoor)obj;
			
			writer.WriteProperty("totalLights", instance.totalLights, ES3Type_int.Instance);
			writer.WriteProperty("offLights", instance.offLights, ES3Type_GameObjectArray.Instance);
			writer.WriteProperty("onLights", instance.onLights, ES3Type_GameObjectArray.Instance);
			writer.WritePropertyByRef("doorAnim", instance.doorAnim);
			writer.WritePropertyByRef("kiplingBlocker", instance.kiplingBlocker);
			writer.WritePrivateField("isCurrentlyOpen", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (PoweredDoor)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "totalLights":
						instance.totalLights = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "offLights":
						instance.offLights = reader.Read<UnityEngine.GameObject[]>(ES3Type_GameObjectArray.Instance);
						break;
					case "onLights":
						instance.onLights = reader.Read<UnityEngine.GameObject[]>(ES3Type_GameObjectArray.Instance);
						break;
					case "doorAnim":
						instance.doorAnim = reader.Read<UnityEngine.Animator>();
						break;
					case "kiplingBlocker":
						instance.kiplingBlocker = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "isCurrentlyOpen":
					instance = (PoweredDoor)reader.SetPrivateField("isCurrentlyOpen", reader.Read<System.Boolean>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_PoweredDoorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_PoweredDoorArray() : base(typeof(PoweredDoor[]), ES3UserType_PoweredDoor.Instance)
		{
			Instance = this;
		}
	}
}