using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;
using WularItech_solutions.Services;


namespace WularItech_solutions.Controllers
{
    public class ProductController : Controller
    {
        private readonly SqlDbContext dbContext;
        private readonly ICloudinaryService cloudinaryService;
        private readonly ITokenService tokenService;

        public ProductController(SqlDbContext dbContext, ICloudinaryService cloudinaryService, ITokenService tokenService)
        {
            this.dbContext = dbContext;
            this.cloudinaryService = cloudinaryService;
            this.tokenService = tokenService;
        }
        [HttpGet]
        public ActionResult CreateProduct()
        {
            var token = Request.Cookies["jwt"]; if (string.IsNullOrEmpty(token) || !tokenService.IsAdmin(token)) return Unauthorized("Only admins can create products.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product model, IFormFile image)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token) || !tokenService.IsAdmin(token))
                return Unauthorized("Only admins can create products.");

            ModelState.Remove("ProductImage");
            if (!ModelState.IsValid)
                return View(model);

            if (await dbContext.Products.AnyAsync(p => p.ProductName == model.ProductName))
            {
                ViewBag.Message = "Product already exists";
                return View(model);
            }
            // Upload image to Cloudinary if file is provided
            if (image != null && image.Length > 0)
            {
                model.ProductImage = await cloudinaryService.UploadImageAsync(image, "products");
            }

            dbContext.Products.Add(model);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<ActionResult> DeleteProduct(Guid ProductId)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token) || !tokenService.IsAdmin(token))
                return Unauthorized("Only admins can create products.");
            var product = dbContext.Products.Find(ProductId);
            if (product == null)
            {
                return NotFound();
            }
            dbContext.Products.Remove(product);
            dbContext.SaveChanges();
            return RedirectToAction("Index");

        }
        [HttpGet]
        public async Task<ActionResult> UpdateProduct(Guid ProductId)
        {
            var token = Request.Cookies["jwt"]; if (string.IsNullOrEmpty(token) || !tokenService.IsAdmin(token)) return Unauthorized("Only admins can create products.");
            var product = dbContext.Products.Find(ProductId);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateProduct(Product model, IFormFile image)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token) || !tokenService.IsAdmin(token))
                return Unauthorized("Only admins can update products.");

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Kindly fill all the fields";
                return View(model);
            }

            var existingProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == model.ProductId);
            if (existingProduct == null)
            {
                ViewBag.Message = "Product not found";
                return View(model);
            }

            // Update fields
            existingProduct.ProductName = model.ProductName;
            existingProduct.ProductDescription = model.ProductDescription;
            existingProduct.ProductPrice = model.ProductPrice;
            existingProduct.ProductStock = model.ProductStock;

            // Upload new image if provided
            if (image != null && image.Length > 0)
            {
                existingProduct.ProductImage = await cloudinaryService.UploadImageAsync(image, "products");
            }

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Product");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await dbContext.Products.ToListAsync();
            return View(products);
        }
        [HttpGet]
        public IActionResult Details(Guid id)
        {
            var product = dbContext.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            return View(product); // Pass the product to the view 
        }

    }
}
