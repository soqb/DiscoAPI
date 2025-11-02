using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Item : Asset, IAssetRef<Item>
{
    public readonly string displayName;
    public readonly string description;
    public readonly string bigImageLocation;
    public readonly string iconImageLocation;
    public int valueInCents;
    public string? conversation;
    public IAssetRef<Item>? stackingItem;
    public bool isVessel;
    public string? pickupSound;
    public bool multipleAllowed;
    
    public Item(string id, string displayName, string description, string bigImageLocation, string iconImageLocation) : base(id)
    {
        this.displayName = displayName;
        this.description = description;
        this.bigImageLocation = bigImageLocation;
        this.iconImageLocation = iconImageLocation;
    }
    
    [JsonIgnore]
    public new AssetLocation<Item> Location => new(source, id);
    Item? IAssetRef<Item>.Resolve(IDiscoManager mgr) => (Item?)((IAssetRef)this).Resolve(mgr);

}

public record EquippableItem : Item
{
    public readonly ItemEquipSlot equipSlot;
    public readonly string itemPrefabLocation;
    public Modifier[]? equipEffects;
    public string? heldIconLocation;
    public string? equipOrbName;
    public ItemEquipSlot[]? coversSlots;
    public bool autoEquip;
    
    public EquippableItem(string id, string displayName, string description, string bigImageLocation, 
        string iconImageLocation, ItemEquipSlot slot, string itemPrefabLocation) 
        : base(id, displayName, description, bigImageLocation, iconImageLocation)
    {
        this.assetType = new(typeof(Item));
        this.equipSlot = slot;
        this.itemPrefabLocation = itemPrefabLocation;
    }
}

public record SubstanceItem : EquippableItem
{
    public readonly ItemGroup group;
    public readonly Modifier[] substanceEffects;
    public int effectDuration;
    
    public SubstanceItem(string id, string displayName, string description, string bigImageLocation, 
        string iconImageLocation, string itemPrefabLocation, ItemGroup group, Modifier[] substanceEffects) 
        : base(id, displayName, description, bigImageLocation, iconImageLocation, ItemEquipSlot.HeldInHand, itemPrefabLocation)
    {
        this.assetType = new(typeof(Item));
        this.effectDuration = 60;
        this.group = group;
        this.substanceEffects = substanceEffects;
    }
}

public enum ItemEquipSlot
{
    // ARMOR and COAT are in basegame but unused
    Shirt = 1, 
    Jacket = 2, 
    Pants = 4, 
    Neck = 5, 
    Glasses = 6, 
    Gloves = 7, 
    Hat = 8, 
    Shoes = 9, 
    HeldInHand = 10
}

public enum ItemGroup
{
    None = 0,
    Alcohol = 1,
    Smokes = 2,
    Speed = 4,
    Pyrholidon = 5,
    Tare = 6,
}