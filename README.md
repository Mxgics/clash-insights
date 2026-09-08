# Clash Insights

Personal player tracking and a clan dashboard, running locally with Angular, ASP.NET Core and PostgreSQL.

Run from PowerShell with Docker Desktop running:

    ./scripts/Start-Local.ps1

Open http://127.0.0.1:5188. The default is clearly labelled synthetic demo data. The script generates an ignored local database password and starts a PostgreSQL container bound to loopback. Ctrl+C stops the API and collector; docker compose stop stops the database without deleting history.

See docs/runbooks/v1-local.md for live configuration, backup and recovery. Hosted operation and authentication belong to v2; see docs/runbooks/v2-hosting-auth.md. No live API credentials are included.
