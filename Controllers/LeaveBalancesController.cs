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
    public class LeaveBalancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveBalancesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/leavebalances
        [HttpGet]
        [Authorize(Roles = "Manager,HR")]
        public async Task<ActionResult<IEnumerable<LeaveBalance>>> GetLeaveBalances()
        {
            var leaveBalances = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .Include(lb => lb.LeaveType)
                .ToListAsync();

            if (User.IsInRole("HR"))
            {
                return Ok(leaveBalances);
            }

            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Ok(new List<LeaveBalance>());
            }

            var teamBalances = leaveBalances
                .Where(lb => lb.Employee!.ManagerId == currentEmployeeId)
                .ToList();

            return Ok(teamBalances);
        }

        // GET: api/leavebalances/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveBalance>> GetLeaveBalance(int id)
        {
            var leaveBalance = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .Include(lb => lb.LeaveType)
                .FirstOrDefaultAsync(lb => lb.Id == id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            if (User.IsInRole("HR"))
            {
                return Ok(leaveBalance);
            }

            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Forbid();
            }

            if (User.IsInRole("Manager") && leaveBalance.Employee!.ManagerId == currentEmployeeId)
            {
                return Ok(leaveBalance);
            }

            if (leaveBalance.EmployeeId == currentEmployeeId)
            {
                return Ok(leaveBalance);
            }

            return Forbid();
        }

        // GET: api/leavebalances/employee/5?year=2026
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<LeaveBalance>>> GetLeaveBalancesByEmployee(int employeeId, [FromQuery] int? year)
        {
            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId);

            if (!User.IsInRole("HR") && employeeId != currentEmployeeId)
            {
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

            var query = _context.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.EmployeeId == employeeId);

            if (year.HasValue)
            {
                query = query.Where(lb => lb.Year == year.Value);
            }

            var leaveBalances = await query.ToListAsync();
            return Ok(leaveBalances);
        }

        // POST: api/leavebalances
        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<LeaveBalance>> CreateLeaveBalance(LeaveBalance leaveBalance)
        {
            _context.LeaveBalances.Add(leaveBalance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeaveBalance), new { id = leaveBalance.Id }, leaveBalance);
        }

        // PUT: api/leavebalances/5
        [HttpPut("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateLeaveBalance(int id, LeaveBalance leaveBalance)
        {
            if (id != leaveBalance.Id)
            {
                return BadRequest();
            }

            _context.Entry(leaveBalance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.LeaveBalances.AnyAsync(lb => lb.Id == id);
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

        // DELETE: api/leavebalances/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> DeleteLeaveBalance(int id)
        {
            var leaveBalance = await _context.LeaveBalances.FindAsync(id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            _context.LeaveBalances.Remove(leaveBalance);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}