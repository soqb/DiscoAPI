using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Subtask(string subid, string title, [JsonProperty("timed")] bool isTimed = false);

public enum TaskReward
{
	Minor = 2,
	Major = 4,
	Standard = 3,
}

public record Task : Asset, IAssetRef<Task>
{
	public readonly string title;
	public readonly string description;
	public readonly TaskReward reward;
	public readonly List<Subtask> subtasks;
	[JsonProperty("timed")]
	public readonly bool isTimed;

	public Task(string id, string title, string description, TaskReward reward, List<Subtask> subtasks, bool isTimed = false) : base(id)
	{
		this.title = title;
		this.description = description;
		this.reward = reward;
		this.isTimed = isTimed;
		this.subtasks = subtasks;
	}

	[JsonIgnore]
	public new AssetLocation<Task> Location => new(source, id);
	Task? IAssetRef<Task>.Resolve(IDiscoManager mgr) => (Task?)((IAssetRef)this).Resolve(mgr);
}
