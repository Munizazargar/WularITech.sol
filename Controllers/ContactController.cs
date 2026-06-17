using Microsoft.AspNetCore.Mvc;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class ContactController : Controller
    {
        private readonly SqlDbContext dbContext;

        public ContactController(SqlDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Contact contact)
        {
            if (!ModelState.IsValid)
            {
                return View(contact);
            }

            contact.CreatedAt = DateTime.Now;

            dbContext.Contacts.Add(contact);
            await dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "✅ Your message has been sent successfully!";
            return RedirectToAction("Index");
        }
    }
}