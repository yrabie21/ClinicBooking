using System;
using System.Linq;
using System.Threading.Tasks;
using ClinicBooking.Data;
using ClinicBooking.Models;
using ClinicBooking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IScheduleService _schedule;
        private readonly IBookingService _booking;
        private readonly UserManager<ApplicationUser> _um;

        public AppointmentsController(
            ApplicationDbContext db,
            IScheduleService schedule,
            IBookingService booking,
            UserManager<ApplicationUser> um)
        {
            _db = db;
            _schedule = schedule;
            _booking = booking;
            _um = um;
        }

        // GET: /Appointments/Search
        [HttpGet]
        public async Task<IActionResult> Search()
        {
            ViewBag.Specialties = await _db.Specialties
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View();
        }

        // GET: /Appointments/DoctorsBySpecialty?id=5   (AJAX)
        [HttpGet]
        public async Task<IActionResult> DoctorsBySpecialty(int id)
        {
            var doctors = await _db.Doctors
                .Include(d => d.User)
                .Where(d => d.SpecialtyId == id)
                .ToListAsync();

            var payload = doctors.Select(d => new
            {
                d.Id,
                Name = d.User != null ? d.User.FullName : "(no user)"
            });

            return Json(payload);
        }

        // GET: /Appointments/FreeSlots?doctorId=1&date=2025-09-15   (AJAX)
        [HttpGet]
        public async Task<IActionResult> FreeSlots(int doctorId, DateTime date)
        {
            var free = await _schedule.GetFreeSlotsAsync(doctorId, date);
            var payload = free.Select(s => s.ToString("yyyy-MM-dd HH:mm"));
            return Json(payload);
        }

        // POST: /Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int doctorId, DateTime start)
        {
            var user = await _um.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(Search));
            }

            try
            {
                var startLocal = DateTime.SpecifyKind(start, DateTimeKind.Local);
                await _booking.BookAsync(user.Id, doctorId, startLocal);
                TempData["msg"] = "Booked.";
            }
            catch (Exception ex)
            {
                TempData["err"] = ex.Message;
            }

            return RedirectToAction(nameof(My));
        }

        // GET: /Appointments/My
        [HttpGet]
        public async Task<IActionResult> My()
        {
            var user = await _um.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(Search));
            }

            var list = await _db.Appointments
                .Include(a => a.Doctor)!.ThenInclude(d => d!.User)
                .Where(a => a.PatientUserId == user.Id)
                .OrderByDescending(a => a.StartDateTime)
                .ToListAsync();

            return View(list);
        }

        // POST: /Appointments/Cancel/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var user = await _um.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(My));
            }

            var ok = await _booking.CancelAsync(id, user.Id);
            TempData[ok ? "msg" : "err"] = ok ? "Cancelled." : "Cannot cancel.";
            return RedirectToAction(nameof(My));
        }

        // POST: /Appointments/Reschedule/123  (form/query must include newStart)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(long id, DateTime newStart)
        {
            var user = await _um.GetUserAsync(User);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(My));
            }

            var ok = await _booking.RescheduleAsync(
                id,
                user.Id,
                DateTime.SpecifyKind(newStart, DateTimeKind.Local));

            TempData[ok ? "msg" : "err"] = ok ? "Rescheduled." : "Cannot reschedule.";
            return RedirectToAction(nameof(My));
        }
    }
}
