using System;
using System.Collections.Generic;
using CollageMode;
using DiscoAPI.Common.Assets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime;

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

			if (el.name == "CollageMode")
			{
				cm = el;
				break;
			}
		}

		if (cm != null)
		{
			var szr = cm.GetComponent<CollageMode.CollageSerializer>();
			GameObject.DontDestroyOnLoad(szr);
			result = new(szr.operationsSets);
		}
		SceneManager.UnloadScene(scn);
	}

	private Action<Scene, LoadSceneMode> WithFetchingAndCleanup(Action onceDone)
	=> (scn, mode) =>
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
		if (scn.name == "Lobby" && result == null)
		{
			LoadCollage(onceDone);
		}
		else
		{
			onceDone();
		}
	}
}

public class ModWorld
{
	public global::World discoWorld;

	public bool IsRunning => discoWorld.isRunning;
	public CharacterSheet you;
	private CollageCatalogueMarshall catalogueMarshall = new();

	public Catalogue? Catalogue => catalogueMarshall.result;

	public ModWorld(World discoWorld)
	{
		this.discoWorld = discoWorld;
		you = new(discoWorld.you);
	}

	public void MarshallSceneLoad(Action onceDone) => catalogueMarshall.MarshallSceneLoad(onceDone);
}

public class CharacterSheet
{
	public static Dictionary<SM.CharacterSheet, CharacterSheet> reverseIndex = new();
	public static CharacterSheet GetForSM(SM.CharacterSheet sheet)
	{
		if (!reverseIndex.ContainsKey(sheet)) reverseIndex.Add(sheet, new(sheet));
		return reverseIndex[sheet];
	}

	public SM.CharacterSheet sm;
	public Dictionary<AssetLocation, SM.Skill> skillMap = new();

	public CharacterSheet(SM.CharacterSheet sm)
	{
		this.sm = sm;
	}

	public void EnsureSkillsInstalled()
	{
		var skills = DiscoRunner.manager.Assets.skills;

		for (int i = 0; i <= skills.MaxId; i++)
		{
			var sk = skills[i];
			if (sk == null || skillMap.ContainsKey(sk.Location)) continue;
			var skill = i <= Skill.VANILLA_MAX ? sm.GetSkill((SM.SkillType)i) : new SM.Skill((SM.SkillType)i, sm);
			skillMap.Add(skills[i].Location, skill);
		}
	}
}
