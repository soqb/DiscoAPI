using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes;

namespace DiscoAPI.Runtime.Components;

public class ComponentKey<D, T, P>
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

public class ModEntityRegistry<T, P> where T : notnull, ModEntity<T, P> where P : Il2CppObjectBase
{
	// NB: A CWT is like a dictionary which doesn't store or keepalive keys or values.
	//     Any given P always maps to the same T.
	private ConditionalWeakTable<P, T> entities = new();
	private Func<P, T> entityFactory;

	public ModEntityRegistry(Func<P, T> factory)
	{
		entityFactory = factory;
	}

	public ComponentKey<D, T, P> Register<D>(Func<T, D> componentFactory) where D : notnull
	{
		ComponentKey<D, T, P> key = new(this, componentFactory);
		// TBD: anything here?
		return key;
	}

	public T EntityOf(P p) => entities.GetValue(p, (p) => entityFactory(p));
}

public abstract class ModEntity<T, P>
	where T : ModEntity<T, P>
	where P : Il2CppObjectBase
{
	private Dictionary<object, object> components = new();
	private ModEntityRegistry<T, P> registry;

	protected ModEntity(ModEntityRegistry<T, P> registry, P entity)
	{
		this.registry = registry;
		EntityBase = entity;
	}


	public P EntityBase { get; }

	public IEnumerable<object> ComponentData => components.Values;
	public D Add<D>(ComponentKey<D, T, P> key) where D : notnull
	{
		DiscoRunner.Log.LogInfo($"adding {typeof(D)} to {typeof(T)}");
		D d = key.factory((T)(object)this);
		components.Add(key, d);
		return d;
	}
	public D? Get<D>(ComponentKey<D, T, P> key) where D : notnull => (D?)components.GetValueOrDefault(key);
	public D GetOrAdd<D>(ComponentKey<D, T, P> key) where D : notnull => components.TryGetValue(key, out var d) ? (D)d : Add(key);
	public bool Remove<D>(ComponentKey<D, T, P> key) where D : notnull => components.Remove(key);
	public bool Contains<D>(ComponentKey<D, T, P> key) where D : notnull => components.ContainsKey(key);
}
