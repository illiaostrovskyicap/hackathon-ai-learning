using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PathfinderAPI.Controllers;

[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DashboardController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("DefaultConnection is missing");
    }

    [HttpGet("api/dashboard/{userId}")]
    public async Task<IActionResult> GetDashboard(string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return BadRequest(new { message = "Invalid userId" });
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        var modules = new List<DashboardModule>();
        var learningPlanId = "";

        // 1. Берём последний learning plan
        await using (var planCommand = new NpgsqlCommand(
            """
        SELECT id, plan_json::text
        FROM learning_plans
        WHERE user_id = @userId
        ORDER BY created_at DESC
        LIMIT 1;
        """,
            db
        ))
        {
            planCommand.Parameters.AddWithValue("userId", parsedUserId);

            await using var reader = await planCommand.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                learningPlanId = reader.GetString(0);
                var json = reader.GetString(1);

                var plan = JsonSerializer.Deserialize<DashboardLearningPlan>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                modules = plan?.Modules ?? new List<DashboardModule>();
            }
        }

        // 2. Берём реальные assessment-ы из БД
        var assessmentByModule = new Dictionary<string, DashboardModuleAssessment>();

        await using (var assessmentsCommand = new NpgsqlCommand(
            """
        SELECT module_id, module_title, score, passed, completed_at
        FROM module_assessments
        WHERE user_id = @userId
          AND (@learningPlanId = '' OR learning_plan_id = @learningPlanId);
        """,
            db
        ))
        {
            assessmentsCommand.Parameters.AddWithValue("userId", parsedUserId);
            assessmentsCommand.Parameters.AddWithValue("learningPlanId", learningPlanId);

            await using var reader = await assessmentsCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                assessmentByModule[reader.GetString(0)] = new DashboardModuleAssessment
                {
                    ModuleId = reader.GetString(0),
                    ModuleTitle = reader.GetString(1),
                    Score = reader.GetInt32(2),
                    Passed = reader.GetBoolean(3),
                    CompletedAt = reader.GetDateTime(4)
                };
            }
        }

        // 3. Overlay assessment-ов на modules из plan_json
        foreach (var module in modules)
        {
            if (assessmentByModule.TryGetValue(module.Id, out var assessment))
            {
                module.Status = assessment.Passed ? "completed" : module.Status;
                module.Assessment = new DashboardAssessment
                {
                    Score = assessment.Score
                };
            }
        }

        var completedModules = modules
            .Where(m => string.Equals(m.Status, "completed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalHours = completedModules.Sum(m => m.EstimatedHours);
        var modulesCompleted = completedModules.Count;
        var modulesTotal = modules.Count;

        var technologiesMastered = completedModules
            .SelectMany(m => m.Topics ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .Take(12)
            .ToList();

        // 4. Assessment pass rate — строго из module_assessments
        var totalAssessments = assessmentByModule.Count;
        var passedAssessments = assessmentByModule.Values.Count(a => a.Passed);

        var assessmentPassRate = totalAssessments > 0
            ? (int?)Convert.ToInt32(Math.Round(passedAssessments * 100.0 / totalAssessments))
            : null;

        // 5. Weekly activity из module_assessments + interview_sessions
        var weeklyBuckets = new Dictionary<string, WeeklyActivityBucket>
        {
            ["Week 1"] = new(),
            ["Week 2"] = new(),
            ["Week 3"] = new(),
            ["Week 4"] = new()
        };

        var now = DateTime.UtcNow;

        string GetWeekKey(DateTime date)
        {
            var daysAgo = (now.Date - date.Date).Days;

            return daysAgo switch
            {
                <= 6 => "Week 4",
                <= 13 => "Week 3",
                <= 20 => "Week 2",
                _ => "Week 1"
            };
        }

        foreach (var assessment in assessmentByModule.Values)
        {
            if (assessment.Passed && assessment.CompletedAt >= now.AddDays(-28))
            {
                weeklyBuckets[GetWeekKey(assessment.CompletedAt)].AssessmentsPassed++;
                weeklyBuckets[GetWeekKey(assessment.CompletedAt)].Scores.Add(assessment.Score);
            }
        }

        var interviewScoreTrend = new List<object>();

        await using (var interviewCommand = new NpgsqlCommand(
            """
        SELECT score, completed_at
        FROM interview_sessions
        WHERE user_id = @userId
          AND status = 'completed'
          AND score IS NOT NULL
        ORDER BY completed_at ASC;
        """,
            db
        ))
        {
            interviewCommand.Parameters.AddWithValue("userId", parsedUserId);

            await using var reader = await interviewCommand.ExecuteReaderAsync();

            var index = 1;

            while (await reader.ReadAsync())
            {
                var score = reader.GetInt32(0);
                var completedAt = reader.IsDBNull(1)
                    ? now
                    : reader.GetDateTime(1);

                interviewScoreTrend.Add(new
                {
                    date = reader.IsDBNull(1)
                        ? $"Interview {index}"
                        : completedAt.ToString("MM/dd"),
                    score
                });

                if (completedAt >= now.AddDays(-28))
                {
                    var weekKey = GetWeekKey(completedAt);
                    weeklyBuckets[weekKey].InterviewsCompleted++;
                    weeklyBuckets[weekKey].Scores.Add(score);
                }

                index++;
            }
        }

        var weeklyActivity = weeklyBuckets.Select(x => new
        {
            week = x.Key,
            assessmentsPassed = x.Value.AssessmentsPassed,
            interviewsCompleted = x.Value.InterviewsCompleted,
            averageScore = x.Value.Scores.Count > 0
                ? (int?)Convert.ToInt32(Math.Round(x.Value.Scores.Average()))
                : null
        }).ToList();

        // 6. Planned vs Actual — actual берём из пройденных assessment-ов
        var plannedVsActual = modules
            .Take(6)
            .Select(m =>
            {
                var completed = assessmentByModule.ContainsKey(m.Id);
                return new
                {
                    module = m.Title.Length > 18 ? m.Title[..18] + "..." : m.Title,
                    planned = m.EstimatedHours,
                    actual = completed ? m.EstimatedHours : 0
                };
            })
            .ToList();

        return Ok(new
        {
            totalHours,
            modulesCompleted,
            modulesTotal,
            technologiesMastered,
            assessmentPassRate,
            weeklyActivity,
            interviewScoreTrend,
            plannedVsActual
        });
    }
}

public class DashboardModuleAssessment
{
    public string ModuleId { get; set; } = "";
    public string ModuleTitle { get; set; } = "";
    public int Score { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class WeeklyActivityBucket
{
    public int AssessmentsPassed { get; set; }
    public int InterviewsCompleted { get; set; }
    public List<int> Scores { get; set; } = new();
}

public class DashboardLearningPlan
{
    public List<DashboardModule> Modules { get; set; } = new();
}

public class DashboardModule
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedHours { get; set; }
    public string Status { get; set; } = "not-started";
    public List<string> Topics { get; set; } = new();
    public DashboardAssessment? Assessment { get; set; }
}

public class DashboardAssessment
{
    public double? Score { get; set; }
}