using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
namespace DiscoAPI.Runtime;

public struct Location
{
    public string? path;

    public Location(string? path)
    {
        this.path = path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? null;
    }

    public Location Get(string subpath) => path == null ? null : Path.Combine(path, subpath);
    public Location Get(params string[] subpaths)
    {
        if (path == null) return null;

        string all = path;
        foreach (string p in subpaths) all = Path.Combine(all, p);
        return all;
    }

    public static implicit operator string?(Location l) => l.path;
    public static implicit operator Location(string? path) => new(path);

    internal static bool FullPathEq(string a, string b)
    {
        // not perfect (it could never be..) but okay:
        string Norm(string x) => x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Norm(a), Norm(b), cmp);
    }

    public static Location GetFromAssembly(Assembly assembly, ManualLogSource? log)
    {
        string? path = Path.GetDirectoryName(assembly.Location);
        if (path == null)
        {
            if (log != null) log.LogWarning("no assembly path, so no viable location");
            return null;
        }

        DirectoryInfo? target = new DirectoryInfo(path).Parent;
        DirectoryInfo plugins = new(BepInEx.Paths.PluginPath);

        while (target != null)
        {
            if (FullPathEq(target.FullName, plugins.FullName))
            {
                return path;
            }

            target = target.Parent;
        }

        if (log != null) log.LogWarning("the assembly was not found to be inside the BepInEx plugins directory, so no viable location");
        return null;
    }

}
