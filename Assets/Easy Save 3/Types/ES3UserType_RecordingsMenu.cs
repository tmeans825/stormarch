using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("recordingUnlockedMatrix", "conversationMatrix")]
	public class ES3UserType_RecordingsMenu : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_RecordingsMenu() : base(typeof(RecordingsMenu)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (RecordingsMenu)obj;
			
			writer.WriteProperty("recordingUnlockedMatrix", instance.recordingUnlockedMatrix, ES3Type_boolArray.Instance);
			writer.WriteProperty("conversationMatrix", instance.conversationMatrix, ES3Type_StringArray.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (RecordingsMenu)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "recordingUnlockedMatrix":
						instance.recordingUnlockedMatrix = reader.Read<System.Boolean[]>(ES3Type_boolArray.Instance);
						break;
					case "conversationMatrix":
						instance.conversationMatrix = reader.Read<System.String[]>(ES3Type_StringArray.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_RecordingsMenuArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_RecordingsMenuArray() : base(typeof(RecordingsMenu[]), ES3UserType_RecordingsMenu.Instance)
		{
			Instance = this;
		}
	}
}