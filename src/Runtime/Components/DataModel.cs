using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using DiscoAPI.Common.Assets;
using Il2CppInterop.Runtime.InteropTypes;

namespace DiscoAPI.Runtime.Components;

public sealed class ComponentKey<D, T, P>
	where T : ModEntity<T, P>
	where P : Il2CppObjectBase
	where D : notnull
{
	public AssetLocation Location { get; }
	public ModEntityRegistry<T, P> Registry { get; }

	internal ComponentKey(ModEntityRegistry<T, P> registry, AssetLocation location)
	{
		Registry = registry;
		Location = location;
	}

	public D? Of(T t) => t.Get<D>(this);
	public D? Of(P p) => Of(Registry.EntityOf(p));
	public bool TryOf(T t, [NotNullWhen(true)] out D? value) => t.TryGet<D>(this, out value);
	public bool TryOf(P p, [NotNullWhen(true)] out D? value) => TryOf(Registry.EntityOf(p), out value);
}

public interface IEntityProvider<T, P> where T : ModEntity<T, P> where P : Il2CppObjectBase
{
	T Map(P p);
}

public class CWTEntityMap<T, P> : IEntityProvider<T, P> where T : ModEntity<T, P> where P : Il2CppObjectBase
{
	// NB: A CWT is like a dictionary which doesn't store or keepalive keys or values.
	//     Any given P always maps to the same T.
	private ConditionalWeakTable<P, T> entities = new();
	private Func<P, T> initializer;

	public CWTEntityMap(Func<P, T> initializer)
	{
		this.initializer = initializer;
	}

	public T Map(P p)
	{
		if (entities.TryGetValue(p, out T? t)) return t;
		else return entities.GetValue(p, _ => initializer(p));
	}
}

public class PersistentEntityMap<T, P> : IEntityProvider<T, P> where T : ModEntity<T, P> where P : UnityEngine.Object
{
	private ConcurrentDictionary<P, T> entities = new();
	private Func<P, T> initializer;

	public PersistentEntityMap(Func<P, T> initializer)
	{
		this.initializer = initializer;
	}

	public T Map(P p)
	{
		T? t;
		if (entities.TryGetValue(p, out t)) return t;
		t = initializer(p);
		entities.TryAdd(p, t);
		return t;
	}

}

public class ModEntityRegistry<T, P> where T : notnull, ModEntity<T, P> where P : Il2CppObjectBase
{
	internal IEntityProvider<T, P> entities;

	public ModEntityRegistry(IEntityProvider<T, P> entities)
	{
		this.entities = entities;
	}

	public ComponentKey<D, T, P> Register<D>(string source, string id) where D : notnull
	{
		ComponentKey<D, T, P> key = new(this, new(new AssetType(typeof(ComponentKey<D, T, P>)), source, id));
		// TBD: anything here?
		return key;
	}

	public T EntityOf(P p) => entities.Map(p);
}

public interface IComponentStore
{
	bool Contains(object key);
	object? Get(object key);
	bool TryGet(object key, [NotNullWhen(true)] out object? component);
	void Add(object key, object component);
	bool Remove(object key);
	IEnumerable<object> Values { get; }
}

public class DictComponentStore : IComponentStore
{
	private Dictionary<object, object> components = new();
	public IEnumerable<object> Values => components.Values;

	public bool Contains(object key) => components.ContainsKey(key);
	public object? Get(object key) => components.GetValueOrDefault(key);
	public bool TryGet(object key, [NotNullWhen(true)] out object? component) => components.TryGetValue(key, out component);
	public void Add(object key, object component) => components.Add(key, component);
	public bool Remove(object key) => components.Remove(key);
}

public abstract class ModEntity<T, P>
	where T : ModEntity<T, P>
	where P : Il2CppObjectBase
{
	protected abstract IComponentStore Components { get; }
	protected ModEntityRegistry<T, P> registry;

	protected ModEntity(ModEntityRegistry<T, P> registry, P entity)
	{
		this.registry = registry;
		EntityBase = entity;

		ComponentLifecycleTracker.EntitySpawned(this);
	}

	~ModEntity()
	{
		ComponentLifecycleTracker.EntityDespawned(this);
	}

	public virtual P EntityBase { get; protected set; }

	public static implicit operator P(ModEntity<T, P> entity) => entity.EntityBase;

	public IEnumerable<object> ComponentData => Components.Values;

	public D? Get<D>(ComponentKey<D, T, P> key) where D : notnull => (D?)Components.Get(key);
	public bool Contains<D>(ComponentKey<D, T, P> key) where D : notnull => Components.Contains(key);
	public bool Remove<D>(ComponentKey<D, T, P> key) where D : notnull
	{
		ComponentLifecycleTracker.ComponentRemoved(this, key);
		return Components.Remove(key);
	}

	public void Add<D>(ComponentKey<D, T, P> key, D component) where D : notnull
	{
		ComponentLifecycleTracker.ComponentAdded(this, component);
		Components.Add(key, component);
	}
	public bool TryGet<D>(ComponentKey<D, T, P> key, [NotNullWhen(true)] out D? component) where D : notnull
	{
		bool success = Components.TryGet(key, out object? a);
		component = (D?)a;
		return success;
	}
	public D GetOrCreate<D>(ComponentKey<D, T, P> key, Func<T, D> factory) where D : notnull
	{
		if (Components.TryGet(key, out object? component)) return (D)component;
		D d = factory((T)this);
		Add(key, d);
		return d;
	}
}

public static class ComponentLifecycleTracker
{
	private static ManualLogSource log = Logger.CreateLogSource("DiscoAPI (CLT)");

	private static string Entity<T, P>(ModEntity<T, P> entity)
		where T : ModEntity<T, P>
		where P : Il2CppObjectBase
	{
		return $"entity {entity} for object {entity.EntityBase}";
	}

	public static void EntitySpawned<T, P>(ModEntity<T, P> entity)
		where T : ModEntity<T, P>
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"SPAWN {Entity(entity)}");
	}

	public static void EntityDespawned<T, P>(ModEntity<T, P> entity)
		where T : ModEntity<T, P>
		where P : Il2CppObjectBase
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"DESPAWN {Entity(entity)}");
	}

	public static void ComponentAdded<D, T, P>(ModEntity<T, P> entity, D d)
		where T : ModEntity<T, P>
		where P : Il2CppObjectBase
		where D : notnull
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"ADD component {d} ON {Entity(entity)}");
	}

	public static void ComponentRemoved<D, T, P>(ModEntity<T, P> entity, ComponentKey<D, T, P> key)
		where T : ModEntity<T, P>
		where P : Il2CppObjectBase
		where D : notnull
	{
		if (!DiscoAPISettings.ComponentLifecycleTracking) return;
		log.LogDebug($"REMOVE component {entity.Get(key)} ON {Entity(entity)}");
	}
}
