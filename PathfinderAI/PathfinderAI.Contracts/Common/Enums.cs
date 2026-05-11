namespace PathfinderAI.Contracts.Common;

public enum ExperienceLevel
{
    Beginner = 0,
    Junior = 1,
    Middle = 2,
    Senior = 3
}

public enum PlanGenerationStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

public enum LearningPlanStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public enum LearningModuleStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3
}

public enum LearningResourceType
{
    Article = 0,
    Video = 1,
    Exercise = 2,
    Documentation = 3,
    Course = 4
}

public enum AssistantMessageRole
{
    System = 0,
    User = 1,
    Assistant = 2
}
