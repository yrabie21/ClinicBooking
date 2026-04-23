namespace ClinicBooking.Models;
public class DoctorTimeOff
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }
    public Doctor? Doctor { get; set; }
}
