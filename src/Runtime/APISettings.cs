using System;
using System.Linq;
using BepInEx.Configuration;

namespace DiscoAPI.Runtime;

public class DiscoAPISettings
{
	public static DiscoAPISettings Instance => DiscoAPIPlugin.Instance.settings;

	private ConfigEntry<bool> allowAchievements;
	private ConfigEntry<bool> enableLuaConsole;
	private ConfigEntry<bool> dumpSourcesOnStartup;
	private ConfigEntry<bool> logMore;
	private ConfigEntry<bool> enableDeveloperMode;
	private ConfigEntry<bool> allChecksPass;

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
	public static bool EnableDeveloperMode
	{
		get => Instance.enableDeveloperMode.Value;
		set => Instance.enableDeveloperMode.Value = value;
	}
	public static bool AllChecksPass
	{
		get => Instance.allChecksPass.Value;
		set => Instance.allChecksPass.Value = value;
	}

	private EventHandler NowAndLater(Action something)
	{
		LobbyLoadExecutor.OnLobbyLoad += something;
		return (_, _) => something();
	}

	public DiscoAPISettings(ConfigFile cfg)
	{
		cfg.SaveOnConfigSet = true;
		allowAchievements = cfg.Bind(
			"General",
			"AllowAchievements",
			false,
			"Re-enable achievements — the API disables them by default for safety"
		);

		enableLuaConsole = cfg.Bind(
			"Tools",
			"EnableLuaConsole",
			false,
			"Enable the use of the Lua Console (for dialogue) with ctrl+enter"
		);
		enableLuaConsole.SettingChanged += NowAndLater(() => LobbyLoadExecutor.OnLobbyLoad += () =>
		{
			if (EnableLuaConsole) LuaConsoleManager.AttachLuaConsole();
		});

		dumpSourcesOnStartup = cfg.Bind(
			"Tools",
			"DumpSourcesOnStartup",
			false,
			"Dump the contents of the base game asset tables for introspection"
		);

		enableDeveloperMode = cfg.Bind(
			"Tools",
			"EnableDeveloperMode",
			false,
			"Open up base game tooling"
		);
		enableDeveloperMode.SettingChanged += NowAndLater(() => LobbyLoadExecutor.OnLobbyLoad += () =>
		{
			var modes = Sunshine.DebugModes.Singleton;

			if (EnableDeveloperMode) modes.SetDeveloperMode();
			else modes.UnsetDeveloperMode();
		});

		allChecksPass = cfg.Bind(
			"Tools",
			"AllChecksPass",
			false,
			"Feeling lucky?"
		);

		logMore = cfg.Bind(
			"Debug",
			"ExtendedLogging",
			false,
			"Enable logging for more parts of the core game"
		);

		Log.COMPONENT[] needsSwitching = Enum.GetValues<Log.COMPONENT>().Where(f => !Log.IsComponentActive(f)).ToArray();

		logMore.SettingChanged += NowAndLater(() =>
		{
			// LocalizationCustomSystem.LocalizationManager.Singleton.DebugLogs = LogMore;
			foreach (var elem in needsSwitching)
			{
				Log.componentState[elem] = LogMore;
			}
		});
	}
}
