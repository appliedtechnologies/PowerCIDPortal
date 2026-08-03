import { enableProdMode, provideZoneChangeDetection } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';
import { AppConfig } from './app/shared/config/app.config';

export function getBaseUrl() {
  return document.getElementsByTagName('base')[0].href;
}

const providers = [
  { provide: 'BASE_URL', useFactory: getBaseUrl, deps: [] }
];

if (environment.production) {
  enableProdMode();
}

async function bootstrap(): Promise<void> {
  const configUrl = `assets/config/config.${environment.name}.json`;
  const response = await fetch(configUrl);

  if (!response.ok) {
    throw new Error(`Could not load file '${configUrl}': ${response.status} ${response.statusText}`);
  }

  AppConfig.settings = await response.json();
  await platformBrowserDynamic(providers).bootstrapModule(AppModule, {
    applicationProviders: [provideZoneChangeDetection()],
  });
}

bootstrap().catch(err => console.error(err));
