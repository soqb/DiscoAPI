using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using UnityEngine;

namespace DiscoAPI.Runtime;

// <summary>
// All the assets and such which are used by the API itself.
// </summary>
public class InherentProvider : IDiscoProvider
{
	public string Guid => "discoapi";
	public DiscoSource Source { get; }
	public AssetSource Assets => Source.Assets;

	public static InherentProvider Instance = null!;


	private class InherentAssetRouter : IAssetRouter
	{
		IAssetRoute<Sprite> IAssetRouter.Portraits => new LooseSpriteRouter(InherentProvider.Instance.Location);
	}

	public string? Location { get; }
	public IAssetRouter Router { get; }

	public const string DUMMY_NONE_SKILL = "API DUMMY NONE SKILL";

	public InherentProvider()
	{
		Source = DiscoRunner.manager.CreateSource(Guid, DiscoRunner.Log);
		Instance = this;

		Location = DiscoPlugin.GetLocationFromAssembly(GetType().Assembly, DiscoRunner.Log);
		Router = new MemoizedAssetRouter(new InherentAssetRouter());
	}

	public void OnDialogueBundleLoad()
	{
		// we have to introduce a dummy actor with a simple portrait for the case where no skill is used in the portrait grid.
		Assets.Add(new Actor("dummy-none-skill", DUMMY_NONE_SKILL) { portraitName = "assets/textures/portrait_none.png" });
	}
}
