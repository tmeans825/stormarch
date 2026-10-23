using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("startY", "endY", "extended")]
	public class ES3UserType_Bridge : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Bridge() : base(typeof(Bridge)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Bridge)obj;
			
			writer.WriteProperty("startY", instance.startY, ES3Type_float.Instance);
			writer.WriteProperty("endY", instance.endY, ES3Type_float.Instance);
			writer.WritePrivateField("extended", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Bridge)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "startY":
						instance.startY = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "endY":
						instance.endY = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "extended":
					instance = (Bridge)reader.SetPrivateField("extended", reader.Read<System.Boolean>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_BridgeArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_BridgeArray() : base(typeof(Bridge[]), ES3UserType_Bridge.Instance)
		{
			Instance = this;
		}
	}
}