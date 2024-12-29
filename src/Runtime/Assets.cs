using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Dialogue;
using PC = PixelCrushers.DialogueSystem;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Assets;

class PCArena<T, U> : IAssetArena<U> where U : Asset where T : PC.Asset
{
	private DialogueManager dialogue;
	private Il2CppSystem.Collections.Generic.List<T> pcList;

	private PixelsToDisco uncrusher => dialogue.Parent.Assets.uncrusher;
	private DiscoToPixels crusher => dialogue.Parent.Assets.crusher;

	public PCArena(DialogueManager dialogue, Il2CppSystem.Collections.Generic.List<T> pcList)
	{
		this.dialogue = dialogue;
		this.pcList = pcList;
	}

	public void Insert(U asset)
	{
		var src = dialogue.Parent[asset.sourceGuid!];

		var pcAsset = crusher.Crush(src, asset);
		pcAsset.id = MaxId;
		string assetKind = asset.Type switch
		{
			AssetType.Actor => "actors",
			AssetType.Conversation => "conversations",
			AssetType.Variable => "variables",
			_ => throw new NotSupportedException(),
		};

		string fakeArticyID = $"{asset.sourceGuid}.${assetKind}.{asset.id}";
		pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, fakeArticyID, PC.FieldType.Text));
		dialogue.fakeArticyIDToAssetCache.Add(fakeArticyID, pcAsset);

		if (asset.Type == AssetType.Conversation)
		{
			dialogue.pcDatabase.AddConversation((PC.Conversation)pcAsset);
		}
		else
		{
			pcList.Add((T)pcAsset);
		}
	}

	public U this[int id] => (U)uncrusher.Uncrush(pcList[id]);

	public int Count => pcList.Count;
	public int MaxId => pcList[Count - 1].id + 1;
}

class EnumArena<T, U> : IAssetArena<U> where T : struct, System.Enum where U : Asset
{
	public int baseCount;
	public int baseMax;
	public List<U> extras = new();
	public Recover recover;
	public Filter filter;

	public delegate U Recover(T value);
	public delegate bool Filter(T value);

	public EnumArena(Recover recover, Filter filter)
	{
		this.recover = recover;
		this.filter = filter;

		baseCount = System.Enum.GetNames(typeof(T)).Where(n => filter(Enum.Parse<T>(n))).Count();

		foreach (T t in (T[])(System.Enum.GetValues(typeof(T))))
		{
			int u = System.Convert.ToInt32(t);
			if (u > baseMax) baseMax = u;
		}
	}

	public int MaxId => baseMax + extras.Count;
	public int Count => baseCount + extras.Count;

	public U this[int id]
	{
		get
		{
			if (id > baseMax) return extras[id - baseMax - 1];
			else if (filter(Enum.Parse<T>(id.ToString()))) return DoRecover(id);
			else return null!;
		}
	}
	public void Insert(U asset) => extras.Add(asset);

	private U DoRecover(int id)
	{
		U asset = recover.Invoke(Enum.Parse<T>(id.ToString()));
		asset.sourceGuid = "disco";
		return asset;
	}
}


abstract class AssetTable<T, U> : IAssetTable<U>, LocalIdResolver<int> where U : Asset
{
	class TableEnumerator : IEnumerator<U>
	{
		public int idx;
		public AssetTable<T, U> table;

		public TableEnumerator(AssetTable<T, U> table)
		{
			this.table = table;
			Reset();
		}

		public U Current => table[idx];
		object IEnumerator.Current => this.Current;

		public bool MoveNext()
		{
			return ++idx < table.count;
		}
		public void Reset() => idx = -1;
		public void Dispose() { }
	}

	public IAssetArena<U> arena;
	public int count;

	protected AssetTable(IAssetArena<U> arena)
	{
		this.arena = arena;
	}

	public abstract void Insert(U asset);
	public abstract int ResolveId(int id);
	public abstract int ResolveId(string id);
	public abstract int idOffset { get; }

	public U this[int resolved] => arena[resolved + idOffset];

	int LocalIdResolver<int>.ResolveId(int id) => ResolveId(id);
	int LocalIdResolver<int>.ResolveId(string id) => ResolveId(id);

	IEnumerator IEnumerable.GetEnumerator() => new TableEnumerator(this);
	IEnumerator<U> IEnumerable<U>.GetEnumerator() => new TableEnumerator(this);
}

class VanillaTable<T, U> : AssetTable<T, U> where U : Asset
{
	public VanillaTable(IAssetArena<U> arena)
		: base(arena)
	{
		count = arena.Count;
		DiscoAPIPlugin.Instance.Log.LogInfo($"i have {count} assets!");
	}

	public override void Insert(U asset) => throw new NotSupportedException("cannot add assets to the `disco` source");
	public override int ResolveId(int id) => id;
	public override int ResolveId(string id) => 0;
	public override int idOffset => 0;
}

class ModTable<T, U> : AssetTable<T, U> where U : Asset
{
	private int listStart;
	private IAssetSource parent;
	public override int idOffset { get; }
	public Dictionary<string, int> ids = new();

	public override int ResolveId(int id) => idOffset + id;
	public override int ResolveId(string id) => ResolveId(ids[id]);

	public override void Insert(U asset)
	{
		asset.sourceGuid = parent.Guid;
		ids.Add(asset.id, count);

		arena.Insert(asset);

		count += 1;
	}

