# AGENTS.md - PathfinderAI Web

Purpose: Help coding agents work safely in the React/Vite frontend and understand how it consumes backend, AI agent, and Microsoft Learn MCP output.

## Role

This folder is the WEB solution. It renders the learner experience and should not call the AI agent service or Microsoft Learn MCP directly.

Expected flow:

```text
WEB frontend_hackathon
  -> BACK PathfinderAI API at VITE_API_BASE_URL or http://localhost:5136
  -> AI pathfinder-ai-agents
  -> Microsoft Learn MCP
```

## Backend Contract

The frontend expects a learning plan shaped like:

- `id`
- `track`
- `experience`
- `language`
- `generatedAt`
- `modules[]`

Each module is a roadmap theme/block:

- `id`
- `title`
- `description`
- `estimatedHours`
- `status`
- `topics[]`
- `resources[]`
- `subModules[]`

Each `subModule` is the actual learning module inside the pathway:

- `id`
- `title`
- `description`
- `estimatedHours`
- `topics[]`
- `projectTask`
- `resources[]`

Resources are supporting materials, not the pathway itself.

## Implementation Guidance

- Keep AI/MCP details out of React components.
- Prefer adapting backend responses in `src/app/api.ts`.
- Preserve both camelCase and PascalCase normalization because the backend may return either.
- Do not build frontend logic around a specific Microsoft Learn course. The pathway is modular; resources only support modules.
- If adding fields, make them optional in the UI unless the backend contract is explicitly updated.

## Local Run

```bash
pnpm install
pnpm dev
```

The app defaults to `http://localhost:5136` for the backend unless `VITE_API_BASE_URL` is set.
