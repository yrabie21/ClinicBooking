using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClinicBooking.Data;
using ClinicBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicBooking.ViewModels;



namespace ClinicBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public DoctorsController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> users,
                                 RoleManager<IdentityRole> roles)
        {
            _context = context;
            _users = users;
            _roles = roles;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            var query = _context.Doctors
                                .Include(d => d.Specialty)
                                .Include(d => d.User);
            return View(await query.ToListAsync());
        }

        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Specialty)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        // ======== CREATE (User + Doctor in one step) ========

        // GET: Doctors/Create
        public IActionResult Create()
        {
            var vm = new CreateDoctorVm
            {
                // Specialties dropdown
                Specialties = _context.Specialties
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    })
                    .ToList(),

                // Pre-fill 7 days of working hours
                WorkingHours = Enumerable.Range(0, 7).Select(day => new WorkingHourVm
                {
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    IsActive = false
                }).ToList()
            };

            return View(vm);
        }


        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDoctorVm vm)
        {
            if (!ModelState.IsValid)
            {
                // repopulate specialties if validation fails
                vm.Specialties = _context.Specialties
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList();
                return View(vm);
            }

            // Ensure Doctor role exists
            if (!await _roles.RoleExistsAsync("Doctor"))
                await _roles.CreateAsync(new IdentityRole("Doctor"));

            // Check if email already exists
            var existing = await _users.FindByEmailAsync(vm.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                vm.Specialties = _context.Specialties
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList();
                return View(vm);
            }

            // Create Identity user
            var user = new ApplicationUser
            {
                Email = vm.Email,
                UserName = vm.Email,
                FullName = vm.FullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var createRes = await _users.CreateAsync(user, vm.Password);
            if (!createRes.Succeeded)
            {
                foreach (var e in createRes.Errors) ModelState.AddModelError("", e.Description);
                vm.Specialties = _context.Specialties
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList();
                return View(vm);
            }

            await _users.AddToRoleAsync(user, "Doctor");

            // Create Doctor profile
            var doctor = new Doctor
            {
                UserId = user.Id,
                SpecialtyId = vm.SpecialtyId,
                Room = vm.Room ?? "",
                SlotMinutes = vm.SlotMinutes <= 0 ? 15 : vm.SlotMinutes,
                Fee = vm.Fee
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            // Save working hours
            foreach (var wh in vm.WorkingHours.Where(w => w.IsActive))
            {
                _context.DoctorWorkingHours.Add(new DoctorWorkingHour
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = wh.DayOfWeek,
                    StartTime = TimeOnly.FromTimeSpan(wh.StartTime),
                    EndTime = TimeOnly.FromTimeSpan(wh.EndTime),
                    IsActive = true
                });
            }
            await _context.SaveChangesAsync();

            TempData["msg"] = $"Doctor {vm.FullName} created successfully.";
            return RedirectToAction(nameof(Index));
        }


        // ======== EDIT (Doctor entity only; not Identity user) ========

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                       .Include(d => d.User)
                                       .FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null) return NotFound();

            ViewData["SpecialtyId"] = new SelectList(_context.Specialties.OrderBy(s => s.Name), "Id", "Name", doctor.SpecialtyId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SpecialtyId,Room,SlotMinutes,Fee,UserId")] Doctor doctor)
        {
            if (id != doctor.Id) return NotFound();

            // Do not allow changing UserId here from UI (enforce original)
            var original = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (original == null) return NotFound();
            doctor.UserId = original.UserId;

            if (!ModelState.IsValid)
            {
                ViewData["SpecialtyId"] = new SelectList(_context.Specialties.OrderBy(s => s.Name), "Id", "Name", doctor.SpecialtyId);
                return View(doctor);
            }

            try
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Doctor updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoctorExists(doctor.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ======== DELETE ========

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Specialty)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Doctor deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id) => _context.Doctors.Any(e => e.Id == id);
    }

    
}
