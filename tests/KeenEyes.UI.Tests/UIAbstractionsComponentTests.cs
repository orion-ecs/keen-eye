using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Comprehensive tests for UI.Abstractions component factory methods and properties.
/// </summary>
public class UIAbstractionsComponentTests
{
    #region UIText Tests

    [Fact]
    public void UIText_Create_HasDefaultSettings()
    {
        var text = UIText.Create("Hello World", 18f);

        Assert.Equal("Hello World", text.Content);
        Assert.Equal(18f, text.FontSize);
        Assert.Equal(FontHandle.Invalid, text.Font);
        Assert.Equal(new Vector4(1, 1, 1, 1), text.Color);
        Assert.Equal(TextAlignH.Left, text.HorizontalAlign);
        Assert.Equal(TextAlignV.Top, text.VerticalAlign);
        Assert.False(text.WordWrap);
        Assert.Equal(TextOverflow.Visible, text.Overflow);
    }

    [Fact]
    public void UIText_CreateWithDefaultFontSize_Uses16()
    {
        var text = UIText.Create("Test");

        Assert.Equal("Test", text.Content);
        Assert.Equal(16f, text.FontSize);
    }

    [Fact]
    public void UIText_Centered_HasCenteredAlignment()
    {
        var text = UIText.Centered("Centered Text", 20f);

        Assert.Equal("Centered Text", text.Content);
        Assert.Equal(20f, text.FontSize);
        Assert.Equal(TextAlignH.Center, text.HorizontalAlign);
        Assert.Equal(TextAlignV.Middle, text.VerticalAlign);
        Assert.False(text.WordWrap);
    }

    [Fact]
    public void UIText_CenteredWithDefaultFontSize_Uses16()
    {
        var text = UIText.Centered("Test");

        Assert.Equal(16f, text.FontSize);
        Assert.Equal(TextAlignH.Center, text.HorizontalAlign);
        Assert.Equal(TextAlignV.Middle, text.VerticalAlign);
    }

    #endregion

    #region UIImage Tests

    [Fact]
    public void UIImage_Create_HasDefaultSettings()
    {
        var texture = new TextureHandle(123);
        var image = UIImage.Create(texture);

        Assert.Equal(texture, image.Texture);
        Assert.Equal(Vector4.One, image.Tint);
        Assert.Equal(ImageScaleMode.ScaleToFit, image.ScaleMode);
        Assert.Equal(Rectangle.Empty, image.SourceRect);
        Assert.True(image.PreserveAspect);
    }

    [Fact]
    public void UIImage_Stretch_HasStretchMode()
    {
        var texture = new TextureHandle(456);
        var image = UIImage.Stretch(texture);

        Assert.Equal(texture, image.Texture);
        Assert.Equal(Vector4.One, image.Tint);
        Assert.Equal(ImageScaleMode.Stretch, image.ScaleMode);
        Assert.Equal(Rectangle.Empty, image.SourceRect);
        Assert.False(image.PreserveAspect);
    }

    [Fact]
    public void UIImage_FromAtlas_HasSourceRect()
    {
        var texture = new TextureHandle(789);
        var sourceRect = new Rectangle(10, 20, 100, 200);
        var image = UIImage.FromAtlas(texture, sourceRect);

        Assert.Equal(texture, image.Texture);
        Assert.Equal(Vector4.One, image.Tint);
        Assert.Equal(ImageScaleMode.ScaleToFit, image.ScaleMode);
        Assert.Equal(sourceRect, image.SourceRect);
        Assert.True(image.PreserveAspect);
    }

    #endregion

    #region UIStyle Tests

    [Fact]
    public void UIStyle_SolidColor_HasCorrectColor()
    {
        var color = new Vector4(0.5f, 0.3f, 0.8f, 1.0f);
        var style = UIStyle.SolidColor(color);

        Assert.Equal(color, style.BackgroundColor);
        Assert.Equal(TextureHandle.Invalid, style.BackgroundTexture);
    }

