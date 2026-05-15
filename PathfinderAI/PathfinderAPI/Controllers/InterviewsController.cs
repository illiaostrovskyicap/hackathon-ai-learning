using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/interviews")]
public class InterviewsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public InterviewsController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] InterviewMessageRequest request)
    {
        var agentsBaseUrl = _configuration["Agents:BaseUrl"];

        if (string.IsNullOrWhiteSpace(agentsBaseUrl))
        {
            return StatusCode(500, new { message = "Agents:BaseUrl is missing" });
        }

        var responseCount = request.History.Count(x => x.Role == "user");

        //var weeklyStudyHours = await GetWeeklyStudyHours(request.UserId);

        var agentPayload = new
        {
            agent_name = "progress-coach",

            message = $$$"""
            You are acting as an AI interview coach.

            Interview type: {request.InterviewType}
            Candidate track: {request.Track}
            Candidate experience: {request.Experience}
            Candidate latest answer:
            {request.Message}

            Candidate has answered {responseCount} times.

            Return ONLY valid JSON:
            {{
        
                "reply": "next interview question or final summary",
                "feedback": "short feedback on the candidate's latest answer",
                "score": 0,
                "isComplete": false,
                "strengths": ["string"],
                "improvements": ["string"]
            }}

            Rules:
            - Ask exactly ONE next interview question if response_count is less than 5.
            - If response_count is 5 or more, finish the interview.
            - When finishing, set isComplete to true.
            - When finishing, return final score from 1 to 100.
            - When not finished, set isComplete to false.
            - Always return meaningful feedback.
            - Never return markdown.
            - Never return text outside JSON.
            """,

            context = new
            {
                track_id = request.Track,
                preferred_locale = "en-US",
                experience_level = MapExperience(request.Experience),

                weekly_study_hours = 6,

                interview_type = request.InterviewType,
                history = request.History,
                response_count = responseCount,

                current_goal = "Practice interview skills",
                completed_skills = Array.Empty<string>(),
                skills_to_improve = Array.Empty<string>(),
                completed_modules = 0,
                in_progress_modules = 0,
                remaining_modules = 0,
                recent_questions = Array.Empty<string>()
            }
        };

        var client = _httpClientFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"{agentsBaseUrl.TrimEnd('/')}/api/agents/invoke",
            agentPayload
        );

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new
            {
                message = "Agent service failed",
                details = body
            });
        }

        var agent = JsonSerializer.Deserialize<AgentInvokeResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        var cleanJson = CleanJson(agent?.OutputText ?? "");

        var result = JsonSerializer.Deserialize<InterviewAgentResult>(
            cleanJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result == null)
        {
            return StatusCode(500, new { message = "Invalid agent response", raw = cleanJson });
        }

        if (responseCount >= 5)
        {
            result.IsComplete = true;

            if (result.Score <= 0)
            {
                result.Score = 75;
            }

            if (result.Strengths.Count == 0)
            {
                result.Strengths = new List<string>
        {
            "Communicates answers clearly",
            "Shows basic understanding of the discussed topics"
        };
            }

            if (result.Improvements.Count == 0)
            {
                result.Improvements = new List<string>
        {
            "Add more concrete examples",
            "Explain trade-offs and reasoning in more detail"
        };
            }

            if (string.IsNullOrWhiteSpace(result.Reply))
            {
                result.Reply = "Interview complete. Here is your final feedback.";
            }
        }

        var finalMessages = request.History
            .Append(new InterviewChatMessage
            {
                Role = "assistant",
                Content = result.Reply
            })
            .ToList();

        var questionScores = new List<QuestionScoreResponse>();

        if (!string.IsNullOrWhiteSpace(request.Message) &&
            !string.IsNullOrWhiteSpace(result.Feedback) &&
            result.Score > 0)
        {
            questionScores.Add(new QuestionScoreResponse
            {
                Question = request.Message,
                Score = result.Score,
                Feedback = result.Feedback
            });
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        await using var saveCommand = new NpgsqlCommand(
            """
    INSERT INTO interview_sessions (
        id,
        user_id,
        interview_type,
        track,
        experience,
        status,
        messages_json,
        score,
        strengths_json,
        improvements_json,
        question_scores_json,
        created_at,
        updated_at,
        completed_at
    )
    VALUES (
        @id,
        @userId,
        @interviewType,
        @track,
        @experience,
        @status,
        CAST(@messagesJson AS JSONB),
        @score,
        CAST(@strengthsJson AS JSONB),
        CAST(@improvementsJson AS JSONB),
        CAST(@questionScoresJson AS JSONB),
        NOW(),
        NOW(),
        @completedAt
    )
    ON CONFLICT (id)
    DO UPDATE SET
        interview_type = EXCLUDED.interview_type,
        track = EXCLUDED.track,
        experience = EXCLUDED.experience,
        status = EXCLUDED.status,
        messages_json = EXCLUDED.messages_json,
        score = EXCLUDED.score,
        strengths_json = EXCLUDED.strengths_json,
        improvements_json = EXCLUDED.improvements_json,
        question_scores_json =
            interview_sessions.question_scores_json || EXCLUDED.question_scores_json,
        updated_at = NOW(),
        completed_at = EXCLUDED.completed_at;
    """,
            db
        );

        saveCommand.Parameters.AddWithValue("id", request.SessionId);
        saveCommand.Parameters.AddWithValue("userId", Guid.Parse(request.UserId!));
        saveCommand.Parameters.AddWithValue("interviewType", request.InterviewType);
        saveCommand.Parameters.AddWithValue("track", request.Track);
        saveCommand.Parameters.AddWithValue("experience", request.Experience);
        saveCommand.Parameters.AddWithValue("status", result.IsComplete ? "completed" : "in-progress");
        saveCommand.Parameters.AddWithValue("messagesJson", JsonSerializer.Serialize(finalMessages));
        saveCommand.Parameters.AddWithValue("score", result.IsComplete ? result.Score : DBNull.Value);
        saveCommand.Parameters.AddWithValue("strengthsJson", JsonSerializer.Serialize(result.Strengths ?? new()));
        saveCommand.Parameters.AddWithValue("improvementsJson", JsonSerializer.Serialize(result.Improvements ?? new()));
        saveCommand.Parameters.AddWithValue("questionScoresJson", JsonSerializer.Serialize(questionScores));
        saveCommand.Parameters.AddWithValue("completedAt", result.IsComplete ? DateTime.UtcNow : DBNull.Value);

        await saveCommand.ExecuteNonQueryAsync();

        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserInterviews(string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return BadRequest(new { message = "Invalid userId" });
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
        SELECT
            id,
            user_id,
            interview_type,
            track,
            experience,
            status,
            messages_json::text,
            score,
            strengths_json::text,
            improvements_json::text,
            question_scores_json::text
        FROM interview_sessions
        WHERE user_id = @userId
        ORDER BY updated_at DESC;
        """,
            db
        );

        command.Parameters.AddWithValue("userId", parsedUserId);

        var sessions = new List<InterviewSessionResponse>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var score = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7);

            sessions.Add(new InterviewSessionResponse
            {
                Id = reader.GetString(0),
                UserId = reader.GetGuid(1).ToString(),
                Type = reader.GetString(2),
                Track = reader.GetString(3),
                Experience = reader.GetString(4),
                Status = reader.GetString(5),
                Messages = JsonSerializer.Deserialize<List<InterviewChatMessage>>(reader.GetString(6)) ?? new(),
                Report = score.HasValue
                    ? new InterviewReportResponse
                    {
                        OverallScore = score.Value,
                        Strengths = JsonSerializer.Deserialize<List<string>>(reader.GetString(8)) ?? new(),
                        Improvements = JsonSerializer.Deserialize<List<string>>(reader.GetString(9)) ?? new(),
                        QuestionScores = JsonSerializer.Deserialize<List<QuestionScoreResponse>>(reader.GetString(10)) ?? new()
                    }
                    : null
            });
        }

        return Ok(sessions);
    }

    [HttpGet("user/{userId}/stats")]
    public async Task<IActionResult> GetUserInterviewStats(string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return BadRequest(new { message = "Invalid userId" });
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
        SELECT
            COUNT(*)::int AS total_sessions,
            COUNT(*) FILTER (WHERE status = 'completed')::int AS completed_sessions,
            AVG(score) FILTER (WHERE status = 'completed' AND score IS NOT NULL) AS average_score
        FROM interview_sessions
        WHERE user_id = @userId;
        """,
            db
        );

        command.Parameters.AddWithValue("userId", parsedUserId);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Ok(new
            {
                totalSessions = 0,
                completed = 0,
                averageScore = (int?)null
            });
        }

        return Ok(new
        {
            totalSessions = reader.GetInt32(0),
            completed = reader.GetInt32(1),
            averageScore = reader.IsDBNull(2)
                ? null
                : (int?)Convert.ToInt32(Math.Round(reader.GetDecimal(2)))
        });
    }

    private async Task<int> GetWeeklyStudyHours(string? userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return 6;
        }

        await using var db = new NpgsqlConnection(GetConnectionString());
        await db.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
        SELECT COALESCE(weekly_hours, 6)
        FROM learning_plans
        WHERE user_id = @userId
        ORDER BY generated_at DESC
        LIMIT 1;
        """,
            db
        );

        command.Parameters.AddWithValue("userId", parsedUserId);

        var result = await command.ExecuteScalarAsync();

        return result is int hours ? hours : 6;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("DefaultConnection is missing");
    }

    private static string MapExperience(string experience)
    {
        return experience?.ToLowerInvariant() switch
        {
            "beginner" => "Beginner",
            "junior" => "Junior",
            "intermediate" => "Junior",
            "middle" => "Middle",
            "advanced" => "Middle",
            "senior" => "Senior",
            _ => "Beginner"
        };
    }

    private static string CleanJson(string text)
    {
        return text
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }
}

public class InterviewMessageRequest
{
    public string? UserId { get; set; }
    public string SessionId { get; set; } = "";
    public string InterviewType { get; set; } = "technical";
    public string Track { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Message { get; set; } = "";
    public List<InterviewChatMessage> History { get; set; } = new();
}

public class InterviewChatMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class InterviewAgentResult
{
    public string Reply { get; set; } = "";
    public string Feedback { get; set; } = "";
    public int Score { get; set; }
    public bool IsComplete { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
}

public class InterviewSessionResponse
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Track { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Status { get; set; } = "in-progress";
    public List<InterviewChatMessage> Messages { get; set; } = new();
    public InterviewReportResponse? Report { get; set; }
}

public class InterviewReportResponse
{
    public int OverallScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public List<QuestionScoreResponse> QuestionScores { get; set; } = new();
}

public class QuestionScoreResponse
{
    public string Question { get; set; } = "";
    public int Score { get; set; }
    public string Feedback { get; set; } = "";
}