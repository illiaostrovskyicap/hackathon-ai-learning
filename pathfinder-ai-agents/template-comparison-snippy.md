# Template Comparison: Azure-Samples/snippy

This document compares the current `pathfinder-ai-agents` project with the
`Azure-Samples/snippy` AZD template.

Local template copy:

```text
/Users/dkap/Desktop/hackaton/azd-template-scratch/snippy
```

## Summary

`snippy` is useful for PathfinderAI, but not as a direct replacement for the current project.

Use `snippy` as a reference for:

- Azure Functions hosting
- MCP tool triggers
- Cosmos DB vector search
- Durable agent workflows
- Azure infra modules and AZD hooks

Keep the current project as the MVP base for:

- FastAPI internal agent API
- .NET backend integration
- Azure OpenAI Responses API tool loop
- backend-owned tools
- local development simplicity

## Current Project

Current shape:

```text
src/pathfinder_ai_agents/
├── agents.py
├── clients.py
├── config.py
├── contracts.py
├── learner_tools.py
├── learn_tools.py
├── main.py
├── observability.py
├── orchestrator.py
├── prompts.py
├── routes.py
└── tool_registry.py
```

Runtime:

- FastAPI
- OpenAI Python SDK
- Azure OpenAI-compatible `/openai/v1/` endpoint
- API-key auth for local MVP testing
- App Service target in `azure.yaml`
- backend-owned tool registry
- no Docker requirement

Current strengths:

- Small and easy to reason about
- Good fit behind the .NET backend
- Direct local testing with `uvicorn`
- Stable tool contract for learner state and Microsoft Learn stubs
- Fast iteration on prompts and API contracts

Current gaps:

- No real Cosmos DB repository yet
- No real Microsoft Learn MCP client yet
- No durable workflow support
- No hosted MCP server endpoint
- Infra is minimal compared with production AI workloads

## Snippy Template

Template shape:

```text
src/
├── data/
│   └── cosmos_ops.py
├── tools/
│   └── vector_search.py
├── durable_agents.py
├── function_app.py
├── host.json
├── local.settings.example.json
└── requirements.txt
```

Runtime:

- Azure Functions
- MCP tool triggers
- Microsoft Agent Framework
- `agent-framework-azurefunctions`
- Durable Functions orchestration
- Cosmos DB with vector index
- Azure OpenAI embeddings
- `DefaultAzureCredential` for production auth

Infra:

- Azure Functions
- API Management
- Cosmos DB
- Azure OpenAI / Cognitive Services
- Durable Task Scheduler
- monitoring
- app registration
- RBAC modules
- provisioning hooks

Snippy strengths:

- Strong Azure-native MCP example
- Good Cosmos DB vector search reference
- Good Durable Functions and multi-agent workflow reference
- Richer production infra than our current project
- Shows how to expose tools through Azure Functions decorators

Snippy tradeoffs:

- More complex than our MVP needs
- Uses Azure Functions rather than FastAPI
- Uses Microsoft Agent Framework rather than our current Responses API loop
- More template/domain code to remove
- More infra dependencies before product behavior is validated

## Code Style Differences

Current project style:

- Flat package modules
- Pydantic contracts
- explicit FastAPI routes
- small service classes
- typed tool registry
- line length 100
- `pyproject.toml` dependency and lint configuration
- concise comments

Snippy style:

- function-first Azure Functions app
- decorator-heavy triggers
- module-level constants and tool property objects
- extensive teaching comments
- line length 120
- `requirements.txt` for Functions runtime dependencies
- async Azure SDK usage
- environment variables read directly with `os.environ`

Recommended PathfinderAI style:

- Keep our flatter module layout.
- Keep Pydantic contracts for API boundaries.
- Keep concise comments.
- Borrow Snippy's async Cosmos and MCP patterns selectively.
- Do not copy Snippy's long tutorial comments into product code.
- Keep environment access centralized in `config.py`.

## What To Borrow

Borrow now:

- `azure.yaml` hooks pattern for post-provision settings generation
- RBAC-focused infra module organization
- Cosmos DB repository ideas from `src/data/cosmos_ops.py`
- vector search structure from `src/tools/vector_search.py`
- MCP tool naming and schema discipline from `function_app.py`

Borrow later:

- Durable orchestration for long-running roadmap generation
- Microsoft Agent Framework only if we move to hosted/durable agents
- Azure Functions MCP triggers if we split tools into a remote MCP server
- API Management if we need enterprise gateway controls

Avoid for now:

- Full Azure Functions migration
- Full Durable Functions dependency
- Full Cosmos vector search before content ingestion exists
- Devcontainer/Docker assumptions

## Recommended Migration Path

Step 1: Keep FastAPI as the internal agent service.

Use the current `pathfinder_ai_agents.main:app` entrypoint and App Service target.

Step 2: Add Cosmos-backed repositories.

Port only the useful ideas from Snippy's `cosmos_ops.py`:

- async client lifecycle
- `DefaultAzureCredential` support for production
- container creation policy
- vector index pattern later

Do not copy the snippet schema. Create PathfinderAI-specific documents:

- learner profile
- learner progress
- skill matrix
- module recommendations
- agent interaction events

Step 3: Replace hardcoded learner tools.

Map current tools to repositories:

- `get_learner_profile` -> Cosmos learner profile
- `get_current_progress` -> Cosmos progress snapshot
- `get_skill_matrix` -> Cosmos skill matrix
- `get_recommended_modules` -> Cosmos + Microsoft Learn retrieval

Step 4: Replace Microsoft Learn stubs.

Keep current public tool names:

- `search_microsoft_docs`
- `fetch_microsoft_doc`
- `search_microsoft_code_samples`

Replace only the internals of `LearnMcpClient`.

Step 5: Decide whether remote MCP hosting is needed.

If yes, use Snippy's Azure Functions MCP trigger style as the reference.

Step 6: Add durable workflows only after MVP.

Candidate durable workflows:

- full roadmap generation
- weekly progress recalculation
- large Microsoft Learn retrieval and ranking
- assessment-based skill matrix refresh

## Decision

Do not migrate the whole project to Snippy.

Use Snippy as an implementation reference for Azure-native backend capabilities while keeping the
current PathfinderAI service small and backend-owned.

The next practical move is Cosmos repository work, not hosted agents or Durable Functions.