    [Fact]
    public void UIStyle_Transparent_HasZeroColor()
    {
        var style = UIStyle.Transparent;

        Assert.Equal(Vector4.Zero, style.BackgroundColor);
        Assert.Equal(TextureHandle.Invalid, style.BackgroundTexture);
    }

    [Fact]
    public void UIStyle_BorderOnly_HasBorderAndNoBg()
    {
        var borderColor = new Vector4(0.2f, 0.4f, 0.6f, 1.0f);
        var style = UIStyle.BorderOnly(borderColor, 2.5f);

        Assert.Equal(Vector4.Zero, style.BackgroundColor);
        Assert.Equal(TextureHandle.Invalid, style.BackgroundTexture);
        Assert.Equal(borderColor, style.BorderColor);
        Assert.Equal(2.5f, style.BorderWidth);
    }

    #endregion

    #region UIScrollable Tests

    [Fact]
    public void UIScrollable_Vertical_HasCorrectSettings()
    {
        var scrollable = UIScrollable.Vertical(25f);

        Assert.False(scrollable.HorizontalScroll);
        Assert.True(scrollable.VerticalScroll);
        Assert.Equal(Vector2.Zero, scrollable.ScrollPosition);
        Assert.Equal(25f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_VerticalWithDefaultSensitivity_Uses20()
    {
        var scrollable = UIScrollable.Vertical();

        Assert.True(scrollable.VerticalScroll);
        Assert.Equal(20f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_Horizontal_HasCorrectSettings()
    {
        var scrollable = UIScrollable.Horizontal(30f);

        Assert.True(scrollable.HorizontalScroll);
        Assert.False(scrollable.VerticalScroll);
        Assert.Equal(Vector2.Zero, scrollable.ScrollPosition);
        Assert.Equal(30f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_HorizontalWithDefaultSensitivity_Uses20()
    {
        var scrollable = UIScrollable.Horizontal();

        Assert.True(scrollable.HorizontalScroll);
        Assert.Equal(20f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_Both_HasCorrectSettings()
    {
        var scrollable = UIScrollable.Both(15f);

        Assert.True(scrollable.HorizontalScroll);
        Assert.True(scrollable.VerticalScroll);
        Assert.Equal(Vector2.Zero, scrollable.ScrollPosition);
        Assert.Equal(15f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_BothWithDefaultSensitivity_Uses20()
    {
        var scrollable = UIScrollable.Both();

        Assert.True(scrollable.HorizontalScroll);
        Assert.True(scrollable.VerticalScroll);
        Assert.Equal(20f, scrollable.ScrollSensitivity);
    }

    [Fact]
    public void UIScrollable_GetMaxScroll_ReturnsCorrectValues()
    {
        var scrollable = new UIScrollable
        {
            ContentSize = new Vector2(1000, 800)
        };

        var maxScroll = scrollable.GetMaxScroll(new Vector2(400, 300));

        Assert.Equal(600f, maxScroll.X);
        Assert.Equal(500f, maxScroll.Y);
    }

    [Fact]
    public void UIScrollable_GetMaxScroll_ReturnsZeroWhenContentFits()
    {
        var scrollable = new UIScrollable
        {
            ContentSize = new Vector2(100, 100)
        };

        var maxScroll = scrollable.GetMaxScroll(new Vector2(400, 300));

        Assert.Equal(0f, maxScroll.X);
        Assert.Equal(0f, maxScroll.Y);
    }

    [Fact]
    public void UIScrollable_ClampScrollPosition_ClampsToValidBounds()
    {
        var scrollable = new UIScrollable
        {
            ContentSize = new Vector2(1000, 800),
            ScrollPosition = new Vector2(700, 600)
        };

        scrollable.ClampScrollPosition(new Vector2(400, 300));

        Assert.Equal(600f, scrollable.ScrollPosition.X);
        Assert.Equal(500f, scrollable.ScrollPosition.Y);
    }

    [Fact]
    public void UIScrollable_ClampScrollPosition_ClampsNegativeToZero()
    {
        var scrollable = new UIScrollable
        {
            ContentSize = new Vector2(1000, 800),
            ScrollPosition = new Vector2(-50, -100)
        };

        scrollable.ClampScrollPosition(new Vector2(400, 300));

        Assert.Equal(0f, scrollable.ScrollPosition.X);
        Assert.Equal(0f, scrollable.ScrollPosition.Y);
    }

    #endregion

    #region UIScrollbarThumb Tests

    [Fact]
    public void UIScrollbarThumb_Constructor_SetsValues()
    {
        var scrollView = new Entity(123, 1);
        var thumb = new UIScrollbarThumb(scrollView, true);

        Assert.Equal(scrollView, thumb.ScrollView);
        Assert.True(thumb.IsVertical);
    }

    [Fact]
    public void UIScrollbarThumb_Horizontal_HasCorrectOrientation()
    {
        var scrollView = new Entity(456, 2);
        var thumb = new UIScrollbarThumb(scrollView, false);

        Assert.Equal(scrollView, thumb.ScrollView);
        Assert.False(thumb.IsVertical);
    }

    #endregion

    #region UIEdges Tests

    [Fact]
    public void UIEdges_All_SetsAllSidesToSameValue()
    {
        var edges = UIEdges.All(10f);

        Assert.Equal(10f, edges.Left);
        Assert.Equal(10f, edges.Top);
        Assert.Equal(10f, edges.Right);
        Assert.Equal(10f, edges.Bottom);
    }

    [Fact]
    public void UIEdges_Symmetric_SetsHorizontalAndVertical()
    {
        var edges = UIEdges.Symmetric(15f, 20f);

        Assert.Equal(15f, edges.Left);
        Assert.Equal(20f, edges.Top);
        Assert.Equal(15f, edges.Right);
        Assert.Equal(20f, edges.Bottom);
    }

    [Fact]
    public void UIEdges_Symmetric_HorizontalSizeIsCorrect()
    {
        var edges = UIEdges.Symmetric(15f, 20f);

        Assert.Equal(30f, edges.HorizontalSize);
    }

    [Fact]
    public void UIEdges_Symmetric_VerticalSizeIsCorrect()
    {
        var edges = UIEdges.Symmetric(15f, 20f);

        Assert.Equal(40f, edges.VerticalSize);
    }

    #endregion

    #region UI Event Tests

    [Fact]
    public void UIClickEvent_Constructor_SetsAllProperties()
    {
        var element = new Entity(1, 1);
        var position = new Vector2(100, 200);
        var evt = new UIClickEvent(element, position, MouseButton.Left);

        Assert.Equal(element, evt.Element);
        Assert.Equal(position, evt.Position);
        Assert.Equal(MouseButton.Left, evt.Button);
    }

    [Fact]
    public void UIPointerEnterEvent_Constructor_SetsProperties()
    {
        var element = new Entity(2, 1);
        var position = new Vector2(50, 75);
        var evt = new UIPointerEnterEvent(element, position);

        Assert.Equal(element, evt.Element);
        Assert.Equal(position, evt.Position);
    }

    [Fact]
    public void UIPointerExitEvent_Constructor_SetsElement()
    {
        var element = new Entity(3, 1);
        var evt = new UIPointerExitEvent(element);

        Assert.Equal(element, evt.Element);
    }

    [Fact]
    public void UIFocusGainedEvent_Constructor_SetsProperties()
    {
        var element = new Entity(4, 1);
        var previous = new Entity(5, 1);
        var evt = new UIFocusGainedEvent(element, previous);

        Assert.Equal(element, evt.Element);
        Assert.Equal(previous, evt.Previous);
    }

    [Fact]
    public void UIFocusGainedEvent_WithNullPrevious_AllowsNull()
    {
        var element = new Entity(4, 1);
        var evt = new UIFocusGainedEvent(element, null);

        Assert.Equal(element, evt.Element);
        Assert.Null(evt.Previous);
    }

    [Fact]
    public void UIFocusLostEvent_Constructor_SetsProperties()
    {
        var element = new Entity(6, 1);
        var next = new Entity(7, 1);
        var evt = new UIFocusLostEvent(element, next);

        Assert.Equal(element, evt.Element);
        Assert.Equal(next, evt.Next);
    }

    [Fact]
    public void UIDragStartEvent_Constructor_SetsProperties()
    {
        var element = new Entity(8, 1);
        var startPos = new Vector2(25, 50);
        var evt = new UIDragStartEvent(element, startPos);

        Assert.Equal(element, evt.Element);
        Assert.Equal(startPos, evt.StartPosition);
    }

    [Fact]
    public void UIDragEvent_Constructor_SetsProperties()
    {
        var element = new Entity(9, 1);
        var position = new Vector2(100, 100);
        var delta = new Vector2(5, -3);
        var evt = new UIDragEvent(element, position, delta);

        Assert.Equal(element, evt.Element);
        Assert.Equal(position, evt.Position);
        Assert.Equal(delta, evt.Delta);
    }

    [Fact]
    public void UIDragEndEvent_Constructor_SetsProperties()
    {
        var element = new Entity(10, 1);
        var endPos = new Vector2(150, 200);
        var evt = new UIDragEndEvent(element, endPos);

        Assert.Equal(element, evt.Element);
        Assert.Equal(endPos, evt.EndPosition);
    }

    [Fact]
    public void UIValueChangedEvent_Constructor_SetsProperties()
    {
        var element = new Entity(11, 1);
        var evt = new UIValueChangedEvent(element, 5, 10);

        Assert.Equal(element, evt.Element);
        Assert.Equal(5, evt.OldValue);
        Assert.Equal(10, evt.NewValue);
    }

    [Fact]
    public void UIValueChangedEvent_WithNullValues_AllowsNull()
    {
        var element = new Entity(11, 1);
        var evt = new UIValueChangedEvent(element, null, null);

        Assert.Equal(element, evt.Element);
        Assert.Null(evt.OldValue);
        Assert.Null(evt.NewValue);
    }

    [Fact]
    public void UISubmitEvent_Constructor_SetsElement()
    {
        var element = new Entity(12, 1);
        var evt = new UISubmitEvent(element);

        Assert.Equal(element, evt.Element);
    }

    [Fact]
    public void UIWindowClosedEvent_Constructor_SetsWindow()
    {
        var window = new Entity(13, 1);
        var evt = new UIWindowClosedEvent(window);

        Assert.Equal(window, evt.Window);
    }

    [Fact]
    public void UISplitterChangedEvent_Constructor_SetsProperties()
    {
        var splitter = new Entity(14, 1);
        var evt = new UISplitterChangedEvent(splitter, 0.3f, 0.5f);

        Assert.Equal(splitter, evt.Splitter);
        Assert.Equal(0.3f, evt.OldRatio);
        Assert.Equal(0.5f, evt.NewRatio);
    }

    [Fact]
    public void UITooltipShowEvent_Constructor_SetsProperties()
    {
        var element = new Entity(15, 1);
        var position = new Vector2(300, 400);
        var evt = new UITooltipShowEvent(element, "Tooltip text", position);

        Assert.Equal(element, evt.Element);
        Assert.Equal("Tooltip text", evt.Text);
        Assert.Equal(position, evt.Position);
    }

    [Fact]
    public void UITooltipHideEvent_Constructor_SetsElement()
    {
        var element = new Entity(16, 1);
        var evt = new UITooltipHideEvent(element);

        Assert.Equal(element, evt.Element);
    }

    [Fact]
    public void UIMenuItemClickEvent_Constructor_SetsProperties()
    {
        var menuItem = new Entity(17, 1);
        var menu = new Entity(18, 1);
        var evt = new UIMenuItemClickEvent(menuItem, menu, "item-1", 3);

        Assert.Equal(menuItem, evt.MenuItem);
        Assert.Equal(menu, evt.Menu);
        Assert.Equal("item-1", evt.ItemId);
        Assert.Equal(3, evt.Index);
    }

    [Fact]
    public void UITreeNodeSelectedEvent_Constructor_SetsProperties()
    {
        var node = new Entity(19, 1);
        var treeView = new Entity(20, 1);
        var evt = new UITreeNodeSelectedEvent(node, treeView);

        Assert.Equal(node, evt.Node);
        Assert.Equal(treeView, evt.TreeView);
    }

    [Fact]
    public void UIPropertyChangedEvent_Constructor_SetsProperties()
    {
        var grid = new Entity(21, 1);
        var row = new Entity(22, 1);
        var evt = new UIPropertyChangedEvent(grid, row, "PropertyName", "old", "new");

        Assert.Equal(grid, evt.PropertyGrid);
        Assert.Equal(row, evt.Row);
        Assert.Equal("PropertyName", evt.PropertyName);
        Assert.Equal("old", evt.OldValue);
        Assert.Equal("new", evt.NewValue);
    }

    [Fact]
    public void UIModalClosedEvent_Constructor_SetsProperties()
    {
        var modal = new Entity(23, 1);
        var evt = new UIModalClosedEvent(modal, ModalResult.OK);

        Assert.Equal(modal, evt.Modal);
        Assert.Equal(ModalResult.OK, evt.Result);
    }

    [Fact]
    public void UIToastDismissedEvent_Constructor_SetsProperties()
    {
        var toast = new Entity(24, 1);
        var evt = new UIToastDismissedEvent(toast, true);

        Assert.Equal(toast, evt.Toast);
        Assert.True(evt.WasManual);
    }

    [Fact]
    public void UIRadialSliceSelectedEvent_Constructor_SetsProperties()
    {
        var slice = new Entity(25, 1);
        var menu = new Entity(26, 1);
        var evt = new UIRadialSliceSelectedEvent(slice, menu, "slice-1", 2);

        Assert.Equal(slice, evt.Slice);
        Assert.Equal(menu, evt.Menu);
        Assert.Equal("slice-1", evt.ItemId);
        Assert.Equal(2, evt.Index);
    }

    #endregion

    #region UIWindow Tests

    [Fact]
    public void UIWindow_Constructor_SetsTitle()
    {
        var window = new UIWindow("Test Window");

        Assert.Equal("Test Window", window.Title);
        Assert.True(window.CanDrag);
        Assert.False(window.CanResize);
        Assert.True(window.CanClose);
        Assert.False(window.CanMinimize);
        Assert.False(window.CanMaximize);
        Assert.Equal(WindowState.Normal, window.State);
        Assert.Equal(0, window.ZOrder);
    }

    [Fact]
    public void UIWindow_DefaultValues_AreCorrect()
    {
        var window = new UIWindow("Window");

        Assert.Equal(new Vector2(100, 50), window.MinSize);
        Assert.Equal(Vector2.Zero, window.MaxSize);
        Assert.Equal(Vector2.Zero, window.RestorePosition);
        Assert.Equal(Vector2.Zero, window.RestoreSize);
        Assert.Equal(Entity.Null, window.ContentPanel);
        Assert.Equal(Entity.Null, window.TitleBar);
    }

    [Fact]
    public void UIWindowTitleBar_Constructor_SetsWindow()
    {
        var window = new Entity(100, 1);
        var titleBar = new UIWindowTitleBar(window);

        Assert.Equal(window, titleBar.Window);
    }

    [Fact]
    public void UIWindowCloseButton_Constructor_SetsWindow()
    {
        var window = new Entity(101, 1);
        var closeButton = new UIWindowCloseButton(window);

        Assert.Equal(window, closeButton.Window);
    }

    [Fact]
    public void UIWindowMinimizeButton_Constructor_SetsWindow()
    {
        var window = new Entity(102, 1);
        var minimizeButton = new UIWindowMinimizeButton(window);

        Assert.Equal(window, minimizeButton.Window);
    }

    [Fact]
    public void UIWindowMaximizeButton_Constructor_SetsWindow()
    {
        var window = new Entity(103, 1);
        var maximizeButton = new UIWindowMaximizeButton(window);

        Assert.Equal(window, maximizeButton.Window);
    }

    [Fact]
    public void UIWindowResizeHandle_Constructor_SetsProperties()
    {
        var window = new Entity(104, 1);
        var handle = new UIWindowResizeHandle(window, ResizeEdge.BottomRight);

        Assert.Equal(window, handle.Window);
        Assert.Equal(ResizeEdge.BottomRight, handle.Edge);
    }

    [Fact]
    public void ResizeEdge_TopLeft_IsCombinedFlag()
    {
        Assert.Equal(ResizeEdge.Top | ResizeEdge.Left, ResizeEdge.TopLeft);
    }

    [Fact]
    public void ResizeEdge_BottomRight_IsCombinedFlag()
    {
        Assert.Equal(ResizeEdge.Bottom | ResizeEdge.Right, ResizeEdge.BottomRight);
    }

    #endregion

    #region UIModal Tests

    [Fact]
    public void UIModal_Constructor_SetsTitle()
    {
        var modal = new UIModal("Confirm Action");

        Assert.Equal("Confirm Action", modal.Title);
        Assert.True(modal.CloseOnBackdropClick);
        Assert.True(modal.CloseOnEscape);
        Assert.False(modal.IsOpen);
        Assert.Equal(Entity.Null, modal.Backdrop);
        Assert.Equal(Entity.Null, modal.ContentContainer);
    }

    [Fact]
    public void UIModal_ConstructorWithParams_SetsAllValues()
    {
        var modal = new UIModal("Alert", closeOnBackdropClick: false, closeOnEscape: false);

        Assert.Equal("Alert", modal.Title);
        Assert.False(modal.CloseOnBackdropClick);
        Assert.False(modal.CloseOnEscape);
    }

    [Fact]
    public void UIModalBackdrop_Constructor_SetsModal()
    {
        var modal = new Entity(200, 1);
        var backdrop = new UIModalBackdrop(modal);

        Assert.Equal(modal, backdrop.Modal);
    }

    [Fact]
    public void UIModalCloseButton_Constructor_SetsModal()
    {
        var modal = new Entity(201, 1);
        var closeButton = new UIModalCloseButton(modal);

        Assert.Equal(modal, closeButton.Modal);
    }

    [Fact]
    public void UIModalButton_Constructor_SetsProperties()
    {
        var modal = new Entity(202, 1);
        var button = new UIModalButton(modal, ModalResult.OK);

        Assert.Equal(modal, button.Modal);
        Assert.Equal(ModalResult.OK, button.Result);
    }

    [Fact]
    public void ModalResult_Values_AreCorrect()
    {
        Assert.Equal(0, (int)ModalResult.None);
        Assert.Equal(1, (int)ModalResult.OK);
        Assert.Equal(2, (int)ModalResult.Cancel);
        Assert.Equal(3, (int)ModalResult.Yes);
        Assert.Equal(4, (int)ModalResult.No);
        Assert.Equal(100, (int)ModalResult.Custom1);
        Assert.Equal(101, (int)ModalResult.Custom2);
        Assert.Equal(102, (int)ModalResult.Custom3);
    }

    #endregion

    #region WindowState Enum Tests

    [Fact]
    public void WindowState_Values_AreCorrect()
    {
        Assert.Equal(0, (int)WindowState.Normal);
        Assert.Equal(1, (int)WindowState.Minimized);
        Assert.Equal(2, (int)WindowState.Maximized);
    }

    #endregion
}
