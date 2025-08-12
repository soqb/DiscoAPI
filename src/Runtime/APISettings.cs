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
    private ConfigEntry<bool> componentLifecycleTracking;
    private ConfigEntry<bool> forceDisableVTCacheCompression;
    private ConfigEntry<bool> drawVTBorders;
    private ConfigEntry<bool> disableVTDiffusionMaps;
    private ConfigEntry<bool> disableVTNormalMaps;
    private ConfigEntry<bool> disableVTSpecularMaps;

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
    public static bool ComponentLifecycleTracking
    {
        get => Instance.componentLifecycleTracking.Value;
        set => Instance.componentLifecycleTracking.Value = value;
    }
    public static bool ForceDisableVTCacheCompression
    {
        get => Instance.forceDisableVTCacheCompression.Value;
        set => Instance.forceDisableVTCacheCompression.Value = value;
    }
    public static bool DrawVTBorders
    {
        get => Instance.drawVTBorders.Value;
        set => Instance.drawVTBorders.Value = value;
    }
    public static bool DisableVTDiffusionMaps
    {
        get => Instance.disableVTDiffusionMaps.Value;
        set => Instance.disableVTDiffusionMaps.Value = value;
    }
    public static bool DisableVTNormalMaps
    {
        get => Instance.disableVTNormalMaps.Value;
        set => Instance.disableVTNormalMaps.Value = value;
    }
    public static bool DisableVTSpecularMaps
    {
        get => Instance.disableVTSpecularMaps.Value;
        set => Instance.disableVTSpecularMaps.Value = value;
    }

    private static EventHandler NowAndLater(Action something)
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

        componentLifecycleTracking = cfg.Bind(
            "Debug",
            "ComponentLifecycleTracking",
            false,
            "Log the lifecycle stages of entities in the component system. May produce a lot of log messages."
        );
        forceDisableVTCacheCompression = cfg.Bind(
            "VirtualTextureDebug",
            "ForceDisableVTCacheCompression",
            false,
            "Disable virtual texture cache compression."
        );
        drawVTBorders = cfg.Bind(
            "VirtualTextureDebug",
            "DrawVTBorders",
            false,
            "Draw colored borders on the edges of virtual texture pages to reveal their shape"
        );
        disableVTDiffusionMaps = cfg.Bind(
            "VirtualTextureDebug",
            "DisableVTDiffusionMaps",
            false,
            "Turn off the diffusion maps (the color properties) of all virtual textures"
        );
        disableVTNormalMaps = cfg.Bind(
            "VirtualTextureDebug",
            "DisableVTNormalMaps",
            false,
            "Turn off the normal maps (the shape properties) of all virtual textures"
        );
        disableVTSpecularMaps = cfg.Bind(
            "VirtualTextureDebug",
            "DisableVTSpecularMaps",
            false,
            "Turn off the specular maps (the lighting properties) of all virtual textures"
        );


        var vtmReset = NowAndLater(() => VirtualTextures.CustomVirtualTextureManager.Reset());
        drawVTBorders.SettingChanged += vtmReset;
        disableVTDiffusionMaps.SettingChanged += vtmReset;
        disableVTNormalMaps.SettingChanged += vtmReset;
        disableVTSpecularMaps.SettingChanged += vtmReset;

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
