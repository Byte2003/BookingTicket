using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Utils;
using System.Diagnostics;
using System.Drawing.Printing;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private const int PAGESIZE = 5;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? pageNumber, string? searchString)
        {
			var movies = await _unitOfWork.Movie.GetAllAsync();
             return View(PaginatedList<Movie>.Create(movies, pageNumber ?? 1, PAGESIZE));
		}
        [HttpGet]
        public async Task<IActionResult> MovieDetail(Guid movie_id)
        {
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync( x => x.MovieID == movie_id);
            return View(movie);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        #region API Calll
        [HttpGet]
        public async Task<IActionResult> GetMoviesByCharacters(string text)
        {
            if (text is not null)
            {
				var movies = _unitOfWork.Movie.GetAllAsync(u => u.MovieName.ToLower().Contains(text.ToLower()));
                var moviestoRender = movies.Result.Take(4);
				return Json(new { data = moviestoRender });
			}
            return Json(new { success = false });
        }
		#endregion
	}
}
