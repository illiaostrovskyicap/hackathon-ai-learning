import json
from typing import Any

from pathfinder_ai_agents.agents import AgentRegistry
from pathfinder_ai_agents.contracts import AgentInvocationRequest, AgentInvocationResponse
from pathfinder_ai_agents.tool_registry import ToolRegistry


class AgentOrchestrator:
    def __init__(self, registry: AgentRegistry, tool_registry: ToolRegistry) -> None:
        self._registry = registry
        self._tool_registry = tool_registry

    def list_agents(self) -> list[str]:
        return self._registry.list_names()

    def list_tools(self) -> list[str]:
        return self._tool_registry.list_names()

    def execute_tool(self, tool_name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        return self._tool_registry.execute(tool_name, json.dumps(arguments))

    def invoke(self, request: AgentInvocationRequest) -> AgentInvocationResponse:
        agent = self._registry.get(request.agent_name)
        return agent.run(request)
