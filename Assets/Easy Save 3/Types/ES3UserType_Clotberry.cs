using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("used")]
	public class ES3UserType_Clotberry : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_Clotberry() : base(typeof(Clotberry)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (Clotberry)obj;
			
			writer.WriteProperty("used", instance.used, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (Clotberry)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "used":
						instance.used = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_ClotberryArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_ClotberryArray() : base(typeof(Clotberry[]), ES3UserType_Clotberry.Instance)
		{
			Instance = this;
		}
	}
}