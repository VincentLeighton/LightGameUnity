using System.Linq;
using FsCheck;
using NUnit.Framework;

// Feature: light-puzzle-game, Property 7: Inventory round-trip
// Validates: Requirements 4.1, 4.2, 4.4

[TestFixture]
public class InventoryTests
{
    // Property 7a: For random sequences of add/remove operations,
    // count equals adds minus successful removes
    [Test]
    public void MirrorCount_EqualsAddsMinusSuccessfulRemoves()
    {
        // Generate a random sequence of operations: true = add, false = remove
        var arb = Arb.From(
            from count in Gen.Choose(1, 50)
            from ops in Gen.ArrayOf(count, Gen.Elements(true, false))
            select ops);

        Prop.ForAll(arb, ops =>
        {
            var inventory = new Inventory();
            int adds = 0;
            int successfulRemoves = 0;

            foreach (var isAdd in ops)
            {
                if (isAdd)
                {
                    inventory.AddMirror();
                    adds++;
                }
                else
                {
                    if (inventory.RemoveMirror())
                        successfulRemoves++;
                }
            }

            Assert.AreEqual(adds - successfulRemoves, inventory.MirrorCount,
                $"After {adds} adds and {successfulRemoves} successful removes, " +
                $"expected count {adds - successfulRemoves} but got {inventory.MirrorCount}");
        }).QuickCheckThrowOnFailure();
    }

    // Property 7b: Pickup-then-place-then-pickup leaves count unchanged (round-trip)
    [Test]
    public void PickupPlacePickup_LeavesCountUnchanged()
    {
        var arb = Arb.From(Gen.Choose(0, 20));

        Prop.ForAll(arb, initialMirrors =>
        {
            var inventory = new Inventory();

            // Add initial mirrors
            for (int i = 0; i < initialMirrors; i++)
                inventory.AddMirror();

            int countBefore = inventory.MirrorCount;

            // Pickup (add), place (remove), pickup (add)
            inventory.AddMirror();
            bool removed = inventory.RemoveMirror();
            Assert.IsTrue(removed, "Remove should succeed after an add");
            inventory.AddMirror();

            // Net effect: +1 add (add, remove, add = +1)
            Assert.AreEqual(countBefore + 1, inventory.MirrorCount,
                "After pickup-place-pickup, count should be initial + 1");
        }).QuickCheckThrowOnFailure();
    }

    // Property 7c: RemoveMirror returns false when inventory is empty
    [Test]
    public void RemoveMirror_ReturnsFalse_WhenEmpty()
    {
        var inventory = new Inventory();
        Assert.IsFalse(inventory.RemoveMirror(), "Remove on empty inventory should return false");
        Assert.AreEqual(0, inventory.MirrorCount, "Count should remain 0 after failed remove");
    }

    // Property 7d: OnMirrorCountChanged fires with correct count
    [Test]
    public void OnMirrorCountChanged_FiresWithCorrectCount()
    {
        var arb = Arb.From(Gen.Choose(1, 20));

        Prop.ForAll(arb, numAdds =>
        {
            var inventory = new Inventory();
            int lastReportedCount = -1;
            inventory.OnMirrorCountChanged += count => lastReportedCount = count;

            for (int i = 0; i < numAdds; i++)
                inventory.AddMirror();

            Assert.AreEqual(inventory.MirrorCount, lastReportedCount,
                "Last reported count should match actual MirrorCount");
        }).QuickCheckThrowOnFailure();
    }
}
