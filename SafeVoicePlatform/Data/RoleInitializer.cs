using Microsoft.AspNetCore.Identity;
using SafeVoicePlatform.Models;

namespace SafeVoicePlatform.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roleNames = { "Individual", "InstitutionAdmin", "SupportOrgStaff", "ITAdmin" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed institutions if none exist
            if (!context.Institutions.Any())
            {
                context.Institutions.AddRange(
                    new Institution
                    {
                        Name = "University of Cape Town",
                        Type = "University",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Institution
                    {
                        Name = "Stellenbosch University",
                        Type = "University",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Institution
                    {
                        Name = "University of Pretoria",
                        Type = "University",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Institution
                    {
                        Name = "University of the Witwatersrand",
                        Type = "University",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Institution
                    {
                        Name = "Durban University of Technology",
                        Type = "University",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            // Seed support organizations if none exist
            if (!context.SupportOrganizations.Any())
            {
                context.SupportOrganizations.AddRange(
                    new SupportOrganization
                    {
                        Name = "South African Depression and Anxiety Group (SADAG)",
                        OrganizationType = "Mental Health",
                        Description = "Mental health support and counseling services",
                        PhoneNumber = "0800 567 567",
                        Email = "info@sadag.org.za",
                        Website = "www.sadag.org.za",
                        Is247Available = true,
                        IsFreeService = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "Gender-Based Violence Command Centre",
                        OrganizationType = "GBV Support",
                        Description = "24/7 support for victims of gender-based violence",
                        PhoneNumber = "0800 428 428",
                        Is247Available = true,
                        IsFreeService = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "Legal Aid South Africa",
                        OrganizationType = "Legal Aid",
                        Description = "Free legal assistance for those who cannot afford legal representation",
                        PhoneNumber = "0800 110 110",
                        Website = "www.legal-aid.co.za",
                        IsFreeService = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "Lifeline South Africa",
                        OrganizationType = "Crisis Support",
                        Description = "Crisis intervention and emotional support",
                        PhoneNumber = "0861 322 322",
                        Is247Available = true,
                        IsFreeService = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "FAMSA (Family and Marriage Society of South Africa)",
                        OrganizationType = "Family Support",
                        Description = "Family counseling and support services",
                        PhoneNumber = "011 975 7107",
                        Website = "www.famsa.org.za",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "Childline South Africa",
                        OrganizationType = "Child Protection",
                        Description = "Support for children and families",
                        PhoneNumber = "0800 055 555",
                        Is247Available = true,
                        IsFreeService = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SupportOrganization
                    {
                        Name = "Rape Crisis Cape Town Trust",
                        OrganizationType = "Sexual Assault Support",
                        Description = "Support for survivors of sexual violence",
                        PhoneNumber = "021 447 9762",
                        Website = "www.rapecrisis.org.za",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            await context.SaveChangesAsync();

            // Create default IT Admin user if none exists
            string adminEmail = "admin@safevoices.com";
            string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "ITAdmin");
                }
            }

            // Seed sample surveys if none exist
            if (!context.Surveys.Any())
            {
                var wellbeingSurvey = new Survey
                {
                    Title = "Wellbeing & Safety Assessment",
                    Description = "Help us understand your current wellbeing and safety concerns. Your responses help us improve.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(3)
                };
                context.Surveys.Add(wellbeingSurvey);
                await context.SaveChangesAsync();

                var questions = new List<SurveyQuestion>
                {
                    new SurveyQuestion
                    {
                        SurveyId = wellbeingSurvey.Id,
                        QuestionText = "How safe do you feel in your current environment?",
                        QuestionType = "rating",
                        DisplayOrder = 1,
                        IsRequired = true
                    },
                    new SurveyQuestion
                    {
                        SurveyId = wellbeingSurvey.Id,
                        QuestionText = "Have you witnessed or experienced any form of harassment or abuse in the past 6 months?",
                        QuestionType = "multiple_choice",
                        Options = "Yes, I experienced,Yes, I witnessed,No",
                        DisplayOrder = 2,
                        IsRequired = true
                    },
                    new SurveyQuestion
                    {
                        SurveyId = wellbeingSurvey.Id,
                        QuestionText = "How would you rate your overall mental wellbeing?",
                        QuestionType = "rating",
                        DisplayOrder = 3,
                        IsRequired = true
                    },
                    new SurveyQuestion
                    {
                        SurveyId = wellbeingSurvey.Id,
                        QuestionText = "Do you know where to report incidents if they occur?",
                        QuestionType = "multiple_choice",
                        Options = "Yes, I know exactly where,Yes, but unsure of process,No, I don't know",
                        DisplayOrder = 4,
                        IsRequired = true
                    },
                    new SurveyQuestion
                    {
                        SurveyId = wellbeingSurvey.Id,
                        QuestionText = "What additional support would you like to see available? (Optional)",
                        QuestionType = "text",
                        DisplayOrder = 5,
                        IsRequired = false
                    }
                };

                context.SurveyQuestions.AddRange(questions);
                await context.SaveChangesAsync();
            }
        }
    }
}