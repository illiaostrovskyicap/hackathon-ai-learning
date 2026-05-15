using System.Text.Json.Serialization;
using PathfinderAPI.Controllers;

public sealed class AdaptiveLearningService
{
    public ProgressUpdateResult ApplyProgressUpdate(
        LearningPlanResponse plan,
        SkillMatrixResponse skillMatrix,
        ProgressUpdateRequest request,
        SkillGrowthEvaluation growthEvaluation,
        AdaptiveSubModuleBlueprint? adaptiveBlueprint = null)
    {
        var promotedSkills = new List<PromotedSkillResponse>();
        var pathwayUpdated = false;
        var timestamp = request.OccurredAtUtc == default ? DateTimeOffset.UtcNow : request.OccurredAtUtc;

        if (request.ItemType.Equals("module", StringComparison.OrdinalIgnoreCase))
        {
            var module = plan.Modules.FirstOrDefault(x => x.Id == request.ItemId);
            if (module is null)
            {
                throw new InvalidOperationException($"Module '{request.ItemId}' was not found.");
            }

            ApplyState(module, request.Action, timestamp);

            if (request.Action.Equals("complete", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var subModule in module.SubModules)
                {
                    if (subModule.Status != "completed")
                    {
                        ApplyState(subModule, "complete", timestamp);
                    }
                }
            }
        }
        else
        {
            var module = plan.Modules.FirstOrDefault(x => x.SubModules.Any(sm => sm.Id == request.ItemId));

            if (module is null)
            {
                throw new InvalidOperationException($"Submodule '{request.ItemId}' was not found.");
            }

            var subModule = module.SubModules.First(x => x.Id == request.ItemId);
            ApplyState(subModule, request.Action, timestamp);

            if (request.Action.Equals("open", StringComparison.OrdinalIgnoreCase))
            {
                ApplyState(module, "open", timestamp);
            }

            if (request.Action.Equals("complete", StringComparison.OrdinalIgnoreCase))
            {
                RecalculateModuleFromChildren(module, timestamp);

                var growthResult = ApplySkillGrowth(skillMatrix, growthEvaluation, timestamp);
                promotedSkills.AddRange(growthResult.PromotedSkills);

                if (promotedSkills.Count > 0)
                {
                    pathwayUpdated = TryApplyAdaptiveUpgrade(plan, promotedSkills, adaptiveBlueprint, timestamp);
                }
            }
        }

        foreach (var module in plan.Modules)
        {
            RecalculateModuleFromChildren(module, timestamp);
        }

        skillMatrix.WeakSkills = skillMatrix.Skills
            .Where(x => x.Score < 45)
            .OrderBy(x => x.Score)
            .Take(5)
            .Select(x => x.Key)
            .ToList();

        plan.SkillMatrix = skillMatrix;

        return new ProgressUpdateResult
        {
            LearningPlan = plan,
            SkillMatrix = skillMatrix,
            PromotedSkills = promotedSkills,
            PathwayUpdated = pathwayUpdated
        };
    }

    public SkillMatrixResponse InitializeSkillMatrix(OnboardingSnapshotResponse onboardingSnapshot)
    {
        var seedSkills = new Dictionary<string, SkillProgressResponse>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in AdaptiveLearningCatalog.GetTrackSkills(onboardingSnapshot.Track))
        {
            seedSkills[skill.Key] = new SkillProgressResponse
            {
                Key = skill.Key,
                Name = skill.Name,
                Score = BaseScoreForExperience(onboardingSnapshot.Experience),
                Level = LevelFromScore(BaseScoreForExperience(onboardingSnapshot.Experience)),
                LastUpdatedAt = onboardingSnapshot.CreatedAtUtc
            };
        }

        foreach (var focusArea in onboardingSnapshot.FocusAreas)
        {
            var key = AdaptiveLearningCatalog.NormalizeKey(focusArea);
            if (!seedSkills.TryGetValue(key, out var existing))
            {
                existing = new SkillProgressResponse
                {
                    Key = key,
                    Name = focusArea,
                    Score = BaseScoreForExperience(onboardingSnapshot.Experience),
                    Level = LevelFromScore(BaseScoreForExperience(onboardingSnapshot.Experience)),
                    LastUpdatedAt = onboardingSnapshot.CreatedAtUtc
                };
                seedSkills[key] = existing;
            }

            existing.Score = Math.Min(90, existing.Score + 5);
            existing.Level = LevelFromScore(existing.Score);
        }

