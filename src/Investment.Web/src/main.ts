import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

const themeStorageKey = 'investment-web-theme';

function applyStoredThemeBeforeBootstrap(): void {
  try {
    const savedTheme = localStorage.getItem(themeStorageKey);
    const isDarkMode = savedTheme === 'dark';

    document.documentElement.classList.toggle('dark-theme', isDarkMode);
    document.documentElement.style.colorScheme = isDarkMode ? 'dark' : 'light';
  } catch {
    // Ignore storage or DOM access failures and fall back to the default theme.
  }
}

applyStoredThemeBeforeBootstrap();

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
