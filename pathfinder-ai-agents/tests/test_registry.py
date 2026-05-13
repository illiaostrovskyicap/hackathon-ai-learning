from pathfinder_ai_agents.agents import AgentRegistry


def test_registry_lists_default_agents() -> None:
    registry = AgentRegistry()
    assert registry.list_names() == []
