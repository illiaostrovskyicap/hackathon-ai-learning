# hackathon-ai-learning

PathfinderAI local workspace with three apps:

- `frontend_hackathon` - React/Vite learner UI
- `PathfinderAI` - .NET backend API with PostgreSQL persistence
- `pathfinder-ai-agents` - Python agent service for roadmap generation and Microsoft Learn lookup

## One-command local startup

From the repository root:

```bash
./scripts/start-dev.sh
```

This script:

- uses a local .NET 10 SDK from `/Users/dkap/.local/share/dotnet`
- uses the bundled Python 3.11 runtime for the agent service
- ensures the local PostgreSQL database `pathfinderai` exists
- restores backend/frontend/agent dependencies
- starts all three services in the background
- writes logs and pid files under `.run/`

Stop everything with:

```bash
./scripts/stop-dev.sh
```

## Local URLs

After startup:

- Frontend: [http://127.0.0.1:5174](http://127.0.0.1:5174)
- Backend health: [http://127.0.0.1:5136/health](http://127.0.0.1:5136/health)
- Agents health: [http://127.0.0.1:8088/healthz](http://127.0.0.1:8088/healthz)
- Swagger UI: [http://127.0.0.1:5136/swagger](http://127.0.0.1:5136/swagger)

Logs:

- `.run/logs/frontend.log`
- `.run/logs/backend.log`
- `.run/logs/agents.log`

## Requirements

Installed locally:

- PostgreSQL client/server reachable on `localhost:5432`
- `psql` and `createdb`
- `pnpm`
- `curl`

Handled by the repo setup:

- .NET SDK 10 installed locally under `/Users/dkap/.local/share/dotnet`
- Python 3.11 runtime via the bundled workspace dependency path

## Azure OpenAI settings

The agent service needs these variables for real roadmap generation:

```bash
export OPENAI_BASE_URL="https://<resource>.services.ai.azure.com/openai/v1/"
export OPENAI_API_KEY="<azure-openai-key>"
export AZURE_OPENAI_DEPLOYMENT_NAME="<deployment-name>"
```

Optional:

```bash
export MICROSOFT_LEARN_MCP_ENDPOINT="https://learn.microsoft.com/api/mcp"
```

If you do not export real Azure OpenAI settings before startup, the stack still runs. In that mode:

- the agent service starts with placeholder values
- Microsoft Learn search endpoints still answer
- backend roadmap generation falls back to the local fallback pathway when model invocation fails

## Current learning-path behavior

The stack now persists learning plans and progress in PostgreSQL instead of relying only on browser storage.

Saved plan behavior:

- onboarding answers are stored with the learning plan
- module and lesson clicks set `startedAt` / `lastOpenedAt`
- explicit completion sets `completedAt`
- the user skill matrix is updated on completion events
- weak skills are recalculated and persisted
- future not-started pathway content can be upgraded without overwriting completed or in-progress work

## Local test account

The backend creates users on first login.

```text
Email: local@test.com
Password: password123
```

## Manual startup, if needed

### Agent service

```bash
cd pathfinder-ai-agents
/Users/dkap/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 -m venv .venv
source .venv/bin/activate
pip install -e '.[dev]'
uvicorn pathfinder_ai_agents.main:app --host 127.0.0.1 --port 8088
```

### Backend

```bash
cd PathfinderAI
export PATH="/Users/dkap/.local/share/dotnet:$PATH"
export ASPNETCORE_URLS="http://127.0.0.1:5136"
export Agents__BaseUrl="http://127.0.0.1:8088"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=pathfinderai;Username=$USER"
dotnet run --project PathfinderAPI/PathfinderAPI.csproj
```

### Frontend

```bash
cd frontend_hackathon
pnpm install
pnpm dev --host 127.0.0.1 --port 5174
```
