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

    #region Day Cell Update Tests

    [Fact]
    public void CalendarDay_UpdateDayCell_SetsIsTodayCorrectly()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var today = DateTime.Today;
        var picker = CreateDatePickerWithDays(world, today);
        layout.Update(0);

        // Find the day cell for today
        Entity? todayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == today.Day && day.Month == today.Month && day.Year == today.Year)
            {
                todayEntity = entity;
                break;
            }
        }

        Assert.NotNull(todayEntity);

        // Navigate away and back to trigger UpdateCalendarGrid
        system.NavigateTo(picker, today.Year, today.Month);

        ref readonly var dayData = ref world.Get<UICalendarDay>(todayEntity.Value);
        Assert.True(dayData.IsToday);
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_SetsIsSelectedCorrectly()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, date);
        layout.Update(0);

        // Find the day cell for selected day
        Entity? selectedEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 15 && day.Month == 6 && day.Year == 2024)
            {
                selectedEntity = entity;
                break;
            }
        }

        Assert.NotNull(selectedEntity);

        // Trigger an update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var dayData = ref world.Get<UICalendarDay>(selectedEntity.Value);
        Assert.True(dayData.IsSelected);
    }

    [Fact]
    public void CalendarDay_Click_UpdatesStyleForSelectedDay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithStyledDays(world, initialDate);
        layout.Update(0);

        // Find a day cell for day 20 with style
        Entity? dayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20 && day.Month == 6 && day.Year == 2024 && world.Has<UIStyle>(entity))
            {
                dayEntity = entity;
                break;
            }
        }

        Assert.NotNull(dayEntity);

        // Click to select the day
        SimulateClick(world, dayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        // Verify selection updated
        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(20, pickerData.Value.Day);
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_SetsDisabledForDateBeforeMin()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 6, 10);
        var picker = CreateDatePickerWithConstraints(world, date, minDate, null);
        layout.Update(0);

        // Create a day cell before min date
        var dayCell = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 5, 6, 2024)
            {
                IsCurrentMonth = true
            })
            .With(new UIStyle())
            .Build();
        world.SetParent(dayCell, picker);

        // Trigger calendar grid update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var dayData = ref world.Get<UICalendarDay>(dayCell);
        Assert.True(dayData.IsDisabled);
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_SetsDisabledForDateAfterMax()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var maxDate = new DateTime(2024, 6, 20);
        var picker = CreateDatePickerWithConstraints(world, date, null, maxDate);
        layout.Update(0);

        // Create a day cell after max date
        var dayCell = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 25, 6, 2024)
            {
                IsCurrentMonth = true
            })
            .With(new UIStyle())
            .Build();
        world.SetParent(dayCell, picker);

        // Trigger calendar grid update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var dayData = ref world.Get<UICalendarDay>(dayCell);
        Assert.True(dayData.IsDisabled);
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_UpdatesTextContent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var picker = CreateDatePicker(world, date);
        layout.Update(0);

        // Create a day cell with text
        var dayCell = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 20, 6, 2024)
            {
                IsCurrentMonth = true
            })
            .With(new UIText { Content = "" })
            .Build();
        world.SetParent(dayCell, picker);

        // Trigger calendar grid update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var text = ref world.Get<UIText>(dayCell);
        Assert.Equal("20", text.Content);
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_GraysOutDisabledText()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 6, 10);
        var picker = CreateDatePickerWithConstraints(world, date, minDate, null);
        layout.Update(0);

        // Create a disabled day cell with text
        var dayCell = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 5, 6, 2024)
            {
                IsCurrentMonth = true
            })
            .With(new UIText { Content = "5" })
            .Build();
        world.SetParent(dayCell, picker);

        // Trigger calendar grid update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var text = ref world.Get<UIText>(dayCell);
        Assert.Equal(0.5f, text.Color.X); // Grayed out
    }

    [Fact]
    public void CalendarDay_UpdateDayCell_GraysOutNonCurrentMonthText()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var date = new DateTime(2024, 6, 15);
        var picker = CreateDatePicker(world, date);
        layout.Update(0);

        // Create a day cell from previous month with text
        var dayCell = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 32, 32))
            .With(new UICalendarDay(picker, 31, 5, 2024)
            {
                IsCurrentMonth = false
            })
            .With(new UIText { Content = "31" })
            .Build();
        world.SetParent(dayCell, picker);

        // Trigger calendar grid update
        system.NavigateTo(picker, 2024, 6);

        ref readonly var text = ref world.Get<UIText>(dayCell);
        Assert.Equal(0.5f, text.Color.X); // Grayed out
    }

    #endregion

    #region Header Display Tests

    [Fact]
    public void SetValue_UpdatesHeaderDisplay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePickerWithHeader(world, initialDate);
        layout.Update(0);

        var newDate = new DateTime(2024, 6, 20);
        system.SetValue(picker, newDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HeaderEntity) && world.Has<UIText>(pickerData.HeaderEntity))
        {
            ref readonly var headerText = ref world.Get<UIText>(pickerData.HeaderEntity);
            Assert.Contains("June", headerText.Content);
            Assert.Contains("2024", headerText.Content);
        }
    }

    [Fact]
    public void NavigateTo_UpdatesHeaderDisplay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePickerWithHeader(world, initialDate);
        layout.Update(0);

        system.NavigateTo(picker, 2024, 12);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HeaderEntity) && world.Has<UIText>(pickerData.HeaderEntity))
        {
            ref readonly var headerText = ref world.Get<UIText>(pickerData.HeaderEntity);
            Assert.Contains("December", headerText.Content);
            Assert.Contains("2024", headerText.Content);
        }
    }

    [Fact]
    public void UpdateHeaderDisplay_DeadHeaderEntity_DoesNotThrow()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 1, 15);
        var picker = CreateDatePickerWithHeader(world, initialDate);
        layout.Update(0);

        // Destroy the header entity
        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HeaderEntity))
        {
            world.Despawn(pickerData.HeaderEntity);
        }

        // Should not throw
        system.NavigateTo(picker, 2024, 6);
    }

    #endregion

    #region Time Display Tests

    [Fact]
    public void UpdateTimeDisplay_Hour12Format_DisplaysCorrectHour()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 13, 30, 0); // 1 PM
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HourEntity) && world.Has<UIText>(pickerData.HourEntity))
        {
            ref readonly var hourText = ref world.Get<UIText>(pickerData.HourEntity);
            Assert.Equal("01", hourText.Content); // 12-hour format
        }
    }

    [Fact]
    public void UpdateTimeDisplay_Hour12Format_Midnight_DisplaysAs12()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 0, 30, 0); // Midnight
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HourEntity) && world.Has<UIText>(pickerData.HourEntity))
        {
            ref readonly var hourText = ref world.Get<UIText>(pickerData.HourEntity);
            Assert.Equal("12", hourText.Content); // Midnight is 12 AM
        }
    }

    [Fact]
    public void UpdateTimeDisplay_Hour12Format_Noon_DisplaysAs12()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 12, 30, 0); // Noon
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HourEntity) && world.Has<UIText>(pickerData.HourEntity))
        {
            ref readonly var hourText = ref world.Get<UIText>(pickerData.HourEntity);
            Assert.Equal("12", hourText.Content); // Noon is 12 PM
        }
    }

    [Fact]
    public void UpdateTimeDisplay_UpdatesMinuteDisplay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 5, 0);
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.MinuteEntity) && world.Has<UIText>(pickerData.MinuteEntity))
        {
            ref readonly var minuteText = ref world.Get<UIText>(pickerData.MinuteEntity);
            Assert.Equal("05", minuteText.Content);
        }
    }

    [Fact]
    public void UpdateTimeDisplay_UpdatesSecondDisplay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 5);
        var picker = CreateDatePickerWithTimeDisplayAndSeconds(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.SecondEntity) && world.Has<UIText>(pickerData.SecondEntity))
        {
            ref readonly var secondText = ref world.Get<UIText>(pickerData.SecondEntity);
            Assert.Equal("05", secondText.Content);
        }
    }

    [Fact]
    public void UpdateTimeDisplay_UpdatesAmPmDisplay_AM()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0); // AM
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.AmPmEntity) && world.Has<UIText>(pickerData.AmPmEntity))
        {
            ref readonly var ampmText = ref world.Get<UIText>(pickerData.AmPmEntity);
            Assert.Equal("AM", ampmText.Content);
        }
    }

    [Fact]
    public void UpdateTimeDisplay_UpdatesAmPmDisplay_PM()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 14, 30, 0); // PM
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Trigger UpdateTimeDisplay by calling SetValue
        system.SetValue(picker, initialDate);

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.AmPmEntity) && world.Has<UIText>(pickerData.AmPmEntity))
        {
            ref readonly var ampmText = ref world.Get<UIText>(pickerData.AmPmEntity);
            Assert.Equal("PM", ampmText.Content);
        }
    }

    [Fact]
    public void UpdateTimeDisplay_DeadHourEntity_DoesNotThrow()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var picker = CreateDatePickerWithTimeDisplay(world, initialDate);
        layout.Update(0);

        // Destroy the hour entity
        ref var pickerData = ref world.Get<UIDatePicker>(picker);
        if (world.IsAlive(pickerData.HourEntity))
        {
            world.Despawn(pickerData.HourEntity);
        }

        // Should not throw
        system.SetValue(picker, new DateTime(2024, 6, 15, 11, 30, 0));
    }

    #endregion

    #region Calendar Day Click Event Tests

    [Fact]
    public void CalendarDay_Click_RaisesDateChangedEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        UIDateChangedEvent? receivedEvent = null;
        world.Subscribe<UIDateChangedEvent>(evt => receivedEvent = evt);

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

        Assert.NotNull(receivedEvent);
        Assert.Equal(15, receivedEvent.Value.OldValue.Day);
        Assert.Equal(20, receivedEvent.Value.NewValue.Day);
    }

    [Fact]
    public void CalendarDay_Click_SameDay_DoesNotRaiseEvent()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        UIDateChangedEvent? receivedEvent = null;
        world.Subscribe<UIDateChangedEvent>(evt => receivedEvent = evt);

        // Find the currently selected day
        Entity? dayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 15 && day.Month == 6 && day.Year == 2024)
            {
                dayEntity = entity;
                break;
            }
        }

        Assert.NotNull(dayEntity);

        SimulateClick(world, dayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        Assert.Null(receivedEvent);
    }

    [Fact]
    public void CalendarDay_Click_DeadPicker_IsIgnored()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithDays(world, initialDate);
        layout.Update(0);

        // Find a day cell
        Entity? dayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20)
            {
                dayEntity = entity;
                break;
            }
        }

        Assert.NotNull(dayEntity);

        // Destroy the picker
        world.Despawn(picker);

        // Should not throw
        SimulateClick(world, dayEntity.Value, new Vector2(16, 16));
        system.Update(0);
    }

    #endregion

    #region Navigation Edge Case Tests

    [Fact]
    public void NavigateTo_InvalidEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        // Should not throw
        system.NavigateTo(Entity.Null, 2024, 6);
    }

    [Fact]
    public void NavigateTo_EntityWithoutDatePicker_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var entity = world.Spawn()
            .With(UIElement.Default)
            .Build();

        // Should not throw
        system.NavigateTo(entity, 2024, 6);
    }

    [Fact]
    public void NavigateTo_MonthZero_DoesNotChange()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePicker(world, initialDate);
        layout.Update(0);

        system.NavigateTo(picker, 2024, 0); // Invalid month

        ref readonly var pickerData = ref world.Get<UIDatePicker>(picker);
        Assert.Equal(6, pickerData.DisplayMonth); // Unchanged
    }

    [Fact]
    public void NavigateToToday_InvalidEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        // Should not throw
        system.NavigateToToday(Entity.Null);
    }

    #endregion

    #region Day Selection Update Tests

    [Fact]
    public void UpdateDaySelection_UpdatesOnlySelectedDay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithStyledDays(world, initialDate);
        layout.Update(0);

        // Count initially selected days
        int initialSelectedCount = 0;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.IsSelected)
            {
                initialSelectedCount++;
            }
        }

        // Find a different day and click it
        Entity? newDayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20 && day.Month == 6 && !day.IsDisabled)
            {
                newDayEntity = entity;
                break;
            }
        }

        Assert.NotNull(newDayEntity);

        SimulateClick(world, newDayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        // Count selected days after selection change
        int finalSelectedCount = 0;
        Entity? selectedEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.IsSelected)
            {
                finalSelectedCount++;
                selectedEntity = entity;
            }
        }

        Assert.Equal(1, finalSelectedCount);
        Assert.NotNull(selectedEntity);
        ref readonly var selectedDay = ref world.Get<UICalendarDay>(selectedEntity.Value);
        Assert.Equal(20, selectedDay.Day);
    }

    [Fact]
    public void UpdateDaySelection_UpdatesStyleForDeselectedDay()
    {
        using var world = new World();
        var layout = SetupLayout(world);
        var system = new UIDatePickerSystem();
        world.AddSystem(system);

        var initialDate = new DateTime(2024, 6, 15);
        var picker = CreateDatePickerWithStyledDays(world, initialDate);
        layout.Update(0);

        // Find the currently selected day
        Entity? currentlySelected = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 15 && day.IsSelected)
            {
                currentlySelected = entity;
                break;
            }
        }

        Assert.NotNull(currentlySelected);

        // Find and click a different day
        Entity? newDayEntity = null;
        foreach (var entity in world.Query<UICalendarDay>())
        {
            ref readonly var day = ref world.Get<UICalendarDay>(entity);
            if (day.Day == 20 && !day.IsDisabled)
            {
                newDayEntity = entity;
                break;
            }
        }

        Assert.NotNull(newDayEntity);

        SimulateClick(world, newDayEntity.Value, new Vector2(16, 16));
        system.Update(0);

        // Verify the previously selected day is no longer selected
        ref readonly var oldSelectedDay = ref world.Get<UICalendarDay>(currentlySelected.Value);
        Assert.False(oldSelectedDay.IsSelected);
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

    private static Entity CreateDatePickerWithStyledDays(World world, DateTime initialValue)
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

        // Create calendar day cells with styles
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
                .With(new UIStyle())
                .With(UIInteractable.Clickable())
                .Build();

            world.SetParent(dayCell, picker);
        }

        return picker;
    }

    private static Entity CreateDatePickerWithHeader(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create header entity
        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(40, 0, 200, 30))
            .With(new UIText { Content = "" })
            .Build();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.Date,
                HeaderEntity = header
            })
            .Build();

        world.SetParent(picker, canvas);
        world.SetParent(header, picker);

        return picker;
    }

    private static Entity CreateDatePickerWithTimeDisplay(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create time display entities
        var hourEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        var minuteEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(35, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        var ampmEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(70, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.DateTime,
                TimeFormat = TimeFormat.Hour12,
                HourEntity = hourEntity,
                MinuteEntity = minuteEntity,
                AmPmEntity = ampmEntity
            })
            .Build();

        world.SetParent(picker, canvas);
        world.SetParent(hourEntity, picker);
        world.SetParent(minuteEntity, picker);
        world.SetParent(ampmEntity, picker);

        return picker;
    }

    private static Entity CreateDatePickerWithTimeDisplayAndSeconds(World world, DateTime initialValue)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create time display entities
        var hourEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        var minuteEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(35, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        var secondEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(70, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        var ampmEntity = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(105, 0, 30, 30))
            .With(new UIText { Content = "" })
            .Build();

        // Create date picker entity
        var picker = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 280, 320))
            .With(new UIDatePicker(initialValue)
            {
                Mode = DatePickerMode.DateTime,
                TimeFormat = TimeFormat.Hour12,
                ShowSeconds = true,
                HourEntity = hourEntity,
                MinuteEntity = minuteEntity,
                SecondEntity = secondEntity,
                AmPmEntity = ampmEntity
            })
            .Build();

        world.SetParent(picker, canvas);
        world.SetParent(hourEntity, picker);
        world.SetParent(minuteEntity, picker);
        world.SetParent(secondEntity, picker);
        world.SetParent(ampmEntity, picker);

        return picker;
    }

    #endregion
}
