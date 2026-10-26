namespace NuvTools.Account.Client.Products;

/// <summary>
/// The gate's answer: the person may use the product now, through this enrolment, with this licence.
/// What they may do inside is the product's to decide from its own roles.
/// </summary>
/// <param name="EnrollmentId">The enrolment the person gets in through.</param>
/// <param name="Status">Always active: a suspended enrolment has no answer.</param>
/// <param name="License">The licence held, on a product that sells licences; <see langword="null"/> on one that does not.</param>
/// <param name="Version">Moves on with any change to the enrolment or the product's licence types.</param>
public record NuvToolsAccountAccess(Guid EnrollmentId, NuvToolsAccountEnrollmentStatus Status, NuvToolsAccountLicense? License, long Version);
