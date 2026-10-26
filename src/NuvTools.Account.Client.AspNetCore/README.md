# NuvTools.Account.Client.AspNetCore

Server side of signing in through **Nuv Tools Account**, for a product's ASP.NET Core API and web host.

A product signs people in at the Account and gets back an access token issued for its own API —
the token's audience is the scope the product's client is given at the Account, for example
`dataprovider-api`. This package lets the product's API accept that token on the one endpoint that
turns it into the product's own session, and lets the product's web host tell its browser client
which Account to use.

## Accepting the Account's token

```csharp
builder.Services.AddAuthentication(/* the product's own default scheme */)
    .AddNuvToolsAccount(options =>
    {
        options.Authority = "https://account.nuvtools.com/";
        options.Audience = "dataprovider-api";
    });
```

The scheme (`NuvToolsAccountDefaults.AuthenticationScheme`) validates the signature against the keys
in the Account's discovery document, and requires exactly that issuer, exactly that audience and an
unexpired token. Claims keep the Account's names (`NuvToolsAccountClaims`). Left unconfigured it
refuses every token.

It is not made the default scheme. Name it on the endpoint that opens the product's session:

```csharp
[HttpPost("account")]
[Authorize(AuthenticationSchemes = NuvToolsAccountDefaults.AuthenticationScheme)]
public async Task<IActionResult> SignInAsync()
{
    var person = User.GetNuvToolsAccountPerson()!; // Subject, Email, Name
    // Find the local user by person.Subject — for example an Identity external login under
    // NuvToolsAccountDefaults.LoginProvider — create or link it, and issue the product's own token.
}
```

`User.GetNuvToolsAccountContext()` says which context the token was issued in: personal, or an
Account organization (`OrganizationId`) with the person's role there (`NuvToolsAccountRoles`:
`Owner`, `Billing`, `Member`). The Account checks the membership when it issues the token and on
every refresh, so a product can open a session inside its own organization linked to that one and
take the Account's role from it.

`User.GetNuvToolsAccountPlans()` — or `GetNuvToolsAccountPlan("clinuv")` — says what the context
subscribes to among the products whose APIs the token is for: the product, the plan and what it
grants (`Entitlements`, the JSON object the product defined in the Account's catalogue). A product
with no plan in the token is not entitled. The Account reads the subscription again on every token
renewal, so a plan bought, changed or lapsed reaches the product with the next token.

The Account issues tokens only for confirmed addresses and holds each address once, so linking a
person seen for the first time to the local user holding the same address is safe. Never re-link an
address already linked to a different subject.

## Telling the browser client which Account to use

```csharp
builder.Services.Configure<NuvToolsAccountClientSettings>(builder.Configuration.GetSection("Account"));
// …
app.MapNuvToolsAccountSettings(); // GET /account-settings.json → { "authority": …, "clientId": … }
```

`NuvTools.Account.Client.Blazor` reads that address by default. Add
`NuvToolsAccountClientSettings.AuthorityOrigin` to the page's `connect-src`: the browser redeems the
authorization code at the Account itself.

## The Account's API for products

The product's backend tells the Account who uses it — as Microsoft 365 licensing does for an app —
while its roles and permissions stay in the product, as Dataverse's do for Dynamics 365.

```csharp
var credential = new DefaultAzureCredential();

builder.Services.AddNuvToolsAccountApi(
    options => options.BaseUrl = "https://account.nuvtools.com",
    async cancellationToken => (await credential.GetTokenAsync(
        new TokenRequestContext(["api://{tenant}/nuv-acc-products-prod/.default"]), cancellationToken)).Token);
```

The managed identity must be among the Account's `Billing:Products:n:CallerApplicationIds` for the
product, and in `Security:Products:AllowedApplicationIds`.

- `INuvToolsAccountApi` enrols an Account organization (or a person on their own) when the product
  creates its tenant, ends the enrolment when it goes, publishes the licence types the product sells,
  and gives or takes licences. Writes name the person acting; the product decides from its own roles
  that they may, and the Account records it.
- `NuvToolsAccountGate.CheckAsync(personId, accountOrganizationId)` answers whether the person may use
  the product now and with which licence — `null` when not. It asks the Account once a minute, keeps
  the last answer up to five minutes while the Account cannot be reached and then refuses, and never
  lets in anybody it has not seen. Call `Evict` after the product changes something itself.

