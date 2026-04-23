using ClinicBooking.Data;
using ClinicBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Controllers
{
    [Authorize(Roles = "Doctor,Admin")]
    public class DoctorScheduleController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public DoctorScheduleController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db; _um = um;
        }

        // GET: /DoctorSchedule/Today?date=2025-09-16
        public async Task<IActionResult> Today(DateTime? date)
        {
            var user = await _um.GetUserAsync(User);

            // which doctor is this user?
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == user!.Id);
            if (doctor == null && !User.IsInRole("Admin"))
                return Unauthorized();

            var targetDate = (date ?? DateTime.UtcNow.Date).Date;

            // if Admin (no doctor record), show all doctors for that date
            var query = _db.Appointments
                .Include(a => a.Doctor)!.ThenInclude(d => d!.User)
                .Include(a => a.Patient)
                .Where(a => a.StartDateTime.Date == targetDate);

            if (!User.IsInRole("Admin"))
                query = query.Where(a => a.DoctorId == doctor!.Id);

            var list = await query.OrderBy(a => a.StartDateTime).ToListAsync();

            ViewBag.TargetDate = targetDate;
            return View(list);
        }

        // POST: /DoctorSchedule/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, ApptStatus status)
        {
            var appt = await _db.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appt == null) return NotFound();

            // Doctors may only update their own appointments; Admin can update all
            if (!User.IsInRole("Admin"))
            {
                var me = await _um.GetUserAsync(User);
                if (appt.Doctor == null || appt.Doctor.UserId != me!.Id)
                    return Forbid();
            }

            // simple guard: don’t allow changing Cancelled to something else
            if (appt.Status == ApptStatus.Cancelled)
            {
                TempData["err"] = "Cannot change a cancelled appointment.";
                return RedirectToAction(nameof(Today));
            }

            appt.Status = status;
            await _db.SaveChangesAsync();

            TempData["msg"] = $"Marked as {status}.";
            return RedirectToAction(nameof(Today));
        }
    }
}
