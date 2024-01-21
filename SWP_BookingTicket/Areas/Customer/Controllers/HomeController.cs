using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.Models;
using System.Diagnostics;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;
        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
             return View();
		}

        public IActionResult Privacy()
        {
            return View();
        }


    }
}
