# Nuv Tools Account SDK

The .NET packages a NuvTools product installs to sign people in through **Nuv Tools Account**.

| Package | For | What it does |
|---|---|---|
| [`NuvTools.Account.Client.AspNetCore`](src/NuvTools.Account.Client.AspNetCore/README.md) | the product's API and web host | Accepts the Account's access tokens issued for the product, names their claims, serves the Account's address to the browser, and calls the Account's API for products — enrolments, licences and the gate |
| [`NuvTools.Account.Client.Blazor`](src/NuvTools.Account.Client.Blazor/README.md) | the product's Blazor WebAssembly app | Authorization code flow with PKCE, the callback, keeping and renewing the Account's token for the product's API, and signing out through the Account |

Both target .NET 8, 9 and 10. Only Nuv Tools products use them: the Account knows each product's client and backend identity. The Data Provider is the first product to use them.

## How a product signs people in

1. The product's client is registered at the Account (redirect address, post-sign-out address, and the
   product API's scope, which is also the audience its tokens carry).
2. The browser leaves for the Account, the person signs in there — password, second factor, passkey,
   Google or Microsoft — and comes back with a code, which the browser redeems.
3. The browser calls the product's API with the Account's access token, renewed with the refresh
   token the Account issues for `offline_access`. The API validates it, asks the Account's gate whether
   the person may use the product, and decides what they may do from its own roles — as Dynamics 365
   does with Entra's token and Dataverse's security roles. No product issues a token of its own.

## Building

```bash
dotnet build NuvTools.Account.Sdk.slnx
dotnet test  NuvTools.Account.Sdk.slnx
```

## Publishing

One workflow per package (`aspnetcore-publish.yml`, `blazor-publish.yml`), run by hand. Unchecked, it
packs and verifies; checked, it pushes the `<Version>` in the project to nuget.org through trusted
publishing — approved in the `nuget` environment — and then tags the commit. Versions follow the
NuvTools rule: major is the .NET version, minor a breaking change, patch everything else.
