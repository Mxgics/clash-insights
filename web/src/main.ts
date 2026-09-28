import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import * as Sentry from '@sentry/angular';

interface ClientConfig {
  sentryDsn?: string;
  environment: string;
  release?: string;
  tracesSampleRate: number;
}

fetch('/api/client-config', { credentials: 'same-origin' })
  .then(async (response) => (response.ok ? ((await response.json()) as ClientConfig) : undefined))
  .then((config) => {
    if (config?.sentryDsn) {
      Sentry.init({
        dsn: config.sentryDsn,
        environment: config.environment,
        release: config.release,
        dataCollection: {
          userInfo: false,
          cookies: false,
          httpHeaders: { request: false, response: false },
          httpBodies: [],
          urlQueryParams: false,
          genAI: { inputs: false, outputs: false },
          databaseQueryData: false,
          queues: false,
          graphQL: { document: false, variables: false },
          stackFrameVariables: false,
        },
        tracesSampleRate: config.tracesSampleRate,
        integrations: [Sentry.browserTracingIntegration()],
      });
    }
    return bootstrapApplication(App, appConfig);
  })
  .catch((error) => console.error(error));
