using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.Validators;

/// <summary>
/// Validation attribute for a date that must already have happened, such as a date of birth.
/// A missing value is left to [Required]: this attribute only judges the value it is given.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute()
        : base("The {0} field must be a date in the past.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is DateOnly date && date < DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
