using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Services;
using System.Drawing;

namespace SWP_BookingTicket.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "admin")]
	public class DashboardController : Controller
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly IEmailSender _emailSender;
		private readonly IUnitOfWork _unitOfWork;
        public DashboardController(UserManager<AppUser> userManager, IEmailSender emailSender, IUnitOfWork unitOfWork)
        {
           _userManager = userManager;
			_emailSender = emailSender;
			_unitOfWork = unitOfWork;
        }
		[HttpGet]
		public IActionResult UserAccount()
		{
			return View();
		}
		[HttpGet]
		public IActionResult Dashboard()
		{
			return View();
		}
		#region API Calls 
		[HttpGet]
		public async Task<IActionResult> GetAllUsers()
		{
			var customerUsers = await _userManager.GetUsersInRoleAsync("customer");							  
			return Json(new {data = customerUsers});
		}
		[HttpPost]
		public async Task<IActionResult> LockAccount(string user_id)
		{
			var user = await _userManager.FindByIdAsync(user_id);
			if (user != null)
			{
				await _userManager.SetLockoutEnabledAsync(user, true);
				await _userManager.SetLockoutEndDateAsync(user, DateTime.Now + TimeSpan.FromMinutes(5));
				await _emailSender.SendEmailAsync(user.Email, "Lock Account", "Your account has been locked");
				return Json(new { success = true, message = "Lock successfully!" });
			}
			return Json(new { success = false, message = "Fail to lock this account" });
		}
		[HttpPost]
		public async Task<IActionResult> UnlockAccount(string user_id)
		{
			var user = await _userManager.FindByIdAsync(user_id);
			if (user != null)
			{
				await _userManager.SetLockoutEndDateAsync(user, DateTime.Now - TimeSpan.FromMinutes(5));
				await _emailSender.SendEmailAsync(user.Email, "Unlock Account", "Your account has been unlocked");
				return Json(new { success = true, message = "UnLock successfully!" });
			}
			return Json(new { success = false, message = "Fail to unlock this account" });
		}

		[HttpGet]
		public async Task<IActionResult> GetAllTickets()
		{
			var tickets = await _unitOfWork.Ticket.GetAllAsync();
			return Json(new {data = tickets});
		}

		[HttpGet]
		public async Task<IActionResult> GetTrendingMovies()
		{
            DateTime tenDaysAgo = DateTime.Now.AddDays(-10);
            var tickets = await _unitOfWork.Ticket.GetAllAsync(u => u.BookedDate > tenDaysAgo);
			var movies = await _unitOfWork.Movie.GetAllAsync();
			var showtimes = await _unitOfWork.Showtime.GetAllAsync();

			var data = from ticket in tickets
								 join showtime in showtimes on ticket.ShowtimeID equals showtime.ShowtimeID
								 join movie in movies on showtime.MovieID equals movie.MovieID
								 select new { movie = movie.MovieName, revenue = ticket.Total };

			var trendingMovies = data.GroupBy( u => u.movie)
				                            .Select(g => new { Movie = g.Key, TotalRevenue = g.Sum(u => u.revenue) })
											.OrderByDescending(u => u.TotalRevenue)
											.Take(3)
											.ToList();

			return Json(new { data = trendingMovies });
		}
		#endregion

	}
}
