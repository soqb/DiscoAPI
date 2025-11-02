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
    private ConfigEntry<bool> enableTabulaRasa;
    private ConfigEntry<bool> forceDisableVTCacheCompression;
    private ConfigEntry<bool> drawVTOverlay;
    private ConfigEntry<bool> spawnVTCacheBillboard;
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
    public static bool EnableTabulaRasa
    {
        get => Instance.enableTabulaRasa.Value;
        set => Instance.enableTabulaRasa.Value = value;
    }
    public static bool ForceDisableVTCacheCompression
    {
        get => Instance.forceDisableVTCacheCompression.Value;
        set => Instance.forceDisableVTCacheCompression.Value = value;
    }
    public static bool DrawVTOverlay
    {
        get => Instance.drawVTOverlay.Value;
        set => Instance.drawVTOverlay.Value = value;
    }
    public static bool SpawnVTCacheBillboard
    {
        get => Instance.spawnVTCacheBillboard.Value;
        set => Instance.spawnVTCacheBillboard.Value = value;
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

        enableTabulaRasa = cfg.Bind(
            "Tools",
            "EnableTabulaRasa",
            false,
            "Enable Tabula Rasa patch to disable vanilla content such as characters, containers, orbs ..."
        );

        forceDisableVTCacheCompression = cfg.Bind(
            "VirtualTextureDebug",
            "ForceDisableVTCacheCompression",
            false,
            "Disable virtual texture cache compression"
        );
        drawVTOverlay = cfg.Bind(
            "VirtualTextureDebug",
            "DrawVTOverlay",
            false,
            "Draw colored borders on the edges of virtual texture pages to reveal their shape"
        );
        spawnVTCacheBillboard = cfg.Bind(
            "VirtualTextureDebug",
            "SpawnVTCacheBillboard",
            false,
            "Spawn a massive billboard sprite over the player which displays the VT diffusion cache"
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

        forceDisableVTCacheCompression.SettingChanged += NowAndLater(() =>
        {
            VirtualTextures.CustomVirtualTextureManager.UpdateCacheCompression(!ForceDisableVTCacheCompression);
            VirtualTextures.CustomVirtualTextureManager.Reset();
        });

        EventHandler vtmReset = (_, _) => VirtualTextures.CustomVirtualTextureManager.Reset();
        drawVTOverlay.SettingChanged += vtmReset;
        disableVTDiffusionMaps.SettingChanged += vtmReset;
        disableVTNormalMaps.SettingChanged += vtmReset;
        disableVTSpecularMaps.SettingChanged += vtmReset;

        spawnVTCacheBillboard.SettingChanged += NowAndLater(() => LobbyLoadExecutor.OnLobbyLoad += () =>
        {
            VirtualTextures.VTCacheBillboard.Toggle(SpawnVTCacheBillboard);
        });

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
