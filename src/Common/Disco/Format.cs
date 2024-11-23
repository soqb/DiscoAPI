using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Common.Format;

public class AssetRefFormat<T> : JsonConverter<T> where T : AssetRef
{
	public delegate T RefFactory(string source, AssetId id);

	public RefFactory maker;

	public AssetRefFormat(AssetRefFormat<T>.RefFactory maker)
	{
		this.maker = maker;
	}

	public T FromString(string asset)
	{
		string[] comps = asset.Split(":");
		return maker(comps[0], (AssetIdString)comps[1]);
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			reader.Read();

			string? source = null;
			AssetId? id = null;
			while (source == null || id == null)
			{
				if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
				string prop = reader.GetString()!;

				if (prop == "source" && source == null)
				{
					if (reader.TokenType == JsonTokenType.String)
					{
						source = reader.GetString();
					}
					else throw new JsonException();
				}
				else if (prop == "id" && id == null)
				{
					if (reader.TokenType == JsonTokenType.Number)
					{
						id = (AssetIdInt)reader.GetInt32()!;
					}
					else if (reader.TokenType == JsonTokenType.String)
					{
						id = (AssetIdString)reader.GetString()!;
					}
					else throw new JsonException();
				}
				else throw new JsonException();
			}

			return maker(source, id);
		}
		else if (reader.TokenType == JsonTokenType.String)
		{
			return FromString(reader.GetString()!);
		}
		else throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.sourceGuid}:{value.id}");
	}
}

public class DiscoSerializer
{
	public delegate T SourceFactory<T>(string guid) where T : IDiscoSource;

	public class AssetsFormat
	{
		public IEnumerable? actors;
		public IEnumerable? conversations;
		public IEnumerable? variables;
	}

	class DiscoSourceFormat
	{
		public DiscoSourceMode mode;
		public string? guid;
		public string? name;
		public string? description;
		public string[]? authors;
		public AssetsFormat? assets;
	}

	public static JsonSerializerOptions Options()
	{
		JsonSerializerOptions opts = new()
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			IncludeFields = true,
		};

		opts.Converters.Add(new AssetRefFormat<ActorRef>((src, id) => throw new Exception()));
		opts.Converters.Add(new AssetRefFormat<VariableRef>((src, id) => throw new Exception()));
		opts.Converters.Add(new AssetRefFormat<ConversationRef>((src, id) => throw new Exception()));

		return opts;
	}

	public static T DeserializeSource<T>(SourceFactory<T> factory, ref Utf8JsonReader reader) where T : IDiscoSource
	{
		var d1 = JsonSerializer.Deserialize<DiscoSourceFormat>(ref reader, Options());

		if (d1 == null || d1.guid == null) throw new JsonException("expected an object with a \"guid\" field");

		var source = factory(d1.guid);
		if (d1.assets != null)
		{
			if (d1.assets.actors != null) foreach (Asset asset in d1.assets.actors) source.Dialogue.Add(asset);
			if (d1.assets.variables != null) foreach (Asset asset in d1.assets.variables) source.Dialogue.Add(asset);
			if (d1.assets.conversations != null) foreach (Asset asset in d1.assets.conversations) source.Dialogue.Add(asset);
		}

		return source;
	}

	public static void SerializeSource(Utf8JsonWriter writer, IDiscoSource src)
	{
		AssetsFormat assets = new();
		assets.actors = src.Dialogue.AssetsByType(AssetType.Actor);
		assets.conversations = src.Dialogue.AssetsByType(AssetType.Conversation);
		assets.variables = src.Dialogue.AssetsByType(AssetType.Variable);

		DiscoSourceFormat dsf = new()
		{
			mode = DiscoSourceMode.Reference,
			guid = src.Guid,
			name = src.DisplayName,
			description = src.Description,
			authors = src.Authors?.ToArray(),
			assets = assets,
		};

		JsonSerializer.Serialize<DiscoSourceFormat>(writer, dsf, Options());
	}
}
