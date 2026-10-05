using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Data;
using SafeVoicePlatform.Models;
using System.Security.Claims;

namespace SafeVoicePlatform.Controllers
{
    [Authorize(Roles = "SupportOrgStaff,ITAdmin")]
    public class SupportOrgController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupportOrgController> _logger;

        public SupportOrgController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SupportOrgController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: SupportOrg/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            int? supportOrgId = null;
            SupportOrganization? organization = null;

            if (userRoles.Contains("SupportOrgStaff") && currentUser.SupportOrgId.HasValue)
            {
                supportOrgId = currentUser.SupportOrgId.Value;
                organization = await _context.SupportOrganizations
                    .FirstOrDefaultAsync(o => o.Id == supportOrgId);
            }

            var query = _context.Referrals
                .Include(r => r.Report)
                .Include(r => r.SupportOrganization)
                .AsQueryable();

            if (supportOrgId.HasValue)
            {
                query = query.Where(r => r.SupportOrgId == supportOrgId.Value);
            }

            var referrals = await query.ToListAsync();

            var viewModel = new SupportOrgDashboardViewModel
            {
                Organization = organization,
                TotalReferrals = referrals.Count,
                PendingReferrals = referrals.Count(r => r.Status == "Pending"),
                InProgressReferrals = referrals.Count(r => r.Status == "In Progress"),
                CompletedReferrals = referrals.Count(r => r.Status == "Completed"),
                UrgentReferrals = referrals.Count(r => r.Report != null && r.Report.IsUrgent),
                ReferralsByType = referrals
                    .Where(r => r.Report != null)
                    .GroupBy(r => r.Report!.ReportType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                StatusDistribution = new List<StatusCountItem>
                {
                    new StatusCountItem { Status = "Pending", Count = referrals.Count(r => r.Status == "Pending") },
                    new StatusCountItem { Status = "In Progress", Count = referrals.Count(r => r.Status == "In Progress") },
                    new StatusCountItem { Status = "Completed", Count = referrals.Count(r => r.Status == "Completed") }
                },
                RecentReferrals = referrals
                    .OrderByDescending(r => r.ReferredAt)
                    .Take(10)
                    .ToList()
            };

            // Calculate monthly trend
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .OrderBy(d => d)
                .ToList();

            foreach (var month in last6Months)
            {
                var monthName = month.ToString("MMM yyyy");
                var count = referrals.Count(r => r.ReferredAt.Year == month.Year &&
                                                r.ReferredAt.Month == month.Month);
                viewModel.MonthlyTrend[monthName] = count;
            }

            return View(viewModel);
        }

        // GET: SupportOrg/Referrals
        [HttpGet]
        public async Task<IActionResult> Referrals(string? status, string? type, DateTime? fromDate, DateTime? toDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            int? supportOrgId = null;

            if (userRoles.Contains("SupportOrgStaff") && currentUser.SupportOrgId.HasValue)
            {
                supportOrgId = currentUser.SupportOrgId.Value;
            }

            var query = _context.Referrals
                .Include(r => r.Report)
                .Include(r => r.SupportOrganization)
                .AsQueryable();

            if (supportOrgId.HasValue)
            {
                query = query.Where(r => r.SupportOrgId == supportOrgId.Value);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(type) && type != "All Types")
            {
                query = query.Where(r => r.Report != null && r.Report.ReportType == type);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReferredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.ReferredAt <= toDate.Value);
            }

            var referrals = await query
                .OrderByDescending(r => r.ReferredAt)
                .ToListAsync();

