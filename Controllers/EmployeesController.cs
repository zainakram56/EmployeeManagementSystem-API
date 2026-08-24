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
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToListAsync();

            if (User.IsInRole("HR"))
            {
                return Ok(employees);
            }

            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Ok(new List<Employee>());
            }

            if (User.IsInRole("Manager"))
            {
                var visible = employees
                    .Where(e => e.Id == currentEmployeeId || e.ManagerId == currentEmployeeId)
                    .ToList();
                return Ok(visible);
            }

            // Normal Employee — sirf apna record
            var self = employees.Where(e => e.Id == currentEmployeeId).ToList();
            return Ok(self);
        }

        // GET: api/employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            if (User.IsInRole("HR"))
            {
                return Ok(employee);
            }

            var currentEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(currentEmployeeIdClaim, out int currentEmployeeId))
            {
                return Forbid();
            }

            if (User.IsInRole("Manager"))
            {
                if (employee.Id == currentEmployeeId || employee.ManagerId == currentEmployeeId)
                {
                    return Ok(employee);
                }
                return Forbid();
            }

            // Normal Employee — sirf apna record
            if (employee.Id == currentEmployeeId)
            {
                return Ok(employee);
            }

            return Forbid();
        }

        // POST: api/employees
        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // PUT: api/employees/5
        [HttpPut("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest();
            }

            _context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Employees.AnyAsync(e => e.Id == id);
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

        // DELETE: api/employees/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}