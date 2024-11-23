using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using DiscoAPI.Common;
using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

abstract class AssetTable<T, U> : IEnumerable<U>, LocalIdResolver<int> where T : PC.Asset where U : Asset
{
    public delegate U Uncrusher(T asset);

    class TableEnumerator : IEnumerator<U>
    {
        public int idx;
        public AssetTable<T, U> table;

        public TableEnumerator(AssetTable<T, U> table)
        {
            this.table = table;
            Reset();
        }

        public U Current => table.uncrush(table.GetRaw(idx));
        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            return ++idx < table.count;
        }
        public void Reset() => idx = -1;
        public void Dispose() { }
    }

    // The PC-run list of actual assets.
    protected Il2CppSystem.Collections.Generic.List<T> pcList;
    public DialogueSource parent;
    public int count;

    private Uncrusher uncrush;

    protected AssetTable(DialogueSource parent, Uncrusher uncrush, Il2CppSystem.Collections.Generic.List<T> pcList)
    {
        this.parent = parent;
        this.uncrush = uncrush;
        this.pcList = pcList;
    }

    public abstract void Insert(U asset);
    public abstract int ResolveId(int id);
    public abstract int ResolveId(string id);

    protected abstract T GetRaw(int resolved);

    int LocalIdResolver<int>.ResolveId(int id) => ResolveId(id);
    int LocalIdResolver<int>.ResolveId(string id) => ResolveId(id);

    IEnumerator IEnumerable.GetEnumerator() => new TableEnumerator(this);
    IEnumerator<U> IEnumerable<U>.GetEnumerator() => new TableEnumerator(this);
}

class BaseGameTable<T, U> : AssetTable<T, U> where T : PC.Asset where U : Asset
{
    public BaseGameTable(DialogueSource parent, Uncrusher uncrush, Il2CppSystem.Collections.Generic.List<T> pcList)
        : base(parent, uncrush, pcList)
    {
        count = pcList.Count;
        DiscoAPIPlugin.Instance.Log.LogInfo($"i have {count} assets!");
    }

    public override void Insert(U asset) => throw new NotSupportedException("cannot add assets to the `disco` source");
    public override int ResolveId(int id) => id;
    public override int ResolveId(string id) => 0;

    protected override T GetRaw(int index) => pcList[index];
}

class ModTable<T, U> : AssetTable<T, U> where T : PC.Asset where U : Asset
{
    public delegate T Crusher(U asset);
    public delegate void Upserter(PC.DialogueDatabase db, T asset);

    private Crusher crush;
    private Upserter upsert;

    private int listStart;
    public int idOffset;
    public Dictionary<string, int> ids = new();

    public override int ResolveId(int id) => idOffset + id;
    public override int ResolveId(string id) => ResolveId(ids[id]);
    protected override T GetRaw(int index) => pcList[listStart + index];

    public override void Insert(U asset)
    {
        asset.sourceGuid = parent.Guid;
        ids.Add(asset.id, count);

        var pcAsset = crush(asset);
        pcAsset.id = idOffset + count;
        string assetKind = asset.Type switch
        {
            AssetType.Actor => "actors",
            AssetType.Conversation => "conversations",
            AssetType.Variable => "variables",
            _ => throw new NotSupportedException(),
        };

        string fakeArticyID = $"{parent.Guid}.${assetKind}.{asset.id}";
        pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, fakeArticyID, PC.FieldType.Text));
        parent.Manager.fakeArticyIDToAssetCache.Add(fakeArticyID, pcAsset);

        upsert(parent.Manager.pcDatabase, pcAsset);
        count += 1;
    }

    public ModTable(DialogueSource source, Il2CppSystem.Collections.Generic.List<T> assets, Uncrusher uncrush, Crusher crush, Upserter upsert)
        : base(source, uncrush, assets)
    {
        parent = source;
        this.pcList = assets;

        idOffset = assets[assets.Count - 1].id + 1;
        listStart = assets.Count;

        this.crush = crush;
        this.upsert = upsert;
    }
}


