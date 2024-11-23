using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class FieldEditor
{
    public string id;
    public Il2CppSystem.Collections.Generic.List<PC.Field> fields;

    public FieldEditor(string id, Il2CppSystem.Collections.Generic.List<PC.Field> fields)
    {
        this.fields = fields;
        this.id = id;
    }

    public void Set(string name, FieldType type, string value)
    {
        var fd = PC.Field.Lookup(fields, name);
        if (fd != null)
        {
            fd.value = value;
        }
        else
        {
            fields.Add(new PC.Field(name, value, (PC.FieldType)type));
        }
    }

    public void Set(string name, bool value) => Set(name, FieldType.Boolean, value.ToString());
    public void Set(string name, int value) => Set(name, FieldType.Number, value.ToString());
}
