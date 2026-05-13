from typing import Any

from pathfinder_ai_agents.tool_registry import AgentTool, ToolRegistry


def create_learner_tool_registry(extra_tools: list[AgentTool] | None = None) -> ToolRegistry:
    return ToolRegistry(
        [
            AgentTool(
                name="get_learner_profile",
                description=(
                    "Return the learner profile used to personalize roadmap and coaching answers."
                ),
                parameters=_object_schema(
                    {
                        "learner_id": {
                            "type": "string",
                            "description": (
                                "Optional learner identifier. Use demo-learner when absent."
                            ),
                        }
                    }
                ),
                handler=get_learner_profile,
            ),
            AgentTool(
                name="get_current_progress",
                description=(
                    "Return current progress, weak skills, and recent learning activity."
                ),
                parameters=_object_schema(
                    {
                        "learner_id": {
                            "type": "string",
                            "description": (
                                "Optional learner identifier. Use demo-learner when absent."
                            ),
                        }
                    }
                ),
                handler=get_current_progress,
            ),
            AgentTool(
                name="get_skill_matrix",
                description=(
                    "Return target-role skill requirements and current learner levels."
                ),
                parameters=_object_schema(
                    {
                        "learner_id": {
                            "type": "string",
                            "description": (
                                "Optional learner identifier. Use demo-learner when absent."
                            ),
                        },
                        "target_role": {
                            "type": "string",
                            "description": (
                                "Optional target role or track id, for example dotnet-backend."
                            ),
                        },
                    }
                ),
                handler=get_skill_matrix,
            ),
            AgentTool(
                name="get_recommended_modules",
                description="Return recommended learning modules for weak or requested skills.",
                parameters=_object_schema(
                    {
                        "learner_id": {
                            "type": "string",
                            "description": (
                                "Optional learner identifier. Use demo-learner when absent."
                            ),
                        },
                        "skill_ids": {
                            "type": "array",
                            "description": "Optional skill ids to prioritize.",
                            "items": {"type": "string"},
                        },
                        "limit": {
                            "type": "integer",
                            "description": "Maximum number of modules to return.",
                            "minimum": 1,
                            "maximum": 10,
                        },
                    }
                ),
                handler=get_recommended_modules,
            ),
            *(extra_tools or []),
        ]
    )


def get_learner_profile(arguments: dict[str, Any]) -> dict[str, Any]:
    learner_id = _learner_id(arguments)
    return {
        "learner_id": learner_id,
        "track_id": "dotnet-backend",
        "target_role": ".NET Backend Developer",
        "experience_level": "Junior",
        "preferred_locale": "en-US",
        "weekly_study_hours": 6,
        "current_goal": "Become interview ready for .NET backend roles with Azure fundamentals.",
        "career_intent": (
            "Grow as a backend engineer who can build and deploy ASP.NET Core services on Azure."
        ),
    }


def get_current_progress(arguments: dict[str, Any]) -> dict[str, Any]:
    learner_id = _learner_id(arguments)
    return {
        "learner_id": learner_id,
        "completed_skills": ["csharp-basics", "aspnet-core-basics"],
        "weak_skills": ["entity-framework", "azure-functions", "azure-app-service"],
        "completed_modules": 3,
        "in_progress_modules": 1,
        "remaining_modules": 8,
        "progress_percent": 27,
        "recent_questions": [
            "How do I build Web APIs in ASP.NET Core?",
            "What should I study next before Azure Functions?",
        ],
        "recent_activity": [
            {
                "type": "module_completed",
                "title": "Build a web API with ASP.NET Core",
                "occurred_at": "2026-05-03T14:25:00Z",
            },
            {
                "type": "assessment_gap",
                "skill_id": "entity-framework",
                "score_percent": 48,
                "occurred_at": "2026-05-04T09:10:00Z",
            },
        ],
    }


def get_skill_matrix(arguments: dict[str, Any]) -> dict[str, Any]:
    learner_id = _learner_id(arguments)
    target_role = arguments.get("target_role") or "dotnet-backend"
    return {
        "learner_id": learner_id,
        "target_role": target_role,
        "skills": [
            {
                "skill_id": "csharp-basics",
                "name": "C# fundamentals",
                "required_level": 3,
                "current_level": 3,
                "gap": 0,
                "evidence": ["completed module", "recent quiz passed"],
            },
            {
                "skill_id": "aspnet-core-basics",
                "name": "ASP.NET Core Web API",
                "required_level": 3,
                "current_level": 2,
                "gap": 1,
                "evidence": [
                    "completed intro module",
                    "needs practice with routing and validation",
                ],
            },
            {
                "skill_id": "entity-framework",
                "name": "Entity Framework Core",
                "required_level": 3,
                "current_level": 1,
                "gap": 2,
                "evidence": ["assessment score below target"],
            },
            {
                "skill_id": "azure-functions",
                "name": "Azure Functions",
                "required_level": 2,
                "current_level": 0,
                "gap": 2,
                "evidence": ["not started"],
            },
            {
                "skill_id": "azure-app-service",
                "name": "Azure App Service",
                "required_level": 2,
                "current_level": 0,
                "gap": 2,
                "evidence": ["not started"],
            },
        ],
    }


def get_recommended_modules(arguments: dict[str, Any]) -> dict[str, Any]:
    learner_id = _learner_id(arguments)
    skill_ids = arguments.get("skill_ids") or [
        "entity-framework",
        "azure-functions",
        "azure-app-service",
    ]
    limit = min(int(arguments.get("limit") or 5), 10)
    modules = [
        {
            "module_id": "ef-core-data-access",
            "title": "Use Entity Framework Core in an ASP.NET Core app",
            "skill_ids": ["entity-framework"],
            "priority": 1,
            "estimated_minutes": 60,
            "source": "internal-demo",
            "url": "https://learn.microsoft.com/training/",
        },
        {
            "module_id": "azure-functions-http-api",
            "title": "Create serverless APIs with Azure Functions",
            "skill_ids": ["azure-functions"],
            "priority": 2,
            "estimated_minutes": 45,
            "source": "internal-demo",
            "url": "https://learn.microsoft.com/azure/azure-functions/",
        },
        {
            "module_id": "deploy-aspnet-app-service",
            "title": "Deploy an ASP.NET Core app to Azure App Service",
            "skill_ids": ["azure-app-service"],
            "priority": 3,
            "estimated_minutes": 50,
            "source": "internal-demo",
            "url": "https://learn.microsoft.com/azure/app-service/",
        },
        {
            "module_id": "aspnet-validation-practice",
            "title": "Practice request validation and error handling in ASP.NET Core",
            "skill_ids": ["aspnet-core-basics"],
            "priority": 4,
            "estimated_minutes": 35,
            "source": "internal-demo",
            "url": "https://learn.microsoft.com/aspnet/core/",
        },
    ]
    selected = [module for module in modules if set(module["skill_ids"]).intersection(skill_ids)]
    return {
        "learner_id": learner_id,
        "requested_skill_ids": skill_ids,
        "modules": selected[:limit],
    }


def _object_schema(properties: dict[str, Any]) -> dict[str, Any]:
    return {
        "type": "object",
        "properties": properties,
        "additionalProperties": False,
    }


def _learner_id(arguments: dict[str, Any]) -> str:
    return str(arguments.get("learner_id") or "demo-learner")
