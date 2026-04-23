using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace ClinicBooking.Services;
public interface IScheduleService
{
    Task<int> GetDoctorSlotMinutesAsync(int doctorId);
    Task<List<DateTime>> GetFreeSlotsAsync(int doctorId, DateTime date);
}
