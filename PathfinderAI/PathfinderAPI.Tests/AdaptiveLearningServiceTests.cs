using PathfinderAPI.Controllers;
using Xunit;

namespace PathfinderAPI.Tests;

public sealed class AdaptiveLearningServiceTests
{
    [Fact]
    public void OpeningSubmoduleStartsSubmoduleAndParentModule()
    {
        var service = new AdaptiveLearningService();
        var timestamp = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var plan = CreatePlan();
        var matrix = CreateSkillMatrix();

        var result = service.ApplyProgressUpdate(
            plan,
            matrix,
            new ProgressUpdateRequest
            {
                ItemType = "submodule",
                ItemId = "sub-1",
                Action = "open",
                OccurredAtUtc = timestamp
            },
            new SkillGrowthEvaluation());

        Assert.Equal("in-progress", result.LearningPlan.Modules[0].Status);
        Assert.Equal("in-progress", result.LearningPlan.Modules[0].SubModules[0].Status);
        Assert.Equal(timestamp, result.LearningPlan.Modules[0].StartedAt);
        Assert.Equal(timestamp, result.LearningPlan.Modules[0].SubModules[0].StartedAt);
    }

    [Fact]
    public void CompletingSubmodulePromotesSkillAndAppendsAdaptiveLessonToFutureModule()
    {
        var service = new AdaptiveLearningService();
        var timestamp = new DateTimeOffset(2026, 5, 15, 11, 0, 0, TimeSpan.Zero);
        var plan = CreatePlan();
        var matrix = CreateSkillMatrix();

        var result = service.ApplyProgressUpdate(
            plan,
            matrix,
            new ProgressUpdateRequest
            {
                ItemType = "submodule",
                ItemId = "sub-1",
                Action = "complete",
                OccurredAtUtc = timestamp
            },
            new SkillGrowthEvaluation
            {
                SkillDeltas = new List<SkillDelta>
                {
                    new()
                    {
                        Key = "sql",
                        Name = "SQL",
                        Delta = 10
                    }
                }
            });

        Assert.True(result.PathwayUpdated);
        Assert.Single(result.PromotedSkills);
        Assert.Equal("intermediate", result.SkillMatrix.Skills.Single(x => x.Key == "sql").Level);
        Assert.Contains(result.LearningPlan.Modules[1].SubModules, x => x.Id.StartsWith("adaptive-sql-", StringComparison.Ordinal));
        Assert.Single(result.LearningPlan.AdaptiveState.Updates);
    }

    private static LearningPlanResponse CreatePlan()
    {
        return new LearningPlanResponse
        {
            Id = "plan-1",
            Track = "backend",
            Experience = "beginner",
            Language = "en",
            SkillMatrix = CreateSkillMatrix(),
            OnboardingSnapshot = new OnboardingSnapshotResponse
            {
                Track = "backend",
                Experience = "beginner",
                Goal = "get-job-ready",
                WeeklyHours = 6,
                FocusAreas = new List<string> { "Architecture" }
            },
            Modules = new List<ModuleResponse>
            {
                new()
                {
                    Id = "mod-1",
                    Title = "Databases",
                    Description = "Learn data access",
                    EstimatedHours = 4,
                    Status = "not-started",
                    Topics = new List<string> { "SQL" },
                    SubModules = new List<SubModuleResponse>
                    {
                        new()
                        {
                            Id = "sub-1",
                            Title = "Write SQL queries",
                            Description = "Practice SQL",
                            EstimatedHours = 2,
                            Status = "not-started",
                            Topics = new List<string> { "SQL" },
                            Skills = new List<SkillReferenceResponse>
                            {
                                new() { Key = "sql", Name = "SQL", Weight = 1 }
                            }
                        }
                    }
                },
                new()
                {
                    Id = "mod-2",
                    Title = "APIs",
                    Description = "Build APIs",
                    EstimatedHours = 4,
                    Status = "not-started",
                    Topics = new List<string> { "REST" },
                    SubModules = new List<SubModuleResponse>()
                }
            }
        };
    }

    private static SkillMatrixResponse CreateSkillMatrix()
    {
        return new SkillMatrixResponse
        {
            Skills = new List<SkillProgressResponse>
            {
                new()
                {
                    Key = "sql",
                    Name = "SQL",
                    Score = 35,
                    Level = "beginner",
                    LastUpdatedAt = DateTimeOffset.UtcNow
                }
            },
            WeakSkills = new List<string> { "sql" }
        };
    }
}
