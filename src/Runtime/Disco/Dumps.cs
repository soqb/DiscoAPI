using System.IO;
using System.Text.Json;
using DiscoAPI.Common.Format;

namespace DiscoAPI.Runtime;

public class DumpDiscoSources
{
	public static void FullDump()
	{
		string dumpDir = Path.Join(BepInEx.Paths.BepInExRootPath, "discoDumps");
		Directory.CreateDirectory(dumpDir);
		foreach (var source in DiscoRunner.manager.linearSources)
			DumpSource(source, Path.Join(dumpDir, $"{source.Guid}.disco-source.json"));
	}

	public static void DumpSource(DiscoSource source, string path)
	{
		var opts = new JsonWriterOptions() { Indented = true };
		using var stream = new MemoryStream();

		{
			using var writer = new Utf8JsonWriter(stream, opts);
			DiscoSerializer.SerializeSource(writer, source);
		}

		string dumpDir = Path.Join(BepInEx.Paths.BepInExRootPath, "discoDumps");
		Directory.CreateDirectory(dumpDir);
		File.WriteAllBytes(path, stream.ToArray());
	}
}
