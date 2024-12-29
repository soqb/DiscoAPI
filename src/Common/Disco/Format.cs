using System;
using System.Collections;
using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Format;

public class AssetRefFormat<T> : JsonConverter
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

	public override void WriteJson(JsonWriter writer, object? ass, JsonSerializer serializer)
	{
		if (ass == null) writer.WriteNull();
		else writer.WriteValue(ass?.ToString());
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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
						id = (AssetIdInt)reader.ReadAsInt32()!;
					}
					else if (reader.TokenType == JsonToken.String)
					{
						id = (AssetIdString)reader.ReadAsString()!;
					}
					else throw new JsonException();
				}
				else throw new JsonException();
			}

			return maker(source, id);
		}
		else if (reader.TokenType == JsonToken.String)
		{
			return FromString(reader.ReadAsString()!);
		}
		else throw new JsonException();
	}

	public override bool CanConvert(Type objectType) => objectType == typeof(T);
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

		opts.Converters.Add(new AssetRefFormat<AssetRef>((src, id) => throw new Exception()));
		// opts.Converters.Add(new AssetRefFormat<VariableRef>((src, id) => throw new Exception()));
		// opts.Converters.Add(new AssetRefFormat<ConversationRef>((src, id) => throw new Exception()));

		return opts;
	}

	public static T DeserializeSource<T>(SourceFactory<T> factory, JsonReader reader) where T : IDiscoSource
	{
		var izer = JsonSerializer.Create(Settings());
		var d1 = izer.Deserialize<DiscoSourceFormat>(reader);

		if (d1 == null || d1.guid == null) throw new JsonException("expected an object with a \"guid\" field");

		var source = factory(d1.guid);
		if (d1.assets != null)
		{
			if (d1.assets.actors != null) foreach (Asset? asset in d1.assets.actors) source.Assets.Add(asset!);
			if (d1.assets.variables != null) foreach (Asset? asset in d1.assets.variables) source.Assets.Add(asset!);
			if (d1.assets.conversations != null) foreach (Asset? asset in d1.assets.conversations) source.Assets.Add(asset!);
		}

		return source;
	}

	public static void SerializeSource(JsonWriter writer, IDiscoSource src)
	{
		var izer = JsonSerializer.Create(Settings());

		AssetsFormat assets = new();
		assets.actors = src.Assets.actors;
		assets.conversations = src.Assets.conversations;
		assets.variables = src.Assets.variables;

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
