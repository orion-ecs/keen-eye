# Inventory System

## Problem

You want entities to carry items, with support for stacking, equipping, and item effects.

## Solution

### Item Components

```csharp
// Item definition (what the item IS)
[Component]
public partial struct Item : IComponent
{
    public int ItemId;
    public int StackCount;
    public int MaxStack;
}

[Component]
public partial struct ItemStats : IComponent
{
    public int Damage;
    public int Armor;
    public float SpeedBonus;
}

[TagComponent]
public partial struct Consumable : ITagComponent { }

[TagComponent]
public partial struct Equipment : ITagComponent { }

// Relationship: item belongs to inventory
[Component]
public partial struct InInventory : IComponent
{
    public Entity Owner;
    public int SlotIndex;
}

// Relationship: item is equipped
[Component]
public partial struct Equipped : IComponent
{
    public Entity Owner;
    public EquipSlot Slot;
}

public enum EquipSlot
{
    MainHand,
    OffHand,
    Head,
    Chest,
    Legs,
    Feet
}
```

### Inventory Component

```csharp
[Component]
public partial struct Inventory : IComponent
{
    public int MaxSlots;
    public int UsedSlots;
}
```

### Item Management System

```csharp
public class InventorySystem : SystemBase
{
    public bool TryAddItem(Entity owner, int itemId, int count = 1)
    {
        if (!World.Has<Inventory>(owner))
            return false;

        ref var inventory = ref World.Get<Inventory>(owner);

        // First, try to stack with existing items
        foreach (var itemEntity in World.Query<Item, InInventory>())
        {
            ref readonly var inInv = ref World.Get<InInventory>(itemEntity);
            if (inInv.Owner != owner)
                continue;

            ref var item = ref World.Get<Item>(itemEntity);
            if (item.ItemId != itemId)
                continue;

            int canAdd = item.MaxStack - item.StackCount;
            if (canAdd > 0)
            {
                int toAdd = Math.Min(canAdd, count);
                item.StackCount += toAdd;
                count -= toAdd;

                if (count == 0)
                    return true;
            }
        }

        // Create new stacks for remaining items
        while (count > 0 && inventory.UsedSlots < inventory.MaxSlots)
        {
            var itemDef = ItemDatabase.Get(itemId);
            int stackSize = Math.Min(count, itemDef.MaxStack);

            World.Spawn()
                .With(new Item
                {
                    ItemId = itemId,
                    StackCount = stackSize,
                    MaxStack = itemDef.MaxStack
                })
                .With(new InInventory
                {
                    Owner = owner,
                    SlotIndex = FindEmptySlot(owner)
                })
                .Build();

            count -= stackSize;
            inventory.UsedSlots++;
        }

        return count == 0;  // True if all items were added
    }

    public bool TryRemoveItem(Entity owner, int itemId, int count = 1)
    {
        var buffer = World.GetCommandBuffer();
        int remaining = count;

        foreach (var itemEntity in World.Query<Item, InInventory>())
        {
            ref readonly var inInv = ref World.Get<InInventory>(itemEntity);
            if (inInv.Owner != owner)
                continue;

            ref var item = ref World.Get<Item>(itemEntity);
            if (item.ItemId != itemId)
                continue;

            int toRemove = Math.Min(item.StackCount, remaining);
            item.StackCount -= toRemove;
            remaining -= toRemove;

            if (item.StackCount == 0)
            {
                buffer.Despawn(itemEntity);
                World.Get<Inventory>(owner).UsedSlots--;
            }

            if (remaining == 0)
                break;
        }

        buffer.Execute();
        return remaining == 0;
    }

    public int GetItemCount(Entity owner, int itemId)
    {
        int total = 0;

        foreach (var itemEntity in World.Query<Item, InInventory>())
        {
            ref readonly var inInv = ref World.Get<InInventory>(itemEntity);
            if (inInv.Owner != owner)
                continue;

            ref readonly var item = ref World.Get<Item>(itemEntity);
            if (item.ItemId == itemId)
                total += item.StackCount;
        }

        return total;
    }

    private int FindEmptySlot(Entity owner)
    {
        var usedSlots = new HashSet<int>();

        foreach (var itemEntity in World.Query<InInventory>())
        {
            ref readonly var inInv = ref World.Get<InInventory>(itemEntity);
            if (inInv.Owner == owner)
                usedSlots.Add(inInv.SlotIndex);
        }

        for (int i = 0; i < 100; i++)
        {
            if (!usedSlots.Contains(i))
                return i;
        }

        return -1;
    }
}
```

