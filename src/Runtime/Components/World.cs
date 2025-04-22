using System;
using CollageMode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DiscoAPI.Runtime.Components;

public class ModWorld : ModEntity<ModWorld, global::World>
{
	public static ModEntityRegistry<ModWorld, global::World> Registry { get; } = new(w => new(w));
	public static ModWorld Of(global::World w) => Registry.EntityOf(w);

	public bool IsRunning => EntityBase.isRunning;
	public ModCharacterSheet You { get; }
	private CollageCatalogueMarshall catalogueMarshall = new();

	public Catalogue? Catalogue => catalogueMarshall.result;

	public ModWorld(World disco) : base(Registry, disco)
	{
		You = ModCharacterSheet.Of(EntityBase.you);
	}

	public void MarshallSceneLoad(Action onceDone) => catalogueMarshall.MarshallSceneLoad(onceDone);
}
public class Catalogue
{
	private CollageMode.OperationsSetCatalogue inner;

	public Catalogue(OperationsSetCatalogue inner)
	{
		this.inner = inner;
	}

	public GameObject GetCharacter(string name)
	{
		var op = inner.GetOperations<CollageMode.PlaceCharacterOperation>()[name];
		return op.Cast<CollageMode.PlaceCharacterOperation>().characterPrefab.gameObject;
	}
}

public class CollageCatalogueMarshall
{
	public Catalogue? result = null;
	private UnityAction<Scene, LoadSceneMode>? queuedLoad;

	public const string SCENE_NAME = "Scenes/CollageMode";

	private void FetchCollageCatalogue(Scene scn)
	{
		if (scn.name == null || scn.name != "CollageMode") return;

		GameObject? cm = null;
		foreach (var el in scn.GetRootGameObjects())
		{
			el.SetActiveRecursively(false);
			if (el.name != "CollageMode") continue;

			cm = el;
			break;
		}

		if (cm != null)
		{
			var szr = cm.GetComponent<CollageMode.CollageSerializer>();
			GameObject.DontDestroyOnLoad(szr);
			result = new(szr.operationsSets);
		}
		SceneManager.UnloadScene(scn);
	}

	private Action<Scene, LoadSceneMode> WithFetchingAndCleanup(Action onceDone) => (scn, mode) =>
	{
		SceneManager.remove_sceneLoaded(queuedLoad);
		queuedLoad = null;

		FetchCollageCatalogue(scn);

		onceDone();
	};

	private void LoadCollage(Action onceDone)
	{

		queuedLoad = (UnityAction<Scene, LoadSceneMode>)WithFetchingAndCleanup(onceDone);
		SceneManager.add_sceneLoaded(queuedLoad);

		SceneManager.LoadScene(SCENE_NAME, LoadSceneMode.Additive);
	}

	public void MarshallSceneLoad(Action onceDone)
	{
		var scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

		if (scn.name == "Lobby" && result == null) LoadCollage(onceDone);
		else onceDone();
	}
}
