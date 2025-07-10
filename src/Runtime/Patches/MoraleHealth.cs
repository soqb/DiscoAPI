using HarmonyLib;
using SM = Sunshine.Metric;
using SD = Sunshine.Dialogue;
using System;
using DiscoAPI.Runtime.Components;

namespace DiscoAPI.Runtime.Patches;

public static class MoraleHealthPatches
{
	private static ModEntity<SM.CharacterSheet> You = DiscoRunner.world!.You();
	private static SkillContainer YouSkills = CharacterComponents.Skills.Of(You)!;

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.VolitionHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayVolitionHealAmount(ref int __result, int hoursSlept)
	{
		__result = Math.Min(Math.Abs(YouSkills.MoraleRaw.damageValue), hoursSlept);
		return false;
	}

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.EnduranceHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayEnduranceHealAmount(ref int __result, int hoursSlept)
	{
		__result = Math.Min(Math.Abs(YouSkills.HealthRaw.damageValue), hoursSlept);
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.DamageVolition))]
	[HarmonyPrefix]
	private static bool OnDamageVolition(int amount)
	{
		Sunshine.EndgameManager.NewspaperToShow = null;
		if (amount > 0)
		{
			YouSkills.MoraleRaw.DamageValue(amount);
			You.EntityBase.Recalc();
			CharacterManipulations.PlayVolitionDamageVisual();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.DamagedMorale, (-amount).ToString());
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.HealVolition))]
	[HarmonyPrefix]
	private static bool OnHealVolition(int amount)
	{
		if (amount > 0)
		{
			SM.Skill morale = YouSkills.MoraleRaw;
			morale.HealValue(Math.Min(amount, morale.maximumValue - morale.value));
			You.EntityBase.Recalc();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.HealedMorale, $"+{amount}");
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.DamageEndurance))]
	[HarmonyPrefix]
	private static bool OnDamageEndurance(int amount)
	{
		Sunshine.EndgameManager.NewspaperToShow = null;
		if (amount > 0)
		{
			YouSkills.HealthRaw.DamageValue(amount);
			You.EntityBase.Recalc();
			CharacterManipulations.PlayVolitionDamageVisual();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.DamagedHealth, (-amount).ToString());
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.HealEndurance))]
	[HarmonyPrefix]
	private static bool OnHealEndurance(int amount)
	{
		if (amount > 0)
		{
			SM.Skill health = YouSkills.HealthRaw;
			health.HealValue(Math.Min(amount, health.maximumValue - health.value));
			You.EntityBase.Recalc();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.HealedHealth, $"+{amount}");
		}
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentVolition))]
	[HarmonyPrefix]
	private static bool OnCurrentVolition(ref double __result)
	{
		__result = YouSkills.MoraleRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasVolitionDamage))]
	[HarmonyPrefix]
	private static bool OnHasVolitionDamage(ref bool __result)
	{
		__result = YouSkills.MoraleRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllVolition))]
	[HarmonyPrefix]
	private static bool OnHealAllVolition()
	{
		CharacterManipulations.HealVolition(-YouSkills.MoraleRaw.damageValue);
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentEndurance))]
	[HarmonyPrefix]
	private static bool OnCurrentEndurance(ref double __result)
	{
		__result = YouSkills.HealthRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasEnduranceDamage))]
	[HarmonyPrefix]
	private static bool OnHasEnduranceDamage(ref bool __result)
	{
		__result = YouSkills.HealthRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllEndurance))]
	[HarmonyPrefix]
	private static bool OnHealAllEndurance()
	{
		CharacterManipulations.HealEndurance(-YouSkills.HealthRaw.damageValue);
		return false;
	}

	[HarmonyPatch(typeof(PortraitVisualizer), nameof(PortraitVisualizer.RefreshStats))]
	[HarmonyPrefix]
	private static bool OnRefreshStats(PortraitVisualizer __instance)
	{
		if (__instance.characterSheet == null) return true;

		var skills = CharacterComponents.Skills.Of(__instance.characterSheet)!;
		SM.Skill morale = skills.MoraleRaw;
		SM.Skill health = skills.HealthRaw;
		__instance.volition.Max = morale.maximumValue;
		__instance.endurance.Max = health.maximumValue;
		__instance.volition.Current = morale.value;
		__instance.endurance.Current = health.value;
		return false;
	}
}
