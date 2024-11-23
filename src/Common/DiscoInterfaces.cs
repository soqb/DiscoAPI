using System.Collections.Generic;
using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Common;

public interface IDiscoSource
{
    public string Guid { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public List<string>? Authors { get; }
    public IDiscoManager Manager { get; }
    public IDialogueSource Dialogue { get; }

    public void LogWarning(string message);
    public void LogInfo(string message);
    public void LogDebug(string message);
    public void LogError(string message);
    public void LogFatal(string message);
}

public interface IDiscoManager
{
    public IDialogueManager Dialogue { get; }
    public IDiscoSource GetSource(string key);
}
