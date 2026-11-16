# PTI Employee Synchronization — Domain-Driven Design

This is the **PTI Employee Synchronization bounded context**, not a general employee/personnel system.

## Ubiquitous language

| Domain term | Meaning |
| --- | --- |
| **EmployeeNumber** | PTI identity. Keep it as a string (never parse it as an integer). |
| **EmployeePin** | PTI credential. Keep its original formatting, including leading zeroes. |
| **PtiEmployee** | Valid employee data for synchronization; cannot be created without number, PIN, name and department. |
| **Synchronize** | Save an employee; the remote system reports Created or Updated. |
| **ChangeActivation** | Set an existing employee's active state. It never deletes the employee. |
| **PtiEmployeeSnapshot** | Remote read projection; may be incomplete, so it is validated before becoming a write command. |
| **EmployeeSyncOutcome** | Result of a synchronization command, including the domain change kind. |

## Boundaries and dependency direction

```text
Genetec observers / EmployeeManager
    |   (existing IEmployeeApiSrv and legacy DTOs)
    v
Infrastructure inbound adapter: GraphQLEmployeeApiSrv
    |   (maps legacy DTO -> domain model)
    v
Application: EmployeeSynchronizationService
    |   (depends only on domain + outbound port)
    v
Outbound port: IPtiEmployeeGateway
    ^
    |   implements
Infrastructure outbound adapter: GraphQLEmployeeGateway
    |   (maps domain -> GraphQL EmployeeInput)
    v
PtiGraphQLClient -> PTI GraphQL API
```

- `LGC.PTI.EmployeeSync.Domain`: immutable identity and PIN value objects, `PtiEmployee` entity and `EmployeeChangeKind` vocabulary; **no GraphQL, REST, Genetec, DI or logging dependencies**.
- `LGC.PTI.EmployeeSync.Application`: synchronization and activation use cases; outbound port `IPtiEmployeeGateway`; **no HTTP or GraphQL types**.
- `LGC.PTI.HTTPClient/GraphQL`: the anti-corruption layer around the external GraphQL schema and the legacy `IEmployeeApiSrv` contract.
- `LGC.PTI.GraphQL.Transport`: infrastructure protocol (GraphQL.Client, login, raw Firebase JWT authorization, expiry, retry and server error parsing).

## Enforced rules

1. **No employee deletion.** Deactivation changes only `Active = false` using the existing upsert mutation.
2. **No arbitrary deactivation.** Exactly one non-deleted employee must match the requested number; otherwise return an error without writing.
3. **No partial mutation.** A remote record missing mandatory fields (especially the PIN) cannot be transformed into a write command.
4. **Preserve existing data.** When changing activation, keep PIN, names, department, type and unrestricted-usage value.
5. **Keep PIN formatting.** `"00123"` and `"123"` are distinct values. Do not write the PIN to normal logs or `ToString()`.
6. **Use Upsert.** Creation and updating do not require a preliminary GET; map GraphQL `status: "new"/"updated"` at the outbound adapter into `EmployeeChangeKind.Created/Updated`.
7. **Do not trust transport success alone.** The GraphQL client validates protocol errors; the gateway validates number, status, deletion state and per-employee errors.

## Compatibility / migration scope

- Existing Refit implementation and observers are unchanged; GraphQL is **not yet registered in the plugin's DI container**.
- `GraphQLEmployeeApiSrv` retains all members of the current `IEmployeeApiSrv`, including legacy empty-result behavior for `TryGetEmployee`.
- Existing PIN/Mapster mapping remains in the Genetec-facing boundary.
- Unit tests use real production source for the adapter and fake HTTP/GraphQL infrastructure, without live API calls.
- **Not yet DDD-migrated:** `EmployeeManager` orchestration, plugin DI settings/UI, incident/logging policies, and live Genetec integration. These belong to a follow-up migration, not this change.
- GraphQL currently provides `getEmployees` without a single-employee query; `ChangeActivationAsync` must read the collection. Consider a single-ID query if PTI provides one later.

## Test

```shell
dotnet test LGC.PTI.GraphQL.Transport.Tests/LGC.PTI.GraphQL.Transport.Tests.csproj --configuration Release
```

The solution-wide build still requires the existing Genetec SDK and other private dependencies in the configured development environment.
