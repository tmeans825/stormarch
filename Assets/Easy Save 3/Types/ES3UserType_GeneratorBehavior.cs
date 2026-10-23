using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("activeIndex")]
	public class ES3UserType_GeneratorBehavior : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_GeneratorBehavior() : base(typeof(GeneratorBehavior)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (GeneratorBehavior)obj;
			
			writer.WritePrivateField("activeIndex", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (GeneratorBehavior)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "activeIndex":
					instance = (GeneratorBehavior)reader.SetPrivateField("activeIndex", reader.Read<System.Int32>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_GeneratorBehaviorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_GeneratorBehaviorArray() : base(typeof(GeneratorBehavior[]), ES3UserType_GeneratorBehavior.Instance)
		{
			Instance = this;
		}
	}
}