namespace DiscoAPI.Common.Assets;

public record Area : Asset, IAssetRef<Area>
{
    public readonly string scenePath;
    public readonly int visualRadius;
    public readonly bool isOutside;
    public readonly bool isDreamScene;
    public readonly bool hasCustomZoomLimits;
    public readonly float customMinZoomLimit;
    public readonly float customMaxZoomLimit;
    public readonly string navMeshPath;
    public readonly string? vtPath;

    public Area(string areaId, string scenePath, string navMeshPath, bool isOutside, int visualRadius = 18) : base(areaId)
    {
        this.scenePath = scenePath;
        this.isOutside = isOutside;
        this.navMeshPath = navMeshPath;
        this.visualRadius = visualRadius;
    }

    public Area(string areaId, string scenePath, string navMeshPath, bool isOutside, int visualRadius = 18,
        bool isDreamScene = false, bool hasCustomZoomLimits = false, float customMinZoomLimit = 0f,
        float customMaxZoomLimit = 0f) : base(areaId)
    {
        this.scenePath = scenePath;
        this.navMeshPath = navMeshPath;
        this.isOutside = isOutside;
        this.isDreamScene = isDreamScene;
        this.visualRadius = visualRadius;
        this.hasCustomZoomLimits = hasCustomZoomLimits;
        this.customMinZoomLimit = customMinZoomLimit;
        this.customMaxZoomLimit = customMaxZoomLimit;
    }

    public new AssetLocation<Area> Location => new(source, id);
    Area? IAssetRef<Area>.Resolve(IDiscoManager mgr) => (Area?)((IAssetRef)this).Resolve(mgr);
}

public class Areas
{
    public static AssetLocation<Area> Whirling_int_f1 = new("Whirling-int-f1");
    public static AssetLocation<Area> Whirling_int_f2 = new("Whirling-int-f2");
    public static AssetLocation<Area> Whirling_int_f3 = new("Whirling-int-f3");
    public static AssetLocation<Area> Whirling_int_f3_antechamber = new("Whirling-int-f3-antechamber");
    public static AssetLocation<Area> Martinaise_ext = new("Martinaise-ext");
    public static AssetLocation<Area> Doomed_commerce_int_f1 = new("Doomed-commerce-int-f1");
    public static AssetLocation<Area> Doomed_commerce_int_f2 = new("Doomed-commerce-int-f2");
    public static AssetLocation<Area> Doomed_commerce_int_s1 = new("Doomed-commerce-int-s1");
    public static AssetLocation<Area> Capeside_coalchamber_int = new("Capeside-coalchamber-int");
    public static AssetLocation<Area> Secretary_int = new("Secretary-int");
    public static AssetLocation<Area> Pawnshop_int = new("Pawnshop-int");
    public static AssetLocation<Area> Cunos_shack_int = new("Cunos-shack-int");
    public static AssetLocation<Area> Capesider_smoker_int = new("Capeside-smoker-int");
    public static AssetLocation<Area> Sea_fortress_int = new("Sea-fortress-int");
    public static AssetLocation<Area> Tent_int = new("Tent-int");
    public static AssetLocation<Area> Second_home_int = new("Second-home-int");
    public static AssetLocation<Area> FV_house_int = new("FV-house-int");
    public static AssetLocation<Area> Instigators_lair_int = new("Instigators-lair-int");
    public static AssetLocation<Area> Union_container_int = new("Union-container-int");
    public static AssetLocation<Area> Union_boss_int = new("Union-boss-int");
    public static AssetLocation<Area> Capeside_wcw_int = new("Capeside-wcw-int");
    public static AssetLocation<Area> Crypto_garys_apt_int = new("Crypto-garys-apt-int");
    public static AssetLocation<Area> Dream_2 = new("Dream-2");
    public static AssetLocation<Area> Dream_3_ext = new("Dream-3-ext");
    public static AssetLocation<Area> Dream_3_int = new("Dream-3-int");
    public static AssetLocation<Area> FV_shack_int = new("FV-shack-int");
    public static AssetLocation<Area> Feld_int = new("Feld-int");
    public static AssetLocation<Area> Commustudent_int = new("Commustudent-int");
}
