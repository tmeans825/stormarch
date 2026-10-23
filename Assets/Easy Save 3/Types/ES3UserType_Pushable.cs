using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("finalLoc")]
	public class ES3UserType_Pushable : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Pushable() : base(typeof(Pushable)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Pushable)obj;
			
			writer.WritePrivateField("finalLoc", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Pushable)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "finalLoc":
					instance = (Pushable)reader.SetPrivateField("finalLoc", reader.Read<UnityEngine.Vector3>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_PushableArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_PushableArray() : base(typeof(Pushable[]), ES3UserType_Pushable.Instance)
		{
			Instance = this;
		}
	}
}