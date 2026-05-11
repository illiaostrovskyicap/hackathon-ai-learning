---
description: "Repository-wide, always-on instructions for AI coding agents. See AGENTS.md for details."
---

# Repository Copilot Instructions

- **Primary reference:** See [AGENTS.md](../AGENTS.md) for full agent guidance and links to key files.
- **Project root:** `PathfinderAPI/` — main entry: `PathfinderAPI/Program.cs`.
- **Build:** Run `dotnet restore` then `dotnet build` at the repository root or inside `PathfinderAPI`.
- **Run (development):** `dotnet run --project PathfinderAPI` — app exposes Swagger UI in Development (OpenAPI at `/openapi/v1.json`).
- **Edit guidance:** Prefer minimal, focused edits. When changing public API routes, add or update tests where appropriate.
- **Patterns:** Follow the minimal-API style and record types used in `Program.cs`.

If additional, area-specific instructions are needed (frontend, infra, tests), create `.github/instructions/*.instructions.md` files and reference them here.
