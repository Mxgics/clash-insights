# ADR 002: Hosted visibility, identity, and deployment boundary

Status: accepted for implementation; provider selection and deployment remain pending.

## Decision

Clash Insights remains one container with Angular served by ASP.NET Core and PostgreSQL as its store. Anonymous users receive only three manually approved clans and explicitly approved players through `/api/public/*`. Auth0 authorization-code flow with PKCE and a secure server cookie protects `/api/private/*`; identity must also match a configured subject or email allowlist. Everything not expressly public is private, and denied tags return 404.

Hosted startup fails closed unless Live mode, exactly three public clan tags, Auth0 settings, an identity allowlist, and trusted proxy addresses are present. Forwarded headers are accepted only from those proxies. HTTPS/HSTS, CSP/frame denial, request limits, antiforgery, auth rate limiting, generic errors, and private cache controls are applied server-side.

## Rationale

Filtering only in Angular would expose snapshots, history, attempts, status, and error metadata through direct calls. Query-boundary filtering is consistent and testable. Server-side OIDC avoids stored passwords and client secrets. A single artifact reduces version skew; the PostgreSQL advisory lock prevents duplicate collectors across replicas.

## Consequences

`the locally tracked clan` is private even though it is locally tracked. Data-protection keys must persist. Render is preferred only if staging proves acceptable egress and cost; otherwise Azure App Service is the fallback. This ADR does not authorize provisioning or deployment.
