using NuvTools.Common.ResultWrapper;

namespace NuvTools.Account.Client.Products;

/// <summary>
/// The Account's <c>products/me</c> API, as the calling product: its enrolments, the licence types it
/// sells and the licences people hold, and the gate.
/// </summary>
/// <remarks>
/// <para>
/// Every write names the person acting — as the product read them from the Account token they called
/// it with — and the product has already decided, from its own roles, that they may. The Account
/// records who acted and checks only what it holds itself: that an owner enrols, and that a licence
/// goes to somebody of the tenant.
/// </para>
/// <para>
/// Unconfigured, every call fails without a request. A call the Account cannot answer fails with a
/// result that is not "not found", so a caller can tell "no" from "could not ask".
/// </para>
/// </remarks>
public interface INuvToolsAccountProducts
{
    /// <summary>Whether a person may use the product now, in an Account organization or on their own; not found when not.</summary>
    Task<IResult<NuvToolsAccountAccess>> GetAccessAsync(Guid personId, Guid? organizationId, CancellationToken cancellationToken = default);

    /// <summary>The enrolment of an Account organization, or of a person on their own; not found when there is none.</summary>
    Task<IResult<NuvToolsAccountEnrollment>> GetEnrollmentAsync(Guid? organizationId, Guid? personId, CancellationToken cancellationToken = default);

    /// <summary>Enrols an Account organization, or a person on their own. The acting person must own it; enrolling again answers the existing one.</summary>
    Task<IResult<NuvToolsAccountEnrollment>> EnrollAsync(Guid actingPersonId, Guid? organizationId, Guid? personId, CancellationToken cancellationToken = default);

    /// <summary>Suspends or reactivates an enrolment.</summary>
    Task<IResult<NuvToolsAccountEnrollment>> SetEnrollmentStatusAsync(Guid actingPersonId, Guid enrollmentId, NuvToolsAccountEnrollmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>Ends an enrolment with its licences, when the product's own tenant goes.</summary>
    Task<IResult> EndEnrollmentAsync(Guid actingPersonId, Guid enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the licence types the product sells; an empty list: it sells none.</summary>
    Task<IResult> PublishLicenseTypesAsync(IReadOnlyList<NuvToolsAccountLicenseType> licenseTypes, CancellationToken cancellationToken = default);

    /// <summary>The people of an enrolment's tenant, with the licence each holds.</summary>
    Task<IResult<IReadOnlyList<NuvToolsAccountEnrollmentPerson>>> GetPeopleAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>Gives a person a licence, replacing theirs, or takes it away when <paramref name="licenseType"/> is <see langword="null"/>; billed seats follow.</summary>
    Task<IResult<NuvToolsAccountEnrollmentPerson>> SetLicenseAsync(Guid actingPersonId, Guid enrollmentId, Guid personId, string? licenseType, CancellationToken cancellationToken = default);
}
