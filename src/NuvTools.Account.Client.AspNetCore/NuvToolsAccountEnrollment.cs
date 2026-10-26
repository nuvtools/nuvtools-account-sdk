namespace NuvTools.Account.Client.AspNetCore;

/// <summary>An organization's or a person's use of the product, as the Account holds it.</summary>
/// <param name="EnrollmentId">The reference to keep next to the product's own tenant.</param>
/// <param name="OrganizationId">The Account organization enrolled, or <see langword="null"/> for a person.</param>
/// <param name="PersonId">The person enrolled on their own, or <see langword="null"/> for an organization.</param>
/// <param name="Status">Whether its people get in.</param>
/// <param name="EnabledByPersonId">Who enrolled it.</param>
/// <param name="EnabledOnUtc">When.</param>
public record NuvToolsAccountEnrollment(
    Guid EnrollmentId,
    Guid? OrganizationId,
    Guid? PersonId,
    NuvToolsAccountEnrollmentStatus Status,
    Guid EnabledByPersonId,
    DateTimeOffset EnabledOnUtc);
