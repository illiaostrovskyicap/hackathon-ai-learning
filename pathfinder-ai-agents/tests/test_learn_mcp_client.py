import json
import asyncio


from pathfinder_ai_agents.clients import LearnMcpClient


def test_parse_streamable_http_response_reads_event_prefixed_sse() -> None:
    client = LearnMcpClient("https://learn.microsoft.com/api/mcp")
    payload = {
        "result": {
            "content": [
                {
                    "type": "text",
                    "text": json.dumps(
                        {
                            "results": [
                                {
                                    "title": "Create a web API with ASP.NET Core",
                                    "contentUrl": "https://learn.microsoft.com/aspnet/core/tutorials/first-web-api",
                                    "description": "Build HTTP APIs with ASP.NET Core.",
                                }
                            ]
                        }
                    ),
                }
            ]
        },
        "jsonrpc": "2.0",
        "id": "pathfinder-request",
    }

    parsed = client._parse_streamable_http_response(
        f"event: message\ndata: {json.dumps(payload)}\n\n"
    )

    results = client._normalize_mcp_results(parsed, limit=3)

    assert results == [
        {
            "title": "Create a web API with ASP.NET Core",
            "url": "https://learn.microsoft.com/aspnet/core/tutorials/first-web-api",
            "source_tool": "microsoft_docs_search",
            "content_type": "documentation",
            "summary": "Build HTTP APIs with ASP.NET Core.",
            "matched_skills": [],
        }
    ]


def test_search_code_samples_uses_real_microsoft_code_sample_tool(monkeypatch) -> None:
    client = LearnMcpClient("https://learn.microsoft.com/api/mcp")
    calls = []

    async def fake_call_tool(tool_name, arguments):
        calls.append((tool_name, arguments))
        return {
            "result": {
                "content": [
                    {
                        "type": "text",
                        "text": json.dumps(
                            {
                                "results": [
                                    {
                                        "name": "Azure Functions samples",
                                        "contentUrl": "https://github.com/Azure-Samples/functions",
                                        "description": "Sample Azure Functions apps.",
                                    }
                                ]
                            }
                        ),
                    }
                ]
            }
        }

    monkeypatch.setattr(client, "_call_tool", fake_call_tool)

    result = asyncio.run(client.search_code_samples("azure functions", limit=2))

    assert calls == [
        (
            "microsoft_code_sample_search",
            {
                "query": "azure functions",
                "limit": 2,
            },
        )
    ]
    assert result["is_stubbed"] is False
    assert result["results"][0]["content_type"] == "code_sample"
