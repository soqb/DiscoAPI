using System.IO;
using Newtonsoft.Json;
using DiscoAPI.Common.Format;
using System.Text;

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
		StringBuilder sb = new();

		{
			StringWriter sw = new(sb);
			var wtr = new JsonTextWriter(sw) { Indentation = 4, IndentChar = ' ' };
			DiscoSerializer.SerializeSource(wtr, source);
		}

		string dumpDir = Path.Join(BepInEx.Paths.BepInExRootPath, "discoDumps");
		Directory.CreateDirectory(dumpDir);
		File.WriteAllText(path, sb.ToString());
	}
}
