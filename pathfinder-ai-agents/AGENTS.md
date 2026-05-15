# AGENTS.md - PathfinderAI AI Agents

Purpose: Help coding agents work safely in the Python AI orchestration service and understand how it provides structured pathways and Microsoft Learn MCP resources to the backend.

## Role

This folder is the AI solution. It owns:

- agent prompts
- Azure OpenAI / Foundry-compatible model calls
- backend-owned tool definitions
- Microsoft Learn MCP transport and result normalization
- AI response metadata

It does not own:

- frontend rendering
- authentication
- learner persistence
- canonical learning-plan storage
- direct browser-to-MCP calls

## Integration Flow

```text
BACK PathfinderAI API
  -> POST /api/agents/invoke
  -> roadmap-planner returns strict pathway JSON
  -> BACK maps JSON into LearningPlanResponse

BACK PathfinderAI API
  -> POST /api/tools/search_microsoft_docs
  -> LearnMcpClient calls https://learn.microsoft.com/api/mcp
  -> normalized resource JSON
```

## Pathway Contract

The `roadmap-planner` agent should return JSON only:

```json
{
  "title": "string",
  "summary": "string",
  "themes": [
    {
      "title": "string",
      "description": "string",
      "estimatedHours": 20,
      "topics": ["string"],
      "lessons": [
        {
          "title": "string",
          "description": "string",
          "estimatedHours": 6,
          "topics": ["string"],
          "projectTask": "string",
          "resourceQuery": "string"
        }
      ]
    }
  ]
}
```

Themes are broad roadmap stages. Lessons are practical learning modules. MCP resources support lessons; they should not define the whole pathway.

## Microsoft Learn MCP

Use the real Microsoft Learn MCP endpoint:

```text
https://learn.microsoft.com/api/mcp
```

Current tool mapping:

- `search_microsoft_docs` -> MCP `microsoft_docs_search`
- `fetch_microsoft_doc` -> MCP `microsoft_docs_fetch`
- `search_microsoft_code_samples` -> MCP `microsoft_code_sample_search`

Normalize MCP responses before returning them to BACK. Keep responses compact: title, URL, content type, summary, source tool, and matched skills are enough for the current backend.

## Implementation Guidance

- Keep tool handlers safe for both direct `/api/tools/{tool}` calls and agent-internal function calls.
- Await async tools before serializing tool output back to the model.
- Add tests for MCP SSE parsing, result normalization, and tool execution behavior when changing this service.
- Preserve fallback behavior for local demos, but mark fallback responses with `is_stubbed: true`.

## Local Run

```bash
python -m venv .venv
source .venv/bin/activate
pip install -e '.[dev]'
uvicorn pathfinder_ai_agents.main:app --reload --port 8088
```

Required environment includes OpenAI/Azure OpenAI settings and optionally `MICROSOFT_LEARN_MCP_ENDPOINT`.
