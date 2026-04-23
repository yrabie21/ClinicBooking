using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Models;
public class Doctor
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int SpecialtyId { get; set; }
    public string Room { get; set; } = "";
    public int SlotMinutes { get; set; } = 15;

    [Precision(18, 2)]
    public decimal? Fee { get; set; }
    public ApplicationUser? User { get; set; }
    public Specialty? Specialty { get; set; }
    public ICollection<DoctorWorkingHour> WorkingHours { get; set; } = new List<DoctorWorkingHour>();
}
