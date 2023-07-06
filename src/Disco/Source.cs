using BepInEx.Logging;
using DiscoAPI.Dialogue;

namespace DiscoAPI;

public class DiscoSource
{
    public ManualLogSource log;
    public DiscoManager manager;
    public DialogueSource dialogue;
    public readonly string guid;

    public DiscoSource(DiscoManager manager, string guid)
    {
        this.manager = manager;
        this.guid = guid;
        dialogue = new DialogueSource(this);

        log = new ManualLogSource(guid);
        BepInEx.Logging.Logger.Sources.Add(log);
    }

}