# NuvTools.Account.Client.Blazor

Browser side of signing in through **Nuv Tools Account**, for a product's Blazor WebAssembly app: the
authorization code flow with PKCE as a public client, the callback that redeems the code, and signing
out through the Account's end-session page.

The Account's access token is handed to the product once, on the callback — typically to exchange
it at the product's API for the product's own session (see `NuvTools.Account.Client.AspNetCore`). No
refresh token is asked of the Account.

## Registering

```csharp
builder.Services.AddNuvToolsAccountSignIn(options =>
{
    options.Scopes.Add("dataprovider-api");   // the product API's scope, which is also its audience
    options.DefaultReturnPath = "/portal";
});
```

With `Authority` and `ClientId` left empty, they are read from `account-settings.json` on the app's own
host, which `MapNuvToolsAccountSettings` serves per environment. The client must be registered at the
Account with the redirect address `{base}authentication/login-callback` and the post-sign-out address
`{base}` (both configurable).

## Signing in

```csharp
// The sign-in screen
if (!AccountSignIn.SigningOut)
    await AccountSignIn.StartAsync(returnUrl);

// The page at /authentication/login-callback
var completed = await AccountSignIn.CompleteAsync(new Uri(Navigation.Uri),
    accountAccessToken => Authentication.SignInWithAccountAsync(accountAccessToken));

Navigation.NavigateTo(completed.Succeeded ? completed.Data!.ReturnUrl : "/login?failed=true", replace: true);
```

`CompleteAsync` runs once per page load: opening a session usually rebuilds the callback page, and
the second instance receives the first one's answer. The state is spent on the way in, so the same
response cannot be used twice. Return addresses off the app are replaced by `DefaultReturnPath`.

## Organizations

Add the Account's own API to the scopes (`options.Scopes.Add("account-api")`, and give the client that
scope at the Account) to list the person's organizations for a switcher:

```csharp
var organizations = await AccountSignIn.GetOrganizationsAsync(); // Id, Name, Role
await AccountSignIn.SwitchAsync(organizationId, returnUrl);      // null = personal context
```

`SwitchAsync` asks the Account for that organization with `prompt=none`, so no sign-in page appears
while the Account session lasts; the callback completes as for a sign-in, with a token naming the
organization and the person's role. `GetOrganizationsAsync` uses the access token of the last sign-in
in the tab and fails once it has expired; a switch or a sign-in brings a new one.

## Signing out

```csharp
var accountSignOut = await AccountSignIn.SignOutAddressAsync(); // read before clearing the session
await Authentication.LogoutAsync();
Navigation.NavigateTo(accountSignOut ?? "/", forceLoad: true);
```

`SignOutAddressAsync` sets `SigningOut`, which a sign-in screen checks so that ending the local
session does not start a new sign-in and cancel the trip to the Account. `ManageAddressAsync` gives
the Account's page where the person manages their password, second factors and passkeys.
