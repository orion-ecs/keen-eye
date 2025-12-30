using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIDatePickerSystem date/time selection and calendar navigation.
/// </summary>
public class UIDatePickerSystemTests
{
    #region Component Tests

    [Fact]
    public void UIDatePicker_Initialization_SetsDisplayMonthYear()
    {
        var date = new DateTime(2024, 6, 15);
        var picker = new UIDatePicker(date);

        Assert.Equal(6, picker.DisplayMonth);
        Assert.Equal(2024, picker.DisplayYear);
    }

    [Fact]
    public void UIDatePicker_DefaultMode_IsDate()
    {
        var picker = new UIDatePicker(DateTime.Now);

        Assert.Equal(DatePickerMode.Date, picker.Mode);
    }

    [Fact]
    public void UIDatePicker_DefaultTimeFormat_Is24Hour()
    {
        var picker = new UIDatePicker(DateTime.Now);

        Assert.Equal(TimeFormat.Hour24, picker.TimeFormat);
    }

    [Fact]
    public void UIDatePicker_DefaultShowSeconds_IsFalse()
    {
        var picker = new UIDatePicker(DateTime.Now);

        Assert.False(picker.ShowSeconds);
    }

    [Fact]
    public void UICalendarDay_Initialization_SetsValues()
    {
        var pickerEntity = new Entity(1, 1);
        var calendarDay = new UICalendarDay(pickerEntity, 15, 6, 2024);

        Assert.Equal(pickerEntity, calendarDay.DatePicker);
        Assert.Equal(15, calendarDay.Day);
        Assert.Equal(6, calendarDay.Month);
        Assert.Equal(2024, calendarDay.Year);
    }

    [Fact]
    public void UITimeSpinner_Initialization_SetsField()
    {
        var pickerEntity = new Entity(1, 1);
        var spinner = new UITimeSpinner(pickerEntity, TimeField.Hour);

        Assert.Equal(pickerEntity, spinner.DatePicker);
        Assert.Equal(TimeField.Hour, spinner.Field);
    }

    #endregion

    #region API Tests

    [Fact]
    public void SetValue_ValidEntity_UpdatesValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        var newDate = new DateTime(2024, 6, 20);
        system.SetValue(picker, newDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(newDate, pickerData.Value);
    }

    [Fact]
    public void SetValue_UpdatesDisplayMonthYear()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        var newDate = new DateTime(2024, 6, 20);
        system.SetValue(picker, newDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(6, pickerData.DisplayMonth);
        Assert.Equal(2024, pickerData.DisplayYear);
    }