        var matrix = new SkillMatrixResponse
        {
            Skills = seedSkills.Values
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        matrix.WeakSkills = matrix.Skills
            .Where(x => x.Score < 45)
            .OrderBy(x => x.Score)
            .Take(5)
            .Select(x => x.Key)
            .ToList();

        return matrix;
    }

    public static string LevelFromScore(double score)
    {
        if (score >= 75)
        {
            return "advanced";
        }

        if (score >= 40)
        {
            return "intermediate";
        }

        return "beginner";
    }

    public static List<SkillReferenceResponse> InferSkills(string track, IEnumerable<string> topics, string title)
    {
        var inferred = new Dictionary<string, SkillReferenceResponse>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in AdaptiveLearningCatalog.GetTrackSkills(track))
        {
            inferred[skill.Key] = new SkillReferenceResponse
            {
                Key = skill.Key,
                Name = skill.Name,
                Weight = 0.3
            };
        }

        var text = $"{title} {string.Join(' ', topics)}";
        foreach (var match in AdaptiveLearningCatalog.MatchSkills(text))
        {
            inferred[match.Key] = match;
        }

        return inferred.Values
            .OrderByDescending(x => x.Weight)
            .Take(4)
            .ToList();
    }

    private static void ApplyState(ModuleResponse module, string action, DateTimeOffset timestamp)
    {
        module.LastOpenedAt = timestamp;

        if (action.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            module.StartedAt ??= timestamp;
            if (module.Status == "not-started")
            {
                module.Status = "in-progress";
            }
            return;
        }

        if (action.Equals("complete", StringComparison.OrdinalIgnoreCase))
        {
            module.StartedAt ??= timestamp;
            module.CompletedAt = timestamp;
            module.Status = "completed";
        }
    }

    private static void ApplyState(SubModuleResponse subModule, string action, DateTimeOffset timestamp)
    {
        subModule.LastOpenedAt = timestamp;

        if (action.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            subModule.StartedAt ??= timestamp;
            if (subModule.Status == "not-started")
            {
                subModule.Status = "in-progress";
            }
            return;
        }

        if (action.Equals("complete", StringComparison.OrdinalIgnoreCase))
        {
            subModule.StartedAt ??= timestamp;
            subModule.CompletedAt = timestamp;
            subModule.Status = "completed";
        }
    }

    private static void RecalculateModuleFromChildren(ModuleResponse module, DateTimeOffset timestamp)
    {
        if (module.SubModules.Count == 0)
        {
            return;
        }

        if (module.SubModules.All(x => x.Status == "completed"))
        {
            module.Status = "completed";
            module.StartedAt ??= module.SubModules
                .Where(x => x.StartedAt.HasValue)
                .Select(x => x.StartedAt)
                .OrderBy(x => x)
                .FirstOrDefault();
            module.CompletedAt = module.SubModules
                .Where(x => x.CompletedAt.HasValue)
                .Select(x => x.CompletedAt)
                .OrderByDescending(x => x)
                .FirstOrDefault() ?? timestamp;
            return;
        }

        if (module.SubModules.Any(x => x.Status != "not-started"))
        {
            module.Status = "in-progress";
            module.StartedAt ??= module.SubModules
                .Where(x => x.StartedAt.HasValue)
                .Select(x => x.StartedAt)
                .OrderBy(x => x)
                .FirstOrDefault() ?? timestamp;
            module.CompletedAt = null;
            return;
        }

        module.Status = "not-started";
        module.CompletedAt = null;
    }

    private static SkillGrowthResult ApplySkillGrowth(
        SkillMatrixResponse skillMatrix,
        SkillGrowthEvaluation growthEvaluation,
        DateTimeOffset timestamp)
    {
        var promotedSkills = new List<PromotedSkillResponse>();

        foreach (var growth in growthEvaluation.SkillDeltas)
        {
            var skill = skillMatrix.Skills.FirstOrDefault(x => x.Key.Equals(growth.Key, StringComparison.OrdinalIgnoreCase));
            if (skill is null)
            {
                skill = new SkillProgressResponse
                {
                    Key = growth.Key,
                    Name = growth.Name,
                    Score = 0,
                    Level = "beginner",
                    LastUpdatedAt = timestamp
                };
                skillMatrix.Skills.Add(skill);
            }

            var previousLevel = skill.Level;
            skill.Score = Math.Clamp(skill.Score + growth.Delta, 0, 100);
            skill.Level = LevelFromScore(skill.Score);
            skill.LastUpdatedAt = timestamp;

            if (!string.Equals(previousLevel, skill.Level, StringComparison.OrdinalIgnoreCase))
            {
                promotedSkills.Add(new PromotedSkillResponse
                {
                    Key = skill.Key,
                    Name = skill.Name,
                    PreviousLevel = previousLevel,
                    NewLevel = skill.Level,
                    Score = skill.Score
                });
            }
        }

        return new SkillGrowthResult { PromotedSkills = promotedSkills };
    }

    private static bool TryApplyAdaptiveUpgrade(
        LearningPlanResponse plan,
        List<PromotedSkillResponse> promotedSkills,
        AdaptiveSubModuleBlueprint? adaptiveBlueprint,
        DateTimeOffset timestamp)
    {
        var targetModule = plan.Modules.FirstOrDefault(x => x.Status == "not-started");
        if (targetModule is null)
        {
            return false;
        }

        var promotedSkill = promotedSkills[0];
        var alreadyApplied = plan.AdaptiveState.Updates.Any(x =>
            x.SkillKey.Equals(promotedSkill.Key, StringComparison.OrdinalIgnoreCase)
            && x.TargetModuleId == targetModule.Id);

        if (alreadyApplied)
        {
            return false;
        }

        var blueprint = adaptiveBlueprint ?? new AdaptiveSubModuleBlueprint
        {
            Title = $"Stretch: {promotedSkill.Name} in real-world scenarios",
            Description = $"Apply your stronger {promotedSkill.Name} skills in a more demanding track-specific exercise.",
            EstimatedHours = 3,
            Topics = new List<string> { promotedSkill.Name, "Adaptive Growth", "Practice" },
            ProjectTask = $"Build a focused exercise that proves you can use {promotedSkill.Name} beyond the basics.",
            ResourceQuery = $"{plan.Track} advanced {promotedSkill.Name} practice"
        };

        var adaptiveSubModule = new SubModuleResponse
        {
            Id = $"adaptive-{AdaptiveLearningCatalog.NormalizeKey(promotedSkill.Key)}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            Title = blueprint.Title,
            Description = blueprint.Description,
            EstimatedHours = blueprint.EstimatedHours,
            Topics = blueprint.Topics,
            ProjectTask = blueprint.ProjectTask,
            ResourceQuery = blueprint.ResourceQuery,
            Status = "not-started",
            Skills = new List<SkillReferenceResponse>
            {
                new()
                {
                    Key = promotedSkill.Key,
                    Name = promotedSkill.Name,
                    Weight = 1
                }
            }
        };

        targetModule.SubModules.Add(adaptiveSubModule);
        targetModule.Topics = targetModule.Topics
            .Concat(new[] { promotedSkill.Name })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        targetModule.EstimatedHours += adaptiveSubModule.EstimatedHours;

        plan.AdaptiveState.Updates.Add(new AdaptivePathwayUpdateResponse
        {
            TriggeredAt = timestamp,
            SkillKey = promotedSkill.Key,
            SkillName = promotedSkill.Name,
            PreviousLevel = promotedSkill.PreviousLevel,
            NewLevel = promotedSkill.NewLevel,
            TargetModuleId = targetModule.Id,
            AddedSubModuleId = adaptiveSubModule.Id,
            Summary = $"Added a stretch lesson to {targetModule.Title} after {promotedSkill.Name} moved from {promotedSkill.PreviousLevel} to {promotedSkill.NewLevel}."
        });

        return true;
    }

    private static double BaseScoreForExperience(string experience) =>
        experience switch
        {
            "advanced" => 62,
            "intermediate" => 38,
            _ => 20
        };
}

