from typing import Any, Final

from openai import OpenAI

from pathfinder_ai_agents.config import Settings

import httpx


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


import json


class LearnMcpClient:
    """Microsoft Learn MCP client with fallback resources."""

    def __init__(self, endpoint: str | None) -> None:
        self._endpoint = endpoint or "https://learn.microsoft.com/api/mcp"

    async def search_docs(self, query: str, limit: int = 5) -> dict[str, Any]:
        try:
            mcp_response = await self._call_tool(
                "microsoft_docs_search",
                {
                    "query": query,
                    "limit": limit,
                },
            )

            results = self._normalize_mcp_results(mcp_response, limit)

            if results:
                return {
                    "endpoint": self._endpoint,
                    "query": query,
                    "results": results,
                    "is_stubbed": False,
                }
        except Exception as exc:
            return {
                "endpoint": self._endpoint,
                "query": query,
                "results": self._fallback_results(query, limit),
                "is_stubbed": True,
                "fallback_reason": str(exc),
            }

        return {
            "endpoint": self._endpoint,
            "query": query,
            "results": self._fallback_results(query, limit),
            "is_stubbed": True,
        }

    async def fetch_doc(self, url: str) -> dict[str, Any]:
        return {
            "endpoint": self._endpoint,
            "url": url,
            "source_tool": "microsoft_docs_fetch",
            "title": "Microsoft Learn resource",
            "content_type": "documentation",
            "summary": "Open the Microsoft Learn link for the full article.",
            "key_points": [],
            "is_stubbed": False,
        }

    async def search_code_samples(self, query: str, limit: int = 5) -> dict[str, Any]:
        return await self.search_docs(f"{query} code sample", limit)

    async def _call_tool(self, tool_name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        payload = {
            "jsonrpc": "2.0",
            "id": "pathfinder-request",
            "method": "tools/call",
            "params": {
                "name": tool_name,
                "arguments": arguments,
            },
        }

        async with httpx.AsyncClient(timeout=30) as client:
            response = await client.post(
                self._endpoint,
                json=payload,
                headers={
                    "Accept": "application/json, text/event-stream",
                    "Content-Type": "application/json",
                },
            )

        response.raise_for_status()
        return self._parse_streamable_http_response(response.text)

    def _parse_streamable_http_response(self, raw_text: str) -> dict[str, Any]:
        raw_text = raw_text.strip()

        if not raw_text:
            return {}

        if raw_text.startswith("data:"):
            events = []
            for line in raw_text.splitlines():
                line = line.strip()
                if line.startswith("data:"):
                    data = line.removeprefix("data:").strip()
                    if data and data != "[DONE]":
                        events.append(json.loads(data))

            return events[-1] if events else {}

        return json.loads(raw_text)

    def _normalize_mcp_results(self, response: dict[str, Any], limit: int) -> list[dict[str, Any]]:
        result = response.get("result", response)

        content = result.get("content") or result.get("results") or []

        if isinstance(content, dict):
            content = content.get("results", [])

        if isinstance(content, str):
            try:
                content = json.loads(content)
            except json.JSONDecodeError:
                return []

        normalized: list[dict[str, Any]] = []
        seen_urls: set[str] = set()

        for item in content:
            if isinstance(item, dict) and item.get("type") == "text":
                try:
                    parsed = json.loads(item.get("text", "{}"))
                    if isinstance(parsed, dict):
                        nested = parsed.get("results", [])
                        content.extend(nested)
                    continue
                except json.JSONDecodeError:
                    continue

            if not isinstance(item, dict):
                continue

            title = item.get("title") or item.get("name") or "Microsoft Learn resource"
            url = item.get("url") or item.get("contentUrl") or item.get("link") or "#"
            summary = item.get("summary") or item.get("description") or ""

            if url in seen_urls:
                continue

            seen_urls.add(url)

            normalized.append(
                {
                    "title": title,
                    "url": url,
                    "source_tool": "microsoft_docs_search",
                    "content_type": "documentation",
                    "summary": summary,
                    "matched_skills": [],
                }
            )

            if len(normalized) >= limit:
                break

        return normalized

    def _fallback_results(self, query: str, limit: int) -> list[dict[str, Any]]:
        q = query.lower()

        microsoft_catalog = [
        {
            "keywords": ["dotnet", ".net", "asp.net", "c#", "web api", "minimal api", "backend"],
            "title": "Create a web API with ASP.NET Core",
            "url": "https://learn.microsoft.com/aspnet/core/tutorials/first-web-api",
            "content_type": "documentation",
            "summary": "Build REST APIs with ASP.NET Core.",
        },
        {
            "keywords": ["entity framework", "ef core", "orm", "database", "sql"],
            "title": "Entity Framework Core documentation",
            "url": "https://learn.microsoft.com/ef/core/",
            "content_type": "documentation",
            "summary": "EF Core docs for querying, migrations, relationships, and database access.",
        },
        {
            "keywords": ["azure", "app service", "hosting", "deployment", "production"],
            "title": "Host ASP.NET Core on Azure App Service",
            "url": "https://learn.microsoft.com/aspnet/core/host-and-deploy/azure-apps/",
            "content_type": "documentation",
            "summary": "Deploy and operate ASP.NET Core apps on Azure App Service.",
        },
        {
            "keywords": ["azure functions", "serverless", "function", "trigger"],
            "title": "Azure Functions overview",
            "url": "https://learn.microsoft.com/azure/azure-functions/functions-overview",
            "content_type": "documentation",
            "summary": "Azure Functions concepts, triggers, bindings, and serverless patterns.",
        },
        {
            "keywords": ["postgres", "postgresql", "azure database", "database"],
            "title": "Azure Database for PostgreSQL documentation",
            "url": "https://learn.microsoft.com/azure/postgresql/",
            "content_type": "documentation",
            "summary": "Azure PostgreSQL setup, connection, security, and operations.",
        },
        {
            "keywords": ["azure monitor", "monitoring", "logging", "observability", "metrics"],
            "title": "Azure Monitor overview",
            "url": "https://learn.microsoft.com/azure/azure-monitor/overview",
            "content_type": "documentation",
            "summary": "Metrics, logs, alerts, and observability with Azure Monitor.",
        },
        {
            "keywords": ["identity", "authentication", "authorization", "jwt", "oauth", "entra"],
            "title": "Microsoft identity platform documentation",
            "url": "https://learn.microsoft.com/entra/identity-platform/",
            "content_type": "documentation",
            "summary": "Authentication, authorization, OAuth, OpenID Connect, and Microsoft identity platform.",
        },
        {
            "keywords": ["maui", ".net maui", "mobile", "android", "ios", "cross-platform"],
            "title": ".NET MAUI documentation",
            "url": "https://learn.microsoft.com/dotnet/maui/",
            "content_type": "documentation",
            "summary": "Build cross-platform mobile and desktop apps with .NET MAUI.",
        },
        {
            "keywords": ["blazor", "webassembly", "components"],
            "title": "ASP.NET Core Blazor documentation",
            "url": "https://learn.microsoft.com/aspnet/core/blazor/",
            "content_type": "documentation",
            "summary": "Build interactive web UIs with Blazor.",
        },
    ]

        external_catalog = [
        {
            "keywords": ["html", "semantic", "forms", "accessibility", "markup"],
            "title": "HTML basics",
            "url": "https://developer.mozilla.org/en-US/docs/Learn/Getting_started_with_the_web/HTML_basics",
            "content_type": "documentation",
            "summary": "HTML structure and semantic markup fundamentals.",
        },
        {
            "keywords": ["css", "layout", "responsive", "flexbox", "grid"],
            "title": "CSS layout",
            "url": "https://developer.mozilla.org/en-US/docs/Learn/CSS/CSS_layout",
            "content_type": "documentation",
            "summary": "Responsive layouts with flexbox and grid.",
        },
        {
            "keywords": ["javascript", "js", "dom", "events", "async", "promise"],
            "title": "JavaScript Guide",
            "url": "https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide",
            "content_type": "documentation",
            "summary": "JavaScript fundamentals, DOM, events, and async programming.",
        },
        {
            "keywords": ["react", "components", "hooks", "state", "props"],
            "title": "React Learn",
            "url": "https://react.dev/learn",
            "content_type": "course",
            "summary": "Official React learning path.",
        },
        {
            "keywords": ["docker", "container", "dockerfile", "compose"],
            "title": "Docker getting started",
            "url": "https://docs.docker.com/get-started/",
            "content_type": "documentation",
            "summary": "Docker basics for containers, images, and workflows.",
        },
    ]

        microsoft_results = self._rank_catalog(microsoft_catalog, q)
        external_results = self._rank_catalog(external_catalog, q)

        results = microsoft_results + external_results

        if not results:
            results = microsoft_catalog[:2] + external_catalog[:2]

        return results[:limit]
    
    def _rank_catalog(self, catalog: list[dict[str, Any]], q: str) -> list[dict[str, Any]]:
        scored: list[tuple[int, dict[str, Any]]] = []

        for item in catalog:
            score = 0

            for keyword in item["keywords"]:
                if keyword in q:
                    score += 3

            for word in item["title"].lower().split():
                if len(word) > 3 and word in q:
                    score += 1

            if score > 0:
                scored.append((score, item))

        scored.sort(key=lambda x: x[0], reverse=True)

        return [
            {
            "title": item["title"],
            "url": item["url"],
            "source_tool": "fallback_catalog",
            "content_type": item["content_type"],
            "summary": item["summary"],
            "matched_skills": item["keywords"][:3],
            }
            for _, item in scored
        ]