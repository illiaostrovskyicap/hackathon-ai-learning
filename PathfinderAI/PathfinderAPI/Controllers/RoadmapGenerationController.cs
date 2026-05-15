using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Npgsql;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/learning-plans")]
public class RoadmapGenerationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdaptiveLearningService _adaptiveLearningService;

    public RoadmapGenerationController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        AdaptiveLearningService adaptiveLearningService)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _adaptiveLearningService = adaptiveLearningService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveLearningPlan([FromQuery] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest(new { message = "Invalid userId" });
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        await EnsureLearningPlansTable(db);

        await using var command = new NpgsqlCommand(
            """
            SELECT plan_json
            FROM learning_plans
            WHERE user_id = @userId
            ORDER BY updated_at DESC
            LIMIT 1;
            """,
            db
        );

        command.Parameters.AddWithValue("userId", userGuid);

        var result = await command.ExecuteScalarAsync();

        if (result is null)
        {
            return NotFound(new { message = "No active learning plan" });
        }

        return Content(result.ToString()!, "application/json");
    }

    [HttpPost("generate-roadmap")]
    public async Task<IActionResult> GenerateRoadmap([FromBody] GenerateRoadmapRequest request)
    {
        var agentsBaseUrl = _configuration["Agents:BaseUrl"];
        var onboardingSnapshot = new OnboardingSnapshotResponse
        {
            Track = request.Track,
            Experience = request.Experience,
            Language = request.Language,
            Goal = request.Goal,
            WeeklyHours = request.WeeklyHours <= 0 ? 6 : request.WeeklyHours,
            FocusAreas = request.FocusAreas,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var agentPayload = new
        {
            agent_name = "roadmap-planner",
            message = $"""
            Create a structured learning roadmap for a developer.

            Selected onboarding data:
            Track: {request.Track}
            Experience: {request.Experience}
            Language: {request.Language}
            Goal: {request.Goal}
            Weekly study time: {request.WeeklyHours} hours
            Focus areas: {string.Join(", ", request.FocusAreas)}

            Return JSON only.

            Roadmap structure:
            - 4 to 6 major themes.
            - Each theme must contain practical learning modules, not exact courses.
            - Each learning module must include a resourceQuery for finding supporting Microsoft Learn materials.
            - Major theme is a UI roadmap block.
            - Learning modules are shown inside the module detail page.
            - Microsoft Learn resources will support modules; they should not define the pathway.
            """,
            context = new
            {
                track_id = request.Track,
                preferred_locale = request.Language == "en" ? "en-US" : request.Language,
                experience_level = MapExperience(request.Experience),
                goal = request.Goal,
                weekly_study_hours = request.WeeklyHours <= 0 ? 6 : request.WeeklyHours,
                focus_area = request.FocusAreas,
                current_goal = $"Build a learning roadmap for {request.Track}",
                completed_skills = Array.Empty<string>(),
                skills_to_improve = Array.Empty<string>(),
                completed_modules = 0,
                in_progress_modules = 0,
                remaining_modules = 5,
                recent_questions = Array.Empty<string>()
            }
        };

        var client = _httpClientFactory.CreateClient();

        AiRoadmapResponse? aiRoadmap;
        if (string.IsNullOrWhiteSpace(agentsBaseUrl))
        {
            aiRoadmap = BuildFallbackRoadmap(request);
        }
        else
        {
            var agentResponse = await client.PostAsJsonAsync(
                $"{agentsBaseUrl.TrimEnd('/')}/api/agents/invoke",
                agentPayload
            );

            var body = await agentResponse.Content.ReadAsStringAsync();

            if (!agentResponse.IsSuccessStatusCode)
            {
                aiRoadmap = BuildFallbackRoadmap(request);
            }
            else
            {
            var agent = JsonSerializer.Deserialize<AgentInvokeResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var cleanJson = CleanJson(agent?.OutputText ?? "");

            try
            {
                aiRoadmap = JsonSerializer.Deserialize<AiRoadmapResponse>(
                    cleanJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            }
            catch (JsonException)
            {
                aiRoadmap = BuildFallbackRoadmap(request);
            }

            if (aiRoadmap?.Themes.Count is null or 0)
            {
                aiRoadmap = BuildFallbackRoadmap(request);
            }
            }
        }

        var initialSkillMatrix = _adaptiveLearningService.InitializeSkillMatrix(onboardingSnapshot);

        var plan = new LearningPlanResponse
        {
            Id = "plan-" + Guid.NewGuid(),
            UserId = request.UserId,
            Track = request.Track,
            Experience = request.Experience,
            Language = request.Language,
            GeneratedAt = DateTimeOffset.UtcNow,
            OnboardingSnapshot = onboardingSnapshot,
            SkillMatrix = initialSkillMatrix,
            AdaptiveState = new AdaptivePathwayResponse(),
            Modules = aiRoadmap?.Themes.Select((theme, index) => new ModuleResponse
            {
                Id = $"mod-{index + 1}",
                Title = theme.Title,
                Description = theme.Description,
                EstimatedHours = theme.EstimatedHours,
                Status = "not-started",
                Topics = theme.Topics,
                Resources = new List<ResourceResponse>(),
                SubModules = theme.Lessons.Select((lesson, lessonIndex) => new SubModuleResponse
                {
                    Id = $"mod-{index + 1}-lesson-{lessonIndex + 1}",
                    Title = lesson.Title,
                    Description = lesson.Description,
                    EstimatedHours = lesson.EstimatedHours,
                    Topics = lesson.Topics,
                    ProjectTask = lesson.ProjectTask,
                    ResourceQuery = lesson.ResourceQuery,
                    Status = "not-started",
                    Skills = AdaptiveLearningService.InferSkills(request.Track, lesson.Topics, lesson.Title),
                    Resources = new List<ResourceResponse>()
                }).ToList()
            }).ToList() ?? new List<ModuleResponse>()
        };

        foreach (var module in plan.Modules)
        {
            if (module.SubModules.Count > 0)
            {
                module.EstimatedHours = module.SubModules.Sum(x => x.EstimatedHours);
                module.Skills = module.SubModules
                    .SelectMany(x => x.Skills)
                    .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.Weight).First())
                    .ToList();
            }
            else
            {
                module.Skills = AdaptiveLearningService.InferSkills(request.Track, module.Topics, module.Title);
            }
        }

        for (var i = 0; i < plan.Modules.Count; i++)
        {
            var themeResources = new List<ResourceResponse>();

            for (var j = 0; j < plan.Modules[i].SubModules.Count; j++)
            {
                var sub = plan.Modules[i].SubModules[j];

                var resourceQueryModule = new ModuleResponse
                {
                    Id = sub.Id,
                    Title = sub.Title,
                    Description = sub.Description,
                    EstimatedHours = sub.EstimatedHours,
                    Status = "not-started",
                    Topics = sub.Topics,
                    ResourceQuery = sub.ResourceQuery,
                    Resources = new List<ResourceResponse>()
                };

                sub.Resources = string.IsNullOrWhiteSpace(agentsBaseUrl)
                    ? CreateFallbackResources(resourceQueryModule, i * 10 + j)
                    : await LoadResourcesForModule(
                        client,
                        agentsBaseUrl,
                        request.Track,
                        resourceQueryModule,
                        i * 10 + j
                    );

                themeResources.AddRange(sub.Resources);
            }

            plan.Modules[i].Resources = themeResources
                .Where(r =>
                    !string.IsNullOrWhiteSpace(r.Url)
                    && r.Url != "#"
                    && (r.Url.StartsWith("http://") || r.Url.StartsWith("https://"))
                )
                .GroupBy(r => r.Url.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .Take(8)
                .ToList();
        }

        if (Guid.TryParse(request.UserId, out var userGuid))
        {
            await using var db = new NpgsqlConnection(GetConnectionString());
            await db.OpenAsync();

            await EnsureLearningPlansTable(db);
            await EnsureUserLearningProfilesTable(db);

            var planJson = JsonSerializer.Serialize(plan);

            await using var saveCommand = new NpgsqlCommand(
                """
                INSERT INTO learning_plans (id, user_id, track, experience, language, plan_json, created_at, updated_at)
                VALUES (@id, @userId, @track, @experience, @language, CAST(@planJson AS JSONB), NOW(), NOW())
                ON CONFLICT (id)
                DO UPDATE SET
                plan_json = EXCLUDED.plan_json,
                updated_at = NOW();
                """,
                db
            );

            saveCommand.Parameters.AddWithValue("id", plan.Id);
            saveCommand.Parameters.AddWithValue("userId", userGuid);
            saveCommand.Parameters.AddWithValue("track", request.Track);
            saveCommand.Parameters.AddWithValue("experience", request.Experience);
            saveCommand.Parameters.AddWithValue("language", request.Language);
            saveCommand.Parameters.AddWithValue("planJson", planJson);

            await saveCommand.ExecuteNonQueryAsync();

            var skillMatrixJson = JsonSerializer.Serialize(plan.SkillMatrix);
            var onboardingSnapshotJson = JsonSerializer.Serialize(plan.OnboardingSnapshot);

            await using var saveProfileCommand = new NpgsqlCommand(
                """
                INSERT INTO user_learning_profiles (user_id, onboarding_snapshot_json, skill_matrix_json, created_at, updated_at)
                VALUES (@userId, CAST(@onboardingSnapshotJson AS JSONB), CAST(@skillMatrixJson AS JSONB), NOW(), NOW())
                ON CONFLICT (user_id)
                DO UPDATE SET
                onboarding_snapshot_json = EXCLUDED.onboarding_snapshot_json,
                skill_matrix_json = EXCLUDED.skill_matrix_json,
                updated_at = NOW();
                """,
                db
            );

            saveProfileCommand.Parameters.AddWithValue("userId", userGuid);
            saveProfileCommand.Parameters.AddWithValue("onboardingSnapshotJson", onboardingSnapshotJson);
            saveProfileCommand.Parameters.AddWithValue("skillMatrixJson", skillMatrixJson);
            await saveProfileCommand.ExecuteNonQueryAsync();

            await using var updateUserCommand = new NpgsqlCommand(
                """
                UPDATE users
                SET has_completed_onboarding = true
                WHERE id = @userId;
                """,
                db
            );

            updateUserCommand.Parameters.AddWithValue("userId", userGuid);
            await updateUserCommand.ExecuteNonQueryAsync();
        }

        return Ok(plan);
    }

    private static string MapExperience(string experience)
    {
        return experience switch
        {
            "beginner" => "Beginner",
            "intermediate" => "Junior",
            "advanced" => "Middle",
            _ => "Beginner"
        };
    }

    private static AiRoadmapResponse BuildFallbackRoadmap(GenerateRoadmapRequest request)
    {
        var trackName = string.IsNullOrWhiteSpace(request.Track)
            ? "software development"
            : request.Track;

        return new AiRoadmapResponse
        {
            Title = $"{trackName} modular learning pathway",
            Summary = "Local fallback pathway generated when the AI planner is unavailable. Microsoft Learn resources are still attached through MCP.",
            Themes = new List<AiRoadmapTheme>
            {
                new AiRoadmapTheme
                {
                    Title = "Core foundations",
                    Description = $"Build the baseline concepts and tooling needed for {trackName}.",
                    EstimatedHours = 10,
                    Topics = new List<string> { trackName, "Fundamentals", "Developer Tools" },
                    Lessons = new List<AiRoadmapLesson>
                    {
                        new AiRoadmapLesson
                        {
                            Title = "Set up the development workflow",
                            Description = "Prepare local tooling, project structure, and a repeatable development loop.",
                            EstimatedHours = 4,
                            Topics = new List<string> { "CLI", "Git", "Project Setup" },
                            ProjectTask = "Create a starter project and document the commands needed to build and run it.",
                            ResourceQuery = $"{trackName} developer setup tools git command line"
                        },
                        new AiRoadmapLesson
                        {
                            Title = "Practice the core programming model",
                            Description = "Work through the essential syntax, data flow, and debugging patterns for the selected track.",
                            EstimatedHours = 6,
                            Topics = new List<string> { "Programming", "Debugging", "Control Flow" },
                            ProjectTask = "Build a small feature and debug one intentionally introduced defect.",
                            ResourceQuery = $"{trackName} fundamentals debugging tutorial"
                        }
                    }
                },
                new AiRoadmapTheme
                {
                    Title = "Build practical application features",
                    Description = "Turn foundations into user-facing or API-facing modules.",
                    EstimatedHours = 14,
                    Topics = new List<string> { trackName, "Application Architecture", "APIs" },
                    Lessons = new List<AiRoadmapLesson>
                    {
                        new AiRoadmapLesson
                        {
                            Title = "Design module boundaries",
                            Description = "Break a feature into clear inputs, outputs, and responsibilities.",
                            EstimatedHours = 4,
                            Topics = new List<string> { "Architecture", "Modules", "DTOs" },
                            ProjectTask = "Define contracts for a task-management feature before implementing it.",
                            ResourceQuery = $"{trackName} application architecture modules DTOs"
                        },
                        new AiRoadmapLesson
                        {
                            Title = "Implement data-driven behavior",
                            Description = "Add persistence or state management around a realistic feature workflow.",
                            EstimatedHours = 5,
                            Topics = new List<string> { "Data", "State", "Persistence" },
                            ProjectTask = "Store and update tasks, then handle empty and error states.",
                            ResourceQuery = $"{trackName} data persistence state management"
                        },
                        new AiRoadmapLesson
                        {
                            Title = "Expose and consume API operations",
                            Description = "Connect feature behavior through HTTP endpoints or API calls.",
                            EstimatedHours = 5,
                            Topics = new List<string> { "HTTP", "REST", "API Integration" },
                            ProjectTask = "Create or consume CRUD operations for the task-management feature.",
                            ResourceQuery = $"{trackName} REST API CRUD HTTP tutorial"
                        }
                    }
                },
                new AiRoadmapTheme
                {
                    Title = "Quality and production readiness",
                    Description = "Make the project testable, observable, and ready to deploy.",
                    EstimatedHours = 12,
                    Topics = new List<string> { "Testing", "Deployment", "Observability" },
                    Lessons = new List<AiRoadmapLesson>
                    {
                        new AiRoadmapLesson
                        {
                            Title = "Add focused automated tests",
                            Description = "Cover critical behavior with tests that validate real user or API workflows.",
                            EstimatedHours = 4,
                            Topics = new List<string> { "Testing", "Integration Tests", "Quality" },
                            ProjectTask = "Write tests for the happy path and one failure path in your feature.",
                            ResourceQuery = $"{trackName} automated testing integration tests"
                        },
                        new AiRoadmapLesson
                        {
                            Title = "Prepare for deployment",
                            Description = "Configure build, environment settings, and deployment checks.",
                            EstimatedHours = 4,
                            Topics = new List<string> { "Deployment", "Configuration", "CI/CD" },
                            ProjectTask = "Create a deployment checklist and run a production build locally.",
                            ResourceQuery = $"{trackName} deployment CI CD production build"
                        },
                        new AiRoadmapLesson
                        {
                            Title = "Add basic monitoring signals",
                            Description = "Capture the health and errors needed to operate the application after release.",
                            EstimatedHours = 4,
                            Topics = new List<string> { "Monitoring", "Logging", "Health Checks" },
                            ProjectTask = "Add a health check and log one meaningful failure path.",
                            ResourceQuery = $"{trackName} logging monitoring health checks"
                        }
                    }
                }
            }
        };
    }

    private static List<ModuleResponse> BuildModulesFromAgentOutput(string? outputText, string track)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            return new List<ModuleResponse>
            {
                CreateModule(1, $"{track} Foundations", "Start with the core concepts and basic workflow.", 8, new[] { "Basics", "Tools", "Best Practices" }),
                CreateModule(2, $"{track} Practice Project", "Build a practical project using the selected career track.", 12, new[] { "Project", "Implementation", "Debugging" }),
                CreateModule(3, $"{track} Production Skills", "Learn how to prepare, test, and improve your work.", 10, new[] { "Testing", "Quality", "Deployment" })
            };
        }

        var chunks = outputText
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(5)
            .ToList();

        if (chunks.Count < 3)
        {
            chunks = new List<string>
            {
                outputText,
                $"Practice core {track} skills with a guided project.",
                $"Review, test, and polish your {track} portfolio work."
            };
        }

        return chunks.Select((line, index) =>
            CreateModule(
                index + 1,
                ExtractTitle(line, track, index + 1),
                line.Trim(),
                8 + index * 3,
                ExtractTopics(line, track)
            )
        ).ToList();
    }

    private static ModuleResponse CreateModule(
        int index,
        string title,
        string description,
        int estimatedHours,
        IEnumerable<string> topics)
    {
        return new ModuleResponse
        {
            Id = $"mod-{index}",
            Title = title,
            Description = description,
            EstimatedHours = estimatedHours,
            Status = "not-started",
            Topics = topics.ToList(),
            Resources = new List<ResourceResponse>
            {
                new ResourceResponse
                {
                    Id = $"res-{index}-1",
                    Title = "AI-generated study notes",
                    Type = "article",
                    Url = "#"
                }
            }
        };
    }

    private static string ExtractTitle(string line, string track, int index)
    {
        var cleaned = line
            .Replace("-", "")
            .Replace("*", "")
            .Replace("#", "")
            .Trim();

        if (cleaned.Length > 55)
            cleaned = cleaned[..55] + "...";

        return string.IsNullOrWhiteSpace(cleaned)
            ? $"{track} Module {index}"
            : cleaned;
    }

    private static IEnumerable<string> ExtractTopics(string line, string track)
    {
        var words = line
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim(',', '.', ':', ';', '-', '*'))
            .Where(w => w.Length > 4)
            .Take(4)
            .ToList();

        return words.Count > 0 ? words : new[] { track, "Practice" };
    }

    private static string CleanJson(string text)
    {
        return text
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }

    private async Task<List<ResourceResponse>> LoadResourcesForModule(
        HttpClient client,
        string agentsBaseUrl,
        string track,
        ModuleResponse module,
        int moduleIndex)
    {
        var query = string.Join(" ", new[]
        {
            module.ResourceQuery,
            string.IsNullOrWhiteSpace(module.ResourceQuery) ? track : "",
            module.Title,
            module.Description,
            string.Join(" ", module.Topics.Take(5))
        }).Trim();

        var toolPayload = new
        {
            query,
            limit = 3
        };

        var response = await client.PostAsJsonAsync(
            $"{agentsBaseUrl.TrimEnd('/')}/api/tools/search_microsoft_docs",
            toolPayload
        );

        if (!response.IsSuccessStatusCode)
        {
            return CreateFallbackResources(module, moduleIndex);
        }

        var body = await response.Content.ReadAsStringAsync();

        var searchResult = JsonSerializer.Deserialize<ToolSearchResponse>(
            body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        var resources = searchResult?.Results
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Url)
                && item.Url != "#"
                && (item.Url.StartsWith("http://") || item.Url.StartsWith("https://"))
            )
            .GroupBy(item => item.Url.Trim().ToLowerInvariant())
            .Select(group => group.First())
            .Take(3)
            .Select((item, index) => new ResourceResponse
            {
                Id = $"res-{moduleIndex + 1}-{index + 1}",
                Title = item.Title,
                Type = MapResourceType(item.ContentType),
                Url = item.Url
            })
            .ToList();

        return resources is { Count: > 0 }
            ? resources
            : CreateFallbackResources(module, moduleIndex);
    }

    private static string MapResourceType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "video" => "video",
            "course" => "course",
            "project" => "project",
            "documentation" => "article",
            _ => "article"
        };
    }

    private static List<ResourceResponse> CreateFallbackResources(ModuleResponse module, int moduleIndex)
    {
        var query = Uri.EscapeDataString(
            $"{module.Title} {string.Join(" ", module.Topics)}"
        );

        return new List<ResourceResponse>
        {
            new ResourceResponse
            {
                Id = $"res-{moduleIndex + 1}-1",
                Title = $"{module.Title} learning search",
                Type = "article",
                Url = $"https://learn.microsoft.com/search/?terms={query}"
            }
        };
    }

    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing");
        }

        return connectionString;
    }

    private static async Task EnsureLearningPlansTable(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
        CREATE TABLE IF NOT EXISTS learning_plans (
            id TEXT PRIMARY KEY,
            user_id UUID NOT NULL,
            track TEXT NOT NULL,
            experience TEXT NOT NULL,
            language TEXT NOT NULL,
            plan_json JSONB NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NOT NULL DEFAULT NOW()
        );
        """,
            connection
        );

        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureUserLearningProfilesTable(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS user_learning_profiles (
                user_id UUID PRIMARY KEY,
                onboarding_snapshot_json JSONB NOT NULL,
                skill_matrix_json JSONB NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            );
            """,
            connection
        );

        await command.ExecuteNonQueryAsync();
    }




}

