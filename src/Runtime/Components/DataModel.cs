using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes;

namespace DiscoAPI.Runtime.Components;

public sealed class ComponentKey<D, T, P>
	where T : ModEntity<T, P>
	where P : Il2CppObjectBase
	where D : notnull
{

	public ModEntityRegistry<T, P> Registry { get; }

	internal Func<T, D> factory;

	internal ComponentKey(ModEntityRegistry<T, P> registry, Func<T, D> factory)
	{
		Registry = registry;
		this.factory = factory;
	}

	public D AddTo(T t) => t.Add(this);
	public D AddTo(P p) => AddTo(Registry.EntityOf(p));
	public D? Of(T t) => t.Get<D>(this);
	public D? Of(P p) => Of(Registry.EntityOf(p));
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

	public T Map(P p) => entities.GetValue(p, p => initializer(p));
}

public class ModEntityRegistry<T, P> where T : notnull, ModEntity<T, P> where P : Il2CppObjectBase
{
	private IEntityProvider<T, P> entities;

	public ModEntityRegistry(IEntityProvider<T, P> entities)
	{
		this.entities = entities;
	}

	public ComponentKey<D, T, P> Register<D>(Func<T, D> componentFactory) where D : notnull
	{
		ComponentKey<D, T, P> key = new(this, componentFactory);
		// TBD: anything here?
		return key;
	}

	public T EntityOf(P p) => entities.Map(p);
}

public interface IComponentStore
{
	bool Contains(object key);
	object? Get(object key);
	void Add(object key, object component);
	bool Remove(object key);
	IEnumerable<object> Values { get; }
}

public class DictComponentStore : IComponentStore
{
	private Dictionary<object, object> components = new();
	public IEnumerable<object> Values => components.Values;

	public void Add(object key, object component) => components.Add(key, component);
	public bool Contains(object key) => components.ContainsKey(key);
	public object? Get(object key) => components.GetValueOrDefault(key);
	public bool Remove(object key) => components.Remove(key);
}

public abstract class ModEntity<T, P>
	where T : ModEntity<T, P>
	where P : Il2CppObjectBase
{
	protected abstract IComponentStore Components { get; }
	private ModEntityRegistry<T, P> registry;

	protected ModEntity(ModEntityRegistry<T, P> registry, P entity)
	{
		this.registry = registry;
		EntityBase = entity;
	}

	public P EntityBase { get; }

	public static implicit operator P(ModEntity<T, P> entity) => entity.EntityBase;

	public IEnumerable<object> ComponentData => Components.Values;
	public D Add<D>(ComponentKey<D, T, P> key) where D : notnull
	{
		D d = key.factory((T)(object)this);
		Components.Add(key, d);
		return d;
	}
	public D? Get<D>(ComponentKey<D, T, P> key) where D : notnull => (D?)Components.Get(key);
	public D GetOrAdd<D>(ComponentKey<D, T, P> key) where D : notnull => Components.Contains(key) ? Get(key)! : Add(key);
	public bool Remove<D>(ComponentKey<D, T, P> key) where D : notnull => Components.Remove(key);
	public bool Contains<D>(ComponentKey<D, T, P> key) where D : notnull => Components.Contains(key);
}
