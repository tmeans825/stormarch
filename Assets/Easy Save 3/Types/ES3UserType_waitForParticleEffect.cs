using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("hasPlayed")]
	public class ES3UserType_waitForParticleEffect : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_waitForParticleEffect() : base(typeof(waitForParticleEffect)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (waitForParticleEffect)obj;
			
			writer.WriteProperty("hasPlayed", instance.hasPlayed, ES3Type_bool.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (waitForParticleEffect)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "hasPlayed":
						instance.hasPlayed = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_waitForParticleEffectArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_waitForParticleEffectArray() : base(typeof(waitForParticleEffect[]), ES3UserType_waitForParticleEffect.Instance)
		{
			Instance = this;
		}
	}
}