# AGENTS.md - PathfinderAI Backend

Purpose: Help coding agents work safely in the .NET backend and understand how it coordinates WEB, AI agents, and Microsoft Learn MCP resources.

Quick links

- Project root: [PathfinderAPI](PathfinderAPI)
- Main entry: [PathfinderAPI/Program.cs](PathfinderAPI/Program.cs#L1)
- Roadmap bridge: [PathfinderAPI/Controllers/RoadmapGenerationController.cs](PathfinderAPI/Controllers/RoadmapGenerationController.cs)

What agents should know

- This is the BACK solution. It is the contract adapter between WEB and AI.
- This is a .NET web API using top-level statements in `Program.cs`.
- Swagger/OpenAPI is enabled for the Development environment; the JSON endpoint is `/openapi/v1.json`.
- `RoadmapGenerationController` calls the AI service and maps AI output into the frontend learning-plan DTO.
- The backend calls `pathfinder-ai-agents` for Microsoft Learn MCP resources. The frontend should not call MCP directly.

Integration flow:

```text
frontend_hackathon
  -> POST /api/learning-plans/generate-roadmap
  -> POST {Agents:BaseUrl}/api/agents/invoke
  -> POST {Agents:BaseUrl}/api/tools/search_microsoft_docs
  -> LearningPlanResponse
```

Roadmap model:

- Top-level `Modules` are broad pathway themes rendered as roadmap cards.
- `SubModules` are the actual learning modules users follow.
- `Resources` are supporting Microsoft Learn materials attached to modules.
- Do not treat MCP results as the whole pathway or force users into one exact course.
- Use the agent-provided `ResourceQuery` when available to find better module resources.

Build & Run

- Restore & build: `dotnet restore` then `dotnet build` at the repository root or in `PathfinderAPI`.
- Run (development): `dotnet run --project PathfinderAPI` — app uses HTTPS redirection and Swagger UI in Development.
- Configure `Agents:BaseUrl`, for example `http://127.0.0.1:8088`, before generating roadmaps.

Conventions & guidance for agents

- Prefer minimal, focused edits. When changing public API routes, add or update tests where appropriate.
- Keep the frontend DTO stable unless WEB is updated in the same change.
- Validate and normalize AI JSON in the backend before saving or returning it.
- Keep AI prompts and MCP transport details in `pathfinder-ai-agents` when possible.
- Link to heavy documentation rather than embedding it here. If you add more instructions, keep entries short and actionable.

Where to expand

- If this repo grows (frontend, infra, tests), create sub-instructions files under `.github/instructions/` and reference them here.

Contact/Notes

- If unsure about conventions, open an issue or ask for clarification in the repo.
