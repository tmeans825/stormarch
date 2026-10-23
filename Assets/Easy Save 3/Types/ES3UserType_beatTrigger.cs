using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("enabled")]
	public class ES3UserType_beatTrigger : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_beatTrigger() : base(typeof(beatTrigger)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (beatTrigger)obj;
			
			writer.WriteProperty("enabled", instance.enabled, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (beatTrigger)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "enabled":
						instance.enabled = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_beatTriggerArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_beatTriggerArray() : base(typeof(beatTrigger[]), ES3UserType_beatTrigger.Instance)
		{
			Instance = this;
		}
	}
}