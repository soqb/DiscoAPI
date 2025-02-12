using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class FieldEditor
{
    public string path;
    public Il2CppSystem.Collections.Generic.List<PC.Field> fields;

    public FieldEditor(string path, Il2CppSystem.Collections.Generic.List<PC.Field> fields)
    {
        this.fields = fields;
        this.path = path;
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

    public void Set(string name, string value) => Set(name, FieldType.Text, value.ToString());
    public void Set(string name, bool value) => Set(name, FieldType.Boolean, value.ToString());
    public void Set(string name, int value) => Set(name, FieldType.Number, value.ToString());
    public void Set(string name, double value) => Set(name, FieldType.Number, value.ToString());
}
