using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("shell", "lancerShell", "electroShell", "batonShell", "modifiedBatonShell", "noiseRadius", "upgradedBatonRange", "upgradedBatonSpeed", "globalInfestationCooldown", "muzzleFlashColors", "alignmentArray", "alignmentHistory", "unlockedAlignmentAbilities", "componentCount", "techCount", "homesteadTier", "deathDropScene", "deathDropPos", "deathDropComponentCount", "flagDesignInt", "flagPrimaryColorInt", "flagSecondaryColorInt", "hasBaton", "hasKipling", "shockBatonGun", "shieldsKeyCode", "skillTree", "maxFirebugs", "shotgunSpread", "shotgunBulletCount", "damage", "critChance", "electroAOE", "shockBatonDamage", "shockBatonBulletLife", "shockBatonStunLength", "shockBatonRate", "batonShieldCost", "conductorMultiplier", "conductorCharges", "shields", "maxShields", "shieldRechargeCooldown", "shieldRechargeSpeed", "shieldPopup", "shieldPing", "bullet", "lancerBullet", "electroBullet", "shockBullet", "modifiedShockBullet", "gunSpriteSheetName", "health", "maxHealth", "bleedAmount", "bleedCooldown", "bloodPing", "blood", "healthPopup", "critPopup", "medPopup", "grenade", "firebomb", "huckSpeedMultiplier", "maxThrowMag", "baseSpeed", "slowdownModifier", "slideSpeed", "slideSlowdownModifier", "slideCooldownIncrement", "chestSpriteSheetName", "legsSpriteSheetName", "skinSpriteSheetName", "hairSpriteSheetName", "startingDirection", "faction")]
	public class ES3UserType_PlayerController : ES3ComponentType
	{
		public static ES3Type Instance = null;

		public ES3UserType_PlayerController() : base(typeof(PlayerController)){ Instance = this; priority = 1;}


		protected override void WriteComponent(object obj, ES3Writer writer)
		{
			var instance = (PlayerController)obj;
			
			writer.WritePropertyByRef("shell", instance.shell);
			writer.WritePropertyByRef("lancerShell", instance.lancerShell);
			writer.WritePropertyByRef("electroShell", instance.electroShell);
			writer.WritePropertyByRef("batonShell", instance.batonShell);
			writer.WritePropertyByRef("modifiedBatonShell", instance.modifiedBatonShell);
			writer.WriteProperty("noiseRadius", instance.noiseRadius, ES3Type_float.Instance);
			writer.WriteProperty("upgradedBatonRange", instance.upgradedBatonRange, ES3Type_float.Instance);
			writer.WriteProperty("upgradedBatonSpeed", instance.upgradedBatonSpeed, ES3Type_float.Instance);
			writer.WriteProperty("globalInfestationCooldown", instance.globalInfestationCooldown, ES3Type_float.Instance);
			writer.WriteProperty("muzzleFlashColors", instance.muzzleFlashColors, ES3Type_ColorArray.Instance);
			writer.WriteProperty("alignmentArray", instance.alignmentArray, ES3Type_Vector2.Instance);
			writer.WriteProperty("alignmentHistory", instance.alignmentHistory, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(System.Collections.Generic.List<UnityEngine.Vector2>)));
			writer.WriteProperty("unlockedAlignmentAbilities", instance.unlockedAlignmentAbilities, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(System.Collections.Generic.List<System.String>)));
			writer.WriteProperty("componentCount", instance.componentCount, ES3Type_int.Instance);
			writer.WriteProperty("techCount", instance.techCount, ES3Type_int.Instance);
			writer.WriteProperty("homesteadTier", instance.homesteadTier, ES3Type_int.Instance);
			writer.WriteProperty("deathDropScene", instance.deathDropScene, ES3Type_string.Instance);
			writer.WriteProperty("deathDropPos", instance.deathDropPos, ES3Type_Vector3.Instance);
			writer.WriteProperty("deathDropComponentCount", instance.deathDropComponentCount, ES3Type_int.Instance);
			writer.WriteProperty("flagDesignInt", instance.flagDesignInt, ES3Type_int.Instance);
			writer.WriteProperty("flagPrimaryColorInt", instance.flagPrimaryColorInt, ES3Type_int.Instance);
			writer.WriteProperty("flagSecondaryColorInt", instance.flagSecondaryColorInt, ES3Type_int.Instance);
			writer.WriteProperty("hasBaton", instance.hasBaton, ES3Type_bool.Instance);
			writer.WriteProperty("hasKipling", instance.hasKipling, ES3Type_bool.Instance);
			writer.WritePrivateField("shockBatonGun", instance);
			writer.WritePrivateField("shieldsKeyCode", instance);
			writer.WriteProperty("skillTree", instance.skillTree, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(PlayerController.playerSkill[,])));
			writer.WritePrivateField("maxFirebugs", instance);
			writer.WriteProperty("shotgunSpread", instance.shotgunSpread, ES3Type_float.Instance);
			writer.WriteProperty("shotgunBulletCount", instance.shotgunBulletCount, ES3Type_int.Instance);
			writer.WriteProperty("damage", instance.damage, ES3Type_floatArray.Instance);
			writer.WriteProperty("critChance", instance.critChance, ES3Type_float.Instance);
			writer.WriteProperty("electroAOE", instance.electroAOE, ES3Type_float.Instance);
			writer.WriteProperty("shockBatonDamage", instance.shockBatonDamage, ES3Type_floatArray.Instance);
			writer.WriteProperty("shockBatonBulletLife", instance.shockBatonBulletLife, ES3Type_float.Instance);
			writer.WriteProperty("shockBatonStunLength", instance.shockBatonStunLength, ES3Type_float.Instance);
			writer.WriteProperty("shockBatonRate", instance.shockBatonRate, ES3Type_float.Instance);
			writer.WriteProperty("batonShieldCost", instance.batonShieldCost, ES3Type_float.Instance);
			writer.WriteProperty("conductorMultiplier", instance.conductorMultiplier, ES3Type_float.Instance);
			writer.WritePrivateField("conductorCharges", instance);
			writer.WriteProperty("shields", instance.shields, ES3Type_float.Instance);
			writer.WriteProperty("maxShields", instance.maxShields, ES3Type_float.Instance);
			writer.WriteProperty("shieldRechargeCooldown", instance.shieldRechargeCooldown, ES3Type_float.Instance);
			writer.WriteProperty("shieldRechargeSpeed", instance.shieldRechargeSpeed, ES3Type_float.Instance);
			writer.WritePropertyByRef("shieldPopup", instance.shieldPopup);
			writer.WritePropertyByRef("shieldPing", instance.shieldPing);
			writer.WritePropertyByRef("bullet", instance.bullet);
			writer.WritePropertyByRef("lancerBullet", instance.lancerBullet);
			writer.WritePropertyByRef("electroBullet", instance.electroBullet);
			writer.WritePropertyByRef("shockBullet", instance.shockBullet);
			writer.WritePropertyByRef("modifiedShockBullet", instance.modifiedShockBullet);
			writer.WriteProperty("gunSpriteSheetName", instance.gunSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("health", instance.health, ES3Type_float.Instance);
			writer.WriteProperty("maxHealth", instance.maxHealth, ES3Type_float.Instance);
			writer.WriteProperty("bleedAmount", instance.bleedAmount, ES3Type_float.Instance);
			writer.WriteProperty("bleedCooldown", instance.bleedCooldown, ES3Type_float.Instance);
			writer.WritePropertyByRef("bloodPing", instance.bloodPing);
			writer.WritePropertyByRef("blood", instance.blood);
			writer.WritePropertyByRef("healthPopup", instance.healthPopup);
			writer.WritePropertyByRef("critPopup", instance.critPopup);
			writer.WritePropertyByRef("medPopup", instance.medPopup);
			writer.WritePropertyByRef("grenade", instance.grenade);
			writer.WritePropertyByRef("firebomb", instance.firebomb);
			writer.WriteProperty("huckSpeedMultiplier", instance.huckSpeedMultiplier, ES3Type_float.Instance);
			writer.WriteProperty("maxThrowMag", instance.maxThrowMag, ES3Type_float.Instance);
			writer.WriteProperty("baseSpeed", instance.baseSpeed, ES3Type_float.Instance);
			writer.WriteProperty("slowdownModifier", instance.slowdownModifier, ES3Type_float.Instance);
			writer.WriteProperty("slideSpeed", instance.slideSpeed, ES3Type_float.Instance);
			writer.WriteProperty("slideSlowdownModifier", instance.slideSlowdownModifier, ES3Type_float.Instance);
			writer.WriteProperty("slideCooldownIncrement", instance.slideCooldownIncrement, ES3Type_float.Instance);
			writer.WriteProperty("chestSpriteSheetName", instance.chestSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("legsSpriteSheetName", instance.legsSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("skinSpriteSheetName", instance.skinSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("hairSpriteSheetName", instance.hairSpriteSheetName, ES3Type_string.Instance);
			writer.WriteProperty("startingDirection", instance.startingDirection, ES3Type_int.Instance);
			writer.WriteProperty("faction", instance.faction, ES3Type_int.Instance);
		}

		protected override void ReadComponent<T>(ES3Reader reader, object obj)
		{
			var instance = (PlayerController)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "shell":
						instance.shell = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "lancerShell":
						instance.lancerShell = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "electroShell":
						instance.electroShell = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "batonShell":
						instance.batonShell = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "modifiedBatonShell":
						instance.modifiedBatonShell = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "noiseRadius":
						instance.noiseRadius = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "upgradedBatonRange":
						instance.upgradedBatonRange = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "upgradedBatonSpeed":
						instance.upgradedBatonSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "globalInfestationCooldown":
						instance.globalInfestationCooldown = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "muzzleFlashColors":
						instance.muzzleFlashColors = reader.Read<UnityEngine.Color[]>(ES3Type_ColorArray.Instance);
						break;
					case "alignmentArray":
						instance.alignmentArray = reader.Read<UnityEngine.Vector2>(ES3Type_Vector2.Instance);
						break;
					case "alignmentHistory":
						instance.alignmentHistory = reader.Read<System.Collections.Generic.List<UnityEngine.Vector2>>();
						break;
					case "unlockedAlignmentAbilities":
						instance.unlockedAlignmentAbilities = reader.Read<System.Collections.Generic.List<System.String>>();
						break;
					case "componentCount":
						instance.componentCount = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "techCount":
						instance.techCount = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "homesteadTier":
						instance.homesteadTier = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "deathDropScene":
						instance.deathDropScene = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "deathDropPos":
						instance.deathDropPos = reader.Read<UnityEngine.Vector3>(ES3Type_Vector3.Instance);
						break;
					case "deathDropComponentCount":
						instance.deathDropComponentCount = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "flagDesignInt":
						instance.flagDesignInt = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "flagPrimaryColorInt":
						instance.flagPrimaryColorInt = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "flagSecondaryColorInt":
						instance.flagSecondaryColorInt = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "hasBaton":
						instance.hasBaton = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "hasKipling":
						instance.hasKipling = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "shockBatonGun":
					instance = (PlayerController)reader.SetPrivateField("shockBatonGun", reader.Read<Gun>(), instance);
					break;
					case "shieldsKeyCode":
					instance = (PlayerController)reader.SetPrivateField("shieldsKeyCode", reader.Read<UnityEngine.KeyCode>(), instance);
					break;
					case "skillTree":
						instance.skillTree = reader.Read<PlayerController.playerSkill[,]>();
						break;
					case "maxFirebugs":
					instance = (PlayerController)reader.SetPrivateField("maxFirebugs", reader.Read<System.Int32>(), instance);
					break;
					case "shotgunSpread":
						instance.shotgunSpread = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shotgunBulletCount":
						instance.shotgunBulletCount = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "damage":
						instance.damage = reader.Read<System.Single[]>(ES3Type_floatArray.Instance);
						break;
					case "critChance":
						instance.critChance = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "electroAOE":
						instance.electroAOE = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shockBatonDamage":
						instance.shockBatonDamage = reader.Read<System.Single[]>(ES3Type_floatArray.Instance);
						break;
					case "shockBatonBulletLife":
						instance.shockBatonBulletLife = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shockBatonStunLength":
						instance.shockBatonStunLength = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shockBatonRate":
						instance.shockBatonRate = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "batonShieldCost":
						instance.batonShieldCost = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "conductorMultiplier":
						instance.conductorMultiplier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "conductorCharges":
					instance = (PlayerController)reader.SetPrivateField("conductorCharges", reader.Read<System.Int32>(), instance);
					break;
					case "shields":
						instance.shields = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "maxShields":
						instance.maxShields = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shieldRechargeCooldown":
						instance.shieldRechargeCooldown = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shieldRechargeSpeed":
						instance.shieldRechargeSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "shieldPopup":
						instance.shieldPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "shieldPing":
						instance.shieldPing = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "bullet":
						instance.bullet = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "lancerBullet":
						instance.lancerBullet = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "electroBullet":
						instance.electroBullet = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "shockBullet":
						instance.shockBullet = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "modifiedShockBullet":
						instance.modifiedShockBullet = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "gunSpriteSheetName":
						instance.gunSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "health":
						instance.health = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "maxHealth":
						instance.maxHealth = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bleedAmount":
						instance.bleedAmount = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bleedCooldown":
						instance.bleedCooldown = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "bloodPing":
						instance.bloodPing = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "blood":
						instance.blood = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "healthPopup":
						instance.healthPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "critPopup":
						instance.critPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "medPopup":
						instance.medPopup = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "grenade":
						instance.grenade = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "firebomb":
						instance.firebomb = reader.Read<UnityEngine.GameObject>(ES3Type_GameObject.Instance);
						break;
					case "huckSpeedMultiplier":
						instance.huckSpeedMultiplier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "maxThrowMag":
						instance.maxThrowMag = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "baseSpeed":
						instance.baseSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slowdownModifier":
						instance.slowdownModifier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideSpeed":
						instance.slideSpeed = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideSlowdownModifier":
						instance.slideSlowdownModifier = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "slideCooldownIncrement":
						instance.slideCooldownIncrement = reader.Read<System.Single>(ES3Type_float.Instance);
						break;
					case "chestSpriteSheetName":
						instance.chestSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "legsSpriteSheetName":
						instance.legsSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "skinSpriteSheetName":
						instance.skinSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "hairSpriteSheetName":
						instance.hairSpriteSheetName = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "startingDirection":
						instance.startingDirection = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "faction":
						instance.faction = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_PlayerControllerArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_PlayerControllerArray() : base(typeof(PlayerController[]), ES3UserType_PlayerController.Instance)
		{
			Instance = this;
		}
	}
}