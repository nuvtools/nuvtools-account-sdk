namespace NuvTools.Account.Client.Products;

/// <summary>A person of an enrolment's tenant and the licence they hold.</summary>
/// <param name="PersonId">Their Account subject.</param>
/// <param name="Name">Their full name.</param>
/// <param name="Email">Their address.</param>
/// <param name="MembershipRole">Their role at the Account organization — 1 member, 2 billing, 3 owner; <see langword="null"/> in a personal enrolment.</param>
/// <param name="License">The code of the licence they hold, if any.</param>
public record NuvToolsAccountEnrollmentPerson(Guid PersonId, string Name, string Email, int? MembershipRole, string? License);
