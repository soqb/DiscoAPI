using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.SaveSystem;
using Il2CppInterop.Runtime.InteropTypes;
using Newtonsoft.Json.Linq;

namespace DiscoAPI.Runtime.Components;

public abstract class ComponentKey<P>
	where P : Il2CppObjectBase
{
	public Type DataType => Location.type.type;
	public AssetLocation Location { get; }
	public ModEntityRegistry<P> Registry { get; }

	public override string ToString()
	{
		return Location.ToString();
	}

	internal ComponentKey(ModEntityRegistry<P> registry, AssetLocation location)
	{
		Registry = registry;
		Location = location;
	}
}

public sealed class ComponentKey<D, P> : ComponentKey<P>
	where P : Il2CppObjectBase
	where D : notnull
{
	public ComponentKey(ModEntityRegistry<P> registry, AssetLocation location) : base(registry, location) { }

	public D? Of(ModEntity<P> t) => t.Get<D>(this);
	public D? Of(P p) => Of(Registry.EntityOf(p));
	public bool TryOf(ModEntity<P> t, [NotNullWhen(true)] out D? value) => t.TryGet<D>(this, out value);
	public bool TryOf(P p, [NotNullWhen(true)] out D? value) => TryOf(Registry.EntityOf(p), out value);
}

public interface IEntityProvider<P>
	where P : Il2CppObjectBase
{
	ModEntity<P> Map(P p);
}

public class CWTEntityMap<P> : IEntityProvider<P>
	where P : Il2CppObjectBase
{
	// NB: A CWT is like a dictionary which doesn't store or keepalive keys or values.
	//     Any given P always maps to the same T.
	private ConditionalWeakTable<P, ModEntity<P>> entities = new();
	private Func<P, ModEntity<P>> initializer;

	public CWTEntityMap(Func<P, ModEntity<P>> initializer)
	{
		this.initializer = initializer;
	}

	public ModEntity<P> Map(P p)
	{
		if (entities.TryGetValue(p, out ModEntity<P>? t)) return t;
		else return entities.GetValue(p, _ => initializer(p));
	}
}

public class PersistentEntityMap<P> : IEntityProvider<P>
	where P : UnityEngine.Object
{
	private ConcurrentDictionary<P, ModEntity<P>> entities = new();
	private Func<P, ModEntity<P>> initializer;

	public PersistentEntityMap(Func<P, ModEntity<P>> initializer)
	{
		this.initializer = initializer;
	}

	public ModEntity<P> Map(P p)
	{
		ModEntity<P>? t;
		if (entities.TryGetValue(p, out t)) return t;
		t = initializer(p);
		entities.TryAdd(p, t);
		return t;
	}

}

public class ModEntityRegistry<P> where P : Il2CppObjectBase
{
	private Dictionary<(string, string), ComponentKey<P>> registered = new();
	internal IEntityProvider<P> entities;

	public ModEntityRegistry(IEntityProvider<P> entities)
	{
		this.entities = entities;
	}

	public ComponentKey<D, P> Register<D>(string source, string id) where D : notnull
	{
		AssetLocation asset = new(new AssetType(typeof(D)), source, id);
		ComponentKey<D, P> key = new(this, asset);

		if (!registered.TryAdd((source, id), key)) throw new Exception($"duplicate component key {asset}");
		return key;
	}

	public IEnumerable<ComponentKey<P>> Keys => registered.Values;

	public ModEntity<P> EntityOf(P p) => entities.Map(p);
}

public interface IComponentStore
{
	bool Contains(AssetLocation key);
	object? Get(AssetLocation key);
	bool TryGet(AssetLocation key, [NotNullWhen(true)] out object? component);
	void Add(AssetLocation key, object component);
	bool Remove(AssetLocation key);
	IEnumerable<(AssetLocation, object)> Entries { get; }
}

public class DictComponentStore : IComponentStore
{
	private Dictionary<AssetLocation, object> components = new();
	public IEnumerable<(AssetLocation, object)> Entries => components.Select(kv => (kv.Key, kv.Value));

