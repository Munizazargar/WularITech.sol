using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class CartController : Controller
    {
        private readonly SqlDbContext dbContext;
        private readonly ITokenService tokenService;
        public CartController(SqlDbContext dbContext, ITokenService tokenService)
        {
            this.dbContext = dbContext;
            this.tokenService = tokenService;
        }
        [HttpPost]
        public IActionResult AddToCart(Guid productId)
        {
            try
            {
                // 1️⃣ Get token from cookie
                var token = Request.Cookies["jwt"];
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Please login to add items to cart.";
                    return Redirect(Request.Headers["Referer"].ToString());
                }


                // 2️⃣ Get userId (Guid) from token
                Guid userId = tokenService.VerifyTokenAndGetId(token);

                // 3️⃣ Check if product already exists in cart
                var existingItem = dbContext.Carts
                    .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    // 4️⃣ Product already in cart → increase quantity
                    existingItem.Quantity += 1;

                }
                else
                {
                    // 5️⃣ Product not in cart → add new row
                    var cartItem = new Cart
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = 1
                    };

                    dbContext.Carts.Add(cartItem);
                }

                // 6️⃣ Save once
                dbContext.SaveChanges();

                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
                return RedirectToAction("Index", "Cart");
            }
        }
        [HttpPost]
        public IActionResult RemoveFromCart(Guid productId)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Please login to manage your cart.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            Guid userId = tokenService.VerifyTokenAndGetId(token);

            var cartItem = dbContext.Carts
                .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
            {
                TempData["Error"] = "This product is not in your cart.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            dbContext.Carts.Remove(cartItem);
            dbContext.SaveChanges();

            TempData["Success"] = "Product removed from cart successfully.";

            return RedirectToAction("Index", "cart"); // Cart page
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            Guid userId = tokenService.VerifyTokenAndGetId(token);

            var cartItems = await dbContext.Carts
                .Where(c => c.UserId == userId)
                .Join(
                    dbContext.Products,
                    cart => cart.ProductId,
                    product => product.ProductId,
                    (cart, product) => new CartViewModel
                    {
                        CartItemId = cart.CartItemId,
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        ProductImage = product.ProductImage,
                        ProductPrice = product.ProductPrice,
                        Quantity = cart.Quantity,
                        Stock = product.ProductStock
                    }
                )
                .ToListAsync();

            return View(cartItems); // Cart/Index.cshtml
        }


    }
}




