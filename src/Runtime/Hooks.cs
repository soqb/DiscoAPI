using System;
using System.Collections.Generic;

namespace DiscoAPI.Runtime;

public record DiscoHook(string id)
{
	private readonly List<Action> actions = new();

	public void Add(Action action) => actions.Add(action);
	public void Remove(Action action) => actions.Remove(action);

	public void Invoke()
	{
		foreach (var action in actions) Guard(action);
	}

	public void Guard(Action hook)
	{
		try
		{
			hook();
		}
		catch (Exception e)
		{
			DiscoRunner.Log.LogError($"error in hook '{id}':");
			DiscoRunner.Log.LogError(e);
		}
	}
}

public static class DiscoHooks
{
	public static event Action OnUpdate
	{
		add => DiscoRunner.update.Add(value);
		remove => DiscoRunner.update.Remove(value);
	}
	public static event Action OnLoad
	{
		add => DiscoRunner.load.Add(value);
		remove => DiscoRunner.load.Remove(value);
	}
	public static event Action OnSceneLoad
	{
		add => DiscoRunner.sceneLoad.Add(value);
		remove => DiscoRunner.sceneLoad.Remove(value);
	}
	public static event Action OnDialogueLoad
	{
		add => DiscoRunner.dialogueLoad.Add(value);
		remove => DiscoRunner.dialogueLoad.Remove(value);
	}
	
	public static event Action OnViewChanging
	{
		add => DiscoRunner.viewChanging.Add(value);
		remove => DiscoRunner.viewChanging.Remove(value);
	}
	
	public static event Action OnViewChangeComplete
	{
		add => DiscoRunner.viewChangeComplete.Add(value);
		remove => DiscoRunner.viewChangeComplete.Remove(value);
	} 
}
