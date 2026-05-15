# hackathon-ai-learning

PathfinderAI local workspace with three solutions:

- `frontend_hackathon` - React/Vite web app.
- `PathfinderAI` - .NET backend API.
- `pathfinder-ai-agents` - Python AI agent service and Microsoft Learn MCP integration.

## Local Startup

Use three terminals from the repository root.

### 1. AI agents

Requirements:

- Python 3.11+
- Azure OpenAI-compatible settings for full roadmap generation
- Microsoft Learn MCP endpoint defaults to `https://learn.microsoft.com/api/mcp`

```bash
cd pathfinder-ai-agents
python3.11 -m venv .venv
source .venv/bin/activate
pip install -e '.[dev]'

export OPENAI_BASE_URL="https://<resource>.services.ai.azure.com/openai/v1/"
export OPENAI_API_KEY="<azure-openai-key>"
export AZURE_OPENAI_DEPLOYMENT_NAME="<deployment-name>"
export MICROSOFT_LEARN_MCP_ENDPOINT="https://learn.microsoft.com/api/mcp"

uvicorn pathfinder_ai_agents.main:app --host 127.0.0.1 --port 8088
```

Health check:

```bash
curl http://127.0.0.1:8088/healthz
```

Microsoft Learn MCP smoke test:

```bash
curl -s -X POST http://127.0.0.1:8088/api/tools/search_microsoft_docs \
  -H 'Content-Type: application/json' \
  -d '{"query":"ASP.NET Core Web API","limit":1}'
```

### 2. Backend API

Requirements:

- .NET SDK 10
- PostgreSQL running locally
- Database named `pathfinderai`

Create the database if needed:

```bash
createdb -h localhost -U "$USER" pathfinderai
```

Run the backend:

```bash
cd PathfinderAI

export ASPNETCORE_URLS="http://localhost:5136"
export Agents__BaseUrl="http://127.0.0.1:8088"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=pathfinderai;Username=$USER"

dotnet run --project PathfinderAPI/PathfinderAPI.csproj
```

Health check:

```bash
curl http://localhost:5136/health
```

Login smoke test:

```bash
curl -X POST http://localhost:5136/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"local@test.com","password":"password123"}'
```

### 3. Web app

Requirements:

- Node.js
- pnpm

```bash
cd frontend_hackathon
pnpm install
pnpm dev --host 127.0.0.1 --port 5174
```

Open:

```text
http://127.0.0.1:5174
```

`5174` is useful when another Vite app is already using `5173`. The backend CORS policy allows both `localhost` and `127.0.0.1` on ports `5173` and `5174`.

## Local Test Account

The backend creates users on first login.

```text
Email: local@test.com
Password: password123
```

## Important Notes

- Microsoft Learn MCP resource lookup can work without Azure OpenAI credentials through `/api/tools/search_microsoft_docs`.
- Full roadmap generation requires valid Azure OpenAI credentials because the backend calls `/api/agents/invoke`.
- If login fails in the browser but `curl` login works, check CORS and make sure the web origin matches the backend allowed origins.
