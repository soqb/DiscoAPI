using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AI;

namespace DiscoAPI.Runtime;

public interface IAssetRoute<T>
{
	DiscoTask<T?> Get(string path);
}

public class EmptyAssetRoute<T> : IAssetRoute<T> where T : Il2CppObjectBase
{
	public DiscoTask<T?> Get(string path) => DiscoTask.Ready<T?>(null);
}

public class AssetBundleRoute<T> : IAssetRoute<T> where T : Il2CppObjectBase
{
	private AssetBundleCreateRequest? bundleLoad;
	private string? bundlePath;
	private string aliasPrefix;

	public AssetBundleRoute(string path, string aliasPrefix = "")
	{
		bundlePath = path;
		this.aliasPrefix = aliasPrefix ?? "";
	}

	public async DiscoTask<T?> Get(string path)
	{
		bundleLoad ??= AssetBundle.LoadFromFileAsync(bundlePath);
		await bundleLoad;
		var finalPath = aliasPrefix + path;
		var finalBundle = bundleLoad.assetBundle;
		if (finalBundle == null || !finalBundle)
		{
			throw new Exception($"failed to load bundle containing {finalPath}");
		}
		if (!finalBundle.Contains(finalPath))
		{
			throw new Exception($"failed to load {finalPath} from {finalBundle.name}: does not exist in this bundle");
		}
		var req = finalBundle.LoadAssetAsync<T>(finalPath);
		await req;
		return req.GetResult()!.TryCast<T>();
	}
}

public class SceneBundleRoute
{
	private AssetBundleCreateRequest? bundleLoad;
	private string? bundlePath;

	public SceneBundleRoute(string? bundlePath)
	{
		this.bundlePath = bundlePath;
	}

	public async DiscoTask<AssetBundle?> Get()
	{
		bundleLoad ??= AssetBundle.LoadFromFileAsync(bundlePath);
		await bundleLoad;
		return bundleLoad.assetBundle;
	}
}

public abstract class LooseFileRoute<T> : IAssetRoute<T> where T : UnityEngine.Object
{
	public string? location;
	private Dictionary<string, DiscoTask<T>> cache = new();

	protected LooseFileRoute(string? location)
	{
		this.location = location;
	}

	public abstract DiscoTask<T> Parse(byte[] bytes);

	private async DiscoTask<T> Execute(string path)
	{
		byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(location!, path));

		return await Parse(bytes);
	}

	public DiscoTask<T?> Get(string path)
	{
		DiscoTask<T> task;

		if (location == null) throw new NullReferenceException($"{GetType().Name}: loose file location was null");
		else if (cache.TryGetValue(path, out task)) return task!;

		task = Execute(path);
		cache[path] = task;
		return task!;
	}
}

public class LooseSpriteRoute : LooseFileRoute<Sprite>
{
	public LooseSpriteRoute(string? location) : base(location) { }

	public override DiscoTask<Sprite> Parse(byte[] bytes)
	{
		return DiscoTask.MainThread(() =>
		{
			Texture2D tex = new(1, 1, TextureFormat.RGB24, false);
			tex.LoadImage(bytes, false);
			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new(0.5f, 0.5f));
		});
	}
}

public interface IAssetRouter
{
	IAssetRoute<Sprite> Portraits => new EmptyAssetRoute<Sprite>();
	SceneBundleRoute SceneBundle => new SceneBundleRoute("UNDEFINED");
	IAssetRoute<NavMeshData> NavMeshes => new EmptyAssetRoute<NavMeshData>();
	IAssetRoute<AudioClip> ClipsForConversation(IAssetRef<Conversation> conversation) => new EmptyAssetRoute<AudioClip>();
}

public class EmptyAssetRouter : IAssetRouter { }

public class MemoizedAssetRouter : IAssetRouter
{
	private IAssetRouter inner;

	public MemoizedAssetRouter(IAssetRouter inner)
	{
		this.inner = inner;

		portraits = new(() => inner.Portraits);
		scenes = new(() => inner.SceneBundle);
		navmeshes = new(() => inner.NavMeshes);
	}

	private Lazy<IAssetRoute<Sprite>> portraits;
	public IAssetRoute<Sprite> Portraits => portraits.Value;

	private Lazy<SceneBundleRoute> scenes;
	public SceneBundleRoute SceneBundle => scenes.Value;

	private Lazy<IAssetRoute<NavMeshData>> navmeshes;
	public IAssetRoute<NavMeshData> NavMeshes => navmeshes.Value;

	private Dictionary<AssetLocation, IAssetRoute<AudioClip>> clipsForConversation = new();
	public IAssetRoute<AudioClip> ClipsForConversation(IAssetRef<Conversation> conversation)
	{
		var location = conversation.Location;
		if (clipsForConversation.TryGetValue(location, out var route)) return route;

		var clips = inner.ClipsForConversation(conversation);
		clipsForConversation.Add(location, clips);
		return clips;
	}
}
