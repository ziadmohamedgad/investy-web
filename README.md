# Investy Web

Investy Web is a local-first investment portfolio tracker for Egyptian assets. It includes an ASP.NET Core API, SQL Server persistence, and an Angular desktop web UI.

## Features

- Track stocks, gold, funds, real estate/property-style assets, and Thndr Cloud Daily assets.
- Record buy/sell/deposit/withdraw transactions.
- Calculate holdings, average buy price, realized and unrealized profit/loss.
- Sync Egyptian stock prices through EODHD.
- Export portfolio data to Excel.
- Dark/light mode and balance hiding.

## First Run

1. Create a free EODHD account at https://eodhd.com/.
2. Start the API and Angular app.
3. On first launch, Investy asks for your EODHD API key.
4. The key is saved in your local database in `AppSettings`, not in source code.

If you cancel the prompt, the app still works for manual data entry, but price search/sync will require adding the key from the sync panel.

## Development Setup

Prerequisites:

- .NET SDK 9
- Node.js 20+
- SQL Server or SQL Server LocalDB
- Angular CLI, or use `npm run start` from the web project

Run the API:

```powershell
dotnet run --project src/Investment.API/Investment.API.csproj
```

Run the web app:

```powershell
cd src/Investment.Web
npm install
npm run start
```

The Angular app expects the API URL configured in `src/Investment.Web/src/environments/environment.ts`.

## Database

The project uses Entity Framework Core migrations. Do not commit a real database backup or local data file.

To create an empty database from the current schema:

```powershell
dotnet ef database update --project src/Investment.Infrastructure --startup-project src/Investment.API
```

The current local portfolio data is intentionally not part of GitHub. New users get an empty database schema and enter their own data.

## Secrets

Do not commit:

- Real EODHD API keys.
- `appsettings.Development.json` with personal settings.
- SQL Server backups, `.mdf`, `.ldf`, `.db`, `.sqlite`, or exported `Investy.xlsx` files.

For local experiments, use environment variables, user secrets, or the in-app EODHD key prompt.
