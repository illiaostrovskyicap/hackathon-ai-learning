from typing import Any

from pathfinder_ai_agents.clients import LearnMcpClient
from pathfinder_ai_agents.tool_registry import AgentTool


def create_microsoft_learn_tools(client: LearnMcpClient) -> list[AgentTool]:
    return [
        AgentTool(
            name="search_microsoft_docs",
            description="Search official Microsoft Learn documentation for grounded references.",
            parameters=_object_schema(
                {
                    "query": {
                        "type": "string",
                        "description": "Search query for Microsoft Learn documentation.",
                    },
                    "limit": {
                        "type": "integer",
                        "description": "Maximum number of results to return.",
                        "minimum": 1,
                        "maximum": 10,
                    },
                },
                required=["query"],
            ),
            handler=lambda arguments: client.search_docs(
                query=str(arguments["query"]),
                limit=_limit(arguments),
            ),
        ),
        AgentTool(
            name="fetch_microsoft_doc",
            description="Fetch a Microsoft Learn documentation page by URL.",
            parameters=_object_schema(
                {
                    "url": {
                        "type": "string",
                        "description": "Microsoft Learn documentation URL to fetch.",
                    }
                },
                required=["url"],
            ),
            handler=lambda arguments: client.fetch_doc(url=str(arguments["url"])),
        ),
        AgentTool(
            name="search_microsoft_code_samples",
            description="Search official Microsoft code samples related to a learner skill.",
            parameters=_object_schema(
                {
                    "query": {
                        "type": "string",
                        "description": "Search query for Microsoft code samples.",
                    },
                    "limit": {
                        "type": "integer",
                        "description": "Maximum number of results to return.",
                        "minimum": 1,
                        "maximum": 10,
                    },
                },
                required=["query"],
            ),
            handler=lambda arguments: client.search_code_samples(
                query=str(arguments["query"]),
                limit=_limit(arguments),
            ),
        ),
    ]


def _object_schema(properties: dict[str, Any], required: list[str] | None = None) -> dict[str, Any]:
    return {
        "type": "object",
        "properties": properties,
        "required": required or [],
        "additionalProperties": False,
    }


def _limit(arguments: dict[str, Any]) -> int:
    return min(int(arguments.get("limit") or 5), 10)