public class AiRoadmapTheme
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedHours { get; set; }
    public List<string> Topics { get; set; } = new();
    [JsonPropertyName("lessons")]
    public List<AiRoadmapLesson> Lessons { get; set; } = new();
}

public class AiRoadmapLesson
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedHours { get; set; }
    public List<string> Topics { get; set; } = new();
    public string ProjectTask { get; set; } = "";
    public string ResourceQuery { get; set; } = "";
}

public class AiRoadmapResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("themes")]
    public List<AiRoadmapTheme> Themes { get; set; } = new();
}

public class GenerateRoadmapRequest
{
    public string? UserId { get; set; }
    public string Track { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Language { get; set; } = "en";
    public string Goal { get; set; } = "get-job-ready";
    public int WeeklyHours { get; set; } = 6;
    public List<string> FocusAreas { get; set; } = new();
}

public class AgentInvokeResponse
{
    [JsonPropertyName("agent_name")]
    public string Agent_Name { get; set; } = "";
    [JsonPropertyName("output_text")]
    public string OutputText { get; set; } = "";
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
}

public class LearningPlanResponse
{
    public string Id { get; set; } = "";
    public string? UserId { get; set; }
    public string Track { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Language { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }
    public OnboardingSnapshotResponse OnboardingSnapshot { get; set; } = new();
    public SkillMatrixResponse SkillMatrix { get; set; } = new();
    public AdaptivePathwayResponse AdaptiveState { get; set; } = new();
    public List<ModuleResponse> Modules { get; set; } = new();
}

public class ModuleResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedHours { get; set; }
    public string Status { get; set; } = "not-started";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LastOpenedAt { get; set; }
    public List<string> Topics { get; set; } = new();
    public List<SkillReferenceResponse> Skills { get; set; } = new();
    public List<ResourceResponse> Resources { get; set; } = new();
    public List<SubModuleResponse> SubModules { get; set; } = new();
    [JsonIgnore]
    public string ResourceQuery { get; set; } = "";
}

public class SubModuleResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedHours { get; set; }
    public string Status { get; set; } = "not-started";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LastOpenedAt { get; set; }
    public List<string> Topics { get; set; } = new();
    public List<SkillReferenceResponse> Skills { get; set; } = new();
    public string ProjectTask { get; set; } = "";
    [JsonIgnore]
    public string ResourceQuery { get; set; } = "";
    public List<ResourceResponse> Resources { get; set; } = new();
}

public class ResourceResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "article";
    public string Url { get; set; } = "#";
}

public class ToolSearchResponse
{
    [JsonPropertyName("results")]
    public List<ToolSearchResult> Results { get; set; } = new();
}

public class ToolSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "#";

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "article";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";
}
