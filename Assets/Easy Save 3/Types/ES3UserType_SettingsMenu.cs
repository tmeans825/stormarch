using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute()]
	public class ES3UserType_SettingsMenu : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_SettingsMenu() : base(typeof(SettingsMenu)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (SettingsMenu)obj;
			
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (SettingsMenu)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_SettingsMenuArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_SettingsMenuArray() : base(typeof(SettingsMenu[]), ES3UserType_SettingsMenu.Instance)
		{
			Instance = this;
		}
	}
}