### Equipment System

```csharp
public class EquipmentSystem : SystemBase
{
    public bool TryEquip(Entity owner, Entity itemEntity, EquipSlot slot)
    {
        // Verify item can be equipped
        if (!World.Has<Equipment>(itemEntity))
            return false;

        // Unequip current item in slot
        TryUnequip(owner, slot);

        // Move from inventory to equipped
        World.Remove<InInventory>(itemEntity);
        World.Add(itemEntity, new Equipped { Owner = owner, Slot = slot });

        // Update owner's stats
        RecalculateStats(owner);

        return true;
    }

    public bool TryUnequip(Entity owner, EquipSlot slot)
    {
        // Find item in slot
        foreach (var itemEntity in World.Query<Equipped>())
        {
            ref readonly var equipped = ref World.Get<Equipped>(itemEntity);
            if (equipped.Owner != owner || equipped.Slot != slot)
                continue;

            // Check inventory space
            ref var inventory = ref World.Get<Inventory>(owner);
            if (inventory.UsedSlots >= inventory.MaxSlots)
                return false;  // No space

            // Move to inventory
            World.Remove<Equipped>(itemEntity);
            World.Add(itemEntity, new InInventory
            {
                Owner = owner,
                SlotIndex = FindEmptySlot(owner)
            });

            inventory.UsedSlots++;
            RecalculateStats(owner);
            return true;
        }

        return false;  // Nothing equipped
    }

    public void RecalculateStats(Entity owner)
    {
        int totalDamage = 0;
        int totalArmor = 0;
        float totalSpeedBonus = 0;

        // Sum stats from all equipped items
        foreach (var itemEntity in World.Query<Equipped, ItemStats>())
        {
            ref readonly var equipped = ref World.Get<Equipped>(itemEntity);
            if (equipped.Owner != owner)
                continue;

            ref readonly var stats = ref World.Get<ItemStats>(itemEntity);
            totalDamage += stats.Damage;
            totalArmor += stats.Armor;
            totalSpeedBonus += stats.SpeedBonus;
        }

        // Apply to owner
        if (World.TryGet<CombatStats>(owner, out var combat))
        {
            combat.BonusDamage = totalDamage;
            combat.BonusArmor = totalArmor;
            World.Set(owner, combat);
        }

        if (World.TryGet<SpeedBuff>(owner, out var speed))
        {
            speed.Multiplier = 1f + totalSpeedBonus;
            World.Set(owner, speed);
        }
    }
}
```

### Consumable System

```csharp
public class ConsumableSystem : SystemBase
{
    public bool TryUseItem(Entity user, Entity itemEntity)
    {
        if (!World.Has<Consumable>(itemEntity))
            return false;

        ref readonly var item = ref World.Get<Item>(itemEntity);

        // Apply item effect
        ApplyConsumableEffect(user, item.ItemId);

        // Reduce stack
        ref var stack = ref World.Get<Item>(itemEntity);
        stack.StackCount--;

        if (stack.StackCount == 0)
        {
            World.Despawn(itemEntity);
            World.Get<Inventory>(user).UsedSlots--;
        }

        return true;
    }

    private void ApplyConsumableEffect(Entity user, int itemId)
    {
        switch (itemId)
        {
            case ItemIds.HealthPotion:
                World.Add(user, new HealReceived { Amount = 50 });
                break;

            case ItemIds.SpeedPotion:
                World.Add(user, new SpeedBuff
                {
                    Multiplier = 1.5f,
                    RemainingDuration = 30f
                });
                break;

            case ItemIds.Antidote:
                World.Remove<PoisonDebuff>(user);
                break;
        }
    }
}
```

