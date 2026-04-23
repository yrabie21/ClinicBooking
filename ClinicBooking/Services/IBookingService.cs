using ClinicBooking.Models;
using System;
using System.Threading.Tasks;
namespace ClinicBooking.Services;
public interface IBookingService
{
    Task<Appointment> BookAsync(string patientUserId, int doctorId, DateTime start);
    Task<bool> CancelAsync(long apptId, string userId);
    Task<bool> RescheduleAsync(long apptId, string userId, DateTime newStart);
}