public sealed class ProgressUpdateRequest
{
    public string UserId { get; set; } = "";
    public string ItemType { get; set; } = "module";
    public string ItemId { get; set; } = "";
    public string Action { get; set; } = "open";
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int MinutesSpent { get; set; }
    public double? AssessmentScore { get; set; }
    public string? Reflection { get; set; }
    public string? ResourceTitle { get; set; }
}

public class ProgressUpdateResponse
{
    public LearningPlanResponse LearningPlan { get; set; } = new();
    public SkillMatrixResponse SkillMatrix { get; set; } = new();
    public List<PromotedSkillResponse> PromotedSkills { get; set; } = new();
    public bool PathwayUpdated { get; set; }
}

public sealed class ProgressUpdateResult : ProgressUpdateResponse
{
}

public sealed class SkillMatrixResponse
{
    public List<SkillProgressResponse> Skills { get; set; } = new();
    public List<string> WeakSkills { get; set; } = new();
}

public sealed class SkillProgressResponse
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public string Level { get; set; } = "beginner";
    public DateTimeOffset LastUpdatedAt { get; set; }
}

public sealed class SkillReferenceResponse
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public double Weight { get; set; }
}

public sealed class PromotedSkillResponse
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string PreviousLevel { get; set; } = "";
    public string NewLevel { get; set; } = "";
    public double Score { get; set; }
}

