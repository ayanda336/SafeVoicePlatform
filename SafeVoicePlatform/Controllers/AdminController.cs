using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Data;
using SafeVoicePlatform.Models;

namespace SafeVoicePlatform.Controllers
{
    [Authorize(Roles = "ITAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalReports = await _context.Reports.CountAsync();
            var totalInstitutions = await _context.Institutions.CountAsync();
            var totalSupportOrgs = await _context.SupportOrganizations.CountAsync();
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending");
            var urgentReports = await _context.Reports.CountAsync(r => r.IsUrgent);
            var anonymousReports = await _context.Reports.CountAsync(r => r.IsAnonymous);
            var referralsCount = await _context.Referrals.CountAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalReports = totalReports,
                TotalInstitutions = totalInstitutions,
                TotalSupportOrganizations = totalSupportOrgs,
                PendingReports = pendingReports,
                UrgentReports = urgentReports,
                AnonymousReports = anonymousReports,
                TotalReferrals = referralsCount,
                RecentReports = await _context.Reports
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                ReportsByType = await _context.Reports
                    .GroupBy(r => r.ReportType)
                    .Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() })
                    .ToListAsync(),
                ReportsByStatus = await _context.Reports
                    .GroupBy(r => r.Status)
                    .Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            // Survey Statistics
            ViewBag.TotalSurveys = await _context.Surveys.CountAsync();
            ViewBag.ActiveSurveys = await _context.Surveys.CountAsync(s => s.IsActive);
            ViewBag.TotalSurveyResponses = await _context.SurveyResponses
                .Select(r => r.UserId)
                .Distinct()
                .CountAsync();

            // Recent Surveys
            var recentSurveys = await _context.Surveys
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentSurveys = recentSurveys;

            // Survey Response Counts
            var surveyResponseCounts = new Dictionary<int, int>();
            foreach (var survey in recentSurveys)
            {
                var count = await _context.SurveyResponses
                    .Where(r => r.SurveyId == survey.Id)
                    .Select(r => r.UserId)
                    .Distinct()
                    .CountAsync();
                surveyResponseCounts[survey.Id] = count;
            }
            ViewBag.SurveyResponseCounts = surveyResponseCounts;

