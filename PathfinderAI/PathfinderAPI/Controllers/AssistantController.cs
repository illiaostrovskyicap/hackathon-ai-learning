using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Assistant;
using PathfinderAI.Contracts.Common;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/assistant")]
public sealed class AssistantController : ControllerBase
{
    [HttpPost("sessions")]
    public ActionResult<StartLearningAssistantSessionResponse> StartSession(
        [FromBody] StartLearningAssistantSessionRequest request)
    {
        var response = new StartLearningAssistantSessionResponse(
            SessionId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow);

        return CreatedAtAction(nameof(StartSession), response);
    }

    [HttpPost("messages")]
    public ActionResult<AskLearningAssistantResponse> AskAssistant(
        [FromBody] AskLearningAssistantRequest request)
    {
        var answer = new LearningAssistantMessageContract(
            MessageId: Guid.NewGuid(),
            Role: AssistantMessageRole.Assistant,
            Content: "This is a stub response.",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Citations: Array.Empty<SourceCitationContract>());

        var response = new AskLearningAssistantResponse(
            SessionId: request.SessionId ?? Guid.NewGuid(),
            Answer: answer,
            UsedFallbackResponse: true);

        return Ok(response);
    }
}
