import json
from abc import ABC, abstractmethod
from typing import Any

from pathfinder_ai_agents.clients import FoundryClientFactory
from pathfinder_ai_agents.config import Settings
from pathfinder_ai_agents.contracts import (
    AgentInvocationRequest,
    AgentInvocationResponse,
    AgentName,
    Citation,
)
from pathfinder_ai_agents.prompts import load_prompt
from pathfinder_ai_agents.tool_registry import ToolRegistry


class BaseFoundryAgent(ABC):
    name: AgentName
    system_prompt: str

    def __init__(
        self,
        client_factory: FoundryClientFactory,
        settings: Settings,
        tool_registry: ToolRegistry | None = None,
    ) -> None:
        self._client_factory = client_factory
        self._settings = settings
        self._tool_registry = tool_registry

    @abstractmethod
    def build_user_input(self, request: AgentInvocationRequest) -> str:
        raise NotImplementedError

    def run(self, request: AgentInvocationRequest) -> AgentInvocationResponse:
        openai_client = self._client_factory.get_openai_client()
        tools = self._tool_registry.definitions() if self._tool_registry else []
        called_tools: list[str] = []

        response = openai_client.responses.create(
            model=self._settings.model_deployment_name,
            instructions=self.system_prompt,
            input=self.build_user_input(request),
            tools=tools,
        )

        for _ in range(4):
            tool_calls = self._extract_tool_calls(response)
            if not tool_calls or not self._tool_registry:
                break

            tool_outputs: list[dict[str, str]] = []
            for tool_call in tool_calls:
                called_tools.append(tool_call["name"])
                result = self._tool_registry.execute(
                    tool_call["name"],
                    tool_call.get("arguments"),
                )
                tool_outputs.append(
                    {
                        "type": "function_call_output",
                        "call_id": tool_call["call_id"],
                        "output": json.dumps(result),
                    }
                )

            response = openai_client.responses.create(
                model=self._settings.model_deployment_name,
                instructions=self.system_prompt,
                input=tool_outputs,
                previous_response_id=getattr(response, "id", None),
                tools=tools,
            )

        return AgentInvocationResponse(
            agent_name=self.name,
            output_text=getattr(response, "output_text", "") or "",
            model=self._settings.model_deployment_name,
            citations=self._extract_citations(response),
            metadata={
                "response_id": getattr(response, "id", None),
                "correlation_id": str(request.correlation_id) if request.correlation_id else None,
                "called_tools": called_tools,
            },
        )

    def _extract_tool_calls(self, response: Any) -> list[dict[str, str]]:
        tool_calls: list[dict[str, str]] = []

        for output_item in getattr(response, "output", []) or []:
            item_type = self._get_value(output_item, "type")
            if item_type != "function_call":
                continue

            name = self._get_value(output_item, "name")
            call_id = self._get_value(output_item, "call_id")
            arguments = self._get_value(output_item, "arguments") or "{}"
            if name and call_id:
                tool_calls.append({"name": name, "call_id": call_id, "arguments": arguments})

        return tool_calls

    def _extract_citations(self, response: Any) -> list[Citation]:
        citations: list[Citation] = []

        for output_item in getattr(response, "output", []) or []:
            for content_item in getattr(output_item, "content", []) or []:
                for annotation in getattr(content_item, "annotations", []) or []:
                    title = getattr(annotation, "title", None) or getattr(
                        annotation,
                        "filename",
                        None,
                    )
                    url = getattr(annotation, "url", None)
                    if title and url:
                        citations.append(Citation(title=title, url=url))

        return citations

    def _get_value(self, item: Any, key: str) -> Any:
        if isinstance(item, dict):
            return item.get(key)

        return getattr(item, key, None)


class RoadmapPlannerAgent(BaseFoundryAgent):
    name = AgentName.ROADMAP_PLANNER
    system_prompt = load_prompt("roadmap-planner.system.md")

    def build_user_input(self, request: AgentInvocationRequest) -> str:
        context_json = request.context.model_dump_json(indent=2)
        return (
            "Use the following learner context to update the study roadmap.\n\n"
            f"Question or task:\n{request.message}\n\n"
            f"Learner context:\n{context_json}"
        )


class ProgressCoachAgent(BaseFoundryAgent):
    name = AgentName.PROGRESS_COACH
    system_prompt = load_prompt("progress-coach.system.md")

    def build_user_input(self, request: AgentInvocationRequest) -> str:
        context_json = request.context.model_dump_json(indent=2)
        return (
            "Answer the learner using their current progress state.\n\n"
            f"Learner message:\n{request.message}\n\n"
            f"Progress context:\n{context_json}"
        )


class AgentRegistry:
    def __init__(self) -> None:
        self._agents: dict[AgentName, BaseFoundryAgent] = {}

    def register(self, agent: BaseFoundryAgent) -> None:
        self._agents[agent.name] = agent

    def get(self, name: AgentName) -> BaseFoundryAgent:
        return self._agents[name]

    def list_names(self) -> list[str]:
        return [agent_name.value for agent_name in self._agents]

    @classmethod
    def create_default(
        cls,
        client_factory: FoundryClientFactory,
        settings: Settings,
        tool_registry: ToolRegistry | None = None,
    ) -> "AgentRegistry":
        registry = cls()
        registry.register(RoadmapPlannerAgent(client_factory, settings, tool_registry))
        registry.register(ProgressCoachAgent(client_factory, settings, tool_registry))
        return registry
