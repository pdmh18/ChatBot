using Application.Common.DTOs.Tasks;
using Application.Features.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Common.DTOs;
namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<TaskListItemDto>>> GetList(
            [FromQuery] TaskQueryParameters query,
            CancellationToken cancellationToken)
        {
            var result = await _taskService.GetListAsync(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskDetailDto>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _taskService.GetByIdAsync(id, cancellationToken);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy công việc."
                });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTaskRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var id = await _taskService.CreateAsync(request, cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id },
                    new
                    {
                        maCongViec = id,
                        message = "Tạo công việc thành công."
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateTaskRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var success = await _taskService.UpdateAsync(id, request, cancellationToken);

                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var success = await _taskService.DeleteAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy công việc."
                });
            }

            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateTaskStatusRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var success = await _taskService.UpdateStatusAsync(id, request, cancellationToken);

                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPatch("{id:int}/assign")]
        public async Task<IActionResult> Assign(
            int id,
            [FromBody] AssignTaskRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var success = await _taskService.AssignAsync(id, request, cancellationToken);

                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
