using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("permanentlyOn")]
	public class ES3UserType_ComputerBehavior : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_ComputerBehavior() : base(typeof(ComputerBehavior)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (ComputerBehavior)obj;
			
			writer.WritePrivateField("permanentlyOn", instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (ComputerBehavior)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "permanentlyOn":
					instance = (ComputerBehavior)reader.SetPrivateField("permanentlyOn", reader.Read<System.Boolean>(), instance);
					break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_ComputerBehaviorArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_ComputerBehaviorArray() : base(typeof(ComputerBehavior[]), ES3UserType_ComputerBehavior.Instance)
		{
			Instance = this;
		}
	}
}