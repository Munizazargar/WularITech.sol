using Microsoft.AspNetCore.Mvc;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class ContactController : Controller
    {
       private readonly SqlDbContext dbContext;

       public ContactController(SqlDbContext dbContext)
       {
        this.dbContext=dbContext;
       }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
       
        public IActionResult Index(Contact contact)
        {
            if (!ModelState.IsValid)
            {
                // Validation failed, show errors
                return View(contact);
            }
              TempData["SuccessMessage"] = "✅ Your message has been sent successfully!";

    return RedirectToAction("Index");
           
        }
    }
}
