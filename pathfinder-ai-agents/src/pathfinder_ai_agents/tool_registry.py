import json
from collections.abc import Callable
from dataclasses import dataclass
from json import JSONDecodeError
from typing import Any

ToolHandler = Callable[[dict[str, Any]], dict[str, Any]]


@dataclass(frozen=True)
class AgentTool:
    name: str
    description: str
    parameters: dict[str, Any]
    handler: ToolHandler

    def to_openai_tool(self) -> dict[str, Any]:
        return {
            "type": "function",
            "name": self.name,
            "description": self.description,
            "parameters": self.parameters,
        }


class ToolRegistry:
    def __init__(self, tools: list[AgentTool]) -> None:
        self._tools = {tool.name: tool for tool in tools}

    def definitions(self) -> list[dict[str, Any]]:
        return [tool.to_openai_tool() for tool in self._tools.values()]

    def list_names(self) -> list[str]:
        return list(self._tools)

    def execute(self, name: str, raw_arguments: str | None) -> dict[str, Any]:
        if name not in self._tools:
            return {"error": f"Unknown tool: {name}"}

        tool = self._tools[name]
        try:
            arguments = json.loads(raw_arguments or "{}")
        except JSONDecodeError:
            return {"error": f"Invalid JSON arguments for tool: {name}"}

        return tool.handler(arguments)
