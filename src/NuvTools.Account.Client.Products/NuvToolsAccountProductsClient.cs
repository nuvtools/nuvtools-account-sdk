using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using System.Net.Http.Json;

namespace NuvTools.Account.Client.Products;

/// <inheritdoc cref="INuvToolsAccountProducts" />
/// <remarks>The token is added by <see cref="NuvToolsAccountProductsTokenHandler"/>, on the client this one is built with.</remarks>
public class NuvToolsAccountProductsClient(
    HttpClient httpClient,
    NuvToolsAccountProductsOptions options,
    ILogger<NuvToolsAccountProductsClient> logger) : INuvToolsAccountProducts
{
    /// <summary>The header naming the person a product acts for.</summary>
    public const string ActingPersonHeader = "X-NuvTools-Acting-Person";

    private const string Base = "api/v1/products/me";

    public Task<IResult<NuvToolsAccountAccess>> GetAccessAsync(Guid personId, Guid? organizationId, CancellationToken cancellationToken = default)
        => SendAsync<NuvToolsAccountAccess>(HttpMethod.Get,
            $"{Base}/access?personId={personId}" + (organizationId is { } organization ? $"&organizationId={organization}" : string.Empty),
            null, null, cancellationToken);

    public Task<IResult<NuvToolsAccountEnrollment>> GetEnrollmentAsync(Guid? organizationId, Guid? personId, CancellationToken cancellationToken = default)
        => SendAsync<NuvToolsAccountEnrollment>(HttpMethod.Get,
            organizationId is { } organization ? $"{Base}/enrollments/owner?organizationId={organization}" : $"{Base}/enrollments/owner?personId={personId}",
            null, null, cancellationToken);

    public Task<IResult<NuvToolsAccountEnrollment>> EnrollAsync(Guid actingPersonId, Guid? organizationId, Guid? personId, CancellationToken cancellationToken = default)
        => SendAsync<NuvToolsAccountEnrollment>(HttpMethod.Post, $"{Base}/enrollments", new { organizationId, personId }, actingPersonId, cancellationToken);

    public Task<IResult<NuvToolsAccountEnrollment>> SetEnrollmentStatusAsync(
        Guid actingPersonId, Guid enrollmentId, NuvToolsAccountEnrollmentStatus status, CancellationToken cancellationToken = default)
        => SendAsync<NuvToolsAccountEnrollment>(HttpMethod.Patch, $"{Base}/enrollments/{enrollmentId}", new { status }, actingPersonId, cancellationToken);

    public async Task<IResult> EndEnrollmentAsync(Guid actingPersonId, Guid enrollmentId, CancellationToken cancellationToken = default)
        => await SendAsync<object>(HttpMethod.Delete, $"{Base}/enrollments/{enrollmentId}", null, actingPersonId, cancellationToken, allowEmptyData: true);

    public async Task<IResult> PublishLicenseTypesAsync(IReadOnlyList<NuvToolsAccountLicenseType> licenseTypes, CancellationToken cancellationToken = default)
        => await SendAsync<object>(HttpMethod.Put, $"{Base}/license-types", licenseTypes, null, cancellationToken);

    public Task<IResult<IReadOnlyList<NuvToolsAccountEnrollmentPerson>>> GetPeopleAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<NuvToolsAccountEnrollmentPerson>>(HttpMethod.Get, $"{Base}/enrollments/{enrollmentId}/people", null, null, cancellationToken);

    public Task<IResult<NuvToolsAccountEnrollmentPerson>> SetLicenseAsync(
        Guid actingPersonId, Guid enrollmentId, Guid personId, string? licenseType, CancellationToken cancellationToken = default)
        => licenseType is null
            ? SendAsync<NuvToolsAccountEnrollmentPerson>(HttpMethod.Delete, $"{Base}/enrollments/{enrollmentId}/people/{personId}/license", null, actingPersonId, cancellationToken)
            : SendAsync<NuvToolsAccountEnrollmentPerson>(HttpMethod.Put, $"{Base}/enrollments/{enrollmentId}/people/{personId}/license", new { licenseType }, actingPersonId, cancellationToken);

    private async Task<IResult<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, Guid? actingPersonId, CancellationToken cancellationToken, bool allowEmptyData = false)
    {
        if (!options.IsConfigured) return NotAvailable<T>(path, "not configured");

        try
        {
            using var request = new HttpRequestMessage(method, path);

            if (body is not null) request.Content = JsonContent.Create(body);

            if (actingPersonId is { } acting) request.Headers.Add(ActingPersonHeader, acting.ToString());

            using var response = await httpClient.SendAsync(request, cancellationToken);

            // Every refusal of the Account comes in its own result wrapper, a "not found" included.
            var result = await response.ToResultAsync<T>(cancellationToken);

            if (result.Succeeded || result.ContainsNotFound || (int)response.StatusCode is >= 400 and < 500)
                return result.Succeeded && result.Data is null && !allowEmptyData
                    ? NotAvailable<T>(path, "an answer without data")
                    : result;

            return NotAvailable<T>(path, $"{(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException
                                                     or System.Text.Json.JsonException)
        {
            return NotAvailable<T>(path, exception.Message);
        }
    }

    private IResult<T> NotAvailable<T>(string path, string reason)
    {
        logger.LogWarning("Nuv Tools Account could not be asked {Path}: {Reason}.", path, reason);

        return Result<T>.Fail(NuvToolsAccountProductsMessages.NotAvailable);
    }
}
