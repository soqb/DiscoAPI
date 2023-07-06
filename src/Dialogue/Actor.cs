using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace DiscoAPI.Dialogue;

public class Actor : Asset
{
    public string displayName;

    public Actor(int internalID, string id, string displayName) : base(internalID, id)
    {
        this.displayName = displayName;
    }
}