using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Data;
using SafeVoicePlatform.Models;
using System.Security.Cryptography;
using System.Text;
using SafeVoicePlatform.Services;

namespace SafeVoicePlatform.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ReportsController> _logger;
        private readonly QRCodeService _qrCodeService;


        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ReportsController> logger,
            QRCodeService qrCodeService) // Add this)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _qrCodeService = qrCodeService; // Add this
        }

        // GET: Reports/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Load report types for dropdown
            ViewBag.ReportTypes = GetReportTypes();

            // Get current user and their roles
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Load institutions for dropdown if user is Individual
            if (userRoles.Contains("Individual"))
            {
                var institutions = await _context.Institutions
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.Name)
                    .Select(i => new SelectListItem
                    {
                        Value = i.Id.ToString(),
                        Text = i.Name
                    })
                    .ToListAsync();

                ViewBag.Institutions = institutions;
                ViewBag.ShowInstitutionSelector = true;
            }
            else
            {
                ViewBag.ShowInstitutionSelector = false;
            }

            return View(new Report());
        }

        // POST: Reports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Report report, List<IFormFile>? attachments)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Challenge();
                    }

                    var userRoles = await _userManager.GetRolesAsync(currentUser);

                    // Set report properties
                    report.CreatedAt = DateTime.UtcNow;
                    report.Status = "Pending";
                    report.StatusUpdatedAt = DateTime.UtcNow;

                    // Handle anonymous reporting
                    if (!report.IsAnonymous && currentUser != null)
                    {
                        report.UserId = currentUser.Id;

                        // If user is Individual and they selected an institution from the form
                        if (userRoles.Contains("Individual") && report.InstitutionId.HasValue)
                        {
                            // InstitutionId already set from the form - keep it
                        }
                        // If user is InstitutionAdmin, use their institution
                        else if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
                        {
                            report.InstitutionId = currentUser.InstitutionId.Value;
                        }
                        // If user is Individual but didn't select an institution
                        else if (userRoles.Contains("Individual") && !report.InstitutionId.HasValue)
                        {
                            ModelState.AddModelError("InstitutionId", "Please select your institution");
                            ViewBag.ReportTypes = GetReportTypes();
                            ViewBag.ShowInstitutionSelector = true;
                            var institutions = await _context.Institutions
                                .Where(i => i.IsActive)
                                .OrderBy(i => i.Name)
                                .Select(i => new SelectListItem
                                {
                                    Value = i.Id.ToString(),
                                    Text = i.Name
                                })
                                .ToListAsync();
                            ViewBag.Institutions = institutions;
                            return View(report);
                        }
                    }
                    else
                    {
                        // Generate reference number for anonymous reports
                        report.ReferenceNumber = GenerateReferenceNumber();
                    }

                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();

                    // Add status history
                    var statusHistory = new ReportStatusHistory
                    {
                        ReportId = report.Id,
                        Status = "Pending",
                        Comment = "Report submitted",
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = User.Identity?.Name ?? "Anonymous"
                    };
                    _context.ReportStatusHistories.Add(statusHistory);

                    // Handle file uploads
                    if (attachments != null && attachments.Any())
                    {
                        await UploadAttachments(report.Id, attachments);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Report #{ReportId} created by user {User} for institution {InstitutionId}",
                        report.Id, User.Identity?.Name, report.InstitutionId);

                    // Show success message with reference number if anonymous
                    if (report.IsAnonymous)
                    {
                        TempData["SuccessMessage"] = $"Your anonymous report has been submitted. Your reference number is: {report.ReferenceNumber}. Please save this number to track your report.";
                        TempData["ReferenceNumber"] = report.ReferenceNumber;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Your report has been submitted successfully. You can track its status in My Reports.";
                    }

                    return RedirectToAction(nameof(Success), new { id = report.Id, referenceNumber = report.ReferenceNumber });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating report");
                    ModelState.AddModelError("", "An error occurred while submitting your report. Please try again.");
                }
            }

            // Reload report types if model state is invalid
            ViewBag.ReportTypes = GetReportTypes();

            // Reload institutions if needed
            var currentUserReload = await _userManager.GetUserAsync(User);
            if (currentUserReload != null)
            {
                var userRolesReload = await _userManager.GetRolesAsync(currentUserReload);

                if (userRolesReload.Contains("Individual"))
                {
                    var institutions = await _context.Institutions
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.Name)
                        .Select(i => new SelectListItem
                        {
                            Value = i.Id.ToString(),
                            Text = i.Name
                        })
                        .ToListAsync();

                    ViewBag.Institutions = institutions;
                    ViewBag.ShowInstitutionSelector = true;
                }
            }

            return View(report);
        }

        // GET: Reports/Success
        [HttpGet]
        public IActionResult Success(int id, string? referenceNumber)
        {
            ViewBag.ReportId = id;
            ViewBag.ReferenceNumber = referenceNumber;

            // Generate QR Code for anonymous reports
            if (!string.IsNullOrEmpty(referenceNumber))
            {
                try
                {
                    // Create the tracking URL
                    var trackingUrl = $"{Request.Scheme}://{Request.Host}/Reports/Track?referenceNumber={referenceNumber}";

                    // Generate QR code
                    var qrCodeBytes = _qrCodeService.GenerateQRCode(trackingUrl);
                    ViewBag.QRCodeImage = Convert.ToBase64String(qrCodeBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating QR code for report {ReportId}", id);
                    ViewBag.QRCodeImage = null;
                }
            }

            return View();
        }
        // GET: Reports/MyReports
        [HttpGet]
        public async Task<IActionResult> MyReports()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            List<Report> reports = new List<Report>();

            if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
            {
                // InstitutionAdmin sees reports from their institution
                reports = await _context.Reports
                    .Where(r => r.InstitutionId == currentUser.InstitutionId.Value)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            else if (userRoles.Contains("SupportOrgStaff"))
            {
                // SupportOrgStaff sees referrals (to be implemented later)
                reports = new List<Report>();
            }
            else if (currentUser != null)
            {
                // Regular users see their own reports
                reports = await _context.Reports
                    .Where(r => r.UserId == currentUser.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            return View(reports);
        }

        // GET: Reports/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id, string? referenceNumber)
        {
            Report? report = null;

            // If reference number provided (anonymous tracking)
            if (!string.IsNullOrEmpty(referenceNumber))
            {
                report = await _context.Reports
                    .Include(r => r.StatusHistory)
                    .Include(r => r.Attachments)
                    .FirstOrDefaultAsync(r => r.ReferenceNumber == referenceNumber);
            }
            else
            {
                // Regular authenticated user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);

                report = await _context.Reports
                    .Include(r => r.StatusHistory)
                    .Include(r => r.Attachments)
                    .FirstOrDefaultAsync(r => r.Id == id);

                // Check authorization
                if (report == null)
                {
                    return NotFound();
                }

                // Check if user has permission to view this report
                bool hasPermission = false;

                if (userRoles.Contains("ITAdmin"))
                {
                    hasPermission = true;
                }
                else if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
                {
                    hasPermission = report.InstitutionId == currentUser.InstitutionId.Value;
                }
                else
                {
                    hasPermission = report.UserId == currentUser.Id;
                }

                if (!hasPermission)
                {
                    return Forbid();
                }

                // Load support organizations for InstitutionAdmin/ITAdmin
                if (userRoles.Contains("InstitutionAdmin") || userRoles.Contains("ITAdmin"))
                {
                    var supportOrgs = await _context.SupportOrganizations
                        .Where(o => o.IsActive)
                        .OrderBy(o => o.Name)
                        .Select(o => new SelectListItem
                        {
                            Value = o.Id.ToString(),
                            Text = o.Name
                        })
                        .ToListAsync();

                    ViewBag.SupportOrganizations = supportOrgs;

                    // Check if already referred
                    var existingReferral = await _context.Referrals
                        .Include(r => r.SupportOrganization)
                        .FirstOrDefaultAsync(r => r.ReportId == id);

                    ViewBag.HasReferral = existingReferral != null;
                    ViewBag.ReferralStatus = existingReferral?.Status;
                    ViewBag.ReferralOrganization = existingReferral?.SupportOrganization?.Name;
                    ViewBag.ReferralNotes = existingReferral?.Notes;
                    ViewBag.ReferralId = existingReferral?.Id;
                }
            }

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
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

        // GET: Reports/Track
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Track()
        {
            return View();
        }

        // POST: Reports/Track
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(string referenceNumber)
        {
            if (string.IsNullOrEmpty(referenceNumber))
            {
                ModelState.AddModelError("", "Please enter a reference number");
                return View();
            }

            var report = await _context.Reports
                .Include(r => r.StatusHistory)
                .FirstOrDefaultAsync(r => r.ReferenceNumber == referenceNumber);

            if (report == null)
            {
                ModelState.AddModelError("", "No report found with that reference number");
                return View();
            }

            return RedirectToAction(nameof(Details), new { referenceNumber = report.ReferenceNumber });
        }

        // GET: Reports/Dashboard
        [HttpGet]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Check authorization
            if (!userRoles.Contains("InstitutionAdmin") && !userRoles.Contains("ITAdmin"))
            {
                return Forbid();
            }

            int? institutionId = null;
            Institution? institution = null;

            // Get institution ID based on user role
            if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
            {
                institutionId = currentUser.InstitutionId.Value;
                institution = await _context.Institutions
                    .FirstOrDefaultAsync(i => i.Id == institutionId);
            }

            // Get reports based on institution
            var query = _context.Reports.AsQueryable();

            if (institutionId.HasValue)
            {
                query = query.Where(r => r.InstitutionId == institutionId);
            }

            var reports = await query.ToListAsync();

            // Calculate statistics - Using InstitutionDashboardViewModel
            var viewModel = new InstitutionDashboardViewModel
            {
                Institution = institution,
                TotalReports = reports.Count,
                PendingReports = reports.Count(r => r.Status == "Pending"),
                UnderReviewReports = reports.Count(r => r.Status == "Under Review"),
                ResolvedReports = reports.Count(r => r.Status == "Resolved"),
                UrgentReports = reports.Count(r => r.IsUrgent),
                ReportsByType = reports.GroupBy(r => r.ReportType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                StatusDistribution = new List<StatusCountItem>
        {
            new StatusCountItem { Status = "Pending", Count = reports.Count(r => r.Status == "Pending") },
            new StatusCountItem { Status = "Under Review", Count = reports.Count(r => r.Status == "Under Review") },
            new StatusCountItem { Status = "Resolved", Count = reports.Count(r => r.Status == "Resolved") }
        },
                RecentReports = reports
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToList()
            };

            // Calculate monthly trend (last 6 months)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .OrderBy(d => d)
                .ToList();

            foreach (var month in last6Months)
            {
                var monthName = month.ToString("MMM yyyy");
                var count = reports.Count(r => r.CreatedAt.Year == month.Year &&
                                                r.CreatedAt.Month == month.Month);
                viewModel.MonthlyTrend[monthName] = count;
            }

            return View("Dashboard", viewModel);
        }

        // GET: Reports/InstitutionReports
        [HttpGet]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        public async Task<IActionResult> InstitutionReports(string? status, string? type, DateTime? fromDate, DateTime? toDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            int? institutionId = null;

            if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
            {
                institutionId = currentUser.InstitutionId.Value;
            }

            var query = _context.Reports
                .Include(r => r.User)
                .Include(r => r.StatusHistory)
                .AsQueryable();

            if (institutionId.HasValue)
            {
                query = query.Where(r => r.InstitutionId == institutionId);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(r => r.ReportType == type);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Load report types for filter dropdown
            ViewBag.ReportTypes = GetReportTypes();
            ViewBag.Statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Under Review", Text = "Under Review" },
                new SelectListItem { Value = "Resolved", Text = "Resolved" }
            };

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            return View(reports);
        }

        // POST: Reports/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? comment)
        {
            var report = await _context.Reports.FindAsync(id);

            if (report == null)
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
            else if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
            {
                hasPermission = report.InstitutionId == currentUser.InstitutionId.Value;
            }

            if (!hasPermission)
            {
                return Forbid();
            }

            // Update status
            var oldStatus = report.Status;
            report.Status = status;
            report.StatusUpdatedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            // Add status history
            var statusHistory = new ReportStatusHistory
            {
                ReportId = report.Id,
                Status = status,
                Comment = comment ?? $"Status changed from {oldStatus} to {status}",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = User.Identity?.Name
            };

            _context.ReportStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report status updated to {status}";

            return RedirectToAction(nameof(Details), new { id = report.Id });
        }

        // GET: Reports/Export
        [HttpGet]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        public async Task<IActionResult> Export(DateTime? fromDate, DateTime? toDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            int? institutionId = null;

            if (userRoles.Contains("InstitutionAdmin") && currentUser.InstitutionId.HasValue)
            {
                institutionId = currentUser.InstitutionId.Value;
            }

            var query = _context.Reports.AsQueryable();

            if (institutionId.HasValue)
            {
                query = query.Where(r => r.InstitutionId == institutionId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Build CSV
            var sb = new StringBuilder();
            sb.AppendLine("Report ID,Type,Title,Status,Is Urgent,Is Anonymous,Incident Date,Location,Created At,Updated At");

            foreach (var report in reports)
            {
                sb.AppendLine($"\"{report.Id}\",\"{report.ReportType}\",\"{report.Title}\",\"{report.Status}\",\"{report.IsUrgent}\",\"{report.IsAnonymous}\",\"{report.IncidentDate}\",\"{report.Location}\",\"{report.CreatedAt}\",\"{report.UpdatedAt}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new FileContentResult(bytes, "text/csv")
            {
                FileDownloadName = $"Reports_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            return result;
        }

        // Helper Methods
        private List<SelectListItem> GetReportTypes()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Abuse", Text = "Abuse (Physical, Emotional, Verbal)" },
                new SelectListItem { Value = "Harassment", Text = "Harassment (Sexual, Racial, Bullying)" },
                new SelectListItem { Value = "Unsafe Conditions", Text = "Unsafe Working/Living Conditions" },
                new SelectListItem { Value = "Discrimination", Text = "Discrimination" },
                new SelectListItem { Value = "Sexual Assault", Text = "Sexual Assault" },
                new SelectListItem { Value = "Threats", Text = "Threats or Intimidation" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
        }

        private string GenerateReferenceNumber()
        {
            // Generate a unique reference number: SAFE-YYYYMMDD-XXXXX
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = RandomNumberGenerator.GetBytes(4);
            var uniqueCode = BitConverter.ToString(random).Replace("-", "");
            return $"SAFE-{date}-{uniqueCode}";
        }

        private async Task UploadAttachments(int reportId, List<IFormFile> files)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "reports", reportId.ToString());

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var attachment = new ReportAttachment
                    {
                        ReportId = reportId,
                        FileName = file.FileName,
                        FilePath = Path.Combine("uploads", "reports", reportId.ToString(), uniqueFileName),
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        UploadedAt = DateTime.UtcNow
                    };

                    _context.ReportAttachments.Add(attachment);
                }
            }
        }

        // GET: Reports/CreateReferral (Optional - to show form)
        [HttpGet]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        public async Task<IActionResult> CreateReferral(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }

            // Check if referral already exists
            var existingReferral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.ReportId == reportId);

            if (existingReferral != null)
            {
                TempData["ErrorMessage"] = "A referral already exists for this report.";
                return RedirectToAction(nameof(Details), new { id = reportId });
            }

            // Load support organizations for dropdown
            var supportOrgs = await _context.SupportOrganizations
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();

            ViewBag.SupportOrganizations = supportOrgs;
            ViewBag.ReportId = reportId;

            return View();
        }

        // POST: Reports/CreateReferral
        [HttpPost]
        [Authorize(Roles = "InstitutionAdmin,ITAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReferral(int reportId, int supportOrgId, string? notes)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }

            // Check if referral already exists
            var existingReferral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.ReportId == reportId && r.SupportOrgId == supportOrgId);

            if (existingReferral != null)
            {
                TempData["ErrorMessage"] = "A referral to this organization already exists for this report.";
                return RedirectToAction(nameof(Details), new { id = reportId });
            }

            // Create new referral
            var referral = new Referral
            {
                ReportId = reportId,
                SupportOrgId = supportOrgId,
                Status = "Pending",
                Notes = notes,
                ReferredAt = DateTime.UtcNow,
                ReferredBy = User.Identity?.Name ?? "System"
            };

            _context.Referrals.Add(referral);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Referral created for Report #{ReportId} to Support Organization #{SupportOrgId} by {User}",
                reportId, supportOrgId, User.Identity?.Name);

            TempData["SuccessMessage"] = $"Report has been referred to the support organization successfully!";

            return RedirectToAction(nameof(Details), new { id = reportId });
        }

        // GET: Reports/DownloadQRCode
        [HttpGet]
        public IActionResult DownloadQRCode(string referenceNumber)
        {
            if (string.IsNullOrEmpty(referenceNumber))
            {
                return BadRequest("Reference number is required");
            }

            try
            {
                var trackingUrl = $"{Request.Scheme}://{Request.Host}/Reports/Track?referenceNumber={referenceNumber}";
                var qrCodeBytes = _qrCodeService.GenerateQRCode(trackingUrl);

                return File(qrCodeBytes, "image/png", $"QRCode_{referenceNumber}.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading QR code for reference {ReferenceNumber}", referenceNumber);
                return StatusCode(500, "Error generating QR code");
            }
        }

        // GET: Reports/DownloadReportCard
        [HttpGet]
        public async Task<IActionResult> DownloadReportCard(int id, string? referenceNumber)
        {
            Report? report = null;

            if (!string.IsNullOrEmpty(referenceNumber))
            {
                report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.ReferenceNumber == referenceNumber);
            }
            else
            {
                report = await _context.Reports.FindAsync(id);
            }

            if (report == null)
            {
                return NotFound();
            }

            try
            {
                // Generate tracking URL
                var trackingUrl = $"{Request.Scheme}://{Request.Host}/Reports/Track?referenceNumber={report.ReferenceNumber}";
                var qrCodeBytes = _qrCodeService.GenerateQRCode(trackingUrl);
                var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                // Generate HTML for the report card
                var htmlContent = GenerateReportCardHtml(report, qrCodeBase64);

                // Convert HTML to PDF (using a PDF library or return as HTML)
                // For now, return as HTML that can be printed/saved as PDF
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report card");
                return StatusCode(500, "Error generating report card");
            }
        }

        private string GenerateReportCardHtml(Report report, string qrCodeBase64)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Safe Voice - Report Card</title>
        <style>
            body {{
                font-family: 'Segoe UI', Arial, sans-serif;
                margin: 0;
                padding: 20px;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                min-height: 100vh;
                display: flex;
                justify-content: center;
                align-items: center;
            }}
            .card {{
                max-width: 500px;
                margin: auto;
                background: white;
                border-radius: 20px;
                box-shadow: 0 20px 60px rgba(0,0,0,0.3);
                overflow: hidden;
                animation: fadeIn 0.5s ease;
            }}
            .card-header {{
                background: linear-gradient(135deg, #0066cc 0%, #0052a3 100%);
                color: white;
                padding: 30px;
                text-align: center;
            }}
            .card-header h1 {{
                margin: 0;
                font-size: 28px;
            }}
            .card-header p {{
                margin: 10px 0 0;
                opacity: 0.9;
            }}
            .card-body {{
                padding: 30px;
            }}
            .reference-number {{
                text-align: center;
                background: #f8f9fa;
                padding: 15px;
                border-radius: 10px;
                margin-bottom: 20px;
            }}
            .reference-number h3 {{
                margin: 0 0 5px;
                color: #666;
                font-size: 14px;
            }}
            .reference-number .number {{
                font-size: 24px;
                font-weight: bold;
                color: #0066cc;
                letter-spacing: 1px;
            }}
            .qr-code {{
                text-align: center;
                margin: 20px 0;
                padding: 20px;
                background: white;
                border: 2px dashed #e0e0e0;
                border-radius: 15px;
            }}
            .qr-code img {{
                width: 180px;
                height: 180px;
            }}
            .qr-code p {{
                margin: 10px 0 0;
                color: #666;
                font-size: 12px;
            }}
            .info {{
                margin: 20px 0;
                padding: 15px;
                background: #e7f3ff;
                border-radius: 10px;
                border-left: 4px solid #0066cc;
            }}
            .info p {{
                margin: 5px 0;
                font-size: 14px;
            }}
            .info strong {{
                color: #0066cc;
            }}
            .buttons {{
                display: flex;
                gap: 10px;
                margin-top: 20px;
            }}
            .btn {{
                flex: 1;
                padding: 12px;
                border: none;
                border-radius: 10px;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                text-decoration: none;
                text-align: center;
                display: inline-block;
                transition: transform 0.2s;
            }}
            .btn-primary {{
                background: linear-gradient(135deg, #0066cc 0%, #0052a3 100%);
                color: white;
            }}
            .btn-secondary {{
                background: #6c757d;
                color: white;
            }}
            .btn:hover {{
                transform: translateY(-2px);
            }}
            .footer {{
                text-align: center;
                padding: 20px;
                background: #f8f9fa;
                font-size: 12px;
                color: #666;
            }}
            @keyframes fadeIn {{
                from {{
                    opacity: 0;
                    transform: translateY(20px);
                }}
                to {{
                    opacity: 1;
                    transform: translateY(0);
                }}
            }}
            @media print {{
                body {{
                    background: white;
                    padding: 0;
                }}
                .btn {{
                    display: none;
                }}
                .card {{
                    box-shadow: none;
                }}
            }}
        </style>
    </head>
    <body>
        <div class='card'>
            <div class='card-header'>
                <h1>Safe Voice</h1>
                <p>Anonymous Reporting Platform</p>
            </div>
            <div class='card-body'>
                <div class='reference-number'>
                    <h3>Your Reference Number</h3>
                    <div class='number'>{report.ReferenceNumber}</div>
                </div>
                
                <div class='qr-code'>
                    <img src='data:image/png;base64,{qrCodeBase64}' alt='QR Code' />
                    <p>Scan to track your report status</p>
                </div>
                
                <div class='info'>
                    <p><strong>📋 Report Information</strong></p>
                    <p><strong>Type:</strong> {report.ReportType}</p>
                    <p><strong>Title:</strong> {report.Title}</p>
                    <p><strong>Submitted:</strong> {report.CreatedAt:dddd, MMMM dd, yyyy HH:mm}</p>
                    {(report.IsUrgent ? "<p><strong>⚠️ Urgent:</strong> This report requires immediate attention</p>" : "")}
                </div>
                
                <div class='buttons'>
                    <button class='btn btn-primary' onclick='window.print()'>
                        🖨️ Print / Save as PDF
                    </button>
                    <a href='/Reports/DownloadQRCode?referenceNumber={report.ReferenceNumber}' class='btn btn-secondary'>
                        📱 Download QR Code
                    </a>
                </div>
            </div>
            <div class='footer'>
                <p>Keep this information safe. You'll need it to track your report.</p>
                <p>Visit: {Request.Scheme}://{Request.Host}</p>
            </div>
        </div>
        
        <script>
            // Auto-trigger print dialog? Uncomment if desired
            // window.onload = function() {{ window.print(); }};
        </script>
    </body>
    </html>
    ";
        }
    }
}