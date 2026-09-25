# 003: Activate live clan collection

Date: 2026-09-18.

Problem/cause: the dashboard remained in Demo mode because no local token or tracked clan had been configured. The existing collector already supported the needed clan and current-war routes.

Change: configured the locally tracked clan ([redacted clan tag]) and the supplied credential in ignored local JSON, enabled Live mode, and started the local host. Added a current-war-specific 403 message while preserving generic 401 handling and unavailable data semantics. Excluded local settings from publish output so a future publish cannot accidentally package the token.

Verification: direct clan request returned 200; actual collector persisted and the browser displayed all 40 members. Persisted clan payload reports isWarLogPublic=false; current-war request returned 403 accessDenied and no war snapshot was created. All 23 backend tests passed. Details and observation time are in ../test-plan.md.

Limits: full endpoint documentation requires developer login. No exact API quota is claimed. Player detail collection has no configured targets. Successful war collection awaits public war visibility; no historical observations are invented. No commits, pushes, hosting or firewall changes were made.
