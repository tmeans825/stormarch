using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("wasConsumed")]
	public class ES3UserType_OilthistleBehavior : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_OilthistleBehavior() : base(typeof(OilthistleBehavior)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (OilthistleBehavior)obj;

			writer.WritePrivateField("wasConsumed", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (OilthistleBehavior)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{

					case "wasConsumed":
					instance = (OilthistleBehavior)reader.SetPrivateField("wasConsumed", reader.Read<System.Boolean>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_OilthistleBehaviorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_OilthistleBehaviorArray() : base(typeof(OilthistleBehavior[]), ES3UserType_OilthistleBehavior.Instance)
		{
			Instance = this;
		}
	}
}
