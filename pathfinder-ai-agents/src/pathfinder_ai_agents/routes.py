from typing import Any

from fastapi import APIRouter, Request

from pathfinder_ai_agents.contracts import AgentInvocationRequest, AgentInvocationResponse
from pathfinder_ai_agents.orchestrator import AgentOrchestrator

router = APIRouter()


def _orchestrator(request: Request) -> AgentOrchestrator:
    return request.app.state.orchestrator


@router.get("/healthz")
def healthz() -> dict[str, str]:
    return {"status": "ok"}


@router.get("/api/agents")
def list_agents(request: Request) -> dict[str, list[str]]:
    return {"agents": _orchestrator(request).list_agents()}


@router.get("/api/tools")
def list_tools(request: Request) -> dict[str, list[str]]:
    return {"tools": _orchestrator(request).list_tools()}


@router.post("/api/tools/{tool_name}")
async def execute_tool(
    tool_name: str,
    arguments: dict[str, Any],
    request: Request,
) -> dict[str, Any]:
    return await _orchestrator(request).execute_tool(tool_name, arguments)


@router.post("/api/agents/invoke", response_model=AgentInvocationResponse)
def invoke_agent(request: Request, payload: AgentInvocationRequest) -> AgentInvocationResponse:
    return _orchestrator(request).invoke(payload)
