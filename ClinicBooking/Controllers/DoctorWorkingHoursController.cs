using System;
using System.Linq;
using System.Threading.Tasks;
using ClinicBooking.Data;
using ClinicBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorWorkingHoursController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorWorkingHoursController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DoctorWorkingHours
        public async Task<IActionResult> Index(int? doctorId)
        {
            var query = _context.DoctorWorkingHours
                .Include(w => w.Doctor)
                    .ThenInclude(d => d.User)
                .AsQueryable();

            if (doctorId.HasValue)
                query = query.Where(w => w.DoctorId == doctorId.Value);

            var hours = await query.OrderBy(w => w.DoctorId).ThenBy(w => w.DayOfWeek).ToListAsync();

            // Optional doctor filter dropdown (shows all doctors by name)
            ViewBag.DoctorFilter = new SelectList(
                _context.Doctors.Include(d => d.User)
                    .Select(d => new
                    {
                        d.Id,
                        Name = d.User != null ? d.User.FullName : "(no user linked)"
                    }).ToList(),
                "Id", "Name", doctorId);

            return View(hours);
        }

        // GET: DoctorWorkingHours/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var wh = await _context.DoctorWorkingHours
                .Include(w => w.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (wh == null) return NotFound();

            return View(wh);
        }

        // GET: DoctorWorkingHours/Create
        public IActionResult Create()
        {
            ViewBag.Doctors = new SelectList(
                _context.Doctors.Include(d => d.User)
                    .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                    .ToList(),
                "Id", "Name");

            return View();
        }

        // POST: DoctorWorkingHours/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DoctorId,DayOfWeek,StartTime,EndTime,IsActive")] DoctorWorkingHour doctorWorkingHour)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = new SelectList(
                    _context.Doctors.Include(d => d.User)
                        .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                        .ToList(),
                    "Id", "Name", doctorWorkingHour.DoctorId);

                return View(doctorWorkingHour);
            }

            // Basic validation: EndTime > StartTime
            if (doctorWorkingHour.EndTime <= doctorWorkingHour.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
                ViewBag.Doctors = new SelectList(
                    _context.Doctors.Include(d => d.User)
                        .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                        .ToList(),
                    "Id", "Name", doctorWorkingHour.DoctorId);
                return View(doctorWorkingHour);
            }

            _context.Add(doctorWorkingHour);
            await _context.SaveChangesAsync();
            TempData["msg"] = "Working hour added.";
            return RedirectToAction(nameof(Index));
        }

        // GET: DoctorWorkingHours/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var wh = await _context.DoctorWorkingHours
                .Include(w => w.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wh == null) return NotFound();

            ViewBag.Doctors = new SelectList(
                _context.Doctors.Include(d => d.User)
                    .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                    .ToList(),
                "Id", "Name", wh.DoctorId);

            return View(wh);
        }

        // POST: DoctorWorkingHours/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DoctorId,DayOfWeek,StartTime,EndTime,IsActive")] DoctorWorkingHour doctorWorkingHour)
        {
            if (id != doctorWorkingHour.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = new SelectList(
                    _context.Doctors.Include(d => d.User)
                        .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                        .ToList(),
                    "Id", "Name", doctorWorkingHour.DoctorId);
                return View(doctorWorkingHour);
            }

            if (doctorWorkingHour.EndTime <= doctorWorkingHour.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
                ViewBag.Doctors = new SelectList(
                    _context.Doctors.Include(d => d.User)
                        .Select(d => new { d.Id, Name = d.User != null ? d.User.FullName : "(no user linked)" })
                        .ToList(),
                    "Id", "Name", doctorWorkingHour.DoctorId);
                return View(doctorWorkingHour);
            }

            try
            {
                _context.Update(doctorWorkingHour);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Working hour updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoctorWorkingHourExists(doctorWorkingHour.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: DoctorWorkingHours/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var wh = await _context.DoctorWorkingHours
                .Include(w => w.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (wh == null) return NotFound();

            return View(wh);
        }

        // POST: DoctorWorkingHours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wh = await _context.DoctorWorkingHours.FindAsync(id);
            if (wh != null)
            {
                _context.DoctorWorkingHours.Remove(wh);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Working hour removed.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorWorkingHourExists(int id)
            => _context.DoctorWorkingHours.Any(e => e.Id == id);
    }
}