public class DialogueSource : IDialogueSource
{
    public DiscoSource Parent { get; }
    IDiscoSource IDialogueSource.Parent => this.Parent;
    public DialogueManager Manager => Parent.Manager.Dialogue;
    IDialogueManager IDialogueSource.Manager => this.Manager;
    public string Guid { get => Parent.Guid; }

    public DiscoToPixels crusher;
    public PixelsToDisco uncrusher;

    public Dictionary<LineRef, int> linksAdded = new();

    // initialization is handled in one of the below static methods.
    AssetTable<PC.Actor, Actor> actors = null!;
    AssetTable<PC.Conversation, Conversation> conversations = null!;
    AssetTable<PC.Variable, Variable> variables = null!;

    private DialogueSource(DiscoSource parent)
    {
        Parent = parent;

        crusher = new(this);
        uncrusher = new(this);
    }

    public static DialogueSource CreateDisco(DiscoSource parent)
    {
        DialogueSource src = new(parent);
        src.actors = new BaseGameTable<PC.Actor, Actor>(src, src.uncrusher.Uncrush, src.Manager.pcDatabase.actors);
        src.conversations = new BaseGameTable<PC.Conversation, Conversation>(src, src.uncrusher.Uncrush, src.Manager.pcDatabase.conversations);
        src.variables = new BaseGameTable<PC.Variable, Variable>(src, src.uncrusher.Uncrush, src.Manager.pcDatabase.variables);
        return src;
    }

    public static DialogueSource Create(DiscoSource parent)
    {
        DialogueSource src = new(parent);
        src.actors = new ModTable<PC.Actor, Actor>(
            src,
            src.Manager.pcDatabase.actors,
            src.uncrusher.Uncrush,
            src.crusher.Crush,
            (db, actor) => db.actors.Add(actor)
        );
        src.conversations = new ModTable<PC.Conversation, Conversation>(
            src,
            src.Manager.pcDatabase.conversations,
            src.uncrusher.Uncrush,
            src.crusher.Crush,
            (db, conv) => db.AddConversation(conv)
        );
        src.variables = new ModTable<PC.Variable, Variable>(
            src,
            src.Manager.pcDatabase.variables,
            src.uncrusher.Uncrush,
            src.crusher.Crush,
            (db, variable) => db.variables.Add(variable)
        );

        return src;
    }

    public int CrushedId(AssetType type, AssetId id) => type switch
    {
        AssetType.Actor => id.ResolveIn(actors),
        AssetType.Conversation => id.ResolveIn(conversations),
        AssetType.Variable => id.ResolveIn(variables),
        _ => throw new NotSupportedException("expected a valid asset type."),
    };

    public void Add(Asset asset)
    {
        switch (asset.Type)
        {
            case AssetType.Actor:
                actors.Insert((Actor)asset);
                break;
            case AssetType.Conversation:
                conversations.Insert((Conversation)asset);
                break;
            case AssetType.Variable:
                variables.Insert((Variable)asset);
                break;
        }
    }

    public void InsertLink(Link link)
    {
        if (link.from != null)
        {
            Parent.LogInfo($"inserting link from {link.from} to {link.to}");
            var from = Manager.pcDatabase.GetDialogueEntry(link.from.conversation.CrushedId(Manager.Parent), link.from.lineID);
            from.outgoingLinks.Add(crusher.Crush(link));
            linksAdded.TryGetValue(link.from, out int count);
            linksAdded[link.from] = count + 1;

            if (Manager.pcDatabase.GetDialogueEntry(link.to.conversation.CrushedId(Manager.Parent), link.to.lineID) == null)
                Parent.LogWarning($"link inserted to a non-existant destination ({link.to})");
        }
        else
        {
            Parent.LogWarning("could not insert link because its source was null");
        }
    }

    public IEnumerable<Asset> AssetsByType(AssetType type)
    {
        return type switch
        {
            AssetType.Actor => actors.Select(ass => (Asset)ass),
            AssetType.Conversation => conversations.Select(ass => (Asset)ass),
            AssetType.Variable => variables.Select(ass => (Asset)ass),
            _ => throw new Exception(),
        };
    }
}