            return View(viewModel);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users(string? search, string? role, bool? isActive)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email!.Contains(search) ||
                                         (u.FullName != null && u.FullName.Contains(search)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query.ToListAsync();
            var userList = new List<UserAdminViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserAdminViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Department = user.Department,
                    Roles = userRoles.ToList(),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    InstitutionId = user.InstitutionId,
                    SupportOrgId = user.SupportOrgId,
                    InstitutionName = user.InstitutionId.HasValue ?
                        (await _context.Institutions.FindAsync(user.InstitutionId.Value))?.Name : null,
                    SupportOrgName = user.SupportOrgId.HasValue ?
                        (await _context.SupportOrganizations.FindAsync(user.SupportOrgId.Value))?.Name : null
                });
            }

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            ViewBag.CurrentActive = isActive;

            return View(userList);
        }

        // GET: Admin/EditUser/{id}
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                Department = user.Department,
                IsActive = user.IsActive,
                SelectedRoles = userRoles.ToList(),
                AllRoles = allRoles.Select(r => new SelectListItem { Value = r, Text = r }).ToList(),
                InstitutionId = user.InstitutionId,
                SupportOrgId = user.SupportOrgId
            };

            ViewBag.Institutions = await _context.Institutions
                .Where(i => i.IsActive)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();

            ViewBag.SupportOrgs = await _context.SupportOrganizations
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            return View(model);
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = model.FullName;
                user.Department = model.Department;
                user.IsActive = model.IsActive;
                user.InstitutionId = model.InstitutionId;
                user.SupportOrgId = model.SupportOrgId;

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();

                    if (rolesToAdd.Any())
                    {
                        await _userManager.AddToRolesAsync(user, rolesToAdd);
                    }
                    if (rolesToRemove.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    }

                    _logger.LogInformation("User {UserEmail} updated by admin", user.Email);
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewBag.Institutions = await _context.Institutions
                .Where(i => i.IsActive)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();

            ViewBag.SupportOrgs = await _context.SupportOrganizations
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            return View(model);
        }

        // POST: Admin/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserEmail} status toggled to {Status} by admin",
                    user.Email, user.IsActive ? "Active" : "Inactive");
                TempData["SuccessMessage"] = $"User {user.Email} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Institutions
        public async Task<IActionResult> Institutions()
        {
            var institutions = await _context.Institutions
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(institutions);
        }

        // GET: Admin/CreateInstitution
        public IActionResult CreateInstitution()
        {
            return View(new Institution());
        }

        // POST: Admin/CreateInstitution
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstitution(Institution institution)
        {
            if (ModelState.IsValid)
            {
                institution.CreatedAt = DateTime.UtcNow;
                institution.IsActive = true;
                _context.Institutions.Add(institution);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Institution {InstitutionName} created by admin", institution.Name);
                TempData["SuccessMessage"] = "Institution created successfully!";
                return RedirectToAction(nameof(Institutions));
            }

            return View(institution);
        }

        // GET: Admin/EditInstitution/{id}
        public async Task<IActionResult> EditInstitution(int id)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound();
            }

            return View(institution);
        }

        // POST: Admin/EditInstitution
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInstitution(int id, Institution institution)
        {
            if (id != institution.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Institutions.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = institution.Name;
                existing.Type = institution.Type;
                existing.Address = institution.Address;
                existing.City = institution.City;
                existing.PostalCode = institution.PostalCode;
                existing.Country = institution.Country;
                existing.PhoneNumber = institution.PhoneNumber;
                existing.Email = institution.Email;
                existing.Website = institution.Website;
                existing.IsActive = institution.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Institution {InstitutionName} updated by admin", existing.Name);
                TempData["SuccessMessage"] = "Institution updated successfully!";
                return RedirectToAction(nameof(Institutions));
            }

            return View(institution);
        }

        // GET: Admin/SupportOrganizations
        public async Task<IActionResult> SupportOrganizations()
        {
            var organizations = await _context.SupportOrganizations
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(organizations);
        }

        // GET: Admin/CreateSupportOrganization
        public IActionResult CreateSupportOrganization()
        {
            return View(new SupportOrganization());
        }

        // POST: Admin/CreateSupportOrganization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupportOrganization(SupportOrganization organization)
        {
            if (ModelState.IsValid)
            {
                organization.CreatedAt = DateTime.UtcNow;
                organization.IsActive = true;
                _context.SupportOrganizations.Add(organization);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Support Organization {OrgName} created by admin", organization.Name);
                TempData["SuccessMessage"] = "Support Organization created successfully!";
                return RedirectToAction(nameof(SupportOrganizations));
            }

            return View(organization);
        }

        // GET: Admin/EditSupportOrganization/{id}
        public async Task<IActionResult> EditSupportOrganization(int id)
        {
            var organization = await _context.SupportOrganizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // POST: Admin/EditSupportOrganization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupportOrganization(int id, SupportOrganization organization)
        {
            if (id != organization.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.SupportOrganizations.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = organization.Name;
                existing.OrganizationType = organization.OrganizationType;
                existing.Description = organization.Description;
                existing.Address = organization.Address;
                existing.City = organization.City;
                existing.PostalCode = organization.PostalCode;
                existing.Country = organization.Country;
                existing.PhoneNumber = organization.PhoneNumber;
                existing.Email = organization.Email;
                existing.Website = organization.Website;
                existing.Is247Available = organization.Is247Available;
                existing.IsFreeService = organization.IsFreeService;
                existing.IsActive = organization.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Support Organization {OrgName} updated by admin", existing.Name);
                TempData["SuccessMessage"] = "Support Organization updated successfully!";
                return RedirectToAction(nameof(SupportOrganizations));
            }

            return View(organization);
        }
    }
}