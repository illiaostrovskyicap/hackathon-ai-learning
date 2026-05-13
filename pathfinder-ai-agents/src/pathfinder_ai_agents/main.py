import logging

from fastapi import FastAPI

from pathfinder_ai_agents.agents import AgentRegistry
from pathfinder_ai_agents.clients import FoundryClientFactory, LearnMcpClient
from pathfinder_ai_agents.config import get_settings
from pathfinder_ai_agents.learn_tools import create_microsoft_learn_tools
from pathfinder_ai_agents.learner_tools import create_learner_tool_registry
from pathfinder_ai_agents.observability import configure_observability
from pathfinder_ai_agents.orchestrator import AgentOrchestrator
from pathfinder_ai_agents.routes import router


def create_app() -> FastAPI:
    settings = get_settings()

    logging.basicConfig(level=settings.log_level)
    configure_observability(settings)

    client_factory = FoundryClientFactory(settings)
    learn_mcp_client = LearnMcpClient(settings.microsoft_learn_mcp_endpoint)
    learn_tools = create_microsoft_learn_tools(learn_mcp_client)
    tool_registry = create_learner_tool_registry(extra_tools=learn_tools)
    registry = AgentRegistry.create_default(client_factory, settings, tool_registry)
    orchestrator = AgentOrchestrator(registry, tool_registry)

    app = FastAPI(
        title="PathfinderAI Agents",
        version="0.1.0",
    )
    app.state.orchestrator = orchestrator
    app.include_router(router)
    return app


app = create_app()
