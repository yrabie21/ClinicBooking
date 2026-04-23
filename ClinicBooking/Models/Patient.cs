public class Patient
{
    public int Id { get; set; }

    // Basic identity
    public string FullName { get; set; } = default!;
    public DateTime? BirthDate { get; set; }

    // Contacts (pick one as required)
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }

    // Optional: national id / medical record number
    public string? NationalId { get; set; }

    // Optional link to AspNetUsers if you later enable patient login
    public string? UserId { get; set; }           // FK -> AspNetUsers.Id
}
