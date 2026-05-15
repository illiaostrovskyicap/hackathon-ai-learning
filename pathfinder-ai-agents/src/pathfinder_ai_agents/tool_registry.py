import json
from collections.abc import Callable
from dataclasses import dataclass
from json import JSONDecodeError
from typing import Any

ToolHandler = Callable[[dict[str, Any]], Any]


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

    def execute(self, tool_name: str, raw_arguments: Any) -> Any:
        tool = self._tools[tool_name]

        if isinstance(raw_arguments, str):
            arguments = json.loads(raw_arguments or "{}")
        else:
            arguments = raw_arguments or {}

        return tool.handler(arguments)
