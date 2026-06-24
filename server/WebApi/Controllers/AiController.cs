using Application.Common.DTOs.Ai;
using Application.Features.Ai;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiIntegrationService _aiIntegrationService;

    public AiController(IAiIntegrationService aiIntegrationService)
    {
        _aiIntegrationService = aiIntegrationService;
    }

    [HttpPost("tasks/{taskId:int}/predict-risk")]
    public async Task<ActionResult<RiskPredictionResultDto>> PredictTaskRisk(
        int taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiIntegrationService.PredictTaskRiskAsync(taskId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("tasks/{taskId:int}/match-staff/{userId:int}")]
    public async Task<ActionResult<StaffMatchResultDto>> MatchStaff(
        int taskId,
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiIntegrationService.MatchStaffAsync(taskId, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("tasks/{taskId:int}/suggest-assignees")]
    public async Task<ActionResult<IReadOnlyList<StaffMatchResultDto>>> SuggestAssignees(
        int taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiIntegrationService.SuggestAssigneesAsync(taskId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("bottlenecks/analyze")]
    public async Task<ActionResult<IReadOnlyList<BottleneckResultDto>>> AnalyzeBottlenecks(
        [FromQuery] int topN = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aiIntegrationService.AnalyzeBottlenecksAsync(topN, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private ObjectResult HandleException(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException => NotFound(new { message = ex.Message }),
            ArgumentException => BadRequest(new { message = ex.Message }),
            InvalidOperationException => BadRequest(new { message = ex.Message }),
            HttpRequestException => StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Không thể gọi AI server.",
                detail = ex.Message
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Lỗi hệ thống khi xử lý AI.",
                detail = ex.Message
            })
        };
    }
}