    [Fact]
    public void SetValue_RespectsMinDate()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 6, 10);
        var picker = CreateDatePickerWithConstraints(world, initialDate, minDate, null);
        layout.Update(0);

        var tooEarlyDate = new DateTime(2024, 6, 5);
        system.SetValue(picker, tooEarlyDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(minDate, pickerData.Value);
    }

    [Fact]
    public void SetValue_RespectsMaxDate()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var maxDate = new DateTime(2024, 6, 20);
        var picker = CreateDatePickerWithConstraints(world, initialDate, null, maxDate);
        layout.Update(0);

        var tooLateDate = new DateTime(2024, 6, 25);
        system.SetValue(picker, tooLateDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(maxDate, pickerData.Value);
    }

    [Fact]
    public void SetValue_InvalidEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        // Should not throw
        system.SetValue(Entity.Null, DateTime.Now);
    }

    [Fact]
    public void GetValue_ValidEntity_ReturnsValue()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var expectedDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePicker(world, expectedDate);
        layout.Update(0);

        var value = system.GetValue(picker);

        Assert.Equal(expectedDate, value);
    }

    [Fact]
    public void GetValue_InvalidEntity_ReturnsMinValue()
    {
        using var world = new World();
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var value = system.GetValue(Entity.Null);

        Assert.Equal(DateTime.MinValue, value);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void NavigateTo_SetsDisplayMonthYear()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        system.NavigateTo(picker, 2024, 6);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(6, pickerData.DisplayMonth);
        Assert.Equal(2024, pickerData.DisplayYear);
    }

    [Fact]
    public void NavigateTo_InvalidMonth_DoesNotChange()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        system.NavigateTo(picker, 2024, 13); // Invalid month

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(1, pickerData.DisplayMonth); // Unchanged
    }

    [Fact]
    public void NavigateToToday_SetsCurrentMonthYear()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2020, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        system.NavigateToToday(picker);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        var today = DateTime.Today;
        Assert.Equal(today.Month, pickerData.DisplayMonth);
        Assert.Equal(today.Year, pickerData.DisplayYear);
    }

    #endregion

    #region Calendar Day Click Tests

    [Fact]
    public void CalendarDay_Click_SelectsDate()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        // Find a day cell for day 20
        Entity? dayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20 && day.Month == 6 && day.Year == 2024 && !day.IsDisabled)
            {
                dayEntity = entity;
                break;
            }
        }

        Assert.NotNull(dayEntity);

        SimulateClick(world, dayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(20, pickerData.Value.Day);
        Assert.Equal(6, pickerData.Value.Month);
        Assert.Equal(2024, pickerData.Value.Year);
    }

    [Fact]
    public void CalendarDay_ClickDisabled_DoesNotSelectDate()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithConstraints(world, initialDate, new DateTime(2024, 6, 10), null);
        layout.Update(0);

        // Find a disabled day cell (before min date)
        var dayEntity = world.Spawn()
            .With(UIElement.Default)
            .With(new UICalendarDay(picker, 5, 6, 2024)
            {
                IsDisabled = true
            })
            .Build();

        SimulateClick(world, dayEntity, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(15, pickerData.Value.Day); // Unchanged
    }

    [Fact]
    public void CalendarDay_Click_PreservesTime()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 45);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        // Find a day cell for day 20
        Entity? dayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20 && day.Month == 6 && day.Year == 2024 && !day.IsDisabled)
            {
                dayEntity = entity;
                break;
            }
        }

        Assert.NotNull(dayEntity);

        SimulateClick(world, dayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(10, pickerData.Value.Hour);
        Assert.Equal(30, pickerData.Value.Minute);
        Assert.Equal(45, pickerData.Value.Second);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void SetValue_RaisesDateChangedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        UIDateChangedEvent? receivedEvent = null;
        world.Subscribe<UIDateChangedEvent>(evt => receivedEvent = evt);

        var newDate = new DateTime(2024, 6, 20);
        system.SetValue(picker, newDate);

        Assert.NotNull(receivedEvent);
        Assert.Equal(picker, receivedEvent.Value.Entity);
        Assert.Equal(initialDate, receivedEvent.Value.OldValue);
        Assert.Equal(newDate, receivedEvent.Value.NewValue);
    }

    [Fact]
    public void SetValue_SameValue_DoesNotRaiseEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, date);
        layout.Update(0);

        UIDateChangedEvent? receivedEvent = null;
        world.Subscribe<UIDateChangedEvent>(evt => receivedEvent = evt);

        system.SetValue(picker, date);

        Assert.Null(receivedEvent);
    }

    [Fact]
    public void NavigateTo_RaisesCalendarNavigatedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        UICalendarNavigatedEvent? receivedEvent = null;
        world.Subscribe<UICalendarNavigatedEvent>(evt => receivedEvent = evt);

        system.NavigateTo(picker, 2024, 6);

        Assert.NotNull(receivedEvent);
        Assert.Equal(picker, receivedEvent.Value.Entity);
        Assert.Equal(2024, receivedEvent.Value.Year);
        Assert.Equal(6, receivedEvent.Value.Month);
    }

    #endregion

    #region Static Helper Tests

    [Fact]
    public void GetDaysInMonth_ReturnsCorrectDays()
    {
        Assert.Equal(31, UIDatePickerSystem.GetDaysInMonth(2024, 1));  // January
        Assert.Equal(29, UIDatePickerSystem.GetDaysInMonth(2024, 2));  // February (leap year)
        Assert.Equal(28, UIDatePickerSystem.GetDaysInMonth(2023, 2));  // February (non-leap)
        Assert.Equal(30, UIDatePickerSystem.GetDaysInMonth(2024, 4));  // April
        Assert.Equal(31, UIDatePickerSystem.GetDaysInMonth(2024, 12)); // December
    }

    [Fact]
    public void GetFirstDayOfMonth_ReturnsCorrectDayOfWeek()
    {
        // June 2024 starts on Saturday
        Assert.Equal(DayOfWeek.Saturday, UIDatePickerSystem.GetFirstDayOfMonth(2024, 6));

        // January 2024 starts on Monday
        Assert.Equal(DayOfWeek.Monday, UIDatePickerSystem.GetFirstDayOfMonth(2024, 1));
    }

    #endregion

    #region Time Spinner Tests

    [Fact]
    public void TimeSpinner_HourClick_IncrementsHour()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Hour);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Hour);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(11, pickerData.Value.Hour);
    }

    [Fact]
    public void TimeSpinner_HourClick_WrapsAt24()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 23, 30, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Hour);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Hour);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(0, pickerData.Value.Hour);
        Assert.Equal(15, pickerData.Value.Day); // Same day
    }

    [Fact]
    public void TimeSpinner_MinuteClick_IncrementsMinute()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Minute);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Minute);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(31, pickerData.Value.Minute);
    }

    [Fact]
    public void TimeSpinner_MinuteClick_WrapsAt60()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 59, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Minute);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Minute);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(0, pickerData.Value.Minute);
        Assert.Equal(10, pickerData.Value.Hour); // Same hour
    }

    [Fact]
    public void TimeSpinner_SecondClick_IncrementsSecond()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 45);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Second);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Second);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(46, pickerData.Value.Second);
    }

    [Fact]
    public void TimeSpinner_SecondClick_WrapsAt60()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 59);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Second);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Second);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(0, pickerData.Value.Second);
        Assert.Equal(30, pickerData.Value.Minute); // Same minute
    }

    [Fact]
    public void TimeSpinner_AmPmClick_TogglesAmPm()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0); // AM
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.AmPm);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.AmPm);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(22, pickerData.Value.Hour); // PM
    }

    [Fact]
    public void TimeSpinner_AmPmClick_KeepsSameDay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 22, 30, 0); // PM
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.AmPm);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.AmPm);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(10, pickerData.Value.Hour); // AM
        Assert.Equal(15, pickerData.Value.Day); // Same day
    }

    [Fact]
    public void TimeSpinner_Click_RaisesDateChangedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Hour);
        layout.Update(0);

        UIDateChangedEvent? receivedEvent = null;
        world.Subscribe<UIDateChangedEvent>(evt => receivedEvent = evt);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Hour);
        Assert.NotNull(spinnerEntity);

        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(10, receivedEvent.Value.OldValue.Hour);
        Assert.Equal(11, receivedEvent.Value.NewValue.Hour);
    }

    [Fact]
    public void TimeSpinner_DeadPicker_IsIgnored()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePickerWithTimeSpinner(world, initialDate, TimeField.Hour);
        layout.Update(0);

        var spinnerEntity = FindTimeSpinner(world, picker, TimeField.Hour);
        Assert.NotNull(spinnerEntity);

        world.Despawn(picker);

        // Should not throw
        SimulateClick(world, spinnerEntity.Value, new Vector2(16, 16));
        system.Update(0);
    }

    #endregion

    #region Navigation Button Tests

    [Fact]
    public void PrevMonthButton_Click_NavigatesToPreviousMonth()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithNavigation(world, initialDate);
        layout.Update(0);

        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        var prevButton = pickerData.PrevMonthButton;

        SimulateClick(world, prevButton, new Vector2(16, 16));
        system.Update(0);

        ref readonly var updatedData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(5, updatedData.DisplayMonth);
        Assert.Equal(2024, updatedData.DisplayYear);
    }

    [Fact]
    public void PrevMonthButton_January_WrapsToDecember()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePickerWithNavigation(world, initialDate);
        layout.Update(0);

        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        var prevButton = pickerData.PrevMonthButton;

        SimulateClick(world, prevButton, new Vector2(16, 16));
        system.Update(0);

        ref readonly var updatedData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(12, updatedData.DisplayMonth);
        Assert.Equal(2023, updatedData.DisplayYear);
    }

    [Fact]
    public void NextMonthButton_Click_NavigatesToNextMonth()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithNavigation(world, initialDate);
        layout.Update(0);

        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        var nextButton = pickerData.NextMonthButton;

        SimulateClick(world, nextButton, new Vector2(16, 16));
        system.Update(0);

        ref readonly var updatedData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(7, updatedData.DisplayMonth);
        Assert.Equal(2024, updatedData.DisplayYear);
    }

    [Fact]
    public void NextMonthButton_December_WrapsToJanuary()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 12, 15);
        var picker = CreateDatePickerWithNavigation(world, initialDate);
        layout.Update(0);

        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        var nextButton = pickerData.NextMonthButton;

        SimulateClick(world, nextButton, new Vector2(16, 16));
        system.Update(0);

        ref readonly var updatedData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(1, updatedData.DisplayMonth);
        Assert.Equal(2025, updatedData.DisplayYear);
    }

    [Fact]
    public void Navigation_Click_RaisesCalendarNavigatedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithNavigation(world, initialDate);
        layout.Update(0);

        UICalendarNavigatedEvent? receivedEvent = null;
        world.Subscribe<UICalendarNavigatedEvent>(evt => receivedEvent = evt);

        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        var nextButton = pickerData.NextMonthButton;

        SimulateClick(world, nextButton, new Vector2(16, 16));
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(2024, receivedEvent.Value.Year);
        Assert.Equal(7, receivedEvent.Value.Month);
    }

    #endregion

    #region Calendar Day Overflow Tests

    [Fact]
    public void CalendarDay_ClickPreviousMonthDay_NavigatesToPreviousMonth()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        // Add a day from the previous month (May 31)
        var prevMonthDay = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 31, 5, 2024)
            {
                IsCurrentMonth = false,
                IsDisabled = false
            })
            .With(UIInteractable.Clickable())
            .Build();
        world.SetParent(prevMonthDay, picker);

        UICalendarNavigatedEvent? navigatedEvent = null;
        world.Subscribe<UICalendarNavigatedEvent>(evt => navigatedEvent = evt);

        SimulateClick(world, prevMonthDay, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(31, pickerData.Value.Day);
        Assert.Equal(5, pickerData.Value.Month);
        Assert.NotNull(navigatedEvent);
        Assert.Equal(5, navigatedEvent.Value.Month);
    }

    [Fact]
    public void CalendarDay_Click_MinDateConstraint_Rejected()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 6, 10);
        var picker = CreateDatePickerWithConstraints(world, initialDate, minDate, null);
        layout.Update(0);

        // Add a day before the min date
        var dayBeforeMin = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 5, 6, 2024)
            {
                IsCurrentMonth = true,
                IsDisabled = false // Not disabled from component perspective
            })
            .With(UIInteractable.Clickable())
            .Build();
        world.SetParent(dayBeforeMin, picker);

        SimulateClick(world, dayBeforeMin, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(15, pickerData.Value.Day); // Unchanged - rejected by min date
    }

    [Fact]
    public void CalendarDay_Click_MaxDateConstraint_Rejected()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var maxDate = new DateTime(2024, 6, 20);
        var picker = CreateDatePickerWithConstraints(world, initialDate, null, maxDate);
        layout.Update(0);

        // Add a day after the max date
        var dayAfterMax = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 25, 6, 2024)
            {
                IsCurrentMonth = true,
                IsDisabled = false
            })
            .With(UIInteractable.Clickable())
            .Build();
        world.SetParent(dayAfterMax, picker);

        SimulateClick(world, dayAfterMax, new Vector2(16, 16));
        system.Update(0);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(15, pickerData.Value.Day); // Unchanged - rejected by max date
    }

    #endregion

    #region Config Tests

    [Fact]
    public void DatePickerConfig_Default_HasExpectedValues()
    {
        var config = DatePickerConfig.Default;

        Assert.Null(config.InitialValue);
        Assert.Equal(DatePickerMode.Date, config.Mode);
        Assert.Equal(TimeFormat.Hour24, config.TimeFormat);
        Assert.False(config.ShowSeconds);
        Assert.Null(config.MinDate);
        Assert.Null(config.MaxDate);
    }

    [Fact]
    public void DatePickerConfig_DateOnly_HasDateMode()
    {
        var config = DatePickerConfig.DateOnly();

        Assert.Equal(DatePickerMode.Date, config.Mode);
    }

    [Fact]
    public void DatePickerConfig_TimeOnly_HasTimeMode()
    {
        var config = DatePickerConfig.TimeOnly();

        Assert.Equal(DatePickerMode.Time, config.Mode);
    }

    [Fact]
    public void DatePickerConfig_DateAndTime_HasDateTimeMode()
    {
        var config = DatePickerConfig.DateAndTime();

        Assert.Equal(DatePickerMode.DateTime, config.Mode);
    }

    [Fact]
    public void DatePickerConfig_WithRange_SetsMinMax()
    {
        var min = new DateTime(2024, 1, 1);
        var max = new DateTime(2024, 12, 31);
        var config = DatePickerConfig.WithRange(min, max);

        Assert.Equal(min, config.MinDate);
        Assert.Equal(max, config.MaxDate);
    }

    [Fact]
    public void DatePickerConfig_FutureOnly_SetsMinDateToToday()
    {
        var config = DatePickerConfig.FutureOnly();

        Assert.Equal(DateTime.Today, config.MinDate);
        Assert.Null(config.MaxDate);
    }

    [Fact]
    public void DatePickerConfig_PastOnly_SetsMaxDateToToday()
    {
        var config = DatePickerConfig.PastOnly();

        Assert.Null(config.MinDate);
        Assert.Equal(DateTime.Today, config.MaxDate);
    }

    #endregion

    #region Helper Methods

    private static UILayoutSystem SetupLayout(World world)
    {
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.Initialize(world);
        layoutSystem.SetScreenSize(800, 600);
        return layoutSystem;
    }

    private static Entity CreateDatePicker(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.Date
            })
            .Build();

        world.SetParent(picker, canvas);

        return picker;
    }

    private static Entity CreateDatePickerWithConstraints(
        World world,
        DateTime initialValue,
        DateTime? minDate,
        DateTime? maxDate)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.Date,
                MinDate = minDate,
                MaxDate = maxDate
            })
            .Build();

        world.SetParent(picker, canvas);

        return picker;
    }

    private static Entity CreateDatePickerWithDays(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.Date
            })
            .Build();

        world.SetParent(picker, canvas);

        // Create some calendar day cells
        for (int day = 1; day <= 30; day++)
        {
            var dayCell = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed((day % 7) * 32, (day / 7) * 32, 32, 32))
                .With(new UICalendarDay(picker, day, initialValue.Month, initialValue.Year)
                {
                    IsCurrentMonth = true,
                    IsSelected = day == initialValue.Day,
                    IsToday = day == DateTime.Today.Day &&
                              initialValue.Month == DateTime.Today.Month &&
                              initialValue.Year == DateTime.Today.Year,
                    IsDisabled = false
                })
                .With(UIInteractable.Clickable())
                .Build();

            world.SetParent(dayCell, picker);
        }

        return picker;
    }

    private static void SimulateClick(World world, Entity entity, Vector2 position)
    {
        var clickEvent = new UIClickEvent(entity, position, MouseButton.Left);
        world.Send(clickEvent);
    }

    private static Entity CreateDatePickerWithTimeSpinner(World world, DateTime initialValue, TimeField field)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.DateTime,
                TimeFormat = TimeFormat.Hour12
            })
            .Build();

        world.SetParent(picker, canvas);

        // Create time spinner
        var spinner = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 50, 30))
            .With(new UITimeSpinner(picker, field))
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(spinner, picker);

        return picker;
    }

    private static Entity? FindTimeSpinner(World world, Entity picker, TimeField field)
    {
        foreach (var entity in world.Query<UITimeSpinner>())
        {
            ref readonly var spinner = ref world.Get<UITimeSpinner>(entity);
            if (spinner.DatePicker == picker && spinner.Field == field)
            {
                return entity;
            }
        }
        return null;
    }

    private static Entity CreateDatePickerWithNavigation(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create navigation buttons
        var prevButton = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 30, 30))
            .With(UIInteractable.Clickable())
            .Build();

        var nextButton = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(250, 0, 30, 30))
            .With(UIInteractable.Clickable())
            .Build();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.Date,
                PrevMonthButton = prevButton,
                NextMonthButton = nextButton
            })
            .Build();

        world.SetParent(picker, canvas);
        world.SetParent(prevButton, picker);
        world.SetParent(nextButton, picker);

        return picker;
    }

    #endregion
}
