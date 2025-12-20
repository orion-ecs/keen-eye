using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Tests;

public class AnimationManagerTests
{
    #region Clip Management

    [Fact]
    public void RegisterClip_ReturnsUniqueId()
    {
        using var manager = new AnimationManager();

        var clip1 = new AnimationClip { Name = "Clip1" };
        var clip2 = new AnimationClip { Name = "Clip2" };

        var id1 = manager.RegisterClip(clip1);
        var id2 = manager.RegisterClip(clip2);

        id1.ShouldNotBe(id2);
        id1.ShouldBeGreaterThan(0);
        id2.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetClip_ReturnsRegisteredClip()
    {
        using var manager = new AnimationManager();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2.5f };

        var id = manager.RegisterClip(clip);
        var retrieved = manager.GetClip(id);

        retrieved.ShouldBe(clip);
        retrieved.Name.ShouldBe("TestClip");
        retrieved.Duration.ShouldBe(2.5f);
    }

    [Fact]
    public void TryGetClip_ReturnsTrueForRegisteredClip()
    {
        using var manager = new AnimationManager();
        var clip = new AnimationClip { Name = "TestClip" };

        var id = manager.RegisterClip(clip);
        var found = manager.TryGetClip(id, out var retrieved);

        found.ShouldBeTrue();
        retrieved.ShouldBe(clip);
    }

    [Fact]
    public void TryGetClip_ReturnsFalseForUnknownId()
    {
        using var manager = new AnimationManager();

        var found = manager.TryGetClip(999, out var retrieved);

        found.ShouldBeFalse();
        retrieved.ShouldBeNull();
    }

    [Fact]
    public void UnregisterClip_RemovesClip()
    {
        using var manager = new AnimationManager();
        var clip = new AnimationClip { Name = "TestClip" };

        var id = manager.RegisterClip(clip);
        manager.ClipCount.ShouldBe(1);

        var removed = manager.UnregisterClip(id);

        removed.ShouldBeTrue();
        manager.ClipCount.ShouldBe(0);
        manager.TryGetClip(id, out _).ShouldBeFalse();
    }

    #endregion

    #region SpriteSheet Management

    [Fact]
    public void RegisterSpriteSheet_ReturnsUniqueId()
    {
        using var manager = new AnimationManager();

        var sheet1 = new SpriteSheet { Name = "Sheet1" };
        var sheet2 = new SpriteSheet { Name = "Sheet2" };

        var id1 = manager.RegisterSpriteSheet(sheet1);
        var id2 = manager.RegisterSpriteSheet(sheet2);

        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void GetSpriteSheet_ReturnsRegisteredSheet()
    {
        using var manager = new AnimationManager();
        var sheet = new SpriteSheet { Name = "TestSheet" };

        var id = manager.RegisterSpriteSheet(sheet);
        var retrieved = manager.GetSpriteSheet(id);

        retrieved.ShouldBe(sheet);
    }

    #endregion

    #region Controller Management

    [Fact]
    public void RegisterController_ReturnsUniqueId()
    {
        using var manager = new AnimationManager();

        var ctrl1 = new AnimatorController { Name = "Controller1" };
        var ctrl2 = new AnimatorController { Name = "Controller2" };

        var id1 = manager.RegisterController(ctrl1);
        var id2 = manager.RegisterController(ctrl2);

        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void GetController_ReturnsRegisteredController()
    {
        using var manager = new AnimationManager();
        var controller = new AnimatorController { Name = "TestController" };

        var id = manager.RegisterController(controller);
        var retrieved = manager.GetController(id);

        retrieved.ShouldBe(controller);
    }

    #endregion

    #region Clear and Dispose

    [Fact]
    public void Clear_RemovesAllAssets()
    {
        using var manager = new AnimationManager();

        manager.RegisterClip(new AnimationClip { Name = "Clip" });
        manager.RegisterSpriteSheet(new SpriteSheet { Name = "Sheet" });
        manager.RegisterController(new AnimatorController { Name = "Controller" });

        manager.ClipCount.ShouldBe(1);
        manager.SpriteSheetCount.ShouldBe(1);
        manager.ControllerCount.ShouldBe(1);

        manager.Clear();

        manager.ClipCount.ShouldBe(0);
        manager.SpriteSheetCount.ShouldBe(0);
        manager.ControllerCount.ShouldBe(0);
    }

    #endregion
}