	public ModTable(IAssetSource source, IAssetArena<U> arena) : base(arena)
	{
		parent = source;

		idOffset = arena.MaxId;
		listStart = arena.Count;
	}
}


interface AssetTableFab
{
	AssetTable<T, U> Make<T, U>(IAssetSource parent, IAssetArena<U> arena) where U : Asset;
}

class VanillaTableFab : AssetTableFab
{
	public AssetTable<T, U> Make<T, U>(
		IAssetSource parent,
		IAssetArena<U> arena
	) where U : Asset => new VanillaTable<T, U>(arena);
}

class ModTableFab : AssetTableFab
{
	public AssetTable<T, U> Make<T, U>(
		IAssetSource parent,
		IAssetArena<U> arena
	) where U : Asset => new ModTable<T, U>(parent, arena);
}

public class AssetSource : IAssetSource
{
	private AssetSource(DiscoSource parent, AssetTableFab tableFab)
	{
		Parent = parent;
		this.tableFab = tableFab;

		skills = tableFab.Make<Sunshine.Metric.SkillType, Skill>(this, Manager.skills);
	}

	public static AssetSource CreateDisco(DiscoSource parent) => new(parent, new VanillaTableFab());
	public static AssetSource Create(DiscoSource parent) => new(parent, new ModTableFab());

	public DiscoSource Parent { get; }
	IDiscoSource IAssetSource.Parent => Parent;

	private AssetTableFab tableFab;

	public AssetManager Manager => Parent.Manager.Assets;

	public IAssetTable<Skill> skills { get; }
	private IAssetTable<Actor>? realActors = null;
	private IAssetTable<Conversation>? realConversations = null;
	private IAssetTable<Variable>? realVariables = null;

	public void OnDialogueBundleLoaded()
	{
		realActors = tableFab.Make<PC.Actor, Actor>(this, Manager.actors);
		realConversations = tableFab.Make<PC.Conversation, Conversation>(this, Manager.conversations);
		realVariables = tableFab.Make<PC.Variable, Variable>(this, Manager.variables);
	}


	public IAssetTable<Actor> actors => Manager.Parent.BundleGuard(realActors);
	public IAssetTable<Conversation> conversations => Manager.Parent.BundleGuard(realConversations);
	public IAssetTable<Variable> variables => Manager.Parent.BundleGuard(realVariables);

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
			case AssetType.Skill:
				skills.Insert((Skill)asset);
				break;
		}
	}

	public int ResolveId(AssetType type, AssetId id) => type switch
	{
		AssetType.Actor => id.ResolveIn(actors),
		AssetType.Conversation => id.ResolveIn(conversations),
		AssetType.Variable => id.ResolveIn(variables),
		AssetType.Skill => id.ResolveIn(skills),
		_ => throw new NotSupportedException("expected a valid asset type."),
	};

	public IEnumerable<Asset> AssetsByType(AssetType type)
	{
		return type switch
		{
			AssetType.Actor => actors.Select(ass => (Asset)ass),
			AssetType.Conversation => conversations.Select(ass => (Asset)ass),
			AssetType.Variable => variables.Select(ass => (Asset)ass),
			AssetType.Skill => skills.Select(ass => (Asset)ass),
			_ => throw new Exception(),
		};
	}
}

public class AssetManager : IAssetManager
{
	public DiscoManager Parent { get; }
	IDiscoManager IAssetManager.Parent => Parent;

	public AssetManager(DiscoManager parent)
	{
		Parent = parent;

		skills = new EnumArena<SM.SkillType, Skill>(RecoverSkill, Skill.IsReal);
	}

	private Skill RecoverSkill(SM.SkillType type)
	{
		string name = SM.Skill.GetActorSkillName(type);
		return new Skill(
			type.ToString(),
			name,
			new(AssetType.Actor, "disco", name),
			Skill.AbilityFromSunshine(SM.Skill.GetAbility(type))
		);
	}

	public int ResolveId(AssetRef ass) => Parent[ass.sourceGuid].Assets.ResolveId(ass.type, ass.id);
	public Asset Resolve(AssetRef ass) => ass.type switch
	{
		AssetType.Actor => actors[ResolveId(ass)],
		AssetType.Conversation => conversations[ResolveId(ass)],
		AssetType.Variable => variables[ResolveId(ass)],
		AssetType.Skill => skills[ResolveId(ass)],
		_ => throw new NotSupportedException("expected a valid asset type."),
	};

	public PixelsToDisco uncrusher = new();
	public DiscoToPixels crusher = new();

	public IAssetArena<Skill> skills { get; }

	private IAssetArena<Actor>? realActors = null;
	private IAssetArena<Conversation>? realConversations = null;
	private IAssetArena<Variable>? realVariables = null;

	public void OnDialogueBundleLoaded()
	{
		var db = Parent.Dialogue.pcDatabase;
		realActors = new PCArena<PC.Actor, Actor>(Parent.Dialogue, db.actors);
		realConversations = new PCArena<PC.Conversation, Conversation>(Parent.Dialogue, db.conversations);
		realVariables = new PCArena<PC.Variable, Variable>(Parent.Dialogue, db.variables);
	}


	public IAssetArena<Actor> actors => Parent.BundleGuard(realActors);
	public IAssetArena<Conversation> conversations => Parent.BundleGuard(realConversations);
	public IAssetArena<Variable> variables => Parent.BundleGuard(realVariables);
}
