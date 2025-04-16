using HarmonyLib;
using SM = Sunshine.Metric;
using SD = Sunshine.Dialogue;
using System;

namespace DiscoAPI.Runtime.Patches;

public static class MoraleHealthPatches
{

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.VolitionHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayVolitionHealAmount(ref int __result, int hoursSlept)
	{
		__result = Math.Min(Math.Abs(DiscoRunner.world!.you.MoraleRaw.damageValue), hoursSlept);
		return false;
	}

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.EnduranceHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayEnduranceHealAmount(ref int __result, int hoursSlept)
	{
		__result = Math.Min(Math.Abs(DiscoRunner.world!.you.HealthRaw.damageValue), hoursSlept);
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.DamageVolition))]
	[HarmonyPrefix]
	private static bool OnDamageVolition(int amount)
	{
		Sunshine.EndgameManager.NewspaperToShow = null;
		if (amount > 0)
		{
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.DamageValue(amount);
			you.Recalc();
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
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.HealValue(Math.Min(amount, you.MoraleRaw.maximumValue - you.MoraleRaw.value));
			you.Recalc();
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
			CharacterSheet you = DiscoRunner.world!.you;
			you.HealthRaw.DamageValue(amount);
			you.Recalc();
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
			CharacterSheet you = DiscoRunner.world!.you;
			you.HealthRaw.HealValue(Math.Min(amount, you.HealthRaw.maximumValue - you.MoraleRaw.value));
			you.Recalc();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.HealedHealth, $"+{amount}");
		}
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentVolition))]
	[HarmonyPrefix]
	private static bool OnCurrentVolition(ref double __result)
	{
		__result = DiscoRunner.world!.you.MoraleRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasVolitionDamage))]
	[HarmonyPrefix]
	private static bool OnHasVolitionDamage(ref bool __result)
	{
		__result = DiscoRunner.world!.you.MoraleRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllVolition))]
	[HarmonyPrefix]
	private static bool OnHealAllVolition()
	{
		CharacterManipulations.HealVolition(-DiscoRunner.world!.you.MoraleRaw.damageValue);
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentEndurance))]
	[HarmonyPrefix]
	private static bool OnCurrentEndurance(ref double __result)
	{
		__result = DiscoRunner.world!.you.HealthRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasEnduranceDamage))]
	[HarmonyPrefix]
	private static bool OnHasEnduranceDamage(ref bool __result)
	{
		__result = DiscoRunner.world!.you.HealthRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllEndurance))]
	[HarmonyPrefix]
	private static bool OnHealAllEndurance()
	{
		CharacterManipulations.HealEndurance(-DiscoRunner.world!.you.HealthRaw.damageValue);
		return false;
	}

	[HarmonyPatch(typeof(PortraitVisualizer), nameof(PortraitVisualizer.RefreshStats))]
	[HarmonyPrefix]
	private static bool OnRefreshStats(PortraitVisualizer __instance)
	{
		if (__instance.characterSheet == null) return true;

		SM.Skill morale = DiscoRunner.world!.you.MoraleRaw;
		SM.Skill health = DiscoRunner.world!.you.HealthRaw;
		__instance.volition.Max = morale.maximumValue;
		__instance.endurance.Max = health.maximumValue;
		__instance.volition.Current = morale.value;
		__instance.endurance.Current = health.value;
		return false;
	}
}
