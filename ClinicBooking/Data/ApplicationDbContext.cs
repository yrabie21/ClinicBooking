using ClinicBooking.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorWorkingHour> DoctorWorkingHours => Set<DoctorWorkingHour>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // -------- Specialty
        b.Entity<Specialty>()
            .HasIndex(x => x.Name)
            .IsUnique();

        // -------- Doctor
        b.Entity<Doctor>(e =>
        {
            // Decimal Fee precision
            e.Property(x => x.Fee).HasColumnType("decimal(10,2)");

            e.Property(x => x.SlotMinutes)
             .HasDefaultValue(15);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Specialty)
             .WithMany()
             .HasForeignKey(x => x.SpecialtyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // -------- DoctorWorkingHour
        b.Entity<DoctorWorkingHour>()
         .ToTable(tb => tb.HasCheckConstraint(
             "CK_WorkingHours_Range", "[StartTime] < [EndTime]"));

        // -------- Appointment
        b.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.Doctor)
             .WithMany()
             .HasForeignKey(a => a.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Patient)
             .WithMany()
             .HasForeignKey(a => a.PatientUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // Time range guard
            e.ToTable(tb => tb.HasCheckConstraint(
                "CK_Appointment_TimeRange", "[StartDateTime] < [EndDateTime]"));

            // Helpful lookup for patient history
            e.HasIndex(x => new { x.PatientUserId, x.StartDateTime })
             .HasDatabaseName("IX_Appointments_Patient_Start");

            // ---- Double-booking prevention (only for ACTIVE appointments)
            // Use the enum values dynamically so we don't hard-code numbers.
            var booked = (int)ApptStatus.Booked;
            var checkedIn = (int)ApptStatus.CheckedIn;

            // Remove any duplicate/plain index; keep only this filtered UNIQUE index.
            e.HasIndex(x => new { x.DoctorId, x.StartDateTime })
             .IsUnique()
             .HasDatabaseName("IX_Appointments_Doctor_Start_Unique_Active")
             .HasFilter($"[Status] IN ({booked},{checkedIn})");
        });
    }
}
