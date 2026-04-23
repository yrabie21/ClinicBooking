using ClinicBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Data;
public static class Seed
{
    public static async Task Run(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var r in new[] { "Admin", "Doctor", "Patient" })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        async Task<ApplicationUser> Ensure(string email, string name, string role)
        {
            var u = await users.FindByEmailAsync(email);
            if (u == null)
            {
                u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
                await users.CreateAsync(u, "Pass123!");
                await users.AddToRoleAsync(u, role);
            }
            return u;
        }

        var admin = await Ensure("admin@clinic.local", "Admin User", "Admin");
        var patient = await Ensure("patient1@clinic.local", "Patient One", "Patient");
        var docUser = await Ensure("dr.smith@clinic.local", "Dr. John Smith", "Doctor");

        if (!await db.Specialties.AnyAsync())
        {
            db.Specialties.AddRange(new Specialty { Name = "Cardiology" }, new Specialty { Name = "Dermatology" });
            await db.SaveChangesAsync();
        }

        if (!await db.Doctors.AnyAsync())
        {
            var spec = await db.Specialties.FirstAsync();
            var d = new Doctor { UserId = docUser.Id, SpecialtyId = spec.Id, Room = "101", SlotMinutes = 15, Fee = 200 };
            db.Doctors.Add(d); await db.SaveChangesAsync();

            db.DoctorWorkingHours.AddRange(
                new DoctorWorkingHour { DoctorId = d.Id, DayOfWeek = 0, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0) },
                new DoctorWorkingHour { DoctorId = d.Id, DayOfWeek = 2, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0) },
                new DoctorWorkingHour { DoctorId = d.Id, DayOfWeek = 4, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0) }
            );
            await db.SaveChangesAsync();
        }
    }
}
