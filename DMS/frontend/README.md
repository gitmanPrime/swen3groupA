# DMS Frontend

Angular frontend for the Document Management System.

## Prerequisites

- Node.js 24.x
- npm 11.x

Development setup verified with Node.js 24.14.0 and npm 11.9.0.
Angular CLI is installed locally through the project dependencies.

## Install dependencies

From the repository root:

```powershell
Set-Location DMS/frontend
npm ci
```

`npm ci` installs the versions recorded in `package-lock.json`.
Commit the lockfile when changing dependencies.

On Windows, use the directory casing `DMS` consistently.
Running tests from a path containing `dms` caused an Angular
injection-context error during setup.

## Run locally

```powershell
npm start
```

Open http://localhost:4200/.
The development server reloads when source files change.
Stop it with Ctrl+C.

The root URL redirects to `/documents`.
Unknown URLs display the "Page not found" page.

## Build

```powershell
npm run build
```

Build output is written to `dist/dms-web/`.

## Run tests

Run the tests once:

```powershell
npm test -- --watch=false
```

Run tests in watch mode:

```powershell
npm test
```

## Project structure

- `src/app/app.*`: application shell.
- `src/app/app.config.ts`: application providers.
- `src/app/app.routes.ts`: route definitions.
- `src/app/features/`: components grouped by feature.
- `src/app/shared/`: reusable components and shared pages.
- `src/styles.scss`: global styles.
- `public/`: static assets.

## Current scope

The initial setup includes the application shell, a dashboard
placeholder, routing, and a fallback page.

REST API integration, document-management features, container
deployment, and the authentication decision are pending.
