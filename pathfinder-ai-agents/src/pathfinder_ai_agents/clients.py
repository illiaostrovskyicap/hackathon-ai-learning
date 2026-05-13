from typing import Any, Final

from openai import OpenAI

from pathfinder_ai_agents.config import Settings


class FoundryClientFactory:
    _DEFAULT_TIMEOUT_SECONDS: Final[float] = 60.0

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._openai_client: OpenAI | None = None

    def get_openai_client(self) -> OpenAI:
        if self._openai_client is None:
            self._openai_client = OpenAI(
                base_url=self._settings.openai_base_url,
                api_key=self._settings.openai_api_key,
                timeout=self._DEFAULT_TIMEOUT_SECONDS,
            )

        return self._openai_client


class LearnMcpClient:
    """Normalized facade for Microsoft Learn MCP results.

    The current implementation is intentionally stubbed. The public methods and response shape are
    the contract we will keep when replacing this with a real Streamable HTTP MCP client.
    """

    def __init__(self, endpoint: str) -> None:
        self._endpoint = endpoint

    def search_docs(self, query: str, limit: int = 5) -> dict[str, Any]:
        results = [
            {
                "title": "Create serverless APIs with Azure Functions",
                "url": "https://learn.microsoft.com/azure/azure-functions/functions-overview",
                "source_tool": "microsoft_docs_search",
                "content_type": "documentation",
                "summary": (
                    "Overview of Azure Functions concepts, hosting, triggers, bindings, and common"
                    " serverless application patterns."
                ),
                "matched_skills": ["azure-functions"],
            },
            {
                "title": "Host ASP.NET Core on Azure App Service",
                "url": "https://learn.microsoft.com/aspnet/core/host-and-deploy/azure-apps/",
                "source_tool": "microsoft_docs_search",
                "content_type": "documentation",
                "summary": (
                    "Guidance for deploying and operating ASP.NET Core applications on Azure App"
                    " Service."
                ),
                "matched_skills": ["azure-app-service", "aspnet-core-basics"],
            },
            {
                "title": "Entity Framework Core documentation",
                "url": "https://learn.microsoft.com/ef/core/",
                "source_tool": "microsoft_docs_search",
                "content_type": "documentation",
                "summary": (
                    "Main documentation hub for EF Core data access, querying, migrations, and"
                    " provider configuration."
                ),
                "matched_skills": ["entity-framework"],
            },
        ]
        return {
            "endpoint": self._endpoint,
            "query": query,
            "results": results[:limit],
            "is_stubbed": True,
        }

    def fetch_doc(self, url: str) -> dict[str, Any]:
        return {
            "endpoint": self._endpoint,
            "url": url,
            "source_tool": "microsoft_docs_fetch",
            "title": "Create serverless APIs with Azure Functions",
            "content_type": "documentation",
            "summary": (
                "Azure Functions lets you run event-driven code without managing servers. For a"
                " backend learner, focus first on HTTP triggers, bindings, local debugging, and"
                " deployment basics."
            ),
            "key_points": [
                "Start with HTTP-triggered functions before timer or queue triggers.",
                "Understand bindings as the bridge between function code and Azure services.",
                "Practice local development before deploying to Azure.",
            ],
            "is_stubbed": True,
        }

    def search_code_samples(self, query: str, limit: int = 5) -> dict[str, Any]:
        samples = [
            {
                "title": "HTTP trigger function in Python",
                "url": "https://learn.microsoft.com/samples/",
                "source_tool": "microsoft_code_sample_search",
                "language": "python",
                "summary": "A starter HTTP-triggered Azure Function sample.",
                "matched_skills": ["azure-functions"],
            },
            {
                "title": "ASP.NET Core minimal API sample",
                "url": "https://learn.microsoft.com/samples/",
                "source_tool": "microsoft_code_sample_search",
                "language": "csharp",
                "summary": "A compact ASP.NET Core Web API sample for routing and handlers.",
                "matched_skills": ["aspnet-core-basics"],
            },
        ]
        return {
            "endpoint": self._endpoint,
            "query": query,
            "results": samples[:limit],
            "is_stubbed": True,
        }
