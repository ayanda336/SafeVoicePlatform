using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Data;
using SafeVoicePlatform.Models;

namespace SafeVoicePlatform.Controllers
{
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SurveyController> _logger;

        public SurveyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SurveyController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Survey/Index - Shows available surveys (Only Individual users can see and take surveys)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();

            // Only show surveys to logged-in users who are Individual
            if (currentUser == null || !userRoles.Contains("Individual"))
            {
                TempData["ErrorMessage"] = "Only registered Individual users can access wellbeing surveys.";
                return RedirectToAction("Index", "Home");
            }

            var now = DateTime.UtcNow;
            var surveys = await _context.Surveys
                .Where(s => s.IsActive && (!s.StartDate.HasValue || s.StartDate <= now) &&
                           (!s.EndDate.HasValue || s.EndDate >= now))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Check which surveys the user has already completed
            var completedSurveyIds = await _context.SurveyResponses
                .Where(r => r.UserId == currentUser.Id && !r.IsAnonymous)
                .Select(r => r.SurveyId)
                .Distinct()
                .ToListAsync();

            ViewBag.CompletedSurveys = completedSurveyIds;
            ViewBag.CurrentUser = currentUser;

            return View(surveys);
        }

        // GET: Survey/Take/{id} - Only Individual users
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Take(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Only Individual users can take surveys
            if (!userRoles.Contains("Individual"))
            {
                TempData["ErrorMessage"] = "Only Individual users can participate in wellbeing surveys.";
                return RedirectToAction("Index", "Home");
            }

            var survey = await _context.Surveys
                .Include(s => s.Questions!.OrderBy(q => q.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            // Check if user already completed this survey
            var alreadyCompleted = await _context.SurveyResponses
                .AnyAsync(r => r.SurveyId == id && r.UserId == currentUser.Id && !r.IsAnonymous);

            if (alreadyCompleted)
            {
                TempData["ErrorMessage"] = "You have already completed this survey.";
                return RedirectToAction(nameof(Index));
            }

            // Check if survey is active
            var now = DateTime.UtcNow;
            if (!survey.IsActive || (survey.StartDate.HasValue && survey.StartDate > now) ||
                (survey.EndDate.HasValue && survey.EndDate < now))
            {
                TempData["ErrorMessage"] = "This survey is not currently available.";
                return RedirectToAction(nameof(Index));
            }

            return View(survey);
        }

        // POST: Survey/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Submit(int surveyId, Dictionary<int, string> answers, bool isAnonymous)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Only Individual users can submit surveys
            if (!userRoles.Contains("Individual"))
            {
                TempData["ErrorMessage"] = "Only Individual users can submit surveys.";
                return RedirectToAction("Index", "Home");
            }

            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                return NotFound();
            }

            string? userId = isAnonymous ? null : currentUser?.Id;

            // Check if user already responded (for non-anonymous)
            if (!isAnonymous && userId != null)
            {
                var existingResponse = await _context.SurveyResponses
                    .FirstOrDefaultAsync(r => r.SurveyId == surveyId && r.UserId == userId);

                if (existingResponse != null)
                {
                    TempData["ErrorMessage"] = "You have already completed this survey.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Save responses
            foreach (var question in survey.Questions!)
            {
                if (answers.TryGetValue(question.Id, out var answer) && !string.IsNullOrEmpty(answer))
                {
                    var response = new SurveyResponse
                    {
                        SurveyId = surveyId,
                        QuestionId = question.Id,
                        Answer = answer,
                        UserId = userId,
                        IsAnonymous = isAnonymous,
                        RespondedAt = DateTime.UtcNow
                    };
                    _context.SurveyResponses.Add(response);
                }
                else if (question.IsRequired)
                {
                    TempData["ErrorMessage"] = "Please answer all required questions.";
                    return RedirectToAction(nameof(Take), new { id = surveyId });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Survey {SurveyId} completed by user {User} (Anonymous: {IsAnonymous})",
                surveyId, userId ?? "Anonymous", isAnonymous);

            TempData["SuccessMessage"] = "Thank you for completing the survey! Your feedback helps us improve.";
            return RedirectToAction(nameof(ThankYou));
        }

        // GET: Survey/ThankYou
        [HttpGet]
        public IActionResult ThankYou()
        {
            return View();
        }

        // GET: Survey/Results/{id} - Only Admins (ITAdmin and InstitutionAdmin)
        [Authorize(Roles = "ITAdmin,InstitutionAdmin")]
        public async Task<IActionResult> Results(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Check if user has admin access
            if (!userRoles.Contains("ITAdmin") && !userRoles.Contains("InstitutionAdmin"))
            {
                return Forbid();
            }

            var survey = await _context.Surveys
                .Include(s => s.Questions!.OrderBy(q => q.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            var results = new List<SurveyResultViewModel>();

            foreach (var question in survey.Questions!)
            {
                var responses = await _context.SurveyResponses
                    .Where(r => r.QuestionId == question.Id)
                    .ToListAsync();

                var totalResponses = responses.Count;

                var result = new SurveyResultViewModel
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    TotalResponses = totalResponses,
                    Responses = responses.GroupBy(r => r.Answer)
                        .Select(g => new AnswerCount
                        {
                            Answer = g.Key,
                            Count = g.Count(),
                            TotalResponses = totalResponses
                        })
                        .ToList()
                };
                results.Add(result);
            }

            ViewBag.Survey = survey;
            ViewBag.UserRole = userRoles.Contains("ITAdmin") ? "ITAdmin" : "InstitutionAdmin";
            return View(results);
        }

        // GET: Survey/AllResults - Admin view all surveys with results
        [Authorize(Roles = "ITAdmin,InstitutionAdmin")]
        public async Task<IActionResult> AllResults()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            if (!userRoles.Contains("ITAdmin") && !userRoles.Contains("InstitutionAdmin"))
            {
                return Forbid();
            }

            var surveys = await _context.Surveys
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var surveyStats = new List<SurveyStatsViewModel>();

            foreach (var survey in surveys)
            {
                var totalResponses = await _context.SurveyResponses
                    .Where(r => r.SurveyId == survey.Id)
                    .Select(r => r.UserId)
                    .Distinct()
                    .CountAsync();

                var anonymousResponses = await _context.SurveyResponses
                    .Where(r => r.SurveyId == survey.Id && r.IsAnonymous)
                    .Select(r => r.UserId)
                    .Distinct()
                    .CountAsync();

                surveyStats.Add(new SurveyStatsViewModel
                {
                    SurveyId = survey.Id,
                    Title = survey.Title,
                    IsActive = survey.IsActive,
                    CreatedAt = survey.CreatedAt,
                    StartDate = survey.StartDate,
                    EndDate = survey.EndDate,
                    TotalResponses = totalResponses,
                    AnonymousResponses = anonymousResponses,
                    IdentifiedResponses = totalResponses - anonymousResponses
                });
            }

            return View(surveyStats);
        }

        // GET: Survey/AdminDashboard - Admin dashboard for surveys
        [Authorize(Roles = "ITAdmin,InstitutionAdmin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            if (!userRoles.Contains("ITAdmin") && !userRoles.Contains("InstitutionAdmin"))
            {
                return Forbid();
            }

            var totalSurveys = await _context.Surveys.CountAsync();
            var activeSurveys = await _context.Surveys.CountAsync(s => s.IsActive);
            var totalResponses = await _context.SurveyResponses.Select(r => r.UserId).Distinct().CountAsync();
            var totalQuestions = await _context.SurveyQuestions.CountAsync();

            var recentSurveys = await _context.Surveys
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalSurveys = totalSurveys;
            ViewBag.ActiveSurveys = activeSurveys;
            ViewBag.TotalResponses = totalResponses;
            ViewBag.TotalQuestions = totalQuestions;
            ViewBag.RecentSurveys = recentSurveys;

            return View();
        }
    }

    public class SurveyResultViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<AnswerCount> Responses { get; set; } = new();
    }

    public class AnswerCount
    {
        public string Answer { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalResponses { get; set; }
        public double Percentage => TotalResponses > 0 ? (Count * 100.0 / TotalResponses) : 0;
    }

    public class SurveyStatsViewModel
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalResponses { get; set; }
        public int AnonymousResponses { get; set; }
        public int IdentifiedResponses { get; set; }
    }
}