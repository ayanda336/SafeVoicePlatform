using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Data;
using System.Text.Json;

namespace SafeVoicePlatform.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(ApplicationDbContext context, ILogger<ChatbotController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var response = await ProcessMessage(request.Message);
                return Json(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return Json(new { success = false, response = "I'm having trouble connecting. Please try again or contact support." });
            }
        }

        private async Task<string> ProcessMessage(string message)
        {
            message = message.ToLower().Trim();

            // Greetings
            if (message.Contains("hello") || message.Contains("hi") || message.Contains("hey"))
            {
                return "Hello! 👋 I'm Safe Voice Assistant. How can I help you today? You can ask me about:\n\n• How to submit a report\n• Anonymous reporting\n• Support services\n• Tracking your report\n• Safety tips\n• Emergency contacts";
            }

            // Report submission help
            if (message.Contains("how to submit") || message.Contains("submit report") || message.Contains("report something"))
            {
                return "📝 **How to Submit a Report:**\n\n1. Click 'Submit Report' in the navigation menu\n2. Select the report type (Abuse, Harassment, etc.)\n3. Provide details about what happened\n4. Choose to report anonymously or with your identity\n5. Add evidence if available\n6. Click 'Submit Report'\n\nNeed urgent help? Call emergency services at 10111 (South Africa).";
            }

            // Anonymous reporting
            if (message.Contains("anonymous") || message.Contains("anonymously"))
            {
                return "🕵️ **Anonymous Reporting:**\n\nYes, you can report completely anonymously!\n\n• Check the 'Report Anonymously' box on the report form\n• Your identity will not be shared\n• You'll receive a unique reference number to track your report\n• No personal information is stored with the report\n\nYour privacy and safety are our priority.";
            }

            // Tracking report
            if (message.Contains("track") || message.Contains("reference number") || message.Contains("check report"))
            {
                return "🔍 **Track Your Report:**\n\nIf you reported anonymously:\n• Use your reference number (format: SAFE-YYYYMMDD-XXXXX)\n• Click 'Track Report' in the navigation menu\n• Enter your reference number to see status\n\nIf you reported with your account:\n• Log in to your account\n• Go to 'My Reports'\n• Click on the report to see details and status\n\nNeed your reference number? Check the confirmation email or message you received after submitting.";
            }

            // Support services
            if (message.Contains("support") || message.Contains("help") || message.Contains("counseling") || message.Contains("counselling"))
            {
                var supportOrgs = await _context.SupportOrganizations
                    .Where(s => s.IsActive)
                    .Take(5)
                    .ToListAsync();

                if (supportOrgs.Any())
                {
                    var response = "🤝 **Support Services Available:**\n\n";
                    foreach (var org in supportOrgs)
                    {
                        response += $"• **{org.Name}** - {org.OrganizationType}\n";
                        if (!string.IsNullOrEmpty(org.PhoneNumber))
                            response += $"  📞 {org.PhoneNumber}\n";
                        if (org.Is247Available)
                            response += $"  🕐 24/7 Available\n";
                        response += "\n";
                    }
                    response += "For more options, visit the Support Services page.";
                    return response;
                }
                return "🤝 **Support Services:**\n\nWe have various support organizations available. Please contact:\n\n• SADAG: 0800 567 567 (Mental Health)\n• GBV Command Centre: 0800 428 428\n• Childline: 0800 055 555\n• Lifeline: 0861 322 322\n\nFor immediate emergency, call 10111 or 112.";
            }

            // Emergency
            if (message.Contains("emergency") || message.Contains("urgent") || message.Contains("danger") || message.Contains("immediate"))
            {
                return "🚨 **EMERGENCY ASSISTANCE** 🚨\n\nIf you are in immediate danger:\n\n📞 **Emergency Numbers:**\n• Police: 10111\n• Ambulance: 10177\n• Mobile Emergency: 112\n\n🇿🇦 **South Africa Specific:**\n• GBV Command Centre: 0800 428 428\n• Childline: 0800 055 555\n• SAPS Crime Stop: 08600 10111\n\n⚠️ If you're in immediate danger, please contact emergency services right away!";
            }

            // Safety tips
            if (message.Contains("safety") || message.Contains("tips") || message.Contains("protect"))
            {
                return "🛡️ **Safety Tips:**\n\n**If you're experiencing harassment or abuse:**\n• Trust your instincts\n• Document incidents (dates, times, details)\n• Save evidence (messages, emails, photos)\n• Tell someone you trust\n• Know your emergency exits\n• Keep emergency numbers handy\n\n**Online Safety:**\n• Use strong, unique passwords\n• Be careful what you share online\n• Block and report harassers\n• Adjust privacy settings\n\n**Remember:** Your safety is the priority. Use the 'Urgent' flag when submitting time-sensitive reports.";
            }

            // Who can use the platform
            if (message.Contains("who") || message.Contains("eligible") || message.Contains("can use"))
            {
                return "👥 **Who Can Use Safe Voice:**\n\n• **Students** - Report incidents at your institution\n• **Employees** - Report workplace issues\n• **Community Members** - Report unsafe conditions\n• **Anyone** who needs to speak up safely\n\nWe serve individuals in universities, workplaces, and communities across South Africa.";
            }

            // What happens after reporting
            if (message.Contains("what happens") || message.Contains("after report") || message.Contains("next"))
            {
                return "📋 **What Happens After You Report:**\n\n1. **Immediate:** You receive a confirmation\n2. **Review:** Your report is reviewed by appropriate personnel\n3. **Action:** If urgent, immediate action is taken\n4. **Update:** Status updates as progress is made\n5. **Resolution:** You're notified when resolved\n\n**Timeline:**\n• Pending: Initial review (1-3 days)\n• Under Review: Investigation (3-10 days)\n• Resolved: Action taken\n\nYou can track your report anytime using your reference number or login.";
            }

            // Platform information
            if (message.Contains("about") || message.Contains("what is") || message.Contains("platform"))
            {
                return "ℹ️ **About Safe Voice:**\n\nSafe Voice is a secure platform for reporting abuse, harassment, and unsafe conditions. We provide:\n\n• 🔒 Secure, confidential reporting\n• 🕵️ Anonymous options\n• 🤝 Connection to support services\n• 📊 Institution monitoring\n• 💬 Resources and guidance\n\nOur mission is to give everyone a safe voice and create safer environments.";
            }

            // Contact support
            if (message.Contains("contact") || message.Contains("reach") || message.Contains("human") || message.Contains("person"))
            {
                return "📧 **Contact Support:**\n\nIf you need assistance from a human:\n\n• Email: support@safevoices.com\n• Phone: 0800 123 456 (Mon-Fri, 9am-5pm)\n\nFor urgent matters, please use the emergency numbers provided.\n\nI'm here to help 24/7! Ask me anything.";
            }

            // Default response
            return "I understand you're looking for help. Here are some things I can assist with:\n\n• **How to submit a report** - Step-by-step guide\n• **Anonymous reporting** - How it works\n• **Support services** - Where to get help\n• **Tracking reports** - Check report status\n• **Safety tips** - Stay safe\n• **Emergency contacts** - Urgent help\n\nJust ask me about any of these topics! If you need immediate help, type 'emergency'.";
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}