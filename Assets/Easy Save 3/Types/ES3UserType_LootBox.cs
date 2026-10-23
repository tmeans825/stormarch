using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("isOpening")]
	public class ES3UserType_LootBox : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_LootBox() : base(typeof(LootBox)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (LootBox)obj;
			
			writer.WriteProperty("isOpening", instance.isOpening, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (LootBox)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "isOpening":
						instance.isOpening = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_LootBoxArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_LootBoxArray() : base(typeof(LootBox[]), ES3UserType_LootBox.Instance)
		{
			Instance = this;
		}
	}
}