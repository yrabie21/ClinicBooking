using System.Data;
using ClinicBooking.Data;
using ClinicBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IScheduleService _schedule;
    private const int LockHoursBeforeStart = 1;

    public BookingService(ApplicationDbContext db, IScheduleService schedule)
    {
        _db = db;
        _schedule = schedule;
    }

    public async Task<Appointment> BookAsync(string patientUserId, int doctorId, DateTime start)
    {
        // Treat incoming start as LOCAL (UI passes local time). Store UTC.
        var localStart = DateTime.SpecifyKind(start, DateTimeKind.Local);
        var slot = await _schedule.GetDoctorSlotMinutesAsync(doctorId);

        // Race-safe: check + insert inside a short, serializable transaction.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // GetFreeSlots returns LOCAL datetimes for the selected day
            var free = await _schedule.GetFreeSlotsAsync(doctorId, localStart);
            if (!free.Contains(localStart))
                throw new InvalidOperationException("Slot not available.");

            var appt = new Appointment
            {
                PatientUserId = patientUserId,
                DoctorId = doctorId,
                StartDateTime = localStart.ToUniversalTime(),
                EndDateTime = localStart.AddMinutes(slot).ToUniversalTime(),
                Status = ApptStatus.Booked
            };

            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return appt;
        }
        catch (DbUpdateException)
        {
            // Unique filtered index (DoctorId, StartDateTime) collided — someone just took it.
            await tx.RollbackAsync();
            throw new InvalidOperationException("That slot was just taken. Please pick another time.");
        }
    }

    public async Task<bool> CancelAsync(long apptId, string userId)
    {
        var a = await _db.Appointments.FindAsync(apptId);
        if (a == null || a.PatientUserId != userId)
            return false;

        // lockout window (UTC because we store UTC)
        if ((a.StartDateTime - DateTime.UtcNow).TotalHours < LockHoursBeforeStart)
            return false;

        // Only allow cancel if it's still active
        if (a.Status != ApptStatus.Booked && a.Status != ApptStatus.CheckedIn)
            return false;

        a.Status = ApptStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RescheduleAsync(long apptId, string userId, DateTime newStart)
    {
        var a = await _db.Appointments.FindAsync(apptId);
        if (a == null || a.PatientUserId != userId)
            return false;

        if ((a.StartDateTime - DateTime.UtcNow).TotalHours < LockHoursBeforeStart)
            return false;

        var newLocalStart = DateTime.SpecifyKind(newStart, DateTimeKind.Local);
        var slot = await _schedule.GetDoctorSlotMinutesAsync(a.DoctorId);

        // Make the reschedule atomic as well
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var free = await _schedule.GetFreeSlotsAsync(a.DoctorId, newLocalStart);
            if (!free.Contains(newLocalStart))
                return false;

            a.StartDateTime = newLocalStart.ToUniversalTime();
            a.EndDateTime = newLocalStart.AddMinutes(slot).ToUniversalTime();
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            // Collision with unique index (another booking at that exact time)
            return false;
        }
    }
}
