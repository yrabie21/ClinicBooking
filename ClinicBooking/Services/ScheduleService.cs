using ClinicBooking.Data;
using ClinicBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Services;
public class ScheduleService : IScheduleService
{
    private readonly ApplicationDbContext _db;
    public ScheduleService(ApplicationDbContext db) => _db = db;

    public async Task<int> GetDoctorSlotMinutesAsync(int doctorId)
        => await _db.Doctors.Where(d => d.Id == doctorId)
                            .Select(d => d.SlotMinutes)
                            .FirstAsync();

    public async Task<List<DateTime>> GetFreeSlotsAsync(int doctorId, DateTime date)
    {
        // Treat input as a LOCAL calendar day
        var localDay = date.Date; // 00:00 local
        var slotMinutes = await GetDoctorSlotMinutesAsync(doctorId);

        // Working hours for that weekday
        var weekday = (int)localDay.DayOfWeek; // Sunday=0
        var windows = await _db.DoctorWorkingHours
            .Where(w => w.DoctorId == doctorId && w.IsActive && w.DayOfWeek == weekday)
            .ToListAsync();

        // If no windows -> no slots
        if (windows.Count == 0) return new List<DateTime>();

        // Build the UTC day window covering the LOCAL day
        var dayStartLocal = localDay;                 // 00:00 local
        var dayEndLocal = localDay.AddDays(1);      // 24:00 local
        var dayStartUtc = DateTime.SpecifyKind(dayStartLocal, DateTimeKind.Local).ToUniversalTime();
        var dayEndUtc = DateTime.SpecifyKind(dayEndLocal, DateTimeKind.Local).ToUniversalTime();

        // Taken appointments for that doctor within this LOCAL day, compared in UTC
        var activeStatuses = new[] { ApptStatus.Booked, ApptStatus.CheckedIn, ApptStatus.Completed };
        // (We consider Completed taken to avoid re-booking history time in the same moment)
        var takenUtcStarts = (await _db.Appointments
            .Where(a => a.DoctorId == doctorId
                     && a.StartDateTime >= dayStartUtc
                     && a.StartDateTime < dayEndUtc
                     && !new[] { ApptStatus.Cancelled, ApptStatus.NoShow }.Contains(a.Status))
            .Select(a => a.StartDateTime)
            .ToListAsync())
            .ToHashSet();

        var nowLocal = DateTime.Now;
        var result = new List<DateTime>();

        foreach (var w in windows)
        {
            // Local window for that day
            var windowStartLocal = localDay.Add(w.StartTime.ToTimeSpan());
            var windowEndLocal = localDay.Add(w.EndTime.ToTimeSpan());

            // Generate slots within the local window
            for (var candLocal = windowStartLocal; candLocal.AddMinutes(slotMinutes) <= windowEndLocal; candLocal = candLocal.AddMinutes(slotMinutes))
            {
                // Skip past times for today
                if (candLocal <= nowLocal) continue;

                // Compare against taken in UTC
                var candUtc = DateTime.SpecifyKind(candLocal, DateTimeKind.Local).ToUniversalTime();

                // If exact start is not taken, it's free
                if (!takenUtcStarts.Contains(candUtc))
                    result.Add(candLocal); // keep local for UI
            }
        }

        return result.OrderBy(x => x).ToList();
    }
}
