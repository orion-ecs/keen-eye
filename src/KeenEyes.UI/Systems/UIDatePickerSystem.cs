using System.Globalization;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles date/time picker interaction and calendar navigation.
/// </summary>
/// <remarks>
/// <para>
/// This system manages:
/// <list type="bullet">
/// <item>Calendar day selection</item>
/// <item>Month/year navigation</item>
/// <item>Time spinner interactions</item>
/// <item>Calendar grid updates</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIDatePickerSystem : SystemBase
{
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to click events for calendar days and navigation
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            clickSubscription?.Dispose();
            clickSubscription = null;
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Most work is event-driven
    }

    private void OnClick(UIClickEvent evt)
    {
        // Handle calendar day click
        if (World.Has<UICalendarDay>(evt.Element))
        {
            HandleDayClick(evt.Element);
            return;
        }

        // Handle time spinner click
        if (World.Has<UITimeSpinner>(evt.Element))
        {
            HandleTimeSpinnerClick(evt.Element);
            return;
        }

        // Handle navigation button clicks
        HandleNavigationClick(evt.Element);
    }

    private void HandleDayClick(Entity dayEntity)
    {
        ref readonly var calendarDay = ref World.Get<UICalendarDay>(dayEntity);
        var pickerEntity = calendarDay.DatePicker;

        if (!World.IsAlive(pickerEntity) || !World.Has<UIDatePicker>(pickerEntity))
        {
            return;
        }

        if (calendarDay.IsDisabled)
        {
            return;
        }

        ref var picker = ref World.Get<UIDatePicker>(pickerEntity);
        var oldValue = picker.Value;

        // Create new date keeping the time component
        var newDate = new DateTime(
            calendarDay.Year,
            calendarDay.Month,
            calendarDay.Day,
            picker.Value.Hour,
            picker.Value.Minute,
            picker.Value.Second,
            picker.Value.Kind);

        // Check min/max constraints
        if (picker.MinDate.HasValue && newDate < picker.MinDate.Value)
        {
            return;
        }

        if (picker.MaxDate.HasValue && newDate > picker.MaxDate.Value)
        {
            return;
        }

        picker.Value = newDate;

        // Update display month/year if needed (for overflow days)
        if (calendarDay.Month != picker.DisplayMonth || calendarDay.Year != picker.DisplayYear)
        {
            picker.DisplayMonth = calendarDay.Month;
            picker.DisplayYear = calendarDay.Year;
            UpdateCalendarGrid(pickerEntity, ref picker);
            World.Send(new UICalendarNavigatedEvent(pickerEntity, picker.DisplayYear, picker.DisplayMonth));
        }
        else
        {
            UpdateDaySelection(pickerEntity, ref picker);
        }

        UpdateHeaderDisplay(pickerEntity, ref picker);

        if (oldValue != newDate)
        {
            World.Send(new UIDateChangedEvent(pickerEntity, oldValue, newDate));
        }
    }

    private void HandleTimeSpinnerClick(Entity spinnerEntity)
    {
        ref readonly var spinner = ref World.Get<UITimeSpinner>(spinnerEntity);
        var pickerEntity = spinner.DatePicker;

        if (!World.IsAlive(pickerEntity) || !World.Has<UIDatePicker>(pickerEntity))
        {
            return;
        }

        ref var picker = ref World.Get<UIDatePicker>(pickerEntity);
        var oldValue = picker.Value;

        // Get the text from the spinner to determine if this is an increment or decrement
        // For now, we'll just increment by 1 on click (full implementation would have up/down buttons)
        DateTime newValue;

        switch (spinner.Field)
        {
            case TimeField.Hour:
                newValue = picker.Value.AddHours(1);
                // Wrap around if needed
                if (newValue.Day != picker.Value.Day)
                {
                    newValue = new DateTime(picker.Value.Year, picker.Value.Month, picker.Value.Day, 0, picker.Value.Minute, picker.Value.Second, picker.Value.Kind);
                }
                break;

            case TimeField.Minute:
                newValue = picker.Value.AddMinutes(1);
                // Keep same hour
                if (newValue.Hour != picker.Value.Hour)
                {
                    newValue = new DateTime(picker.Value.Year, picker.Value.Month, picker.Value.Day, picker.Value.Hour, 0, picker.Value.Second, picker.Value.Kind);
                }
                break;

            case TimeField.Second:
                newValue = picker.Value.AddSeconds(1);
                // Keep same minute
                if (newValue.Minute != picker.Value.Minute)
                {
                    newValue = new DateTime(picker.Value.Year, picker.Value.Month, picker.Value.Day, picker.Value.Hour, picker.Value.Minute, 0, picker.Value.Kind);
                }
                break;

            case TimeField.AmPm:
                newValue = picker.Value.AddHours(12);
                // Keep same day
                if (newValue.Day != picker.Value.Day)
                {
                    newValue = picker.Value.AddHours(-12);
                }
                break;

            default:
                return;
        }

        picker.Value = newValue;
        UpdateTimeDisplay(pickerEntity, ref picker);

        if (oldValue != newValue)
        {
            World.Send(new UIDateChangedEvent(pickerEntity, oldValue, newValue));
        }
    }

    private void HandleNavigationClick(Entity entity)
    {
        // Find which date picker this button belongs to
        foreach (var pickerEntity in World.Query<UIDatePicker>())
        {
            ref var picker = ref World.Get<UIDatePicker>(pickerEntity);

            if (entity == picker.PrevMonthButton)
            {
                NavigatePreviousMonth(pickerEntity, ref picker);
                return;
            }

            if (entity == picker.NextMonthButton)
            {
                NavigateNextMonth(pickerEntity, ref picker);
                return;
            }
        }
    }

    private void NavigatePreviousMonth(Entity pickerEntity, ref UIDatePicker picker)
    {
        picker.DisplayMonth--;
        if (picker.DisplayMonth < 1)
        {
            picker.DisplayMonth = 12;
            picker.DisplayYear--;
        }

        UpdateCalendarGrid(pickerEntity, ref picker);
        UpdateHeaderDisplay(pickerEntity, ref picker);
        World.Send(new UICalendarNavigatedEvent(pickerEntity, picker.DisplayYear, picker.DisplayMonth));
    }

    private void NavigateNextMonth(Entity pickerEntity, ref UIDatePicker picker)
    {
        picker.DisplayMonth++;
        if (picker.DisplayMonth > 12)
        {
            picker.DisplayMonth = 1;
            picker.DisplayYear++;
        }

        UpdateCalendarGrid(pickerEntity, ref picker);
        UpdateHeaderDisplay(pickerEntity, ref picker);
        World.Send(new UICalendarNavigatedEvent(pickerEntity, picker.DisplayYear, picker.DisplayMonth));
    }

    private void UpdateCalendarGrid(Entity pickerEntity, ref UIDatePicker picker)
    {
        // Update all calendar day entities
        foreach (var dayEntity in World.Query<UICalendarDay>())
        {
            ref var calendarDay = ref World.Get<UICalendarDay>(dayEntity);

            if (calendarDay.DatePicker != pickerEntity)
            {
                continue;
            }

            // Recalculate which day this cell should display
            // This would typically be done by the factory, but we update existing cells
            UpdateDayCell(dayEntity, ref calendarDay, ref picker);
        }
    }

    private void UpdateDayCell(Entity dayEntity, ref UICalendarDay calendarDay, ref UIDatePicker picker)
    {
        var today = DateTime.Today;

        calendarDay.IsToday =
            calendarDay.Year == today.Year &&
            calendarDay.Month == today.Month &&
            calendarDay.Day == today.Day;

        calendarDay.IsSelected =
            calendarDay.Year == picker.Value.Year &&
            calendarDay.Month == picker.Value.Month &&
            calendarDay.Day == picker.Value.Day;

        calendarDay.IsCurrentMonth =
            calendarDay.Month == picker.DisplayMonth &&
            calendarDay.Year == picker.DisplayYear;

        // Check if disabled
        var cellDate = new DateTime(calendarDay.Year, calendarDay.Month, calendarDay.Day, 0, 0, 0, DateTimeKind.Unspecified);
        calendarDay.IsDisabled =
            (picker.MinDate.HasValue && cellDate < picker.MinDate.Value.Date) ||
            (picker.MaxDate.HasValue && cellDate > picker.MaxDate.Value.Date);

        // Update visual styling
        if (World.Has<UIStyle>(dayEntity))
        {
            ref var style = ref World.Get<UIStyle>(dayEntity);

            if (calendarDay.IsDisabled)
            {
                style.BackgroundColor = new System.Numerics.Vector4(0.15f, 0.15f, 0.15f, 1f);
            }
            else if (calendarDay.IsSelected)
            {
                style.BackgroundColor = new System.Numerics.Vector4(0.3f, 0.5f, 0.9f, 1f);
            }
            else if (calendarDay.IsToday)
            {
                style.BackgroundColor = new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1f);
            }
            else if (!calendarDay.IsCurrentMonth)
            {
                style.BackgroundColor = new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 0.5f);
            }
            else
            {
                style.BackgroundColor = new System.Numerics.Vector4(0.25f, 0.25f, 0.25f, 1f);
            }
        }

        // Update text content
        if (World.Has<UIText>(dayEntity))
        {
            ref var text = ref World.Get<UIText>(dayEntity);
            text.Content = calendarDay.Day.ToString();

            if (calendarDay.IsDisabled || !calendarDay.IsCurrentMonth)
            {
                text.Color = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f);
            }
            else
            {
                text.Color = System.Numerics.Vector4.One;
            }
        }
    }

    private void UpdateDaySelection(Entity pickerEntity, ref UIDatePicker picker)
    {
        foreach (var dayEntity in World.Query<UICalendarDay>())
        {
            ref var calendarDay = ref World.Get<UICalendarDay>(dayEntity);

            if (calendarDay.DatePicker != pickerEntity)
            {
                continue;
            }

            bool wasSelected = calendarDay.IsSelected;
            calendarDay.IsSelected =
                calendarDay.Year == picker.Value.Year &&
                calendarDay.Month == picker.Value.Month &&
                calendarDay.Day == picker.Value.Day;

            if (wasSelected != calendarDay.IsSelected && World.Has<UIStyle>(dayEntity))
            {
                ref var style = ref World.Get<UIStyle>(dayEntity);

                if (calendarDay.IsSelected)
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.3f, 0.5f, 0.9f, 1f);
                }
                else if (calendarDay.IsToday)
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1f);
                }
                else if (!calendarDay.IsCurrentMonth)
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 0.5f);
                }
                else
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.25f, 0.25f, 0.25f, 1f);
                }
            }
        }
    }

    private void UpdateHeaderDisplay(Entity pickerEntity, ref UIDatePicker picker)
    {
        if (!World.IsAlive(picker.HeaderEntity) || !World.Has<UIText>(picker.HeaderEntity))
        {
            return;
        }

        ref var headerText = ref World.Get<UIText>(picker.HeaderEntity);
        headerText.Content = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(picker.DisplayMonth)} {picker.DisplayYear}";
    }

    private void UpdateTimeDisplay(Entity pickerEntity, ref UIDatePicker picker)
    {
        // Update hour display
        if (World.IsAlive(picker.HourEntity) && World.Has<UIText>(picker.HourEntity))
        {
            ref var hourText = ref World.Get<UIText>(picker.HourEntity);
            int displayHour = picker.Value.Hour;

            if (picker.TimeFormat == TimeFormat.Hour12)
            {
                displayHour = displayHour % 12;
                if (displayHour == 0)
                {
                    displayHour = 12;
                }
            }

            hourText.Content = displayHour.ToString("D2");
        }

        // Update minute display
        if (World.IsAlive(picker.MinuteEntity) && World.Has<UIText>(picker.MinuteEntity))
        {
            ref var minuteText = ref World.Get<UIText>(picker.MinuteEntity);
            minuteText.Content = picker.Value.Minute.ToString("D2");
        }

        // Update second display
        if (picker.ShowSeconds && World.IsAlive(picker.SecondEntity) && World.Has<UIText>(picker.SecondEntity))
        {
            ref var secondText = ref World.Get<UIText>(picker.SecondEntity);
            secondText.Content = picker.Value.Second.ToString("D2");
        }

        // Update AM/PM display
        if (picker.TimeFormat == TimeFormat.Hour12 && World.IsAlive(picker.AmPmEntity) && World.Has<UIText>(picker.AmPmEntity))
        {
            ref var ampmText = ref World.Get<UIText>(picker.AmPmEntity);
            ampmText.Content = picker.Value.Hour >= 12 ? "PM" : "AM";
        }
    }

    /// <summary>
    /// Sets the date/time value of a date picker.
    /// </summary>
    /// <param name="entity">The date picker entity.</param>
    /// <param name="value">The new date/time value.</param>
    public void SetValue(Entity entity, DateTime value)
    {
        if (!World.IsAlive(entity) || !World.Has<UIDatePicker>(entity))
        {
            return;
        }

        ref var picker = ref World.Get<UIDatePicker>(entity);
        var oldValue = picker.Value;

        // Apply constraints
        if (picker.MinDate.HasValue && value < picker.MinDate.Value)
        {
            value = picker.MinDate.Value;
        }

        if (picker.MaxDate.HasValue && value > picker.MaxDate.Value)
        {
            value = picker.MaxDate.Value;
        }

        picker.Value = value;
        picker.DisplayMonth = value.Month;
        picker.DisplayYear = value.Year;

        UpdateCalendarGrid(entity, ref picker);
        UpdateHeaderDisplay(entity, ref picker);
        UpdateTimeDisplay(entity, ref picker);

        if (oldValue != value)
        {
            World.Send(new UIDateChangedEvent(entity, oldValue, value));
        }
    }

    /// <summary>
    /// Gets the current date/time value from a date picker.
    /// </summary>
    /// <param name="entity">The date picker entity.</param>
    /// <returns>The current date/time value, or DateTime.MinValue if invalid.</returns>
    public DateTime GetValue(Entity entity)
    {
        if (!World.IsAlive(entity) || !World.Has<UIDatePicker>(entity))
        {
            return DateTime.MinValue;
        }

        return World.Get<UIDatePicker>(entity).Value;
    }

    /// <summary>
    /// Navigates the calendar to a specific month/year.
    /// </summary>
    /// <param name="entity">The date picker entity.</param>
    /// <param name="year">The year to navigate to.</param>
    /// <param name="month">The month to navigate to (1-12).</param>
    public void NavigateTo(Entity entity, int year, int month)
    {
        if (!World.IsAlive(entity) || !World.Has<UIDatePicker>(entity))
        {
            return;
        }

        if (month < 1 || month > 12)
        {
            return;
        }

        ref var picker = ref World.Get<UIDatePicker>(entity);
        picker.DisplayYear = year;
        picker.DisplayMonth = month;

        UpdateCalendarGrid(entity, ref picker);
        UpdateHeaderDisplay(entity, ref picker);
        World.Send(new UICalendarNavigatedEvent(entity, year, month));
    }

    /// <summary>
    /// Navigates the calendar to today's date.
    /// </summary>
    /// <param name="entity">The date picker entity.</param>
    public void NavigateToToday(Entity entity)
    {
        var today = DateTime.Today;
        NavigateTo(entity, today.Year, today.Month);
    }

    /// <summary>
    /// Gets the number of days in a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The number of days in the month.</returns>
    public static int GetDaysInMonth(int year, int month)
    {
        return DateTime.DaysInMonth(year, month);
    }

    /// <summary>
    /// Gets the day of week for the first day of a month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The day of week (0 = Sunday, 6 = Saturday).</returns>
    public static DayOfWeek GetFirstDayOfMonth(int year, int month)
    {
        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified).DayOfWeek;
    }
}
