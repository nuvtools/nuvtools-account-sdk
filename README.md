# Nuv Tools Account SDK

The .NET packages a NuvTools product installs to sign people in through **Nuv Tools Account**.

| Package | For | What it does |
|---|---|---|
| [`NuvTools.Account.Client.AspNetCore`](src/NuvTools.Account.Client.AspNetCore/README.md) | the product's API and web host | Accepts the Account's access tokens issued for the product, names their claims, and serves the Account's address to the browser |
| [`NuvTools.Account.Client.Blazor`](src/NuvTools.Account.Client.Blazor/README.md) | the product's Blazor WebAssembly app | Authorization code flow with PKCE, the callback, and signing out through the Account |

Both target .NET 8, 9 and 10. The Data Provider is the first product to use them.

## How a product signs people in

1. The product's client is registered at the Account (redirect address, post-sign-out address, and the
   product API's scope, which is also the audience its tokens carry).
2. The browser leaves for the Account, the person signs in there — password, second factor, passkey,
   Google or Microsoft — and comes back with a code, which the browser redeems.
3. The browser presents the Account's access token once to the product's API, which validates it and
   opens the product's own session. Permissions and organizations stay the product's.

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