            // Load filter options
            ViewBag.Statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" }
            };

            ViewBag.ReportTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Types" },
                new SelectListItem { Value = "Abuse", Text = "Abuse" },
                new SelectListItem { Value = "Harassment", Text = "Harassment" },
                new SelectListItem { Value = "Unsafe Conditions", Text = "Unsafe Conditions" },
                new SelectListItem { Value = "Discrimination", Text = "Discrimination" },
                new SelectListItem { Value = "Sexual Assault", Text = "Sexual Assault" },
                new SelectListItem { Value = "Threats", Text = "Threats" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            return View(referrals);
        }

        // GET: SupportOrg/ReferralDetails/5
        [HttpGet]
        public async Task<IActionResult> ReferralDetails(int id)
        {
            var referral = await _context.Referrals
                .Include(r => r.Report)
                .ThenInclude(r => r != null ? r.User : null)
                .Include(r => r.Report != null ? r.Report.StatusHistory : null)
                .Include(r => r.Report != null ? r.Report.Attachments : null)
                .Include(r => r.SupportOrganization)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (referral == null)
            {
                return NotFound();
            }

            // Check authorization
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            bool hasPermission = false;

            if (userRoles.Contains("ITAdmin"))
            {
                hasPermission = true;
            }
            else if (userRoles.Contains("SupportOrgStaff") && currentUser.SupportOrgId.HasValue)
            {
                hasPermission = referral.SupportOrgId == currentUser.SupportOrgId.Value;
            }

            if (!hasPermission)
            {
                return Forbid();
            }

            return View(referral);
        }

        // POST: SupportOrg/UpdateReferralStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReferralStatus(int id, string status, string? notes, bool contactMade, DateTime? contactDate, string? outcome)
        {
            var referral = await _context.Referrals.FindAsync(id);

            if (referral == null)
            {
                return NotFound();
            }

            // Check authorization
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            bool hasPermission = false;

            if (userRoles.Contains("ITAdmin"))
            {
                hasPermission = true;
            }
            else if (userRoles.Contains("SupportOrgStaff") && currentUser.SupportOrgId.HasValue)
            {
                hasPermission = referral.SupportOrgId == currentUser.SupportOrgId.Value;
            }

            if (!hasPermission)
            {
                return Forbid();
            }

            // Update referral
            referral.Status = status;
            referral.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(notes))
            {
                referral.Notes = notes;
            }

            referral.ContactMade = contactMade;

            if (contactDate.HasValue)
            {
                referral.ContactDate = contactDate.Value;
            }

            if (!string.IsNullOrEmpty(outcome))
            {
                referral.Outcome = outcome;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Referral #{ReferralId} status updated to {NewStatus} by {User}",
                id, status, User.Identity?.Name);

            TempData["SuccessMessage"] = $"Referral status updated to {status}";

            return RedirectToAction(nameof(ReferralDetails), new { id = referral.Id });
        }

        // GET: SupportOrg/OrganizationProfile
        [HttpGet]
        public async Task<IActionResult> OrganizationProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            if (!userRoles.Contains("SupportOrgStaff") || !currentUser.SupportOrgId.HasValue)
            {
                return Forbid();
            }

            var organization = await _context.SupportOrganizations
                .FirstOrDefaultAsync(o => o.Id == currentUser.SupportOrgId.Value);

            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // POST: SupportOrg/UpdateOrganizationProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrganizationProfile(SupportOrganization model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            if (!userRoles.Contains("SupportOrgStaff") || !currentUser.SupportOrgId.HasValue)
            {
                return Forbid();
            }

            var organization = await _context.SupportOrganizations
                .FirstOrDefaultAsync(o => o.Id == currentUser.SupportOrgId.Value);

            if (organization == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                organization.Name = model.Name;
                organization.Description = model.Description;
                organization.PhoneNumber = model.PhoneNumber;
                organization.Email = model.Email;
                organization.Website = model.Website;
                organization.Address = model.Address;
                organization.City = model.City;
                organization.Is247Available = model.Is247Available;
                organization.IsFreeService = model.IsFreeService;
                organization.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Organization profile updated successfully!";
                return RedirectToAction(nameof(OrganizationProfile));
            }

            return View("OrganizationProfile", organization);
        }

    }
}