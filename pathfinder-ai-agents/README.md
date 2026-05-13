# PathfinderAI Agents

PathfinderAI Agents is a Python service that runs backend-owned AI agents for learner guidance.
It exposes a small FastAPI surface for the main application backend and calls Azure AI Foundry
through the Azure OpenAI v1-compatible API.

The service is designed as an internal AI orchestration layer. The primary product backend should
own authentication, authorization, learner persistence, Cosmos DB access, and Teams integration.
This service owns agent prompts, model calls, tool execution, and AI response normalization.

## Runtime Model

Current runtime pattern:

- Python 3.11+
- FastAPI
- OpenAI Python SDK
- Azure OpenAI-compatible endpoint ending in `/openai/v1/`
- API-key auth for local and MVP development
- Azure App Service deployment shape through `azure.yaml`
- no Docker requirement

Azure client setup follows the Foundry example style:

```python
from openai import OpenAI

client = OpenAI(
    base_url="https://<resource>.services.ai.azure.com/openai/v1/",
    api_key="<your-api-key>",
)
```

The Azure deployment name is passed as `model=<deployment-name>`.

## Request Flow

Standard flow from Teams or web UI:

```text
Teams web view
  -> .NET backend API
  -> PathfinderAI Agents service
  -> Azure OpenAI model
  -> backend tools
  -> Azure OpenAI final answer
  -> .NET backend API
  -> Teams web view
```

The LLM does not access Cosmos DB, Microsoft Learn MCP, or backend state directly. It can only ask
for registered tools. The Python service executes those tools and sends normalized JSON results
back to the model.

## Project Structure

```text
pathfinder-ai-agents/
├── .env.example
├── azure.yaml
├── playground-notes.md
├── prompts/
│   ├── progress-coach.system.md
│   └── roadmap-planner.system.md
├── pyproject.toml
├── src/
│   └── pathfinder_ai_agents/
│       ├── agents.py
│       ├── clients.py
│       ├── config.py
│       ├── contracts.py
│       ├── learner_tools.py
│       ├── learn_tools.py
│       ├── main.py
│       ├── observability.py
│       ├── orchestrator.py
│       ├── prompts.py
│       ├── routes.py
│       └── tool_registry.py
└── tests/
```

The app intentionally uses a flat module layout. This follows the Foundry sample preference for a
clear `main.py` entrypoint and nearby agent/tool files instead of deep package nesting.

Template comparison notes are stored in
`template-comparison-snippy.md`.

## Agents

`roadmap-planner`

Creates or updates learning roadmap guidance. It is intended for role planning, study sequencing,
gap analysis, and concrete next steps.

`progress-coach`

Answers learner questions using progress-aware context. It is intended for short guidance,
prioritization, and next-action recommendations.

## Tools

Tools are backend-owned functions exposed to the LLM through the Responses API. The model can
request a tool call, but Python code executes the tool.

Learner tools:

- `get_learner_profile`: returns learner profile, target role, locale, weekly capacity, and goal.
- `get_current_progress`: returns completed skills, weak skills, module counts, and recent activity.
- `get_skill_matrix`: returns role skill requirements compared with current learner levels.
- `get_recommended_modules`: returns recommended study modules for weak or requested skills.

Microsoft Learn tools:

- `search_microsoft_docs`: searches Microsoft Learn documentation.
- `fetch_microsoft_doc`: fetches a Microsoft Learn documentation page by URL.
- `search_microsoft_code_samples`: searches Microsoft code samples.

Current state:

- Learner tools return hardcoded demo data.
- Microsoft Learn tools return hardcoded normalized results through `LearnMcpClient`.
- The Microsoft Learn MCP endpoint is configured, but live MCP calls are not implemented yet.

Target state:

- Learner tools read from Cosmos DB or backend APIs.
- Microsoft Learn tools call the real Microsoft Learn MCP server at `https://learn.microsoft.com/api/mcp`.
- Recommended modules combine learner gaps, skill matrix state, Microsoft Learn content, and product rules.

## API

Health:

```http
GET /healthz
```

List agents:

```http
GET /api/agents
```

List tools:

```http
GET /api/tools
```

Execute a tool directly for local development:

```http
POST /api/tools/{tool_name}
```

Invoke an agent:

```http
POST /api/agents/invoke
```

Example agent request:

```json
{
  "agent_name": "progress-coach",
  "message": "What should I study next before I start Azure Functions?",
  "context": {
    "track_id": "dotnet-backend",
    "preferred_locale": "en-US",
    "experience_level": "Junior",
    "weekly_study_hours": 6,
    "current_goal": "Become interview ready for .NET backend roles",
    "completed_skills": ["csharp-basics", "aspnet-core-basics"],
    "weak_skills": ["entity-framework", "azure-functions"],
    "completed_modules": 3,
    "in_progress_modules": 1,
    "remaining_modules": 8,
    "recent_questions": ["How do I build Web APIs in ASP.NET Core?"]
  }
}
```

Example response metadata:

```json
{
  "metadata": {
    "response_id": "resp_...",
    "correlation_id": null,
    "called_tools": ["get_current_progress", "search_microsoft_docs"]
  }
}
```

## Local Setup

Create and activate a virtual environment:

```bash
cd /Users/dkap/Desktop/hackaton/pathfinder-ai-agents
python -m venv .venv
source .venv/bin/activate
```