## Why This Works

### Items as Entities

Each item is its own entity with:
- **Identity**: Has its own unique entity ID
- **Data**: Stack count, stats, etc. as components
- **Relationships**: `InInventory` and `Equipped` link to owner

This enables:
- Items can have arbitrary components (enchantments, durability, etc.)
- Query for items by any criteria
- No fixed inventory size in the owner's data

### Relationship Components

`InInventory` and `Equipped` are relationship components:
- Point to the owner entity
- Include relationship-specific data (slot index)
- Can be queried from either direction

### Stat Recalculation

Instead of storing "current stats" that need constant updating:
1. Store base stats on the entity
2. Store bonus stats on equipment
3. Recalculate totals when equipment changes

This avoids sync issues and makes the calculation explicit.

## Variations

### Item Durability

```csharp
[Component]
public partial struct Durability : IComponent
{
    public int Current;
    public int Max;
}

public class DurabilitySystem : SystemBase
{
    public void OnItemUsed(Entity itemEntity)
    {
        if (!World.TryGet<Durability>(itemEntity, out var durability))
            return;

        durability.Current--;
        World.Set(itemEntity, durability);

        if (durability.Current <= 0)
        {
            // Item breaks
            World.Despawn(itemEntity);
        }
    }
}
```

### Item Enchantments

```csharp
[Component]
public partial struct Enchantment : IComponent
{
    public EnchantmentType Type;
    public int Level;
}

// Items can have multiple enchantments
// Each enchantment is a separate entity with relationship to the item
[Component]
public partial struct EnchantedBy : IComponent
{
    public Entity Enchantment;
}

// Or store as list in component (simpler but less queryable)
[Component]
public partial struct Enchantments : IComponent
{
    public EnchantmentType[] Types;
    public int[] Levels;
}
```

### Loot Tables

```csharp
public static class LootTable
{
    public static void DropLoot(World world, Entity source, Position position)
    {
        var roll = Random.Shared.NextDouble();

        int itemId;
        if (roll < 0.01)       // 1%
            itemId = ItemIds.LegendarySword;
        else if (roll < 0.10)  // 9%
            itemId = ItemIds.RareArmor;
        else if (roll < 0.50)  // 40%
            itemId = ItemIds.CommonPotion;
        else
            return;  // No drop

        // Create dropped item entity
        world.Spawn()
            .With(new Item { ItemId = itemId, StackCount = 1, MaxStack = 1 })
            .With(position)
            .WithTag<DroppedItem>()
            .Build();
    }
}
```

### Crafting

```csharp
public record CraftingRecipe(int OutputItemId, int OutputCount, params (int ItemId, int Count)[] Ingredients);

public class CraftingSystem : SystemBase
{
    private readonly List<CraftingRecipe> recipes = new()
    {
        new CraftingRecipe(ItemIds.HealthPotion, 1,
            (ItemIds.Herb, 3),
            (ItemIds.Water, 1)),

        new CraftingRecipe(ItemIds.IronSword, 1,
            (ItemIds.IronIngot, 5),
            (ItemIds.Wood, 2)),
    };

    public bool TryCraft(Entity crafter, int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count)
            return false;

        var recipe = recipes[recipeIndex];
        var inventorySystem = World.GetSystem<InventorySystem>();

        // Check ingredients
        foreach (var (itemId, count) in recipe.Ingredients)
        {
            if (inventorySystem.GetItemCount(crafter, itemId) < count)
                return false;  // Missing ingredient
        }

        // Remove ingredients
        foreach (var (itemId, count) in recipe.Ingredients)
        {
            inventorySystem.TryRemoveItem(crafter, itemId, count);
        }

        // Add output
        inventorySystem.TryAddItem(crafter, recipe.OutputItemId, recipe.OutputCount);

        return true;
    }
}
```

## See Also

- [Relationships Guide](../relationships.md) - Entity relationships
- [Serialization Guide](../serialization.md) - Saving inventory state
- [Health & Damage](health-damage.md) - Combat stat integration
