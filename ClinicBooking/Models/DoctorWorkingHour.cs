namespace ClinicBooking.Models;
public class DoctorWorkingHour
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int DayOfWeek { get; set; } // 0=Sun..6=Sat
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public Doctor? Doctor { get; set; }
}