	public bool Contains(AssetLocation key) => components.ContainsKey(key);
	public object? Get(AssetLocation key) => components.GetValueOrDefault(key);
	public bool TryGet(AssetLocation key, [NotNullWhen(true)] out object? component) => components.TryGetValue(key, out component);
	public void Add(AssetLocation key, object component) => components[key] = component;
	public bool Remove(AssetLocation key) => components.Remove(key);
}

public sealed class ModEntity<P> : ISaveSerializable<ModEntity<P>>
	where P : Il2CppObjectBase
{
	private IComponentStore Components { get; } = new DictComponentStore();
	private ModEntityRegistry<P> registry;

	internal ModEntity(ModEntityRegistry<P> registry, P entity)
	{
		this.registry = registry;
		EntityBase = entity;

		ComponentLifecycleTracker.EntitySpawned(this);
	}

	~ModEntity()
	{
		ComponentLifecycleTracker.EntityDespawned(this);
	}

	public P EntityBase { get; private set; }

	public static implicit operator P(ModEntity<P> entity) => entity.EntityBase;

	public IEnumerable<(AssetLocation, object)> ComponentData => Components.Entries;

	public object? Get(ComponentKey<P> key) => Components.Get(key.Location);
	public D? Get<D>(ComponentKey<D, P> key) where D : notnull => (D?)Components.Get(key.Location);
	public bool Contains<D>(ComponentKey<D, P> key) where D : notnull => Components.Contains(key.Location);
	public bool Remove<D>(ComponentKey<D, P> key) where D : notnull
	{
		ComponentLifecycleTracker.ComponentRemoved(this, key);
		return Components.Remove(key.Location);
	}

	public void AddUntyped(ComponentKey<P> key, object component)
	{
		ComponentLifecycleTracker.ComponentAdded(this, component);
		Components.Add(key.Location, component);
	}

	public void Add<D>(ComponentKey<D, P> key, D component) where D : notnull
	{
		ComponentLifecycleTracker.ComponentAdded(this, component);
		Components.Add(key.Location, component);
	}
	public bool TryGet<D>(ComponentKey<D, P> key, [NotNullWhen(true)] out D? component) where D : notnull
	{
		bool success = Components.TryGet(key.Location, out object? a);
		component = (D?)a;
		return success;
	}
	public D GetOrCreate<D>(ComponentKey<D, P> key, Func<ModEntity<P>, D> factory) where D : notnull
	{
		if (Components.TryGet(key.Location, out object? component)) return (D)component;
		D d = factory((ModEntity<P>)this);
		Add(key, d);
		return d;
	}

	public JToken Serialize()
	{
		JObject obj = new();

		foreach ((AssetLocation loc, object datum) in ComponentData)
			if (datum is ISaveSerializable ser) obj.Add(loc.ToString(), ser.Serialize());
		return obj;
	}

	public bool TryDeserialize(JToken? token)
	{
		if (token == null || !(token is JObject obj)) return false;

		foreach (var key in registry.Keys)
		{
			if (!(obj.GetValue(key.ToString()) is JToken token2)) continue;
			object datum = Activator.CreateInstance(key.DataType)!;
			if (ModSaveSystem.TryDeserializeUntyped(key.DataType, token2, this, datum)) AddUntyped(key, datum);
		}

		return true;
	}
}

public static class ComponentLifecycleTracker
{
	private static ManualLogSource log = Logger.CreateLogSource("DiscoAPI (CLT)");

	private static string Entity<P>(ModEntity<P> entity)
		where P : Il2CppObjectBase
	{
		return $"entity {entity} for object {entity.EntityBase}";
	}

	public static void EntitySpawned<P>(ModEntity<P> entity)
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"SPAWN {Entity(entity)}");
	}

	public static void EntityDespawned<P>(ModEntity<P> entity)
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"DESPAWN {Entity(entity)}");
	}

	public static void ComponentAdded<P>(ModEntity<P> entity, object d)
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"ADD component {d} ON {Entity(entity)}");
	}

	public static void ComponentRemoved<P>(ModEntity<P> entity, ComponentKey<P> key)
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"REMOVE component {entity.Get(key)} ON {Entity(entity)}");
	}
}
