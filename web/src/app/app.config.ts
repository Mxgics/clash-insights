import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withXsrfConfiguration } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import * as Sentry from '@sentry/angular';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withXsrfConfiguration({ cookieName: '__Host-clash-xsrf', headerName: 'X-XSRF-TOKEN' })),
    provideRouter(routes),
    { provide: ErrorHandler, useValue: Sentry.createErrorHandler({ showDialog: false }) },
  ],
};
