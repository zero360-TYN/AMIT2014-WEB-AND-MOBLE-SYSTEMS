namespace Assignment.Models;

/// <summary>
/// View mode: Week (weekly time grid) or Resources (side-by-side resource comparison)
/// </summary>
public enum SlotViewType
{
    Week,
    Resources
}

/// <summary>
/// Event / booking item data model for the Slot Table
/// </summary>
public class SlotEventData
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    /// <summary>
    /// Resource identifier (matches column ID in Resources view, e.g. staff ID or room ID)
    /// </summary>
    public string? Resource { get; set; }
    /// <summary>
    /// Event background color (e.g. "#3788d8")
    /// </summary>
    public string? BackColor { get; set; }
    /// <summary>
    /// Status label (e.g. pending, confirmed, completed)
    /// </summary>
    public string? Status { get; set; }
    /// <summary>
    /// Optional contextual metadata or business payload
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Column resource definition in Resources view (e.g. staff or room list)
/// </summary>
public class SlotResourceData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Reusable Visual Slot Table configuration model
/// </summary>
public class VisualSlotTableConfig
{
    /// <summary>
    /// Unique container DOM ID (defaults to random GUID to avoid multi-instance conflicts)
    /// </summary>
    public string Id { get; set; } = "slot_table_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// View mode: Week or Resources
    /// </summary>
    public SlotViewType ViewType { get; set; } = SlotViewType.Week;

    /// <summary>
    /// Whether the table is in read-only mode (true: display only; false: customer can click to select slot)
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Base date for initial display (defaults to today)
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Start hour of visible business hours (e.g. 9 for 09:00)
    /// </summary>
    public int BusinessBeginsHour { get; set; } = 9;

    /// <summary>
    /// End hour of visible business hours (e.g. 18 for 18:00)
    /// </summary>
    public int BusinessEndsHour { get; set; } = 18;

    /// <summary>
    /// Time slot granularity in minutes (e.g. 30 or 60, defaults to 60)
    /// </summary>
    public int CellDuration { get; set; } = 60;

    /// <summary>
    /// Whether to show the top navigation toolbar (Prev, Today, Next, title, legends)
    /// </summary>
    public bool ShowNavigation { get; set; } = true;

    // --- Data Sources ---

    /// <summary>
    /// Pre-loaded existing booking/event list (synchronous/static loading)
    /// </summary>
    public List<SlotEventData>? Events { get; set; }

    /// <summary>
    /// API URL for fetching events asynchronously (called during navigation)
    /// </summary>
    public string? EventsUrl { get; set; }

    /// <summary>
    /// Column definitions for Resources view (e.g. staff or room list)
    /// </summary>
    public List<SlotResourceData>? Resources { get; set; }

    // --- Form & Interaction Bindings ---

    /// <summary>
    /// Target input selector for StartTime auto-fill upon slot selection (e.g. "#StartTime")
    /// </summary>
    public string? TargetStartInput { get; set; }

    /// <summary>
    /// Target input selector for EndTime auto-fill upon slot selection (e.g. "#EndTime")
    /// </summary>
    public string? TargetEndInput { get; set; }

    /// <summary>
    /// Target input selector for Resource ID auto-fill upon slot selection (e.g. "#StaffId")
    /// </summary>
    public string? TargetResourceInput { get; set; }

    /// <summary>
    /// JavaScript callback function name called on slot selection: function(slotData) { ... }
    /// </summary>
    public string? OnSlotSelect { get; set; }

    /// <summary>
    /// JavaScript callback function name called when clicking existing event: function(eventData) { ... }
    /// </summary>
    public string? OnEventClick { get; set; }
}
