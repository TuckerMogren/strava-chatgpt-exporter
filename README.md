# Strava ChatGPT Exporter

Personal Strava data exporter for ChatGPT analysis. The app connects to your Strava account, fetches your activities, filters them, and generates JSON, CSV, and Markdown downloads you can upload to ChatGPT manually.

## Runtime

The backend targets .NET 11 preview:

```xml
<TargetFramework>net11.0</TargetFramework>
```

Install a .NET 11 preview SDK before running backend restore, build, or test commands. This machine currently has .NET SDKs through 10 installed, so backend verification requires a .NET 11 SDK install first.

## Create a Strava API App

1. Open the Strava API settings page.
2. Create an application for personal use.
3. Set the callback URL to:

```text
http://localhost:5080/api/strava/callback
```

4. Copy the client id and client secret.

## Environment

Create `.env` from the sample:

```bash
cp .env.example .env
```

Fill in:

```text
STRAVA_CLIENT_ID=
STRAVA_CLIENT_SECRET=
```

For direct backend runs, use environment variables with ASP.NET Core configuration names:

```text
Strava__ClientId=
Strava__ClientSecret=
Strava__RedirectUri=http://localhost:5080/api/strava/callback
ConnectionStrings__AppDb=Data Source=strava-exporter.db
```

## Run Locally

Start both the backend and frontend with one script:

```bash
./start.sh
```

The script loads `.env`, maps `STRAVA_CLIENT_ID` and `STRAVA_CLIENT_SECRET` into ASP.NET Core configuration, starts the API on `http://localhost:5080`, and starts the frontend on `http://localhost:5173`.

Manual backend:

```bash
dotnet restore StravaExporter.slnx
dotnet run --project backend/src/StravaExporter.Api/StravaExporter.Api.csproj
```

Manual frontend:

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`.

Docker Compose:

```bash
docker compose up --build
```

If .NET 11 preview container tags are not available yet, run the backend directly with the installed .NET 11 preview SDK instead of Docker.

## Workflow

1. Click **Connect Strava**.
2. Approve `read,activity:read_all` scopes.
3. Return to `/connected`.
4. Open `/activities`.
5. Filter activities and create an export.
6. Download JSON, CSV, or Markdown from `/exports`.
7. Upload the JSON or Markdown file to ChatGPT and ask for analysis.

This app is for exporting and analyzing your own personal Strava data only. It does not write to Strava, scrape other users, bypass privacy controls, or train AI models on Strava API data.
