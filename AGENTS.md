# AGENTS.md

## Project

This repository contains a personal Strava data exporter for ChatGPT analysis.

## Runtime

Use .NET 11 preview for backend projects.

Target framework:

```xml
<TargetFramework>net11.0</TargetFramework>
```

Because .NET 11 is preview software, avoid unnecessary dependency on unstable APIs unless they clearly improve the app.

## Architecture

Use Clean Architecture:

- Domain: core entities and value objects only.
- Application: CQRS commands, queries, handlers, validators, DTOs, abstractions.
- Infrastructure: Strava API client, OAuth token storage, export writers, persistence.
- Api: ASP.NET Core Minimal API endpoints.
- Frontend: TypeScript React app.

## Backend Standards

- Use Minimal APIs.
- Use nullable reference types.
- Use Refit for Strava HTTP integration.
- Use AutoMapper for model and DTO mapping when useful.
- Use FluentValidation for request validation.
- Use structured logging with Serilog.
- Use SQLite locally.
- Keep Strava implementation details out of Application and Domain layers.
- Do not expose Strava client secrets to the frontend.
- Do not store Strava access tokens in browser storage.
- Do not log access tokens or refresh tokens.

## Frontend Standards

- Use TypeScript.
- Use React with Vite.
- Use TanStack Query.
- Use Zod for validation.
- Keep API calls isolated under `src/api`.
- Do not call Strava directly from the browser.

## Data Usage

This tool is for exporting and analyzing the authenticated user's own Strava data.

Do not add functionality for:

- scraping other users' data
- bypassing Strava privacy controls
- writing to Strava
- training AI/ML models on Strava API data
- public sharing of private activity data
