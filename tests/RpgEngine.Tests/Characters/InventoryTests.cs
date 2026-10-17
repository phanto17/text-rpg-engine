using RpgEngine.Core.Characters;

namespace RpgEngine.Tests.Characters;

public sealed class InventoryTests
{
    [Fact]
    public void Add_SameItemTwice_StacksCount()
    {
        var inventory = new Inventory();

        inventory.Add("potion");
        inventory.Add("potion", 2);

        Assert.Equal(3, inventory.CountOf("potion"));
        Assert.Equal(new InventoryEntry("potion", 3), Assert.Single(inventory.Entries));
    }

    [Fact]
    public void Entries_KeepFirstPickupOrder()
    {
        var inventory = new Inventory();

        inventory.Add("sword");
        inventory.Add("potion");
        inventory.Add("sword");

        Assert.Equal(new[] { "sword", "potion" }, inventory.Entries.Select(e => e.ItemId));
    }

    [Fact]
    public void Remove_PartialStack_KeepsRemainder()
    {
        var inventory = new Inventory();
        inventory.Add("potion", 3);

        Assert.True(inventory.Remove("potion", 2));

        Assert.Equal(1, inventory.CountOf("potion"));
    }

    [Fact]
    public void Remove_WholeStack_RemovesEntry()
    {
        var inventory = new Inventory();
        inventory.Add("potion");

        Assert.True(inventory.Remove("potion"));

        Assert.True(inventory.IsEmpty);
        Assert.False(inventory.Contains("potion"));
    }

    [Fact]
    public void Remove_NotEnough_ReturnsFalseAndKeepsItems()
    {
        var inventory = new Inventory();
        inventory.Add("potion");

        Assert.False(inventory.Remove("potion", 2));
        Assert.False(inventory.Remove("missing"));

        Assert.Equal(1, inventory.CountOf("potion"));
    }

    [Fact]
    public void Contains_ChecksCount()
    {
        var inventory = new Inventory();
        inventory.Add("arrow", 5);

        Assert.True(inventory.Contains("arrow", 5));
        Assert.False(inventory.Contains("arrow", 6));
        Assert.Equal(0, inventory.CountOf("bolt"));
    }

    [Fact]
    public void Add_InvalidArguments_Throw()
    {
        var inventory = new Inventory();

        Assert.Throws<ArgumentException>(() => inventory.Add(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add("potion", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Remove("potion", 0));
    }
}
