namespace ClinicBook.Core.Entities;

/// <summary>
/// A medical specialty, e.g. "Dermatology". One specialty has many doctors.
/// </summary>
public class Specialty
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation property: lets EF Core (and our code) move from a specialty to its doctors.
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
