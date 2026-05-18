# DomainServices

Generic abstractions for service-oriented and domain-driven architectures in .NET. Designed to be consumed as NuGet packages by multiple downstream projects without carrying any project-specific concerns.

## Packages

| Package | Description |
| --- | --- |
| `DomainServices.Core` | Core abstractions: identity, audit, soft-delete, tenant scoping, validation, paged queries, party/person/organization, base responses, repository and domain-service contracts, and a generic HTTP service client. |

## What's inside `DomainServices.Core`

**Abstractions** — single-purpose marker interfaces that compose into richer contracts:

- `IIdentifiable`, `IAuditable`, `ISoftDelete`, `ITenantScoped`, `IValidatable`

**Models**

- `ICoreModel` — anything with a `Guid Id`
- `ICoreDomainModel` — adds audit fields (`DateTimeOffset`), soft-delete, `EnterpriseId` tenant scoping, and validation
- `CoreDomainModel` — abstract base implementing the full contract with sensible defaults and `OnValidate` template-method hook

**Validation**

- `ModelValidator` — fluent `Require`/`RequireNotEmpty`/`RequireNotNull` API, severity-aware results
- `ValidationResult` — `Property`, `Message`, `Severity`

**Query**

- `QueryParameterModel` — page, page size, ordering, search term, filters
- `FilterModel` + `FilterOperator` — field/operator/value triples

**Party**

- `IParty`, `IPerson`, `IOrganization` interfaces
- `Party`, `Person`, `Organization` abstract/concrete base classes
- `PartyType`, `Gender`, `ContactInfo`

**Responses**

- `IBaseResponse` + `IBaseResponse<T>` — combine HTTP semantics (`HttpStatusCode`, `IsSuccessful`) with result semantics (`Errors`, `Message`, `CorrelationId`)
- `BaseResponse` + `BaseResponse<T>` — factories: `Ok`, `Created`, `Updated`, `NoContent`, `NotFound`, `BadRequest`, `Unauthorized`, `Forbidden`, `Conflict`, `ServerError`

**Persistence**

- `IRepository<TModel>` — async CRUD, search, exists, batch save

**Services**

- `IDomainService<TModel>` — broad surface: sync + async CRUD, search, validation hooks, `SetToken`, enterprise scoping
- `DomainService<TModel>` — abstract base implementing the full contract on top of `IRepository<TModel>`, with `OnValidate`/`OnValidateUpdate`/`OnValidateDelete` extension points
- `IServiceClient<TModel>` + `DomainServiceClient<TModel>` — generic `HttpClient`-backed implementation using `System.Text.Json`

## Design principles

- **Generic only** — no project-specific or industry-specific types
- **Composable** — small interfaces (`IAuditable`, `ITenantScoped`) compose into `ICoreDomainModel`; consumers can also opt into individual contracts
- **Serializer-agnostic source** — no `[JsonProperty]`/`[JsonPropertyName]` attributes on contracts; clients can wire in any serializer
- **Modern .NET** — `net10.0`, nullable enabled, `DateTimeOffset` timestamps, `LangVersion=latest`, `TreatWarningsAsErrors=true`

## License

MIT
