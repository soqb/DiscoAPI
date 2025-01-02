using BepInEx.Configuration;

namespace DiscoAPI.Runtime;

public class DiscoAPISettings
{
	public static DiscoAPISettings Instance => DiscoAPIPlugin.Instance.settings;

	public ConfigEntry<bool> allowAchievements;

	public DiscoAPISettings(ConfigFile cfg)
	{
		allowAchievements = cfg.Bind(
			"General",
			"AllowAchievements",
			false,
			"re-enable achievements — the API disables them by default for safety"
		);
	}
}
