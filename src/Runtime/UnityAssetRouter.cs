using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscoAPI.Runtime;

public interface IAssetRoute<T>
{
	AsyncOperationHandle<T?> Get(string path);
}

public class EmptyAssetRoute<T> : IAssetRoute<T> where T : Il2CppObjectBase
{
	public AsyncOperationHandle<T?> Get(string path) => Addressables.ResourceManager.CreateCompletedOperation<T?>(null, null);
}

public class AssetBundleRoute<T> : IAssetRoute<T> where T : Il2CppObjectBase
{
	private AsyncOperationHandle<AssetBundle?> bundle;
	private string aliasPrefix;

	private static AsyncOperationHandle<AssetBundle?> HandleFromCreateRequest(AssetBundleCreateRequest req)
	{
		return AssetUtils.SpoofHandle<AssetBundle>(complete =>
		{
			req.add_completed((Action<AsyncOperation>)(_ => complete(req.assetBundle, null)));
		});
	}

	public AssetBundleRoute(string? path, string? aliasPrefix)
		: this(AssetBundle.LoadFromFileAsync(path), aliasPrefix) { }

	public AssetBundleRoute(AssetBundleCreateRequest req, string? aliasPrefix)
		: this(HandleFromCreateRequest(req), aliasPrefix) { }

	public AssetBundleRoute(AsyncOperationHandle<AssetBundle?> handle, string? aliasPrefix)
	{
		bundle = handle;

		// we don't want to have to unnecessarily unload & reload the bundle so we're just going to leak it.
		bundle.InternalOp.IncrementReferenceCount();

		this.aliasPrefix = aliasPrefix ?? "";
	}

	private void Execute(string path, AssetUtils.Complete<T> complete)
	{
		var req = bundle.Result!.LoadAssetAsync<T>(aliasPrefix + path);
		req.add_completed((Action<AsyncOperation>)(_ => complete(req.GetResult().TryCast<T?>(), null)));
	}

	public AsyncOperationHandle<T?> Get(string path)
		=> AssetUtils.SpoofHandle<T>(complete => Execute(path, complete), bundle);
}

public abstract class LooseFileRoute<T> : IAssetRoute<T> where T : UnityEngine.Object
{
	public string? location;
	private Dictionary<string, AsyncOperationHandle<T?>> cache = new();

	protected LooseFileRoute(string? location)
	{
		this.location = location;
	}

	public abstract T? Parse(byte[] bytes);

	private void Execute(string path, AssetUtils.Complete<T> complete)
	{
		File.ReadAllBytesAsync(Path.Combine(location!, path)).ContinueWith((task) =>
		{
			MainThreadExecutor.Queue(() =>
			{
				T? val = null;
				string? error = null;

				if (task.Status == TaskStatus.RanToCompletion) val = Parse(task.Result);
				else error = task.Exception?.Message ?? $"task completed with status {task.Status}";

				complete(val, error);
			});
		});
	}

	public AsyncOperationHandle<T?> Get(string path)
	{
		AsyncOperationHandle<T?>? handle;

		if (location == null) throw new NullReferenceException($"{GetType().Name}: loose file location was null");
		else if (cache.TryGetValue(path, out handle) && handle.IsValid()) return handle;

		handle = AssetUtils.SpoofHandle<T>(complete => Execute(path, complete));
		cache[path] = handle;
		return handle;
	}
}

public class LooseSpriteRoute : LooseFileRoute<Sprite>
{
	public LooseSpriteRoute(string? location) : base(location) { }

	public override Sprite? Parse(byte[] bytes)
	{
		Texture2D tex = new(1, 1, TextureFormat.RGB24, false);
		ImageConversion.LoadImage(tex, bytes, false);
		return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new(0.5f, 0.5f));
	}
}

public interface IAssetRouter
{
	IAssetRoute<Sprite> Portraits => new EmptyAssetRoute<Sprite>();
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
	}

	private Lazy<IAssetRoute<Sprite>> portraits;
	public IAssetRoute<Sprite> Portraits => portraits.Value;

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
