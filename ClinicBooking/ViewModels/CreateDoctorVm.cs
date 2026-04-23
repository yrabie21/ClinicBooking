using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ClinicBooking.ViewModels { 
public class WorkingHourVm
{
    public int DayOfWeek { get; set; }          // 0 = Sunday … 6 = Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDoctorVm
{
    // Identity fields
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "Pass123!";

    // Doctor fields
    public int SpecialtyId { get; set; }
    public string? Room { get; set; }
    public int SlotMinutes { get; set; } = 15;
    public decimal? Fee { get; set; }

    // Working hours (one per day, editable checkboxes)
    public List<WorkingHourVm> WorkingHours { get; set; } = new();

    // For dropdown
    public IEnumerable<SelectListItem>? Specialties { get; set; }
}

}
