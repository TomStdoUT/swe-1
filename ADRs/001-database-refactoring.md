# ADR 001: Database Architecture and Data Access Refactoring

**Date:** November 8, 2026  
**Status:** Accepted  

## Context
During the initial development of the Polling & Opinion Prediction Platform (`swe-1`), several architectural anti-patterns emerged as the application transitioned to Entity Framework (EF) Core and SQLite:
1. **Entity Leaking:** EF Core tracking entities (`Poll`, `Comment`, `Vote`) were being serialized directly to the frontend. This exposed internal database keys, schema design, and relational mappings to the client, while tightly coupling the API payload to the database structure.
2. **Fat Services & In-Memory Filtering:** The `PollRepository` utilized a generic `GetAll()` method that eager-loaded the entire dataset into memory. The `PollService` then performed business filtering (e.g., `Where(p => p.IsClosed() && !p.IsResolved)`) in RAM, which scales poorly and defeats the purpose of a relational database.
3. **Fragile State Management:** Services called `SaveStateAsync()` on individual repositories sequentially. If a multi-step business transaction (like resolving a poll and awarding points) crashed halfway through, it risked leaving the database in a corrupted, partial state.
4. **Improper Claim Mapping:** Custom JWT claims (`"userId"`) were creating mapping conflicts with ASP.NET Core's default identity middleware, resulting in failed authorizations.

## Decision
To resolve these issues and adhere to clean architecture principles, we decided to implement the following changes:

1. **Introduce Data Transfer Objects (DTOs):** We introduced strict read-DTOs (e.g., `PollFeedDto`) to map data out of EF Core entities before leaving the Service layer.
2. **Push Queries to the Repository:** We created specific, strongly-typed query methods in `IPollRepository` (e.g., `GetUnresolvedClosedPolls()`) to ensure filtering translates to SQL and executes directly on the database engine.
3. **Implement Unit of Work:** We abstracted the `AppDbContext.SaveChangesAsync()` method behind a new `IUnitOfWork` interface. Repositories no longer save their own state; instead, the Service layer orchestrates the business logic and calls `_unitOfWork.SaveChangesAsync()` once at the end of the operation.
4. **Standardize JWT Claims:** We updated the `UserService` to utilize the industry-standard `ClaimTypes.NameIdentifier` for the user's ID, avoiding ASP.NET identity conflicts and removing the need to pass full `User` objects into service methods.

## Consequences

### Positive
* **Data Security & Decoupling:** The frontend is completely decoupled from the database schema. Internal relational structures are no longer exposed over the network.
* **Performance:** Database queries are drastically more efficient as filtering is handled by SQLite rather than the C# application's memory.
* **Data Integrity:** The Unit of Work guarantees atomicity. Complex operations involving multiple entities will either succeed completely or roll back entirely, preventing data corruption.
* **Standards Compliance:** Using standard JWT claims aligns the project with .NET Core best practices and fixes the "Unknown error" 401 Unauthorized bugs.

### Negative
* **Increased Boilerplate:** The codebase now requires more mapping logic between Entities and DTOs, as well as the overhead of maintaining the `IUnitOfWork` interface across all services.