using UnityEngine;

namespace DiscoAPI.Runtime;

public class BundleLoader
{
    // public static void InjectAllTypes()
    // {
    //     var mapField = typeof(IL2CPP).GetField("ourImagesMap", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
    //     var map = (Dictionary<string, IntPtr>)mapField!.GetValue(null)!;

    //     foreach (var entry in map)
    //         DiscoAPIPlugin.Instance.Log.LogDebug($"{entry.Key}: {entry.Value}");

    //     void DoParam<T>()
    //     {
    //         Console.WriteLine(">>>");
    //         var ptr = IL2CPP.GetIl2CppClass("mscorlib.dll", "System.Collections.Generic", "IEnumerable`1");
    //         Console.WriteLine(ptr);
    //         var clazz2 = IL2CPP.il2cpp_class_get_type(ptr);
    //         Console.WriteLine(clazz2);
    //         var ty = Il2CppSystem.Type.internal_from_handle(clazz2);
    //         Console.WriteLine(ty);
    //         var paramptr = Il2CppClassPointerStore.GetNativeClassPointer(typeof(T));
    //         Console.WriteLine(paramptr);
    //         var paramclass = IL2CPP.il2cpp_class_get_type(paramptr);
    //         Console.WriteLine(paramclass);
    //         var paramty = Il2CppSystem.Type.internal_from_handle(paramclass);
    //         Console.WriteLine(paramty);
    //         var generic = ty.MakeGenericType(new Il2CppReferenceArray<Il2CppSystem.Type>(new[] { paramty }));
    //         Console.WriteLine(generic);
    //         Console.WriteLine(generic.TypeHandle);
    //         var hnd = generic.TypeHandle.value;
    //         Console.WriteLine(hnd);
    //         var w = IL2CPP.il2cpp_class_from_type(hnd);
    //         Console.WriteLine(w);
    //         Console.WriteLine("<<<");
    //     }

    //     DoParam<Il2CppSystem.Object>();
    //     DoParam<object>();
    //     // DoParam<object>();

    //     var ptr = IL2CPP.GetIl2CppClass("mscorlib.dll", "System.Collections", "IEnumerable");
    //     DiscoAPIPlugin.Instance.Log.LogDebug($"> big P :: {ptr}");
    //     RuntimeHelpers.RunClassConstructor(typeof(Il2CppSystem.Collections.Generic.IEnumerable<object>).TypeHandle);
    //     // RuntimeHelpers.RunClassConstructor(typeof(Il2CppSystem.Collections.Generic.IEnumerable<Node>).TypeHandle);

    //     // var image = IL2CPP.il2cpp_assembly_get_image(IL2CPP);
    //     // var name = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_image_get_name(image));
    //     // map[name!] = image;
    //     void Register<T>() where T : class
    //     {
    //         Console.WriteLine(typeof(T).FullName);
    //         ClassInjector.RegisterTypeInIl2Cpp<T>();
    //     }
    //     Register<Node>();
    //     // Register<Node.ConnectionType>();
    //     // Register<Node.TypeConstraint>();
    //     Register<Node.InputAttribute>();
    //     Register<Node.OutputAttribute>();
    //     Register<NodeGraph>();
    //     Register<NodePort>();
    //     // Register<NodePort.IO>();
    //     Register<SceneGraph>();
    //     Register<ConversationNode>();
    //     Register<LineNode>();
    //     Register<PortalNode>();
    //     Register<ConversationGraph>();
    //     Register<DiscoAsset>();
    //     Register<DiscoSourceAsset>();
    //     Register<ActorAsset>();
    //     Register<ConversationAsset>();
    // }
    public static void LoadBundle(string path)
    {
        // InjectAllTypes();
        var bundle = AssetBundle.LoadFromFile(path);

        if (bundle != null)
        {
            foreach (var assetName in bundle.AllAssetNames())
            {
                DiscoAPIPlugin.Instance.Log.LogDebug(assetName);
                // var obj = bundle.LoadAsset(assetName);
                // DiscoAPIPlugin.Instance.Log.LogDebug(obj.name);
                // DiscoAPIPlugin.Instance.Log.LogDebug(obj.GetType().FullName);
            }
        }
    }
}
