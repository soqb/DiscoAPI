using System.Diagnostics;
using System.Text;

namespace DiscoAPI;

public class DebugUtils
{
    /// <summary>
    /// Print the combined C# and Il2CPP stacktraces.
    /// </summary>
    private static void PrintStackTrace()
    {
        StringBuilder result = new();

        var sharpStack = new StackTrace().GetFrames();
        for (int i = 1; i < sharpStack.Length; i++)
        {
            var method = sharpStack[i].GetMethod();
            result.AppendLine($"   at {method?.DeclaringType?.ReflectedType?.FullName ?? method?.DeclaringType?.FullName}.{method?.Name}");
        }

        foreach (var frame in new Il2CppSystem.Diagnostics.StackTrace().frames)
        {
            var method = frame.GetMethod();
            result.AppendLine($"   at {method?.DeclaringType?.ReflectedType?.FullName ?? method?.DeclaringType?.FullName}.{method?.Name}");
        }

        DiscoAPIPlugin.Instance.Log.LogDebug(result);
    }

}