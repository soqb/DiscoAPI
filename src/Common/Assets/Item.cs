using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Item : Asset, IAssetRef<Item>
{
    public readonly string displayName;
    public readonly string description;
    public readonly string bigImageLocation; // need impl
    public readonly string iconImageLocation; // need impl
    public readonly int valueInCents;
    public readonly string? conversation;
    public readonly IAssetRef<Item>? stackingItem;
    public readonly bool isVessel;
    public readonly string? pickupSound;
    public readonly bool multipleAllowed;
    
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
    public readonly string itemPrefabLocation; // need impl
    public readonly Modifier[]? equipEffects;
    public readonly string? heldIconLocation; // need impl
    public readonly string? equipOrbName;
    public readonly ItemEquipSlot[]? coversSlots;
    public readonly bool autoEquip;
    
    public EquippableItem(string id, string displayName, string description, string bigImageLocation, 
        string iconImageLocation, ItemEquipSlot slot, string itemPrefabLocation) 
        : base(id, displayName, description, bigImageLocation, iconImageLocation)
    {
        this.equipSlot = slot;
        this.itemPrefabLocation = itemPrefabLocation;
    }
    
}

public record SubstanceItem : EquippableItem
{
    public readonly int effectDuration; // need impl
    public readonly ItemGroup group;
    public readonly int uses = 3; // need impl
    public readonly Tuple<int, Modifier>[] substanceEffects;
    
    public SubstanceItem(string id, string displayName, string description, string bigImageLocation, 
        string iconImageLocation, ItemEquipSlot slot, string itemPrefabLocation, int duration, ItemGroup group) 
        : base(id, displayName, description, bigImageLocation, iconImageLocation, slot, itemPrefabLocation)
    {
        this.effectDuration = duration;
        this.group = group;
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