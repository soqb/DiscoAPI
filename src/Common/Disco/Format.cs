using System;
using Newtonsoft.Json;
using DiscoAPI.Common.Assets;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace DiscoAPI.Common.Format;

internal static class AssetRefFormatThreadStorage
{
	// this is all a bit mad but it works reliably (and miraculously).
	[ThreadStatic]
	internal static int recursionDepth = -1;
}

public class AssetRefFormat<T> : JsonConverter where T : Asset
{
	public static int Depth
	{
		get => AssetRefFormatThreadStorage.recursionDepth;
		set => AssetRefFormatThreadStorage.recursionDepth = value;
	}

	public static IAssetRef<T> FromParts(string source, AssetId id) => new AssetLocation<T>(source, id);

	public static IAssetRef<T> FromString(string asset)
	{
		string[] comps = asset.Split(':');
		return FromParts(comps[0], comps[1]);
	}

	private void GuardDepth(Action shallow, Action deep)
	{
		try
		{
			if (++Depth == 0) shallow();
			else deep();
		}
		finally
		{
			// just to account for the extra depth (unfortuantely) added in `CanConvert`...
			if (--Depth == 0) Depth--;
		}
	}

	public override void WriteJson(JsonWriter writer, object? ass, JsonSerializer serializer) => GuardDepth(
		() => serializer.Serialize(writer, ass),
		() =>
		{

			if (ass == null) writer.WriteNull();
			else writer.WriteValue(((IAssetRef<T>)ass).Location.ToString());
		}
	);

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		object? val = null;
		GuardDepth(
			() => val = serializer.Deserialize(reader, objectType),
			() => val = ReadRef(reader, objectType, existingValue, serializer)
		);

		return val;
	}

	public override bool CanConvert(Type objectType)
	{
		if (!objectType.IsSubclassOf(typeof(IAssetRef<T>))) return false;
		else if (Depth != 0) return true;

		Depth++;
		return false;
	}

	private object? ReadRef(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.StartObject)
		{
			reader.Read();

			string? source = null;
			AssetId? id = null;
			while (source == null || id == null)
			{
				if (reader.TokenType != JsonToken.PropertyName) throw new JsonException();
				string prop = reader.ReadAsString()!;

				if (prop == "source" && source == null)
				{
					if (reader.TokenType == JsonToken.String)
					{
						source = reader.ReadAsString();
					}
					else throw new JsonException();
				}
				else if (prop == "id" && id == null)
				{
					if (reader.TokenType == JsonToken.Integer)
					{
						id = reader.ReadAsInt32();
					}
					else if (reader.TokenType == JsonToken.String)
					{
						id = reader.ReadAsString()!;
					}
					else throw new JsonException();
				}
				else throw new JsonException();
			}

			return FromParts(source, id);
		}
		else if (reader.TokenType == JsonToken.String)
		{
			return FromString(reader.ReadAsString()!);
		}
		else throw new JsonException();
	}
}

public class NamingStrategyEnumFormat : JsonConverter
{
	private struct DbKey(Type type, string name)
	{
		Type type = type;
		string name = name;
	}
	private NamingStrategy strategy;
	private Dictionary<DbKey, string> db = new();

	public NamingStrategyEnumFormat(NamingStrategy strategy)
	{
		this.strategy = strategy;
	}

	public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(System.Enum));

	public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
	{
		string? s = reader.ReadAsString();
		if (s == null) return null;

		string? t;
		if (db.TryGetValue(new(type, s), out t)) return t;

		foreach (var k in Enum.GetNames(type))
		{
			s = strategy.GetPropertyName(k, false);
			db.Add(new(type, k), s);
			if (s == k) t = s;
		}

		return t;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value == null) writer.WriteNull();
		else writer.WriteValue(strategy.GetPropertyName(value.ToString() ?? "", false));
	}
}

public class DiscoSerializer
{
	public delegate T SourceFactory<T>(string guid) where T : IDiscoSource;

	public class AssetsFormat
	{
		public IEnumerable<Asset>? actors;
		public IEnumerable<Asset>? conversations;
		public IEnumerable<Asset>? variables;
		public IEnumerable<Asset>? skills;
	}

	class DiscoSourceFormat
	{
		public string? guid;
		public string? name;
		public string? description;
		public string[]? authors;
		public AssetsFormat? assets;
	}

	public static JsonSerializerSettings Settings()
	{
		JsonSerializerSettings opts = new()
		{
			NullValueHandling = NullValueHandling.Ignore,
		};

		opts.ContractResolver = new NamingStrategyContractResolver(new KebabCaseNamingStrategy());

		opts.Converters.Add(new AssetRefFormat<Asset>());
		opts.Converters.Add(new AssetRefFormat<Dialogue.Actor>());
		opts.Converters.Add(new AssetRefFormat<Dialogue.Conversation>());
		opts.Converters.Add(new AssetRefFormat<Dialogue.Variable>());
		opts.Converters.Add(new AssetRefFormat<Skill>());
		opts.Converters.Add(new NamingStrategyEnumFormat(new KebabCaseNamingStrategy()));

		return opts;
	}

	public static T DeserializeSource<T>(SourceFactory<T> factory, JsonReader reader) where T : IMutableAssets
	{
		var izer = JsonSerializer.Create(Settings());
		var d1 = izer.Deserialize<DiscoSourceFormat>(reader);

		if (d1 == null || d1.guid == null) throw new JsonException("expected an object with a \"guid\" field");

		var source = factory(d1.guid);
		if (d1.assets != null)
		{
			if (d1.assets.actors != null) foreach (Asset? asset in d1.assets.actors) source.Add(asset!);
			if (d1.assets.variables != null) foreach (Asset? asset in d1.assets.variables) source.Add(asset!);
			if (d1.assets.conversations != null) foreach (Asset? asset in d1.assets.conversations) source.Add(asset!);
			if (d1.assets.skills != null) foreach (Asset? asset in d1.assets.skills) source.Add(asset!);
		}

		return source;
	}

	public static void SerializeSource(JsonWriter writer, IDiscoSource src)
	{
		var izer = JsonSerializer.Create(Settings());

		AssetsFormat assets = new();
		assets.actors = src.GetAssetsForType(typeof(Dialogue.Actor));
		assets.conversations = src.GetAssetsForType(typeof(Dialogue.Conversation));
		assets.variables = src.GetAssetsForType(typeof(Dialogue.Variable));
		assets.skills = src.GetAssetsForType(typeof(Skill));

		DiscoSourceFormat dsf = new()
		{
			guid = src.Guid,
			name = src.DisplayName,
			description = src.Description,
			authors = src.Authors?.ToArray(),
			assets = assets,
		};


		izer.Serialize(writer, dsf);
	}
}
