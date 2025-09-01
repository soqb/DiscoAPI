using DiscoAPI.Runtime.Components;
using UnityEngine;

namespace DiscoAPI.Runtime.VirtualTextures;

public static class VTSceneContents
{
	// not sure exactly what the significance is..
	public const float STRANGE_W = 0.08f;
	public const float STRANGE_H = 0.1386f;
	public const string SHADER_PATH = "FO/FO-BG";

	internal static Mesh MeshFromRectangle(Rect rect)
	{
		Mesh mesh = new();
		mesh.vertices = new Vector3[] {
			new(rect.right, rect.top, 0),
			new(rect.right, rect.bottom, 0),
			new(rect.left, rect.bottom, 0),
			new(rect.left, rect.top, 0),
		};

		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		mesh.uv = new Vector2[] { new(0, 1), new(0, 0), new(1, 0), new(1, 1) };

		mesh.normals = new Vector3[] {
			new(0, 0, -1),
			new(0, 0, -1),
			new(0, 0, -1),
			new(0, 0, -1),
		};

		return mesh;
	}

	private static Material MakeMaterial(Rect panel)
	{
		var mat = new Material(Shader.Find(SHADER_PATH));
		mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
		mat.shaderKeywords = new string[] {
			"IS_INTERIOR_SHADER",
			"_CASTSSHADOWS_ON",
			"_OLDHEIGHTMAP_ON",
			"_USESSHADOWS_ON",
		};
		mat.renderQueue = 1990;

		mat.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f, 1f));
		mat.SetColor("_EmissionColor", new Color(0f, 0, 0f, 1f));
		mat.SetColor("_VTInfoBlock", new Color(panel.width, panel.height, panel.x, panel.y));

		mat.SetTexture("_C", null);
		mat.SetTexture("_H", null);
		mat.SetTexture("_LightfieldTex", null);
		mat.SetTexture("_MainTex", null);
		mat.SetTexture("_N", null);
		mat.SetTexture("_O", null);
		mat.SetTexture("_S", null);

		mat.SetFloat("_AttenLift", 0f);
		mat.SetFloat("_BumpScale", 1f);
		mat.SetFloat("_CastsShadows", 1f);
		mat.SetFloat("_Cutoff", 0.5f);
		mat.SetFloat("_DetailNormalMapScale", 1f);
		mat.SetFloat("_DstBlend", 0f);
		mat.SetFloat("_GlossMapScale", 1f);
		mat.SetFloat("_Glossiness", 0.5f);
		mat.SetFloat("_GlossyReflections", 1f);
		mat.SetFloat("_IsInterior", 1f);
		mat.SetFloat("_Metallic", 0f);
		mat.SetFloat("_Mode", 0f);
		mat.SetFloat("_Mult", 1f);
		mat.SetFloat("_OcclusionStrength", 1f);
		mat.SetFloat("_OldHeightmap", 1f);
		mat.SetFloat("_Parallax", 0.02f);
		mat.SetFloat("_ScaleCompensation", 1f);
		mat.SetFloat("_SmoothnessTextureChannel", 0f);
		mat.SetFloat("_SpecularHighlights", 1f);
		mat.SetFloat("_SrcBlend", 1f);
		mat.SetFloat("_UVSec", 0f);
		mat.SetFloat("_UsesBakedLight", 0f);
		mat.SetFloat("_UsesShadows", 1f);
		mat.SetFloat("_ZWrite", 1f);

		return mat;
	}

	public static void InstantiateForTexture(Transform parent, ModEntity<VirtualTexture> tex)
	{
		int w = tex.WidthInPages();
		var czar = VirtualTextureComponents.Customizer.Of(tex);
		GameObject obj = new($"{tex.EntityBase.HashName}-0000");
		obj.transform.parent = parent;

		// idk why these are like this tbh:
		Vector3 aspectRatios = new Vector3(-1f, 5f / 3f, -1f);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localScale = aspectRatios * w;
		obj.transform.localRotation = Quaternion.EulerAngles(Mathf.PI * -1.5f, Mathf.PI, 0f);

		var filter = obj.AddComponent<MeshFilter>();
		filter.mesh = MeshFromRectangle(new(-0.5f, -0.5f, 1f, 1f));

		var render = obj.AddComponent<MeshRenderer>();
		render.allowOcclusionWhenDynamic = true;
		render.material = MakeMaterial(new(0f, 0f, w, w));
	}
}

public static class VTCacheBillboard
{
	public const string GONAME = "vtdebugrect";

	public static void Toggle(bool status)
	{
		if (DebugRectSpawned) FindParent().Find(GONAME).gameObject.active = status;
		if (!status) return;

		SpawnDebugRect();
	}

	private static void SpawnDebugRect()
	{
		GameObject obj = new(GONAME);
		obj.transform.parent = FindParent();

		obj.transform.localPosition = new(0f, 0f, 0f);
		obj.transform.localScale = Vector3.one * 10f;

		var filter = obj.AddComponent<MeshFilter>();
		filter.mesh = VTSceneContents.MeshFromRectangle(new(-0.5f, -0.5f, 1f, 1f));

		var render = obj.AddComponent<MeshRenderer>();
		render.allowOcclusionWhenDynamic = true;

		var material = new Material(Shader.Find("Standard"));
		render.material = material;

		DiscoHooks.OnSceneLoad += UpdateMaterial;
		DebugRectSpawned = true;
	}

	public static void UpdateMaterial()
	{
		var mat = FindParent().Find(GONAME)?.GetComponent<UnityEngine.MeshRenderer>()?.material;
		if (mat != null)
		{
			mat.mainTexture = AmplifyTextureManager.m_runtimeList?[0]?.m_physicalCache?.m_diffuseCache;
		}

	}

	private static bool DebugRectSpawned = false;

	private static Transform FindParent()
	{
		return DiscoRunner.world!.You().EntityBase.gameObject.GetComponent<FortressOccident.Character>().transform;
	}
}
