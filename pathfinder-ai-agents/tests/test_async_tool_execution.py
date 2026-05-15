import json

from pathfinder_ai_agents.agents import BaseFoundryAgent
from pathfinder_ai_agents.contracts import AgentInvocationRequest, AgentName, ExperienceLevel, LearnerContext
from pathfinder_ai_agents.tool_registry import AgentTool, ToolRegistry


class _Response:
    def __init__(self, output, output_text="final") -> None:
        self.id = "resp_1"
        self.output = output
        self.output_text = output_text


class _Responses:
    def __init__(self) -> None:
        self.calls = 0
        self.tool_output = None

    def create(self, **kwargs):
        self.calls += 1
        if self.calls == 1:
            return _Response(
                [
                    {
                        "type": "function_call",
                        "name": "async_tool",
                        "call_id": "call_1",
                        "arguments": "{}",
                    }
                ],
                output_text="",
            )

        self.tool_output = kwargs["input"][0]["output"]
        return _Response([], output_text="final")


class _OpenAiClient:
    def __init__(self) -> None:
        self.responses = _Responses()


class _ClientFactory:
    def __init__(self) -> None:
        self.client = _OpenAiClient()

    def get_openai_client(self):
        return self.client


class _Settings:
    model_deployment_name = "test-model"


class _Agent(BaseFoundryAgent):
    name = AgentName.PROGRESS_COACH
    system_prompt = "test"

    def build_user_input(self, request: AgentInvocationRequest) -> str:
        return request.message


async def _async_tool(_arguments):
    return {"value": "awaited"}


def test_agent_tool_loop_serializes_awaited_async_tool_result() -> None:
    registry = ToolRegistry(
        [
            AgentTool(
                name="async_tool",
                description="Async test tool",
                parameters={"type": "object", "properties": {}},
                handler=_async_tool,
            )
        ]
    )
    factory = _ClientFactory()
    agent = _Agent(factory, _Settings(), registry)

    response = agent.run(
        AgentInvocationRequest(
            agent_name=AgentName.PROGRESS_COACH,
            message="hello",
            context=LearnerContext(
                track_id="backend",
                experience_level=ExperienceLevel.BEGINNER,
                weekly_study_hours=6,
            ),
        )
    )

    assert response.output_text == "final"
    assert json.loads(factory.client.responses.tool_output) == {"value": "awaited"}
