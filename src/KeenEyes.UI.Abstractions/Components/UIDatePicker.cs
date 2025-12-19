namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the date picker display mode.
/// </summary>
public enum DatePickerMode : byte
{
    /// <summary>
    /// Date only mode (calendar view).
    /// </summary>
    Date = 0,

    /// <summary>
    /// Time only mode (hour/minute/second selectors).
    /// </summary>
    Time = 1,

    /// <summary>
    /// Combined date and time mode.
    /// </summary>
    DateTime = 2
}

/// <summary>
/// Specifies the time format for display.
/// </summary>
public enum TimeFormat : byte
{
    /// <summary>
    /// 12-hour format with AM/PM.
    /// </summary>
    Hour12 = 0,

    /// <summary>
    /// 24-hour format.
    /// </summary>
    Hour24 = 1
}

/// <summary>
/// Component for date/time picker widgets.
/// </summary>
/// <remarks>
/// <para>
/// The UIDatePicker component provides interactive date and/or time selection
/// through calendar navigation and time spinners.
/// </para>
/// <para>
/// The picker can operate in date-only, time-only, or combined date-time mode
/// depending on the <see cref="Mode"/> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var picker = world.Spawn()
///     .With(new UIElement { Visible = true })
///     .With(new UIRect { Size = new Vector2(280, 320) })
///     .With(new UIDatePicker(DateTime.Now)
///     {
///         Mode = DatePickerMode.DateTime,
///         TimeFormat = TimeFormat.Hour24
///     })
///     .Build();
/// </code>
/// </example>
/// <param name="value">The initial date/time value.</param>
public struct UIDatePicker(DateTime value) : IComponent
{
    /// <summary>
    /// The display mode (Date, Time, or DateTime).
    /// </summary>
    public DatePickerMode Mode = DatePickerMode.Date;

    /// <summary>
    /// The current selected date/time value.
    /// </summary>
    public DateTime Value = value;

    /// <summary>
    /// The currently displayed year in the calendar view.
    /// </summary>
    public int DisplayYear = value.Year;

    /// <summary>
    /// The currently displayed month in the calendar view (1-12).
    /// </summary>
    public int DisplayMonth = value.Month;

    /// <summary>
    /// The time format for display (12-hour or 24-hour).
    /// </summary>
    public TimeFormat TimeFormat = TimeFormat.Hour24;

    /// <summary>
    /// Whether to show seconds in time mode.
    /// </summary>
    public bool ShowSeconds = false;

    /// <summary>
    /// The minimum selectable date (null for no minimum).
    /// </summary>
    public DateTime? MinDate = null;

    /// <summary>
    /// The maximum selectable date (null for no maximum).
    /// </summary>
    public DateTime? MaxDate = null;

    /// <summary>
    /// Entity reference to the calendar grid container.
    /// </summary>
    public Entity CalendarGridEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the month/year header display.
    /// </summary>
    public Entity HeaderEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the previous month button.
    /// </summary>
    public Entity PrevMonthButton = Entity.Null;

    /// <summary>
    /// Entity reference to the next month button.
    /// </summary>
    public Entity NextMonthButton = Entity.Null;

    /// <summary>
    /// Entity reference to the hour spinner/selector.
    /// </summary>
    public Entity HourEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the minute spinner/selector.
    /// </summary>
    public Entity MinuteEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the second spinner/selector.
    /// </summary>
    public Entity SecondEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the AM/PM toggle (12-hour mode only).
    /// </summary>
    public Entity AmPmEntity = Entity.Null;

    /// <summary>
    /// Gets the first day of the week for calendar display.
    /// </summary>
    public DayOfWeek FirstDayOfWeek = DayOfWeek.Sunday;
}

/// <summary>
/// Component for individual day cells in the calendar grid.
/// </summary>
/// <param name="datePicker">The parent date picker entity.</param>
/// <param name="day">The day of month this cell represents (1-31).</param>
/// <param name="month">The month this cell belongs to.</param>
/// <param name="year">The year this cell belongs to.</param>
public struct UICalendarDay(Entity datePicker, int day, int month, int year) : IComponent
{
    /// <summary>
    /// The parent date picker entity.
    /// </summary>
    public Entity DatePicker = datePicker;

    /// <summary>
    /// The day of month (1-31).
    /// </summary>
    public int Day = day;

    /// <summary>
    /// The month (1-12).
    /// </summary>
    public int Month = month;

    /// <summary>
    /// The year.
    /// </summary>
    public int Year = year;

    /// <summary>
    /// Whether this day is from the current display month (false = prev/next month overflow).
    /// </summary>
    public bool IsCurrentMonth = true;

    /// <summary>
    /// Whether this day is today's date.
    /// </summary>
    public bool IsToday = false;

    /// <summary>
    /// Whether this day is currently selected.
    /// </summary>
    public bool IsSelected = false;

    /// <summary>
    /// Whether this day is disabled (outside min/max range).
    /// </summary>
    public bool IsDisabled = false;
}

/// <summary>
/// Component for time spinner controls (hour, minute, second).
/// </summary>
/// <param name="datePicker">The parent date picker entity.</param>
/// <param name="field">The time field this spinner controls.</param>
public struct UITimeSpinner(Entity datePicker, TimeField field) : IComponent
{
    /// <summary>
    /// The parent date picker entity.
    /// </summary>
    public Entity DatePicker = datePicker;

    /// <summary>
    /// The time field this spinner controls.
    /// </summary>
    public TimeField Field = field;
}

/// <summary>
/// Specifies which time field a spinner controls.
/// </summary>
public enum TimeField : byte
{
    /// <summary>
    /// Hour field.
    /// </summary>
    Hour = 0,

    /// <summary>
    /// Minute field.
    /// </summary>
    Minute = 1,

    /// <summary>
    /// Second field.
    /// </summary>
    Second = 2,

    /// <summary>
    /// AM/PM field (12-hour mode).
    /// </summary>
    AmPm = 3
}

/// <summary>
/// Event raised when the date/time picker value changes.
/// </summary>
/// <param name="Entity">The date picker entity.</param>
/// <param name="OldValue">The previous date/time value.</param>
/// <param name="NewValue">The new date/time value.</param>
public readonly record struct UIDateChangedEvent(
    Entity Entity,
    DateTime OldValue,
    DateTime NewValue);

/// <summary>
/// Event raised when the calendar display month/year changes.
/// </summary>
/// <param name="Entity">The date picker entity.</param>
/// <param name="Year">The new display year.</param>
/// <param name="Month">The new display month (1-12).</param>
public readonly record struct UICalendarNavigatedEvent(
    Entity Entity,
    int Year,
    int Month);
