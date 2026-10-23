using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("isOpen")]
	public class ES3UserType_Vent : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Vent() : base(typeof(Vent)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Vent)obj;
			
			writer.WritePrivateField("isOpen", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Vent)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "isOpen":
					instance = (Vent)reader.SetPrivateField("isOpen", reader.Read<System.Boolean>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_VentArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_VentArray() : base(typeof(Vent[]), ES3UserType_Vent.Instance)
		{
			Instance = this;
		}
	}
}