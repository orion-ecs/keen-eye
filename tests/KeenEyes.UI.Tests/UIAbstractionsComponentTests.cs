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

    #region UITextInput Tests

    [Fact]
    public void UITextInput_SingleLine_CreatesWithDefaultValues()
    {
        var input = UITextInput.SingleLine();

        Assert.Equal(0, input.CursorPosition);
        Assert.Equal(0, input.SelectionStart);
        Assert.Equal(0, input.SelectionEnd);
        Assert.False(input.IsEditing);
        Assert.Equal(0, input.MaxLength);
        Assert.False(input.Multiline);
        Assert.Equal("", input.PlaceholderText);
        Assert.False(input.ShowingPlaceholder); // No placeholder text = not showing placeholder
    }

    [Fact]
    public void UITextInput_SingleLine_WithPlaceholder_SetsPlaceholder()
    {
        var input = UITextInput.SingleLine("Enter name");

        Assert.Equal("Enter name", input.PlaceholderText);
        Assert.False(input.Multiline);
    }

    [Fact]
    public void UITextInput_SingleLine_WithMaxLength_SetsMaxLength()
    {
        var input = UITextInput.SingleLine("", 100);

        Assert.Equal(100, input.MaxLength);
        Assert.False(input.Multiline);
    }

    [Fact]
    public void UITextInput_MultiLine_CreatesWithMultilineTrue()
    {
        var input = UITextInput.MultiLine();

        Assert.Equal(0, input.CursorPosition);
        Assert.Equal(0, input.SelectionStart);
        Assert.Equal(0, input.SelectionEnd);
        Assert.False(input.IsEditing);
        Assert.Equal(0, input.MaxLength);
        Assert.True(input.Multiline);
        Assert.Equal("", input.PlaceholderText);
        Assert.False(input.ShowingPlaceholder); // No placeholder text = not showing placeholder
    }

    [Fact]
    public void UITextInput_MultiLine_WithPlaceholder_SetsPlaceholder()
    {
        var input = UITextInput.MultiLine("Enter description");

        Assert.Equal("Enter description", input.PlaceholderText);
        Assert.True(input.Multiline);
    }

    [Fact]
    public void UITextInput_MultiLine_WithMaxLength_SetsMaxLength()
    {
        var input = UITextInput.MultiLine("", 500);

        Assert.Equal(500, input.MaxLength);
        Assert.True(input.Multiline);
    }

    [Fact]
    public void UITextInput_HasSelection_WhenSelectionStartDiffersFromEnd_ReturnsTrue()
    {
        var input = new UITextInput
        {
            SelectionStart = 5,
            SelectionEnd = 10
        };

        Assert.True(input.HasSelection);
    }

    [Fact]
    public void UITextInput_HasSelection_WhenSelectionStartEqualsEnd_ReturnsFalse()
    {
        var input = new UITextInput
        {
            SelectionStart = 5,
            SelectionEnd = 5
        };

        Assert.False(input.HasSelection);
    }

    [Fact]
    public void UITextInput_GetSelectionRange_WhenStartLessThanEnd_ReturnsInOrder()
    {
        var input = new UITextInput
        {
            SelectionStart = 3,
            SelectionEnd = 8
        };

        var (start, end) = input.GetSelectionRange();

        Assert.Equal(3, start);
        Assert.Equal(8, end);
    }

    [Fact]
    public void UITextInput_GetSelectionRange_WhenStartGreaterThanEnd_ReturnsNormalized()
    {
        var input = new UITextInput
        {
            SelectionStart = 10,
            SelectionEnd = 2
        };

        var (start, end) = input.GetSelectionRange();

        Assert.Equal(2, start);
        Assert.Equal(10, end);
    }

    [Fact]
    public void UITextInput_ClearSelection_SetsStartAndEndToCursorPosition()
    {
        var input = new UITextInput
        {
            CursorPosition = 15,
            SelectionStart = 5,
            SelectionEnd = 20
        };

        input.ClearSelection();

        Assert.Equal(15, input.SelectionStart);
        Assert.Equal(15, input.SelectionEnd);
        Assert.False(input.HasSelection);
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

    #region UI Event Equality Tests

    [Fact]
    public void UIClickEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(1, 1);
        var position = new Vector2(100, 200);
        var evt1 = new UIClickEvent(element, position, MouseButton.Left);
        var evt2 = new UIClickEvent(element, position, MouseButton.Left);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.False(evt1 != evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIClickEvent_Equals_ReturnsFalseForDifferentButton()
    {
        var element = new Entity(1, 1);
        var position = new Vector2(100, 200);
        var evt1 = new UIClickEvent(element, position, MouseButton.Left);
        var evt2 = new UIClickEvent(element, position, MouseButton.Right);

        Assert.False(evt1.Equals(evt2));
        Assert.False(evt1 == evt2);
        Assert.True(evt1 != evt2);
    }

    [Fact]
    public void UIClickEvent_ToString_ContainsValues()
    {
        var element = new Entity(1, 1);
        var evt = new UIClickEvent(element, new Vector2(100, 200), MouseButton.Left);
        var str = evt.ToString();

        Assert.Contains("Element", str);
        Assert.Contains("Position", str);
        Assert.Contains("Button", str);
    }

    [Fact]
    public void UIPointerEnterEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(2, 1);
        var position = new Vector2(50, 75);
        var evt1 = new UIPointerEnterEvent(element, position);
        var evt2 = new UIPointerEnterEvent(element, position);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPointerExitEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(3, 1);
        var evt1 = new UIPointerExitEvent(element);
        var evt2 = new UIPointerExitEvent(element);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIFocusGainedEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(4, 1);
        var previous = new Entity(5, 1);
        var evt1 = new UIFocusGainedEvent(element, previous);
        var evt2 = new UIFocusGainedEvent(element, previous);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIFocusLostEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(6, 1);
        var next = new Entity(7, 1);
        var evt1 = new UIFocusLostEvent(element, next);
        var evt2 = new UIFocusLostEvent(element, next);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDragStartEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(8, 1);
        var startPos = new Vector2(25, 50);
        var evt1 = new UIDragStartEvent(element, startPos);
        var evt2 = new UIDragStartEvent(element, startPos);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDragEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(9, 1);
        var position = new Vector2(100, 100);
        var delta = new Vector2(5, -3);
        var evt1 = new UIDragEvent(element, position, delta);
        var evt2 = new UIDragEvent(element, position, delta);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDragEndEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(10, 1);
        var endPos = new Vector2(150, 200);
        var evt1 = new UIDragEndEvent(element, endPos);
        var evt2 = new UIDragEndEvent(element, endPos);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIValueChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(11, 1);
        var evt1 = new UIValueChangedEvent(element, 5, 10);
        var evt2 = new UIValueChangedEvent(element, 5, 10);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UISubmitEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(12, 1);
        var evt1 = new UISubmitEvent(element);
        var evt2 = new UISubmitEvent(element);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIWindowClosedEvent_Equals_ReturnsTrueForSameValues()
    {
        var window = new Entity(13, 1);
        var evt1 = new UIWindowClosedEvent(window);
        var evt2 = new UIWindowClosedEvent(window);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UISplitterChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var splitter = new Entity(14, 1);
        var evt1 = new UISplitterChangedEvent(splitter, 0.3f, 0.5f);
        var evt2 = new UISplitterChangedEvent(splitter, 0.3f, 0.5f);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITooltipShowEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(15, 1);
        var position = new Vector2(300, 400);
        var evt1 = new UITooltipShowEvent(element, "Tooltip", position);
        var evt2 = new UITooltipShowEvent(element, "Tooltip", position);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITooltipHideEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(16, 1);
        var evt1 = new UITooltipHideEvent(element);
        var evt2 = new UITooltipHideEvent(element);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPopoverOpenedEvent_Equals_ReturnsTrueForSameValues()
    {
        var popover = new Entity(17, 1);
        var trigger = new Entity(18, 1);
        var evt1 = new UIPopoverOpenedEvent(popover, trigger);
        var evt2 = new UIPopoverOpenedEvent(popover, trigger);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPopoverClosedEvent_Equals_ReturnsTrueForSameValues()
    {
        var popover = new Entity(19, 1);
        var evt1 = new UIPopoverClosedEvent(popover);
        var evt2 = new UIPopoverClosedEvent(popover);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIMenuItemClickEvent_Equals_ReturnsTrueForSameValues()
    {
        var menuItem = new Entity(20, 1);
        var menu = new Entity(21, 1);
        var evt1 = new UIMenuItemClickEvent(menuItem, menu, "item-1", 3);
        var evt2 = new UIMenuItemClickEvent(menuItem, menu, "item-1", 3);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIMenuOpenedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(22, 1);
        var parent = new Entity(23, 1);
        var evt1 = new UIMenuOpenedEvent(menu, parent);
        var evt2 = new UIMenuOpenedEvent(menu, parent);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIMenuClosedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(24, 1);
        var evt1 = new UIMenuClosedEvent(menu);
        var evt2 = new UIMenuClosedEvent(menu);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIMenuToggleChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menuItem = new Entity(25, 1);
        var evt1 = new UIMenuToggleChangedEvent(menuItem, true);
        var evt2 = new UIMenuToggleChangedEvent(menuItem, true);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIContextMenuRequestEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(26, 1);
        var target = new Entity(27, 1);
        var position = new Vector2(100, 200);
        var evt1 = new UIContextMenuRequestEvent(menu, position, target);
        var evt2 = new UIContextMenuRequestEvent(menu, position, target);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIRadialMenuOpenedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(28, 1);
        var position = new Vector2(150, 250);
        var evt1 = new UIRadialMenuOpenedEvent(menu, position);
        var evt2 = new UIRadialMenuOpenedEvent(menu, position);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIRadialMenuClosedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(29, 1);
        var evt1 = new UIRadialMenuClosedEvent(menu, false);
        var evt2 = new UIRadialMenuClosedEvent(menu, false);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIRadialSliceChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(30, 1);
        var evt1 = new UIRadialSliceChangedEvent(menu, 0, 1);
        var evt2 = new UIRadialSliceChangedEvent(menu, 0, 1);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIRadialSliceSelectedEvent_Equals_ReturnsTrueForSameValues()
    {
        var slice = new Entity(31, 1);
        var menu = new Entity(32, 1);
        var evt1 = new UIRadialSliceSelectedEvent(slice, menu, "slice-1", 2);
        var evt2 = new UIRadialSliceSelectedEvent(slice, menu, "slice-1", 2);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIRadialMenuRequestEvent_Equals_ReturnsTrueForSameValues()
    {
        var menu = new Entity(33, 1);
        var position = new Vector2(200, 300);
        var evt1 = new UIRadialMenuRequestEvent(menu, position);
        var evt2 = new UIRadialMenuRequestEvent(menu, position);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockPanelDockedEvent_Equals_ReturnsTrueForSameValues()
    {
        var panel = new Entity(34, 1);
        var container = new Entity(35, 1);
        var evt1 = new UIDockPanelDockedEvent(panel, DockZone.Left, container);
        var evt2 = new UIDockPanelDockedEvent(panel, DockZone.Left, container);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockPanelUndockedEvent_Equals_ReturnsTrueForSameValues()
    {
        var panel = new Entity(36, 1);
        var evt1 = new UIDockPanelUndockedEvent(panel, DockZone.Right);
        var evt2 = new UIDockPanelUndockedEvent(panel, DockZone.Right);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockStateChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var panel = new Entity(37, 1);
        var evt1 = new UIDockStateChangedEvent(panel, DockState.Floating, DockState.Docked);
        var evt2 = new UIDockStateChangedEvent(panel, DockState.Floating, DockState.Docked);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockZoneResizedEvent_Equals_ReturnsTrueForSameValues()
    {
        var zone = new Entity(38, 1);
        var evt1 = new UIDockZoneResizedEvent(zone, 100f, 150f);
        var evt2 = new UIDockZoneResizedEvent(zone, 100f, 150f);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockTabChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var tabGroup = new Entity(39, 1);
        var evt1 = new UIDockTabChangedEvent(tabGroup, 0, 1);
        var evt2 = new UIDockTabChangedEvent(tabGroup, 0, 1);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDockRequestEvent_Equals_ReturnsTrueForSameValues()
    {
        var panel = new Entity(40, 1);
        var container = new Entity(41, 1);
        var evt1 = new UIDockRequestEvent(panel, DockZone.Center, container);
        var evt2 = new UIDockRequestEvent(panel, DockZone.Center, container);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIFloatRequestEvent_Equals_ReturnsTrueForSameValues()
    {
        var panel = new Entity(42, 1);
        var position = new Vector2(50, 75);
        var evt1 = new UIFloatRequestEvent(panel, position);
        var evt2 = new UIFloatRequestEvent(panel, position);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITreeNodeSelectedEvent_Equals_ReturnsTrueForSameValues()
    {
        var node = new Entity(43, 1);
        var treeView = new Entity(44, 1);
        var evt1 = new UITreeNodeSelectedEvent(node, treeView);
        var evt2 = new UITreeNodeSelectedEvent(node, treeView);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITreeNodeExpandedEvent_Equals_ReturnsTrueForSameValues()
    {
        var node = new Entity(45, 1);
        var treeView = new Entity(46, 1);
        var evt1 = new UITreeNodeExpandedEvent(node, treeView);
        var evt2 = new UITreeNodeExpandedEvent(node, treeView);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITreeNodeCollapsedEvent_Equals_ReturnsTrueForSameValues()
    {
        var node = new Entity(47, 1);
        var treeView = new Entity(48, 1);
        var evt1 = new UITreeNodeCollapsedEvent(node, treeView);
        var evt2 = new UITreeNodeCollapsedEvent(node, treeView);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITreeNodeDoubleClickedEvent_Equals_ReturnsTrueForSameValues()
    {
        var node = new Entity(49, 1);
        var treeView = new Entity(50, 1);
        var evt1 = new UITreeNodeDoubleClickedEvent(node, treeView);
        var evt2 = new UITreeNodeDoubleClickedEvent(node, treeView);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPropertyChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(51, 1);
        var row = new Entity(52, 1);
        var evt1 = new UIPropertyChangedEvent(grid, row, "Prop", "old", "new");
        var evt2 = new UIPropertyChangedEvent(grid, row, "Prop", "old", "new");

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPropertyCategoryExpandedEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(53, 1);
        var category = new Entity(54, 1);
        var evt1 = new UIPropertyCategoryExpandedEvent(grid, category);
        var evt2 = new UIPropertyCategoryExpandedEvent(grid, category);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIPropertyCategoryCollapsedEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(55, 1);
        var category = new Entity(56, 1);
        var evt1 = new UIPropertyCategoryCollapsedEvent(grid, category);
        var evt2 = new UIPropertyCategoryCollapsedEvent(grid, category);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIModalOpenedEvent_Equals_ReturnsTrueForSameValues()
    {
        var modal = new Entity(57, 1);
        var evt1 = new UIModalOpenedEvent(modal);
        var evt2 = new UIModalOpenedEvent(modal);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIModalClosedEvent_Equals_ReturnsTrueForSameValues()
    {
        var modal = new Entity(58, 1);
        var evt1 = new UIModalClosedEvent(modal, ModalResult.Cancel);
        var evt2 = new UIModalClosedEvent(modal, ModalResult.Cancel);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIModalResultEvent_Equals_ReturnsTrueForSameValues()
    {
        var modal = new Entity(59, 1);
        var button = new Entity(60, 1);
        var evt1 = new UIModalResultEvent(modal, button, ModalResult.Yes);
        var evt2 = new UIModalResultEvent(modal, button, ModalResult.Yes);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIToastShownEvent_Equals_ReturnsTrueForSameValues()
    {
        var toast = new Entity(61, 1);
        var evt1 = new UIToastShownEvent(toast);
        var evt2 = new UIToastShownEvent(toast);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIToastDismissedEvent_Equals_ReturnsTrueForSameValues()
    {
        var toast = new Entity(62, 1);
        var evt1 = new UIToastDismissedEvent(toast, true);
        var evt2 = new UIToastDismissedEvent(toast, true);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIAccordionSectionExpandedEvent_Equals_ReturnsTrueForSameValues()
    {
        var accordion = new Entity(63, 1);
        var section = new Entity(64, 1);
        var evt1 = new UIAccordionSectionExpandedEvent(accordion, section);
        var evt2 = new UIAccordionSectionExpandedEvent(accordion, section);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIAccordionSectionCollapsedEvent_Equals_ReturnsTrueForSameValues()
    {
        var accordion = new Entity(65, 1);
        var section = new Entity(66, 1);
        var evt1 = new UIAccordionSectionCollapsedEvent(accordion, section);
        var evt2 = new UIAccordionSectionCollapsedEvent(accordion, section);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIWindowMinimizedEvent_Equals_ReturnsTrueForSameValues()
    {
        var window = new Entity(67, 1);
        var evt1 = new UIWindowMinimizedEvent(window);
        var evt2 = new UIWindowMinimizedEvent(window);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIWindowMaximizedEvent_Equals_ReturnsTrueForSameValues()
    {
        var window = new Entity(68, 1);
        var evt1 = new UIWindowMaximizedEvent(window);
        var evt2 = new UIWindowMaximizedEvent(window);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIWindowRestoredEvent_Equals_ReturnsTrueForSameValues()
    {
        var window = new Entity(69, 1);
        var evt1 = new UIWindowRestoredEvent(window, WindowState.Minimized);
        var evt2 = new UIWindowRestoredEvent(window, WindowState.Minimized);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIGridColumnResizedEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(70, 1);
        var column = new Entity(71, 1);
        var evt1 = new UIGridColumnResizedEvent(grid, column, 0, 100f, 150f);
        var evt2 = new UIGridColumnResizedEvent(grid, column, 0, 100f, 150f);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIGridRowSelectedEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(72, 1);
        var row = new Entity(73, 1);
        var evt1 = new UIGridRowSelectedEvent(grid, row, 5, true);
        var evt2 = new UIGridRowSelectedEvent(grid, row, 5, true);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIGridSortEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(74, 1);
        var column = new Entity(75, 1);
        var evt1 = new UIGridSortEvent(grid, column, 0, KeenEyes.UI.Abstractions.SortDirection.Ascending);
        var evt2 = new UIGridSortEvent(grid, column, 0, KeenEyes.UI.Abstractions.SortDirection.Ascending);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIGridRowDoubleClickEvent_Equals_ReturnsTrueForSameValues()
    {
        var grid = new Entity(76, 1);
        var row = new Entity(77, 1);
        var evt1 = new UIGridRowDoubleClickEvent(grid, row, 3);
        var evt2 = new UIGridRowDoubleClickEvent(grid, row, 3);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UITextChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var element = new Entity(78, 1);
        var evt1 = new UITextChangedEvent(element, "old", "new");
        var evt2 = new UITextChangedEvent(element, "old", "new");

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIColorChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var picker = new Entity(79, 1);
        var oldColor = new Vector4(1, 0, 0, 1);
        var newColor = new Vector4(0, 1, 0, 1);
        var evt1 = new UIColorChangedEvent(picker, oldColor, newColor);
        var evt2 = new UIColorChangedEvent(picker, oldColor, newColor);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UIDateChangedEvent_Equals_ReturnsTrueForSameValues()
    {
        var picker = new Entity(80, 1);
        var oldDate = new DateTime(2024, 1, 1);
        var newDate = new DateTime(2024, 12, 25);
        var evt1 = new UIDateChangedEvent(picker, oldDate, newDate);
        var evt2 = new UIDateChangedEvent(picker, oldDate, newDate);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    [Fact]
    public void UICalendarNavigatedEvent_Equals_ReturnsTrueForSameValues()
    {
        var picker = new Entity(81, 1);
        var evt1 = new UICalendarNavigatedEvent(picker, 2024, 12);
        var evt2 = new UICalendarNavigatedEvent(picker, 2024, 12);

        Assert.True(evt1.Equals(evt2));
        Assert.True(evt1 == evt2);
        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    #endregion
}
