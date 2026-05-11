# AGENTS.md — Agent guidance for PathfinderAI

Purpose: Provide concise, actionable instructions so coding agents (Copilot/assistant) can work productively in this repository.

Quick links

- Project root: [PathfinderAPI](PathfinderAPI)
- Main entry: [PathfinderAPI/Program.cs](PathfinderAPI/Program.cs#L1)

What agents should know

- This is a .NET web API using top-level statements in `Program.cs`.
- Swagger/OpenAPI is enabled for the Development environment; the JSON endpoint is `/openapi/v1.json`.
- The API exposes a simple `GET /weatherforecast` example; look in `Program.cs` for patterns to follow.

Build & Run

- Restore & build: `dotnet restore` then `dotnet build` at the repository root or in `PathfinderAPI`.
- Run (development): `dotnet run --project PathfinderAPI` — app uses HTTPS redirection and Swagger UI in Development.

Conventions & guidance for agents

- Prefer minimal, focused edits. When changing public API routes, add or update tests where appropriate.
- Follow existing patterns in `Program.cs` (minimal API, map endpoints, record types).
- Link to heavy documentation rather than embedding it here. If you add more instructions, keep entries short and actionable.

Where to expand

- If this repo grows (frontend, infra, tests), create sub-instructions files under `.github/instructions/` and reference them here.

Contact/Notes

- If unsure about conventions, open an issue or ask for clarification in the repo.
