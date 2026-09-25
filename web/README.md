# Frontend development

Run the full Demo application first using the root README. For Angular development:

```powershell
cd web
npm ci
npm start
```

The development server binds to http://127.0.0.1:4200 and proxies API requests to the local ASP.NET Core host on port 5188. Live credentials belong only on the server.

```powershell
npm test -- --watch=false
npm run build
npm run e2e
```

Browser tests use Chromium and require the full application at http://127.0.0.1:5188. Install it once with `npx playwright install chromium`. Run against isolated Demo data; do not use a personal Live database for release tests. Production output is `dist/web/browser`, copied into the API's `wwwroot` by the local startup script or Docker build.
