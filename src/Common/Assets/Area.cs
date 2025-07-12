namespace DiscoAPI.Common.Assets;

public record Area : Asset, IAssetRef<Area>
{
    public readonly string sceneName;
    public readonly int visualRadius;
    public readonly bool isOutside;
    public readonly bool isDreamScene;
    public readonly bool hasCustomZoomLimits;
    public readonly float customMinZoomLimit;
    public readonly float customMaxZoomLimit;
    public readonly string navMeshLocation;

    public Area(string areaId, string sceneName, string navMeshLocation, bool isOutside, int visualRadius = 18) : base(areaId)
    {
        this.sceneName = sceneName;
        this.isOutside = isOutside;
        this.navMeshLocation = navMeshLocation;
        this.visualRadius = visualRadius;
    }

    public Area(string areaId, string sceneName,  string navMeshLocation, bool isOutside, int visualRadius = 18, 
        bool isDreamScene = false, bool hasCustomZoomLimits = false, float customMinZoomLimit = 0f, 
        float customMaxZoomLimit = 0f) : base(areaId)
    {
        this.sceneName = sceneName;
        this.navMeshLocation = navMeshLocation;
        this.isOutside = isOutside;
        this.isDreamScene = isDreamScene;
        this.visualRadius = visualRadius;
        this.hasCustomZoomLimits = hasCustomZoomLimits;
        this.customMinZoomLimit = customMinZoomLimit;
        this.customMaxZoomLimit = customMaxZoomLimit;
    }

    public SceneProperties GetSceneProperties()
    {
        // note: SceneProperties.DisplayName returns null due to missing localization
        return new SceneProperties()
        {
            SceneName = sceneName,
            SceneId = id,
            SaveGameId = id,
            VisualRadius = visualRadius,
            IsOutside = isOutside,
            IsDreamScene = isDreamScene,
            HasCustomZoomLimits = hasCustomZoomLimits,
            CustomMinimumZoomLimit = customMinZoomLimit,
            CustomMaximumZoomLimit = customMaxZoomLimit
        };
    }

    public new AssetLocation<Area> Location => new(source, id);
    Area? IAssetRef<Area>.Resolve(IDiscoManager mgr) => (Area?)((IAssetRef)this).Resolve(mgr);
}