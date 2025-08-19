using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using PortfolioBackend.DTOs;

namespace PortfolioBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            AppDbContext context,
            ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/contact
        [HttpPost]
        public async Task<ActionResult<ContactForm>> SubmitContactForm([FromBody] ContactFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var contactForm = new ContactForm
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Subject = dto.Subject,
                    Message = dto.Message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsReplied = false
                };

                _context.ContactForms.Add(contactForm);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New contact form submission from {dto.Email}: {dto.Subject}");

                return Ok(new { message = "Contact form submitted successfully", id = contactForm.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact form");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/contact (Admin only)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactForm>>> GetContactForms(
            [FromQuery] bool? isRead = null,
            [FromQuery] bool? isReplied = null)
        {
            try
            {
                var query = _context.ContactForms.AsQueryable();

                if (isRead.HasValue)
                    query = query.Where(cf => cf.IsRead == isRead.Value);

                if (isReplied.HasValue)
                    query = query.Where(cf => cf.IsReplied == isReplied.Value);

                return await query
                    .OrderByDescending(cf => cf.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact forms");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/contact/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ContactForm>> GetContactForm([FromRoute] int id)
        {
            try
            {
                var contactForm = await _context.ContactForms
                    .Include(cf => cf.User)
                    .FirstOrDefaultAsync(cf => cf.Id == id);

                if (contactForm == null)
                    return NotFound();

                // Mark as read
                if (!contactForm.IsRead)
                {
                    contactForm.IsRead = true;
                    contactForm.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(contactForm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving contact form with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/contact/5/reply
        [HttpPut("{id:int}/reply")]
        public async Task<IActionResult> ReplyToContactForm(
            [FromRoute] int id,
            [FromBody] string reply)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                    return NotFound();

                contactForm.AdminReply = reply;
                contactForm.IsReplied = true;
                contactForm.IsRead = true;
                contactForm.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin replied to contact form {id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error replying to contact form {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/contact/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteContactForm([FromRoute] int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                    return NotFound();

                _context.ContactForms.Remove(contactForm);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting contact form {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/contact/{id}/read - Mark contact as read
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkContactAsRead([FromRoute] int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                    return NotFound();

                contactForm.IsRead = true;
                contactForm.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Contact form {id} marked as read");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking contact form {id} as read");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/contact/mark-all-read - Mark all contacts as read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllContactsAsRead()
        {
            try
            {
                var unreadContacts = await _context.ContactForms
                    .Where(cf => !cf.IsRead)
                    .ToListAsync();

                foreach (var contact in unreadContacts)
                {
                    contact.IsRead = true;
                    contact.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Marked {unreadContacts.Count} contacts as read");

                return Ok(new { message = $"Marked {unreadContacts.Count} contacts as read", count = unreadContacts.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all contacts as read");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/contact/delete-read - Delete all read contacts
        [HttpDelete("delete-read")]
        public async Task<IActionResult> DeleteReadContacts()
        {
            try
            {
                var readContacts = await _context.ContactForms
                    .Where(cf => cf.IsRead)
                    .ToListAsync();

                _context.ContactForms.RemoveRange(readContacts);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted {readContacts.Count} read contacts");

                return Ok(new { message = $"Deleted {readContacts.Count} read contacts", deletedCount = readContacts.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read contacts");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/contact/seed-dummy-data - Add dummy contact data for testing (Development only)
        [HttpPost("seed-dummy-data")]
        public async Task<IActionResult> SeedDummyContactData()
        {
            try
            {
                // Check if we already have contact data
                var existingCount = await _context.ContactForms.CountAsync();
                if (existingCount > 0)
                {
                    return BadRequest(new { message = "Contact data already exists. Clear existing data first if you want to reseed." });
                }

                var dummyContacts = new List<ContactForm>
                {
                    new ContactForm
                    {
                        Name = "John Smith",
                        Email = "john.smith@techcorp.com",
                        Subject = "Inquiry about Enterprise Solutions",
                        Message = "Hello,\n\nI am interested in your enterprise solutions for our growing company. We are looking for a comprehensive system that can handle our workflow automation and data management needs.\n\nCould you please provide more information about your offerings and pricing?\n\nThank you,\nJohn Smith\nCTO, TechCorp Solutions",
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-7),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "Sarah Johnson",
                        Email = "sarah.j@innovatetech.com",
                        Subject = "Partnership Opportunity",
                        Message = "Dear Codenex Solutions,\n\nI represent InnovateTech, and we're looking for strategic partners to collaborate on upcoming projects. Your portfolio shows impressive work in cloud solutions and AI implementation.\n\nWould you be interested in discussing a potential partnership? I'd love to schedule a call at your convenience.\n\nBest regards,\nSarah Johnson\nBusiness Development Manager",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3),
                        IsRead = true,
                        IsReplied = true,
                        AdminReply = "Thank you for reaching out, Sarah. We'd be happy to discuss partnership opportunities. I'll have our business development team contact you within the next 2 business days to set up a meeting."
                    },
                    new ContactForm
                    {
                        Name = "Michael Chen",
                        Email = "m.chen@startupxyz.io",
                        Subject = "Custom Web Application Development",
                        Message = "Hi,\n\nWe're a fintech startup looking to build a custom web application for our financial services platform. We need expertise in React, Node.js, and secure payment processing.\n\nCan you help us with this project? What's your typical timeline and pricing structure for such projects?\n\nLooking forward to hearing from you!\n\nMichael Chen\nFounder & CEO, StartupXYZ",
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "Emily Rodriguez",
                        Email = "emily.rodriguez@healthcare-plus.com",
                        Subject = "Healthcare Data Analytics Solution",
                        Message = "Hello Codenex Team,\n\nWe are a healthcare provider looking for a data analytics solution to help us better understand patient outcomes and optimize our operations.\n\nWe've seen your work in the healthcare domain and would like to know if you can develop a custom analytics dashboard for our needs.\n\nPlease let me know if you'd like to schedule a consultation.\n\nBest,\nEmily Rodriguez\nIT Director, HealthcarePlus",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        IsRead = true,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "David Wilson",
                        Email = "david.w@retailchain.com",
                        Subject = "E-commerce Platform Migration",
                        Message = "Dear Team,\n\nWe currently run our e-commerce operations on an outdated platform and are looking to migrate to a more modern, scalable solution.\n\nOur requirements include:\n- Multi-tenant architecture\n- Advanced inventory management\n- Integration with existing ERP systems\n- Mobile-responsive design\n\nCould you provide a preliminary assessment and quote?\n\nThanks,\nDavid Wilson\nHead of Digital, RetailChain Ltd.",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "Lisa Thompson",
                        Email = "lisa.thompson@edutech.org",
                        Subject = "Educational Platform Development",
                        Message = "Hi,\n\nWe're a non-profit educational organization working on developing an online learning platform for underserved communities.\n\nWe need a platform that can handle:\n- Video content delivery\n- Interactive quizzes and assessments\n- Progress tracking\n- Multi-language support\n\nDo you have experience with educational technology projects? We'd love to discuss this further.\n\nRegards,\nLisa Thompson\nProgram Manager, EduTech Foundation",
                        CreatedAt = DateTime.UtcNow.AddHours(-18),
                        UpdatedAt = DateTime.UtcNow.AddHours(-6),
                        IsRead = true,
                        IsReplied = true,
                        AdminReply = "Hi Lisa, thank you for reaching out. We have significant experience in educational technology and would be honored to help your foundation. Given your non-profit status, we can offer special pricing. Let's schedule a call to discuss your requirements in detail."
                    },
                    new ContactForm
                    {
                        Name = "Robert Kumar",
                        Email = "robert.kumar@manufacturing.co",
                        Subject = "IoT Integration for Manufacturing",
                        Message = "Hello,\n\nWe're looking to integrate IoT sensors and devices into our manufacturing processes to improve efficiency and reduce downtime.\n\nWe need a solution that can:\n- Collect data from various sensors\n- Provide real-time monitoring\n- Generate predictive maintenance alerts\n- Create comprehensive reports\n\nDo you have experience with industrial IoT implementations?\n\nBest regards,\nRobert Kumar\nOperations Manager",
                        CreatedAt = DateTime.UtcNow.AddHours(-12),
                        UpdatedAt = DateTime.UtcNow.AddHours(-12),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "Amanda Foster",
                        Email = "amanda@creativestudio.design",
                        Subject = "Portfolio Website Redesign",
                        Message = "Hi Codenex,\n\nI run a creative design studio and we need a complete redesign of our portfolio website. We want something modern, visually striking, and fast-loading.\n\nKey requirements:\n- Showcase our design work effectively\n- Easy content management\n- SEO optimization\n- Mobile-first design\n\nWhat's your process for website redesigns?\n\nThanks!\nAmanda Foster\nCreative Director",
                        CreatedAt = DateTime.UtcNow.AddHours(-6),
                        UpdatedAt = DateTime.UtcNow.AddHours(-6),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "James Parker",
                        Email = "james.parker@consulting.com",
                        Subject = "Cloud Migration Services",
                        Message = "Dear Codenex Solutions,\n\nOur consulting firm is looking to migrate our entire infrastructure to the cloud. We currently have on-premise servers hosting various applications and databases.\n\nWe need help with:\n- Migration planning and strategy\n- AWS/Azure setup and configuration\n- Data migration with minimal downtime\n- Staff training on new systems\n- Ongoing support and maintenance\n\nCan you provide a comprehensive migration proposal?\n\nRegards,\nJames Parker\nIT Consultant",
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        UpdatedAt = DateTime.UtcNow.AddHours(-2),
                        IsRead = false,
                        IsReplied = false
                    },
                    new ContactForm
                    {
                        Name = "Maria Gonzalez",
                        Email = "maria.g@nonprofit.org",
                        Subject = "Volunteer Management System",
                        Message = "Hello,\n\nWe're a non-profit organization that manages thousands of volunteers across multiple programs. We need a custom volunteer management system to help us coordinate activities, track volunteer hours, and manage communications.\n\nFeatures we need:\n- Volunteer registration and profiles\n- Event scheduling and sign-ups\n- Hour tracking and reporting\n- Communication tools\n- Reporting and analytics\n\nWould you be interested in working with a non-profit organization?\n\nThank you,\nMaria Gonzalez\nVolunteer Coordinator",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-45),
                        IsRead = false,
                        IsReplied = false
                    }
                };

                _context.ContactForms.AddRange(dummyContacts);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully added {dummyContacts.Count} dummy contact forms for testing");

                return Ok(new 
                { 
                    message = "Dummy contact data seeded successfully", 
                    count = dummyContacts.Count,
                    data = dummyContacts.Select(c => new { c.Id, c.Name, c.Email, c.Subject, c.IsRead, c.IsReplied }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding dummy contact data");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
