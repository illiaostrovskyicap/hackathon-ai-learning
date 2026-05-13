from enum import StrEnum
from typing import Any
from uuid import UUID

from pydantic import BaseModel, Field


class AgentName(StrEnum):
    ROADMAP_PLANNER = "roadmap-planner"
    PROGRESS_COACH = "progress-coach"


class ExperienceLevel(StrEnum):
    BEGINNER = "Beginner"
    JUNIOR = "Junior"
    MIDDLE = "Middle"
    SENIOR = "Senior"


class LearnerContext(BaseModel):
    track_id: str
    preferred_locale: str = "en-US"
    experience_level: ExperienceLevel
    weekly_study_hours: int = Field(ge=1, le=40)
    current_goal: str | None = None
    completed_skills: list[str] = Field(default_factory=list)
    weak_skills: list[str] = Field(default_factory=list)
    completed_modules: int = Field(default=0, ge=0)
    in_progress_modules: int = Field(default=0, ge=0)
    remaining_modules: int = Field(default=0, ge=0)
    recent_questions: list[str] = Field(default_factory=list)


class AgentInvocationRequest(BaseModel):
    agent_name: AgentName
    message: str = Field(min_length=1)
    context: LearnerContext
    correlation_id: UUID | None = None


class Citation(BaseModel):
    title: str
    url: str
    source_type: str = "foundry"


class AgentInvocationResponse(BaseModel):
    agent_name: AgentName
    output_text: str
    model: str
    used_fallback: bool = False
    citations: list[Citation] = Field(default_factory=list)
    metadata: dict[str, Any] = Field(default_factory=dict)