public sealed class AdaptivePathwayResponse
{
    public List<AdaptivePathwayUpdateResponse> Updates { get; set; } = new();
}

public sealed class AdaptivePathwayUpdateResponse
{
    public DateTimeOffset TriggeredAt { get; set; }
    public string SkillKey { get; set; } = "";
    public string SkillName { get; set; } = "";
    public string PreviousLevel { get; set; } = "";
    public string NewLevel { get; set; } = "";
    public string TargetModuleId { get; set; } = "";
    public string AddedSubModuleId { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class OnboardingSnapshotResponse
{
    public string Track { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Language { get; set; } = "en";
    public string Goal { get; set; } = "get-job-ready";
    public int WeeklyHours { get; set; } = 6;
    public List<string> FocusAreas { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SkillGrowthEvaluation
{
    public List<SkillDelta> SkillDeltas { get; set; } = new();
}

public sealed class SkillDelta
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public double Delta { get; set; }
}

public sealed class AdaptiveSubModuleBlueprint
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("estimatedHours")]
    public int EstimatedHours { get; set; }

    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = new();

    [JsonPropertyName("projectTask")]
    public string ProjectTask { get; set; } = "";

    [JsonPropertyName("resourceQuery")]
    public string ResourceQuery { get; set; } = "";
}

internal sealed class SkillGrowthResult
{
    public List<PromotedSkillResponse> PromotedSkills { get; set; } = new();
}

internal static class AdaptiveLearningCatalog
{
    private static readonly Dictionary<string, (string Name, string[] Keywords)> Skills =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["react"] = ("React", new[] { "react", "hooks", "component", "frontend" }),
            ["typescript"] = ("TypeScript", new[] { "typescript", "types", "ts" }),
            ["api-design"] = ("API Design", new[] { "api", "rest", "http", "endpoint" }),
            ["sql"] = ("SQL", new[] { "sql", "query", "database", "postgres" }),
            ["testing"] = ("Testing", new[] { "test", "testing", "quality" }),
            ["architecture"] = ("Architecture", new[] { "architecture", "design", "module" }),
            ["cloud"] = ("Cloud", new[] { "cloud", "deployment", "devops" }),
            ["performance"] = ("Performance", new[] { "performance", "optimization", "scaling" }),
            ["debugging"] = ("Debugging", new[] { "debug", "debugging", "troubleshoot" }),
            ["data-modeling"] = ("Data Modeling", new[] { "schema", "data", "modeling" }),
            ["mobile"] = ("Mobile", new[] { "mobile", "ios", "android" })
        };

    public static IEnumerable<SkillReferenceResponse> GetTrackSkills(string track)
    {
        var normalized = NormalizeKey(track);
        return normalized switch
        {
            "frontend" => new[]
            {
                Create("react", 0.8),
                Create("typescript", 0.7),
                Create("testing", 0.4)
            },
            "backend" => new[]
            {
                Create("api-design", 0.8),
                Create("sql", 0.7),
                Create("architecture", 0.5)
            },
            "full-stack" or "fullstack" => new[]
            {
                Create("react", 0.7),
                Create("api-design", 0.7),
                Create("sql", 0.6)
            },
            "data" => new[]
            {
                Create("sql", 0.8),
                Create("data-modeling", 0.7),
                Create("performance", 0.4)
            },
            "devops" => new[]
            {
                Create("cloud", 0.8),
                Create("performance", 0.5),
                Create("architecture", 0.4)
            },
            "mobile" => new[]
            {
                Create("mobile", 0.8),
                Create("typescript", 0.4),
                Create("testing", 0.4)
            },
            _ => new[]
            {
                Create("architecture", 0.5),
                Create("testing", 0.5),
                Create("debugging", 0.4)
            }
        };
    }

    public static IEnumerable<SkillReferenceResponse> MatchSkills(string text)
    {
        foreach (var (key, value) in Skills)
        {
            if (value.Keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new SkillReferenceResponse
                {
                    Key = key,
                    Name = value.Name,
                    Weight = 0.9
                };
            }
        }
    }

    public static string NormalizeKey(string text)
    {
        return text
            .Trim()
            .ToLowerInvariant()
            .Replace("&", "and", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal);
    }

    private static SkillReferenceResponse Create(string key, double weight) =>
        new()
        {
            Key = key,
            Name = Skills[key].Name,
            Weight = weight
        };
}
