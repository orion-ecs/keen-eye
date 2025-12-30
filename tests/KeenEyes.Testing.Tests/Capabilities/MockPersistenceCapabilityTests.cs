using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockPersistenceCapabilityTests
{
    [Fact]
    public void SaveDirectory_DefaultValue_ReturnsDefaultPath()
    {
        var capability = new MockPersistenceCapability();

        Assert.Equal("./saves", capability.SaveDirectory);
    }

    [Fact]
    public void SaveDirectory_WhenSet_ReturnsNewValue()
    {
        var capability = new MockPersistenceCapability { SaveDirectory = "/custom/path" };

        Assert.Equal("/custom/path", capability.SaveDirectory);
    }

    [Fact]
    public void SaveDirectory_WhenSet_TracksHistory()
    {
        var capability = new MockPersistenceCapability { SaveDirectory = "/path1" };
        _ = capability.SaveDirectorySetCount; // Force evaluation before second set
        capability.SaveDirectory = "/path2";

        Assert.Equal(2, capability.SaveDirectoryHistory.Count);
        Assert.Equal("/path1", capability.SaveDirectoryHistory[0]);
        Assert.Equal("/path2", capability.SaveDirectoryHistory[1]);
    }

    [Fact]
    public void WasSaveDirectorySet_Initially_ReturnsFalse()
    {
        var capability = new MockPersistenceCapability();

        Assert.False(capability.WasSaveDirectorySet);
    }

    [Fact]
    public void WasSaveDirectorySet_AfterSet_ReturnsTrue()
    {
        var capability = new MockPersistenceCapability { SaveDirectory = "/path" };

        Assert.True(capability.WasSaveDirectorySet);
    }

    [Fact]
    public void SaveDirectorySetCount_TracksCount()
    {
        var capability = new MockPersistenceCapability { SaveDirectory = "/path1" };
        _ = capability.SaveDirectorySetCount;
        capability.SaveDirectory = "/path2";
        _ = capability.SaveDirectorySetCount;
        capability.SaveDirectory = "/path3";

        Assert.Equal(3, capability.SaveDirectorySetCount);
    }

    [Fact]
    public void Reset_RestoresDefaultState()
    {
        var capability = new MockPersistenceCapability { SaveDirectory = "/custom/path" };
        _ = capability.SaveDirectorySetCount;
        capability.SaveDirectory = "/another/path";

        capability.Reset();

        Assert.Equal("./saves", capability.SaveDirectory);
        Assert.Empty(capability.SaveDirectoryHistory);
        Assert.False(capability.WasSaveDirectorySet);
        Assert.Equal(0, capability.SaveDirectorySetCount);
    }
}
