using EmployeePortalWeb.Data;
using EmployeePortalWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EmployeePortalWeb.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Employee/Index
        public async Task<IActionResult> Index()
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            if (role == "Admin")
            {
                // Admin can see all employees
                var employees = await _context.Employees.Include(e => e.User).ToListAsync();
                return View(employees);
            }
            else
            {
                var employee = await _context.Employees
                    .Where(e => e.UserId == user.Id)
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return RedirectToAction(nameof(Create));
                }

                return View(new[] { employee });
            }
        }

        // GET: Employee/Create (Admin can add employees)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Employee employee, IFormFile pdfFile)
        {
            if (true)
            {
                if (pdfFile != null && pdfFile.Length > 0)
                {
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    var filePath = Path.Combine(uploadFolder, pdfFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(stream);
                    }

                    employee.PDFDocumentPath = Path.Combine("uploads", pdfFile.FileName);
                }

                employee.UserId = _userManager.GetUserId(User).ToString();
                _context.Add(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee Added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }

        // EmployeeController.cs

        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Employees should only see their own details
            if (User.IsInRole("Employee") && user.Id != id)
            {
                return Forbid(); 
            }

            var employee = await _context.Employees.Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Email == user.UserName);

            if (employee == null)
            {
                ViewBag.ErrorMessage = "Employee not found.";
                return View();
            }
            return View(employee);
        }


        // GET: Employee/Edit/{id}
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Employee") && user.Id != id.ToString()) 
            {
                return Forbid();
            }

            var existingEmployee = await _context.Employees.FindAsync(id);

            if (existingEmployee == null)
            {
                return NotFound(); 
            }

            return View(existingEmployee);
        }

        // POST: Employee/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile pdfFile) 
        {
            if (id != employee.Id)  
            {
                return BadRequest(); 
            }

            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Employee") && user.Id != id.ToString()) 
            {
                return Forbid();
            }

            if (true)
            {
                var existingEmployee = await _context.Employees.FindAsync(id);

                if (existingEmployee == null)
                {
                    return NotFound();
                }

                if (pdfFile != null && pdfFile.Length > 0)
                {
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder); 
                    }

                    var filePath = Path.Combine(uploadFolder, pdfFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(stream);
                    }

                    existingEmployee.PDFDocumentPath = Path.Combine("uploads", pdfFile.FileName);
                }

                // Update employee details
                existingEmployee.FirstName = employee.FirstName;
                existingEmployee.LastName = employee.LastName;

                // Update the employee record in the database
                _context.Update(existingEmployee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee Updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }


        [Authorize(Roles = "Employee")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string email, string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction("Details", "Employee", new { id = userId });
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return RedirectToAction("Details", "Employee", new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Details", "Employee", new { id = userId });
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Details", "Employee", new { id = userId });
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Details", "Employee", new { id = userId });
            }
        }


        // Action to show confirmation page before deletion
        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
        
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            // Redirect to the employee list after deletion
            TempData["SuccessMessage"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
