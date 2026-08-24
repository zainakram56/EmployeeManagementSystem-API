using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInterface.Data;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/leaverequests
        [HttpGet]
        [Authorize(Roles = "Manager,HR")]
        public async Task<ActionResult<IEnumerable<LeaveRequest>>> GetLeaveRequests()
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(lr => lr.LeaveType)
                .ToListAsync();

            if (User.IsInRole("HR"))
            {
                return Ok(leaveRequests);
            }

            // Manager — sirf apni team ki requests
            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Ok(new List<LeaveRequest>());
            }

            var teamRequests = leaveRequests
                .Where(lr => lr.Employee!.ManagerId == currentEmployeeId)
                .ToList();

            return Ok(teamRequests);
        }

        // GET: api/leaverequests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequest>> GetLeaveRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(lr => lr.LeaveType)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (User.IsInRole("HR"))
            {
                return Ok(leaveRequest);
            }

            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Forbid();
            }

            if (User.IsInRole("Manager") && leaveRequest.Employee!.ManagerId == currentEmployeeId)
            {
                return Ok(leaveRequest);
            }

            if (leaveRequest.EmployeeId == currentEmployeeId)
            {
                return Ok(leaveRequest);
            }

            return Forbid();
        }

        // GET: api/leaverequests/employee/5
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<LeaveRequest>>> GetLeaveRequestsByEmployee(int employeeId)
        {
            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId);

            if (!User.IsInRole("HR") && employeeId != currentEmployeeId)
            {
                // Manager apni team ka dekh sakta hai
                if (User.IsInRole("Manager"))
                {
                    var targetEmployee = await _context.Employees.FindAsync(employeeId);
                    if (targetEmployee == null || targetEmployee.ManagerId != currentEmployeeId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.EmployeeId == employeeId)
                .OrderByDescending(lr => lr.AppliedOn)
                .ToListAsync();

            return Ok(leaveRequests);
        }

        // POST: api/leaverequests
        [HttpPost]
        [Authorize(Roles = "Employee,Manager")]
        public async Task<ActionResult<LeaveRequest>> CreateLeaveRequest(LeaveRequest leaveRequest)
        {
            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, leaveRequest);
        }

        // PUT: api/leaverequests/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,HR")]
        public async Task<IActionResult> UpdateLeaveRequest(int id, LeaveRequest leaveRequest)
        {
            if (id != leaveRequest.Id)
            {
                return BadRequest();
            }

            _context.Entry(leaveRequest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.LeaveRequests.AnyAsync(lr => lr.Id == id);
                if (!exists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/leaverequests/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> DeleteLeaveRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            _context.LeaveRequests.Remove(leaveRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}