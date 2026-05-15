using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/learning-plans")]
public sealed class ProgressController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdaptiveLearningService _adaptiveLearningService;

    public ProgressController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        AdaptiveLearningService adaptiveLearningService)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _adaptiveLearningService = adaptiveLearningService;
    }

    [HttpPost("{learningPlanId}/progress")]
    public async Task<ActionResult<ProgressUpdateResponse>> UpdateProgress(
        string learningPlanId,
        [FromBody] ProgressUpdateRequest request)
    {
        if (!Guid.TryParse(request.UserId, out var userGuid))
        {
            return BadRequest(new { message = "Invalid userId" });
        }

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();

        await EnsureLearningPlansTable(connection);
        await EnsureUserLearningProfilesTable(connection);

        var storedPlanJson = await LoadPlanJson(connection, learningPlanId, userGuid);
        if (storedPlanJson is null)
        {
            return NotFound(new { message = "Learning plan not found" });
        }

        var plan = JsonSerializer.Deserialize<LearningPlanResponse>(
            storedPlanJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (plan is null)
        {
            return StatusCode(500, new { message = "Stored learning plan could not be read" });
        }

        var skillMatrix = await LoadSkillMatrix(connection, userGuid, plan);
        var skillGrowth = await EvaluateSkillGrowth(plan, request);
        var adaptiveBlueprint = await BuildAdaptiveBlueprint(plan, skillGrowth);

        var result = _adaptiveLearningService.ApplyProgressUpdate(
            plan,
            skillMatrix,
            request,
            skillGrowth,
            adaptiveBlueprint);

        if (result.PathwayUpdated)
        {
            await EnrichAdaptiveResources(result.LearningPlan);
        }

        var updatedPlanJson = JsonSerializer.Serialize(result.LearningPlan);
        await using var savePlanCommand = new NpgsqlCommand(
            """
            UPDATE learning_plans
            SET plan_json = CAST(@planJson AS JSONB),
                updated_at = NOW()
            WHERE id = @id AND user_id = @userId;
            """,
            connection);

        savePlanCommand.Parameters.AddWithValue("planJson", updatedPlanJson);
        savePlanCommand.Parameters.AddWithValue("id", learningPlanId);
        savePlanCommand.Parameters.AddWithValue("userId", userGuid);
        await savePlanCommand.ExecuteNonQueryAsync();

        var onboardingSnapshotJson = JsonSerializer.Serialize(result.LearningPlan.OnboardingSnapshot);
        var skillMatrixJson = JsonSerializer.Serialize(result.SkillMatrix);

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
            connection);

        saveProfileCommand.Parameters.AddWithValue("userId", userGuid);
        saveProfileCommand.Parameters.AddWithValue("onboardingSnapshotJson", onboardingSnapshotJson);
        saveProfileCommand.Parameters.AddWithValue("skillMatrixJson", skillMatrixJson);
        await saveProfileCommand.ExecuteNonQueryAsync();

        return Ok(new ProgressUpdateResponse
        {
            LearningPlan = result.LearningPlan,
            SkillMatrix = result.SkillMatrix,
            PromotedSkills = result.PromotedSkills,
            PathwayUpdated = result.PathwayUpdated
        });
    }

    private async Task<SkillGrowthEvaluation> EvaluateSkillGrowth(
        LearningPlanResponse plan,
        ProgressUpdateRequest request)
    {
        if (!request.Action.Equals("complete", StringComparison.OrdinalIgnoreCase))
        {
            return new SkillGrowthEvaluation();
        }

        var targetSkills = request.ItemType.Equals("module", StringComparison.OrdinalIgnoreCase)
            ? plan.Modules.FirstOrDefault(x => x.Id == request.ItemId)?.Skills
            : plan.Modules.SelectMany(x => x.SubModules)
                .FirstOrDefault(x => x.Id == request.ItemId)?.Skills;

        var normalizedSkills = targetSkills?.Count > 0
            ? targetSkills!
            : AdaptiveLearningService.InferSkills(
                plan.Track,
                request.ItemType.Equals("module", StringComparison.OrdinalIgnoreCase)
                    ? plan.Modules.FirstOrDefault(x => x.Id == request.ItemId)?.Topics ?? Enumerable.Empty<string>()
                    : plan.Modules.SelectMany(x => x.SubModules).FirstOrDefault(x => x.Id == request.ItemId)?.Topics ?? Enumerable.Empty<string>(),
                request.ItemId);

        var agentsBaseUrl = _configuration["Agents:BaseUrl"];
        if (string.IsNullOrWhiteSpace(agentsBaseUrl))
        {
            return BuildDeterministicGrowth(normalizedSkills, request);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                agent_name = "skill-growth-evaluator",
                message = """
                Evaluate how much a learner's skill matrix should improve.

                Return JSON only with this shape:
                {
                  "skillDeltas": [
                    { "key": "sql", "name": "SQL", "delta": 8.0 }
                  ]
                }

                Rules:
                - Only use the listed skills.
                - Keep deltas bounded.
                - "open" actions should produce zero growth.
                - Reading or completing a module can improve confidence modestly.
                - Assessment evidence can increase the delta.
                """,
                context = new
                {
                    track = plan.Track,
                    action = request.Action,
                    item_type = request.ItemType,
                    item_id = request.ItemId,
                    weekly_hours = plan.OnboardingSnapshot.WeeklyHours,
                    assessment_score = request.AssessmentScore,
                    minutes_spent = request.MinutesSpent,
                    reflection = request.Reflection,
                    weak_skills = plan.SkillMatrix.WeakSkills,
                    target_skills = normalizedSkills.Select(x => new { x.Key, x.Name, x.Weight }).ToList()
                }
            };

            var response = await client.PostAsJsonAsync($"{agentsBaseUrl.TrimEnd('/')}/api/agents/invoke", payload);
            if (!response.IsSuccessStatusCode)
            {
                return BuildDeterministicGrowth(normalizedSkills, request);
            }

            var body = await response.Content.ReadAsStringAsync();
            var agent = JsonSerializer.Deserialize<AgentInvokeResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var cleanJson = (agent?.OutputText ?? string.Empty)
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            var parsed = JsonSerializer.Deserialize<SkillGrowthEvaluation>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.SkillDeltas.Count > 0
                ? CapGrowth(parsed, normalizedSkills)
                : BuildDeterministicGrowth(normalizedSkills, request);
        }
        catch
        {
            return BuildDeterministicGrowth(normalizedSkills, request);
        }
    }

    private async Task<AdaptiveSubModuleBlueprint?> BuildAdaptiveBlueprint(
        LearningPlanResponse plan,
        SkillGrowthEvaluation skillGrowth)
    {
        if (skillGrowth.SkillDeltas.Count == 0)
        {
            return null;
        }

        var strongestDelta = skillGrowth.SkillDeltas
            .OrderByDescending(x => x.Delta)
            .First();

        if (strongestDelta.Delta <= 0)
        {
            return null;
        }

        var agentsBaseUrl = _configuration["Agents:BaseUrl"];
        if (string.IsNullOrWhiteSpace(agentsBaseUrl))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                agent_name = "adaptive-roadmap-patcher",
                message = """
                Write one advanced follow-up learning task for a developer.
                Return JSON only:
                {
                  "title": "...",
                  "description": "...",
                  "estimatedHours": 3,
                  "topics": ["..."],
                  "projectTask": "...",
                  "resourceQuery": "..."
                }
                """,
                context = new
                {
                    track = plan.Track,
                    skill_key = strongestDelta.Key,
                    skill_name = strongestDelta.Name,
                    current_level = plan.SkillMatrix.Skills.FirstOrDefault(x => x.Key == strongestDelta.Key)?.Level,
                    goal = plan.OnboardingSnapshot.Goal,
                    focus_areas = plan.OnboardingSnapshot.FocusAreas
                }
            };

            var response = await client.PostAsJsonAsync($"{agentsBaseUrl.TrimEnd('/')}/api/agents/invoke", payload);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            var agent = JsonSerializer.Deserialize<AgentInvokeResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var cleanJson = (agent?.OutputText ?? string.Empty)
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            return JsonSerializer.Deserialize<AdaptiveSubModuleBlueprint>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private async Task EnrichAdaptiveResources(LearningPlanResponse plan)
    {
        var adaptiveUpdate = plan.AdaptiveState.Updates.LastOrDefault();
        if (adaptiveUpdate is null)
        {
            return;
        }

        var module = plan.Modules.FirstOrDefault(x => x.Id == adaptiveUpdate.TargetModuleId);
        var subModule = module?.SubModules.FirstOrDefault(x => x.Id == adaptiveUpdate.AddedSubModuleId);
        var agentsBaseUrl = _configuration["Agents:BaseUrl"];

        if (module is null || subModule is null || string.IsNullOrWhiteSpace(agentsBaseUrl))
        {
            return;
        }

        var resourceQueryModule = new ModuleResponse
        {
            Id = subModule.Id,
            Title = subModule.Title,
            Description = subModule.Description,
            EstimatedHours = subModule.EstimatedHours,
            Status = subModule.Status,
            Topics = subModule.Topics,
            ResourceQuery = subModule.ResourceQuery,
            Resources = new List<ResourceResponse>()
        };

        var client = _httpClientFactory.CreateClient();
        subModule.Resources = await LoadResourcesForModule(
            client,
            agentsBaseUrl,
            plan.Track,
            resourceQueryModule,
            module.SubModules.Count + 20);
    }

    private static SkillGrowthEvaluation BuildDeterministicGrowth(
        IEnumerable<SkillReferenceResponse> skills,
        ProgressUpdateRequest request)
    {
        var multiplier = request.AssessmentScore switch
        {
            >= 90 => 1.35,
            >= 75 => 1.15,
            >= 60 => 1.0,
            _ => 0.85
        };

        var deltaBase = request.MinutesSpent >= 45 ? 8.0 : 6.0;
        return new SkillGrowthEvaluation
        {
            SkillDeltas = skills
                .Take(3)
                .Select(x => new SkillDelta
                {
                    Key = x.Key,
                    Name = x.Name,
                    Delta = Math.Round(Math.Max(3, deltaBase * Math.Max(0.45, x.Weight) * multiplier), 2)
                })
                .ToList()
        };
    }

    private static SkillGrowthEvaluation CapGrowth(
        SkillGrowthEvaluation evaluation,
        IEnumerable<SkillReferenceResponse> targetSkills)
    {
        var targetKeys = targetSkills.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        evaluation.SkillDeltas = evaluation.SkillDeltas
            .Where(x => targetKeys.Contains(x.Key))
            .Select(x =>
            {
                x.Delta = Math.Clamp(x.Delta, 0, 12);
                return x;
            })
            .ToList();
        return evaluation;
    }

    private async Task<string?> LoadPlanJson(NpgsqlConnection connection, string learningPlanId, Guid userId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT plan_json
            FROM learning_plans
            WHERE id = @id AND user_id = @userId
            LIMIT 1;
            """,
            connection);

        command.Parameters.AddWithValue("id", learningPlanId);
        command.Parameters.AddWithValue("userId", userId);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<SkillMatrixResponse> LoadSkillMatrix(
        NpgsqlConnection connection,
        Guid userId,
        LearningPlanResponse plan)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT skill_matrix_json
            FROM user_learning_profiles
            WHERE user_id = @userId
            LIMIT 1;
            """,
            connection);

        command.Parameters.AddWithValue("userId", userId);
        var result = await command.ExecuteScalarAsync();
        if (result is null)
        {
            return plan.SkillMatrix;
        }

        return JsonSerializer.Deserialize<SkillMatrixResponse>(
            result.ToString() ?? string.Empty,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? plan.SkillMatrix;
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

        var response = await client.PostAsJsonAsync(
            $"{agentsBaseUrl.TrimEnd('/')}/api/tools/search_microsoft_docs",
            new { query, limit = 3 });

        if (!response.IsSuccessStatusCode)
        {
            return CreateFallbackResources(module, moduleIndex);
        }

        var body = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<ToolSearchResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var resources = searchResult?.Results
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Url)
                && item.Url != "#"
                && (item.Url.StartsWith("http://") || item.Url.StartsWith("https://")))
            .GroupBy(item => item.Url.Trim().ToLowerInvariant())
            .Select(group => group.First())
            .Take(3)
            .Select((item, index) => new ResourceResponse
            {
                Id = $"res-{moduleIndex + 1}-{index + 1}",
                Title = item.Title,
                Type = item.ContentType.ToLowerInvariant() switch
                {
                    "video" => "video",
                    "course" => "course",
                    "project" => "project",
                    "documentation" => "article",
                    _ => "article"
                },
                Url = item.Url
            })
            .ToList();

        return resources is { Count: > 0 } ? resources : CreateFallbackResources(module, moduleIndex);
    }

    private static List<ResourceResponse> CreateFallbackResources(ModuleResponse module, int moduleIndex)
    {
        var query = Uri.EscapeDataString($"{module.Title} {string.Join(" ", module.Topics)}");
        return new List<ResourceResponse>
        {
            new()
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
            connection);

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
            connection);

        await command.ExecuteNonQueryAsync();
    }
}
