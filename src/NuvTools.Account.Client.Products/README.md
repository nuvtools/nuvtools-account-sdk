# NuvTools.Account.Client.Products

A product's backend talking to **Nuv Tools Account** about who uses it, as the product: enrolments,
licence types and licences, and the gate.

The split is Microsoft's: the Account is Entra ID plus the Microsoft 365 licences — who the person is,
which organization, whether they may use the product and with which licence. Roles and permissions
stay in the product, as Dataverse security roles do for Dynamics 365.

## Registering

```csharp
builder.Services.AddNuvToolsAccountProducts(options =>
{
    options.BaseUrl = "https://account.nuvtools.com";
    options.Scope = "api://{tenant}/nuv-acc-products-prod/.default";
}, new DefaultAzureCredential());
```

The managed identity must be among the Account's `Billing:Products:n:CallerApplicationIds` for the
product, and in `Security:Products:AllowedApplicationIds`.

## The gate

```csharp
var access = await gate.CheckAsync(personId, accountOrganizationId);   // null: may not use the product
```

`NuvToolsAccountGate` asks the Account once a minute per person and organization, and keeps the last
answer for up to five minutes while the Account cannot be reached; after that it refuses. It never lets
in anybody it has not seen. Call `Evict` after the product changes something itself.

## Enrolments and licences

`INuvToolsAccountProducts` enrols an Account organization (or a person on their own) when the product
creates its tenant, ends the enrolment when the tenant goes, publishes the licence types the product
sells, and gives or takes licences. Writes name the person acting; the product decides from its own
roles that they may, and the Account records it.
