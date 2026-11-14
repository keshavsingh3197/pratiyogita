import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

function dismissBootLoader(): void {
  const loader = document.getElementById('boot-loader');
  if (!loader) return;
  loader.classList.add('hidden');
  window.setTimeout(() => loader.remove(), 220);
}

bootstrapApplication(App, appConfig)
  .then(dismissBootLoader)
  .catch((err) => {
    dismissBootLoader();
    console.error(err);
  });
