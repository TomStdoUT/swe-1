# ADR 002: Stateless JWT Authentication

**Date:** November 15, 2026  
**Status:** Accepted  

## Context
The platform needs to identify users on every request: creating polls, voting with staked points, commenting, following users and buying cosmetics. The backend is a .NET API and the frontend is a separate React (Vite) app that talks to it over `/api`. We needed an authentication approach that is simple to build, works across the two apps, and does not require extra infrastructure.

Options considered:
1. **Server-side cookie sessions** (session stored on the server, session ID in an HTTP-only cookie).
2. **ASP.NET Core Identity** (full user management framework).
3. **Stateless JWT bearer tokens** with BCrypt-hashed passwords.

## Decision
We use stateless JWT bearer tokens:

- Passwords are hashed with **BCrypt** (`BCrypt.Net-Next`) and never stored in plain text.
- On register/login the `UserService` issues an HS256-signed JWT that expires after **7 days**. It carries `ClaimTypes.NameIdentifier` (user id) and `ClaimTypes.Name` (username).
- The frontend stores the token in `sessionStorage` and sends it as an `Authorization: Bearer` header. Endpoints are protected with `[Authorize]`.
- Controllers read the user id from the token claim, so no `User` object has to be passed around (see ADR 001).

ASP.NET Core Identity was rejected as too heavy for the scope of this project (no roles, email confirmation or external logins are needed). Cookie sessions were rejected because they need server-side session state and CSRF protection, and complicate the separate frontend/backend setup.

## Consequences

### Positive
* **No session storage:** the API stays stateless, so there is no session table or cache to maintain.
* **Simple frontend integration:** one header on each `fetch` call, and the Vite dev proxy works without cookie or CORS-credential configuration.
* **Small surface area:** a few lines of configuration instead of a full identity framework.

### Negative
* **No revocation:** a stolen or leaked token stays valid until it expires (7 days). Logging out only deletes the token on the client. Fixing this would need a token blacklist or short-lived access tokens with refresh tokens.
* **XSS exposure:** tokens in `sessionStorage` can be read by any injected script. HTTP-only cookies would protect against this.
* **Weak key handling:** the signing key falls back to a hardcoded string in both `Program.cs` and `UserService` when `Jwt:Key` is not configured. This is acceptable for a course project but must be replaced by a secret from configuration or environment before any real deployment.
* **Relaxed validation:** issuer and audience validation are disabled, which is fine with a single API but would need tightening if more services are added.
* **Open CORS:** the API currently allows any origin, which is only safe because authentication relies on a header and not on cookies.
