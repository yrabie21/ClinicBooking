namespace ClinicBooking.Models;
public enum ApptStatus { Booked, CheckedIn, Completed, Cancelled, NoShow }
public class Appointment
{
    public long Id { get; set; }
    public string PatientUserId { get; set; } = "";
    public int DoctorId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public ApptStatus Status { get; set; } = ApptStatus.Booked;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ApplicationUser? Patient { get; set; }
    public Doctor? Doctor { get; set; }
}
