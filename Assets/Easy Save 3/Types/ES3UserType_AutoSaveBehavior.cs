using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("usedUp")]
	public class ES3UserType_AutoSaveBehavior : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_AutoSaveBehavior() : base(typeof(AutoSaveBehavior)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (AutoSaveBehavior)obj;
			
			writer.WriteProperty("usedUp", instance.usedUp, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (AutoSaveBehavior)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "usedUp":
						instance.usedUp = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_AutoSaveBehaviorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_AutoSaveBehaviorArray() : base(typeof(AutoSaveBehavior[]), ES3UserType_AutoSaveBehavior.Instance)
		{
			Instance = this;
		}
	}
}