using System.Collections.Generic;
using BepInEx.Logging;
using DiscoAPI.Common;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Dialogue;

namespace DiscoAPI.Runtime;

public class DiscoSource : IDiscoSource
{
    public ManualLogSource log;

    public DiscoSourceMode Mode => DiscoSourceMode.Mutable;

    public string Guid { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public List<string>? Authors { get; init; }
    public DialogueSource Dialogue { get; init; }

    public DiscoManager Manager { get; init; }
    IDiscoManager IDiscoSource.Manager => this.Manager;
    IDialogueSource IDiscoSource.Dialogue => this.Dialogue;

    public DiscoSource(DiscoManager manager, string guid, bool isDisco)
    {
        Guid = guid;
        Manager = manager;
        Dialogue = isDisco ? DialogueSource.CreateDisco(this) : DialogueSource.Create(this);

        log = new ManualLogSource(guid);
        BepInEx.Logging.Logger.Sources.Add(log);
    }

    public void LogWarning(string? message) => log.LogWarning(message ?? "null");
    public void LogInfo(string? message) => log.LogInfo(message ?? "null");
    public void LogDebug(string? message) => log.LogDebug(message ?? "null");
    public void LogError(string? message) => log.LogError(message ?? "null");
    public void LogFatal(string? message) => log.LogFatal(message ?? "null");
}
