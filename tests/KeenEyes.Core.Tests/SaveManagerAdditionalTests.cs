using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for SaveManager to improve coverage.
/// Focuses on error paths and edge cases.
/// </summary>
public class SaveManagerAdditionalTests
{
    #region Test Components

    public struct TestPosition : IComponent
    {
        public float X, Y;
    }

    #endregion

    #region TryGetSlotInfo Error Paths

    [Fact]
    public void TryGetSlotInfo_WithNonExistentSlot_ReturnsNull()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-tryget");

        try
        {
            var result = manager.TryGetSlotInfo("NonExistentSlot");

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists("test-saves-tryget"))
            {
                Directory.Delete("test-saves-tryget", true);
            }
        }
    }

    [Fact]
    public void TryGetSlotInfo_WithCorruptedFile_ReturnsNull()
    {
        using var world = new World();
        var saveDir = "test-saves-corrupt";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            // Write corrupted data
            var slotPath = Path.Combine(saveDir, "CorruptedSlot.kesave");
            File.WriteAllBytes(slotPath, [0xFF, 0xFF, 0xFF, 0xFF]);

            var result = manager.TryGetSlotInfo("CorruptedSlot");

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region ValidateSlot Error Paths

    [Fact]
    public void ValidateSlot_WithNonExistentSlot_ReturnsNull()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-validate");

        try
        {
            var result = manager.ValidateSlot("NonExistentSlot");

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists("test-saves-validate"))
            {
                Directory.Delete("test-saves-validate", true);
            }
        }
    }

    [Fact]
    public void ValidateSlot_WithCorruptedFile_ReturnsNull()
    {
        using var world = new World();
        var saveDir = "test-saves-validate-corrupt";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            // Write corrupted data
            var slotPath = Path.Combine(saveDir, "CorruptedSlot.kesave");
            File.WriteAllBytes(slotPath, [0x00, 0x01, 0x02]);

            var result = manager.ValidateSlot("CorruptedSlot");

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region LoadDelta Error Paths

    [Fact]
    public void LoadDelta_WithNonExistentSlot_ThrowsFileNotFoundException()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-loaddelta");

        try
        {
            Assert.Throws<FileNotFoundException>(() =>
                manager.LoadDelta("NonExistentDelta"));
        }
        finally
        {
            if (Directory.Exists("test-saves-loaddelta"))
            {
                Directory.Delete("test-saves-loaddelta", true);
            }
        }
    }

    [Fact]
    public void LoadDelta_WithNullSlotName_ThrowsArgumentNullException()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-loaddelta-null");

        try
        {
            Assert.Throws<ArgumentNullException>(() =>
                manager.LoadDelta(null!));
        }
        finally
        {
            if (Directory.Exists("test-saves-loaddelta-null"))
            {
                Directory.Delete("test-saves-loaddelta-null", true);
            }
        }
    }

    [Fact]
    public void LoadDelta_WithEmptySlotName_ThrowsArgumentException()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-loaddelta-empty");

        try
        {
            Assert.Throws<ArgumentException>(() =>
                manager.LoadDelta(""));
        }
        finally
        {
            if (Directory.Exists("test-saves-loaddelta-empty"))
            {
                Directory.Delete("test-saves-loaddelta-empty", true);
            }
        }
    }

    [Fact]
    public void LoadDelta_WithWhitespaceSlotName_ThrowsArgumentException()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-loaddelta-whitespace");

        try
        {
            Assert.Throws<ArgumentException>(() =>
                manager.LoadDelta("   "));
        }
        finally
        {
            if (Directory.Exists("test-saves-loaddelta-whitespace"))
            {
                Directory.Delete("test-saves-loaddelta-whitespace", true);
            }
        }
    }

    #endregion

    #region Async DeleteSlotAsync Error Paths

    [Fact]
    public async Task DeleteSlotAsync_WithNonExistentSlot_ReturnsFalse()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-deleteasync");

        try
        {
            var result = await manager.DeleteSlotAsync("NonExistentSlot", TestContext.Current.CancellationToken);

            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists("test-saves-deleteasync"))
            {
                Directory.Delete("test-saves-deleteasync", true);
            }
        }
    }

    #endregion

    #region ValidateSlotAsync Error Paths

    [Fact]
    public async Task ValidateSlotAsync_WithNonExistentSlot_ReturnsNull()
    {
        using var world = new World();
        var manager = new SaveManager(world, "test-saves-validateasync");

        try
        {
            var result = await manager.ValidateSlotAsync("NonExistentSlot", TestContext.Current.CancellationToken);

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists("test-saves-validateasync"))
            {
                Directory.Delete("test-saves-validateasync", true);
            }
        }
    }

    [Fact]
    public async Task ValidateSlotAsync_WithCorruptedFile_ReturnsNull()
    {
        using var world = new World();
        var saveDir = "test-saves-validateasync-corrupt";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            // Write corrupted data
            var slotPath = Path.Combine(saveDir, "CorruptedSlot.kesave");
            await File.WriteAllBytesAsync(slotPath, [0x00, 0x01, 0x02], TestContext.Current.CancellationToken);

            var result = await manager.ValidateSlotAsync("CorruptedSlot", TestContext.Current.CancellationToken);

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ListSlots_WithEmptyDirectory_ReturnsEmpty()
    {
        using var world = new World();
        var saveDir = "test-saves-listempty";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            var slots = manager.ListSlots().ToList();

            Assert.Empty(slots);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region GetSlotInfo Tests

    [Fact]
    public void GetSlotInfo_WithNonExistentSlot_ThrowsFileNotFoundException()
    {
        using var world = new World();
        var saveDir = "test-saves-getslotinfo";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            var ex = Assert.Throws<FileNotFoundException>(() =>
                manager.GetSlotInfo("NonExistentSlot"));

            Assert.Contains("NonExistentSlot", ex.Message);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region SlotExists Error Paths

    [Fact]
    public void SlotExists_WithCorruptedFile_ReturnsFalse()
    {
        using var world = new World();
        var saveDir = "test-saves-slotexists-corrupt";
        var manager = new SaveManager(world, saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);

            // Write corrupted data that will cause an exception when reading
            var slotPath = Path.Combine(saveDir, "CorruptedSlot.kesave");
            File.WriteAllBytes(slotPath, [0xDE, 0xAD, 0xBE, 0xEF]);

            var result = manager.SlotExists("CorruptedSlot");

            // SlotExists calls ValidateSlot which catches exceptions
            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion

    #region LoadDelta with Non-Delta Save

    [Fact]
    public void LoadDelta_WithNonDeltaSave_ThrowsInvalidDataException()
    {
        var saveDir = "test-saves-loaddelta-nondelta";

        try
        {
            Directory.CreateDirectory(saveDir);

            using var world = new World();
            world.Components.Register<SerializablePosition>();
            world.SaveDirectory = saveDir;

            // Create a regular (non-delta) save
            var serializer = new TestComponentSerializer()
                .WithComponent<SerializablePosition>();
            world.SaveToSlot("RegularSave", serializer);

            var manager = new SaveManager(world, saveDir);

            // LoadDelta should throw because this is not a delta
            var ex = Assert.Throws<InvalidDataException>(() =>
                manager.LoadDelta("RegularSave"));

            Assert.Contains("not a delta snapshot", ex.Message);
        }
        finally
        {
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
        }
    }

    #endregion
}
