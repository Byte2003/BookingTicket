using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SQLitePCL;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Models.ViewModels;


namespace SWP_BookingTicket.Areas.CinemaManager.Controllers
{
	[Area("CinemaManager")]
	[Authorize(Roles = "cinemaManager")]

	public class ShowtimeController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		public ShowtimeController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{

			var cinemas = await _unitOfWork.Cinema.GetAllAsync();
			var movies = await _unitOfWork.Movie.GetAllAsync();
			var rooms = await _unitOfWork.Room.GetAllAsync();

			// var rooms = await _unitOfWork.Room.GetAllAsync();


			ShowtimeVM showtimeVM = new()
			{
				CinemaList = cinemas.Select(u => new SelectListItem
				{
					Text = u.CinemaName,
					Value = u.CinemaID.ToString(),

				}),
				// TODO: Fix Movie List
				MovieList = movies.Select(u => new SelectListItem
				{
					Text = u.MovieName,
					Value = u.MovieID.ToString(),
				}),

				//RoomList = rooms.Select(u => new SelectListItem
				//{
				//	Text = u.RoomName,
				//	Value = u.RoomID.ToString(),

				//}),
				Showtime = new Showtime()
			};

			return View(showtimeVM);

		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ShowtimeVM showtimeVM)
		{
			if (ModelState.IsValid)
			{
				_unitOfWork.Showtime.Add(showtimeVM.Showtime);
				_unitOfWork.Save();
				return RedirectToAction("Index");
			}
			return View();
		}

		#region API Calls
		[HttpGet]
		public async Task<IActionResult> GetAllShowtime()
		{
			var showtimes = await _unitOfWork.Showtime.GetAllAsync(includeProperties: "Movie,Room");

			// Retrieve cinema names for each showtime
			var showtimesWithCinemaName = new List<object>();
			foreach (var showtime in showtimes)
			{
				var cinemaID = showtime.Room.CinemaID;
				var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinemaID);
				var showtimeData = new
				{
					showtimeID = showtime.ShowtimeID,
					date = showtime.Date,
					time = showtime.Time,
					minute = showtime.Minute,
					movie = showtime.Movie,
					cinema_name = cinema.CinemaName, // Include cinema name here
					room = showtime.Room,

				};
				showtimesWithCinemaName.Add(showtimeData);
			}

			return Json(new { data = showtimesWithCinemaName });
		}
		[HttpGet]
		public async Task<IActionResult> GetRoomList(string cinema_id)
		{
			Guid cinemaID = Guid.Parse(cinema_id);

			var roomList = await _unitOfWork.Room.GetAllAsync(u => u.CinemaID == cinemaID);

			return Json(new { data = roomList });
		}
	}
	#endregion
}



