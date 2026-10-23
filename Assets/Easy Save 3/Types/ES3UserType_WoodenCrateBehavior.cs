using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("isBroken", "finalLoc")]
	public class ES3UserType_WoodenCrateBehavior : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_WoodenCrateBehavior() : base(typeof(WoodenCrateBehavior)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (WoodenCrateBehavior)obj;
			
			writer.WritePrivateField("isBroken", instance);
			writer.WritePrivateField("finalLoc", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (WoodenCrateBehavior)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "isBroken":
					instance = (WoodenCrateBehavior)reader.SetPrivateField("isBroken", reader.Read<System.Boolean>(), instance);
					break;
					case "finalLoc":
					instance = (WoodenCrateBehavior)reader.SetPrivateField("finalLoc", reader.Read<UnityEngine.Vector3>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_WoodenCrateBehaviorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_WoodenCrateBehaviorArray() : base(typeof(WoodenCrateBehavior[]), ES3UserType_WoodenCrateBehavior.Instance)
		{
			Instance = this;
		}
	}
}