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

Requests to `/api` are forwarded to the Docker backend at `http://localhost:8081`.
Start the backend from the repository root before loading the document dashboard:

```powershell
docker compose -f DMS/docker-compose.yml up -d --build dms-api

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

The frontend includes the application shell, routing, a fallback page,
and a document list loaded from the REST API with loading and error messages.
Select a filename to open its metadata page at `/documents/:id`.
From there, download the original file, save or clear its description,
or delete the document and stored file after confirming.
Use "Upload PDF" to upload a non-empty PDF with an optional description.
After a successful upload, return to the document list to see the new document.

Tags, collections, and authentication
are separate follow-up tasks.
