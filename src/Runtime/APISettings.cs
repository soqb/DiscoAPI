using BepInEx.Configuration;

namespace DiscoAPI.Runtime;

public class DiscoAPISettings
{
	public static DiscoAPISettings Instance => DiscoAPIPlugin.Instance.settings;

	private ConfigEntry<bool> allowAchievements;
	private ConfigEntry<bool> enableLuaConsole;
	private ConfigEntry<bool> dumpSourcesOnStartup;
	private ConfigEntry<bool> logMore;

	public static bool AllowAchievements
	{
		get => Instance.allowAchievements.Value;
		set => Instance.allowAchievements.Value = value;
	}
	public static bool EnableLuaConsole
	{
		get => Instance.enableLuaConsole.Value;
		set => Instance.enableLuaConsole.Value = value;
	}
	public static bool DumpSourcesOnStartup
	{
		get => Instance.dumpSourcesOnStartup.Value;
		set => Instance.dumpSourcesOnStartup.Value = value;
	}
	public static bool LogMore
	{
		get => Instance.logMore.Value;
		set => Instance.logMore.Value = value;
	}

	public DiscoAPISettings(ConfigFile cfg)
	{
		cfg.SaveOnConfigSet = true;
		allowAchievements = cfg.Bind(
			"General",
			"AllowAchievements",
			false,
			"re-enable achievements — the API disables them by default for safety"
		);

		enableLuaConsole = cfg.Bind(
			"Tools",
			"EnableLuaConsole",
			false,
			"enable the use of the Lua Console with ctrl+enter"
		);

		dumpSourcesOnStartup = cfg.Bind(
			"Tools",
			"DumpSourcesOnStartup",
			false,
			"dump the contents of the base game asset tables for introspection"
		);

		logMore = cfg.Bind(
			"Debug",
			"LogVerbose",
			false,
			"enable logging for more parts of the core game"
		);
	}
}