Install dependencies:

```bash
pip install -e '.[dev]'
```

Create local config:

```bash
cp .env.example .env
```

Required settings:

```env
OPENAI_BASE_URL=https://<resource>.services.ai.azure.com/openai/v1/
OPENAI_API_KEY=<your-api-key>
AZURE_OPENAI_DEPLOYMENT_NAME=<deployment-name>
MICROSOFT_LEARN_MCP_ENDPOINT=https://learn.microsoft.com/api/mcp
APPLICATIONINSIGHTS_CONNECTION_STRING=
PATHFINDER_APP_ENV=local
PATHFINDER_LOG_LEVEL=INFO
```

Run the service:

```bash
uvicorn pathfinder_ai_agents.main:app --reload --port 8088
```

## Local Testing

List available tools:

```bash
curl -s http://127.0.0.1:8088/api/tools | jq
```

Call a learner tool directly:

```bash
curl -s -X POST http://127.0.0.1:8088/api/tools/get_current_progress \
  -H "Content-Type: application/json" \
  -d '{"learner_id":"demo-learner"}' | jq
```

Call a Microsoft Learn tool directly:

```bash
curl -s -X POST http://127.0.0.1:8088/api/tools/search_microsoft_docs \
  -H "Content-Type: application/json" \
  -d '{"query":"Azure Functions HTTP trigger for backend API","limit":3}' | jq
```

Invoke an agent:

```bash
curl -s -X POST http://127.0.0.1:8088/api/agents/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "agent_name": "progress-coach",
    "message": "What should I study next before Azure Functions?",
    "context": {
      "track_id": "dotnet-backend",
      "preferred_locale": "en-US",
      "experience_level": "Junior",
      "weekly_study_hours": 6,
      "current_goal": "Become interview ready for .NET backend roles",
      "completed_skills": ["csharp-basics", "aspnet-core-basics"],
      "weak_skills": ["entity-framework", "azure-functions"],
      "completed_modules": 3,
      "in_progress_modules": 1,
      "remaining_modules": 8,
      "recent_questions": ["How do I build Web APIs in ASP.NET Core?"]
    }
  }' | jq
```

## Environment Variables

`OPENAI_BASE_URL`

Azure OpenAI-compatible endpoint. It should end with `/openai/v1/`.

`OPENAI_API_KEY`

API key for the Azure AI Foundry resource.

`AZURE_OPENAI_DEPLOYMENT_NAME`

Deployment name used as the `model` value.

`MICROSOFT_LEARN_MCP_ENDPOINT`

Microsoft Learn MCP endpoint. Default: `https://learn.microsoft.com/api/mcp`.

`APPLICATIONINSIGHTS_CONNECTION_STRING`

Optional Azure Monitor Application Insights connection string.

`PATHFINDER_APP_ENV`

Runtime environment label, for example `local`, `dev`, or `prod`.

`PATHFINDER_LOG_LEVEL`

Python logging level.

## Deployment

This project is configured for Azure App Service, not containers.

`azure.yaml` uses App Service hosting. The expected production startup command is:

```bash
gunicorn -w 2 -k uvicorn.workers.UvicornWorker -b 0.0.0.0:8000 pathfinder_ai_agents.main:app
```

## Foundry Template CLI

Microsoft's current hosted-agent scaffolding path is `azd`, not regular `az`.

Local status for this workspace:

- Azure CLI `az` is installed.
- Azure Developer CLI `azd` is not currently installed.

Useful commands when moving this service toward a hosted Foundry agent template:

```bash
azd version
azd ext install azure.ai.agents
azd ext list
```

Create a starter project:

```bash
azd init -t https://github.com/Azure-Samples/azd-ai-starter-basic
```

Initialize an agent definition from a local manifest folder or a remote sample:

```bash
azd ai agent init -m <path-or-url-to-agent.yaml>
```

Run and invoke locally through the Foundry agent host:

```bash
azd ai agent run
azd ai agent invoke --local "What should I study next?"
```

Deploy:

```bash
azd up
```

Foundry hosted-agent samples commonly place `agent.yaml`, `main.py`, dependency files, and any tool
implementation files together in the manifest directory. `azd ai agent init -m ...` copies that
manifest directory into the generated project and wires it into `azure.yaml`.

## Boundaries

The .NET backend should own:

- Teams authentication and Entra ID validation
- user and learner identity mapping
- Cosmos DB persistence
- canonical learner profile and progress state
- product API contracts
- audit, authorization, and data lifecycle rules

The Python agent service should own:

- prompt loading
- agent selection
- Azure OpenAI calls
- tool registration and execution
- response metadata
- Microsoft Learn retrieval adapter

The LLM should own:

- interpreting the user question
- deciding which registered tools are useful
- explaining tool results
- producing the final answer

The LLM should not directly own:

- learner persistence
- Cosmos DB queries
- Microsoft Learn MCP transport
- identity or authorization
- canonical progress state

## Next Implementation Steps

1. Replace hardcoded learner tools with Cosmos DB-backed repositories.
2. Replace stubbed `LearnMcpClient` with the real Microsoft Learn MCP client.
3. Add structured output contracts for roadmap generation.
4. Add tests for tool execution and response metadata.
5. Add persistent conversation and telemetry records.
