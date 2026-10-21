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
