using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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


		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public async Task<IActionResult> Create(ShowtimeVM showtimeVM)
		//{
		//	if (ModelState.IsValid)
		//	{
		//var showtime = showtimeVM.Showtime;
		//var roomID = showtimeVM.Showtime.RoomID;
		//var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == roomID);

		//// Kiểm tra nếu đã tồn tại suất chiếu trong cùng phòng và cùng ngày
		//var existingShowtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.RoomID == room.RoomID && u.Date == showtimeVM.Showtime.Date);

		//if (existingShowtime != null)
		//{
		//	// Tính thời gian bắt đầu và kết thúc của suất chiếu mới
		//	var startTimeOfNewShowtime = showtime.Time * 60 + showtime.Minute;
		//	var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
		//	var movie_duration = movie.Duration;
		//	var endTimeOfNewShowtime = startTimeOfNewShowtime + movie_duration;

		//	// Tính toán thời gian kết thúc của suất chiếu đã tồn tại
		//	var endTimeOfExistingShowtime = existingShowtime.Time * 60 + existingShowtime.Minute + movie_duration;

		//	if (startTimeOfNewShowtime >= endTimeOfExistingShowtime)
		//	{
		//		// Thời gian bắt đầu của suất chiếu mới sau thời gian kết thúc của suất chiếu đã tồn tại, không có xung đột
		//		_unitOfWork.Showtime.Add(showtimeVM.Showtime);
		//		_unitOfWork.Save();
		//		return RedirectToAction("Index");
		//	}
		//	else
		//	{
		//		// Thời gian bắt đầu của suất chiếu mới bị chồng chéo với suất chiếu đã tồn tại
		//		ViewData["ErrorMessage"] = "Start time is not valid.";

		//		var cinemas = await _unitOfWork.Cinema.GetAllAsync();
		//		var movies = await _unitOfWork.Movie.GetAllAsync();

		//		showtimeVM.CinemaList = cinemas.Select(u => new SelectListItem
		//		{
		//			Text = u.CinemaName,
		//			Value = u.CinemaID.ToString(),
		//		});

		//		showtimeVM.MovieList = movies.Select(u => new SelectListItem
		//		{
		//			Text = u.MovieName,
		//			Value = u.MovieID.ToString(),
		//		});
		//		return View(showtimeVM);
		//	}
		//}
		//else
		//{
		//	// Không có suất chiếu khác trong cùng phòng và cùng ngày, tiến hành tạo suất chiếu mới
		//	_unitOfWork.Showtime.Add(showtimeVM.Showtime);
		//	_unitOfWork.Save();
		//	return RedirectToAction("Index");
		//}
		//	}
		//}
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
				var showtime = showtimeVM.Showtime;
				var roomID = showtimeVM.Showtime.RoomID;
				var existingShowtimes = await _unitOfWork.Showtime.GetAllAsync(u => u.RoomID == roomID && u.Date == showtimeVM.Showtime.Date);

				// Biến để kiểm tra xem xung đột về thời gian 
				bool hasTimeConflict = false;

				foreach (var existingShowtime in existingShowtimes)
				{
					// Tính thời gian bắt đầu và kết thúc của suất chiếu đã tồn tại
					var startTimeOfExistingShowtime = existingShowtime.Time * 60 + existingShowtime.Minute;
					var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == existingShowtime.MovieID);
					var movie_duration = movie.Duration;
					var endTimeOfExistingShowtime = startTimeOfExistingShowtime + movie_duration;

					// Tính thời gian bắt đầu của suất chiếu mới
					var startTimeOfNewShowtime = showtime.Time * 60 + showtime.Minute;
					var endTimeOfNewShowtime = startTimeOfNewShowtime + movie_duration;

					// Kiểm tra xem thời gian bắt đầu của suất chiếu mới có bị chồng chéo với bất kỳ suất chiếu nào đã tồn tại không
					if (startTimeOfNewShowtime < endTimeOfExistingShowtime && endTimeOfNewShowtime > startTimeOfExistingShowtime)
					{
						// Thời gian bắt đầu của suất chiếu mới bị chồng chéo với suất chiếu đã tồn tại
						hasTimeConflict = true;
						break; // Thoát khỏi vòng lặp khi gặp xung đột
					}
				}

				if (!hasTimeConflict)
				{
					// Không gặp xung đột về thời gian, tiến hành thêm mới suất chiếu
					_unitOfWork.Showtime.Add(showtime);
					_unitOfWork.Save();
					return RedirectToAction("Index");
				}
				else
				{
					// Xuất thông báo lỗi khi gặp xung đột về thời gian
					ViewData["ErrorMessage"] = "Start time is not valid.";

					var cinemas = await _unitOfWork.Cinema.GetAllAsync();
					var movies = await _unitOfWork.Movie.GetAllAsync();

					showtimeVM.CinemaList = cinemas.Select(u => new SelectListItem
					{
						Text = u.CinemaName,
						Value = u.CinemaID.ToString(),
					});

					showtimeVM.MovieList = movies.Select(u => new SelectListItem
					{
						Text = u.MovieName,
						Value = u.MovieID.ToString(),
					});

					return View(showtimeVM);
				}
			}

			return View(showtimeVM);


		}



		//public async Task<IActionResult> Create(ShowtimeVM showtimeVM)
		//{
		//	if (ModelState.IsValid)
		//	{
		//		var showtime = showtimeVM.Showtime;
		//		var roomID = showtimeVM.Showtime.RoomID;
		//		var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == roomID);
		//		var showtime_to_check = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.RoomID == room.RoomID);
		//		var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);

		//		var movie_duration = movie.Duration;
		//		if (showtime_to_check.Date == showtimeVM.Showtime.Date)
		//		{
		//			// get the endTime from showtime_to_check.Time (hours) showtime_to_check.Minute + movie_duration 
		//			// get theh startTime from showtime.Time and showtime.Minute
		//			// check that whether the startTime is before the endTime or not, if yes, set error and go back to the create page, if no, create new showtime					
		//			int total_min_end = showtime_to_check.Time * 60 + showtime_to_check.Minute + movie.Duration;
		//			int hours = total_min_end / 60;
		//			int minutes = total_min_end % 60;

		//			// Create a TimeOnly object
		//			TimeOnly time_end = new TimeOnly(hours, minutes, 0);
		//			TimeOnly time_start = new TimeOnly(showtime.Time, showtime.Minute, 0);

		//			if (time_start > time_end)
		//			{
		//				_unitOfWork.Showtime.Add(showtimeVM.Showtime);
		//				_unitOfWork.Save();
		//				return RedirectToAction("Index");
		//			}
		//			else
		//			{
		//				// Set the error message in ViewData
		//				ViewData["ErrorMessage"] = "Start time is not valid.";
		//				// Return the view with ViewData
		//				return View(showtimeVM);
		//			}
		//		}
		//		else
		//		{
		//			_unitOfWork.Showtime.Add(showtimeVM.Showtime);
		//			_unitOfWork.Save();
		//			return RedirectToAction("Index");
		//		}


		//	}
		//	return View(showtimeVM);
		//}


		[HttpGet]
		public async Task<IActionResult> Update(Guid showtime_id)
		{
			var cinemas = await _unitOfWork.Cinema.GetAllAsync();
			var movies = await _unitOfWork.Movie.GetAllAsync();

			ShowtimeVM showtimeVM = new ShowtimeVM
			{

				CinemaList = cinemas.Select(u => new SelectListItem
				{
					Text = u.CinemaName,
					Value = u.CinemaID.ToString(),
				}),
				MovieList = movies.Select(u => new SelectListItem
				{
					Text = u.MovieName,
					Value = u.MovieID.ToString(),
				}),
				Showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id),
			};

			var showtimes = showtimeVM;
			return View(showtimes);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(ShowtimeVM showtimeVM)
		{
			if (ModelState.IsValid)
			{
				var updatedShowtime = showtimeVM.Showtime;

				// Tạo một bản sao của suất chiếu hiện tại để lưu trữ dữ liệu cũ
				var oldShowtime = new Showtime
				{
					ShowtimeID = updatedShowtime.ShowtimeID,
					RoomID = updatedShowtime.RoomID,
					Date = updatedShowtime.Date,
					Time = updatedShowtime.Time,
					// Copy các thuộc tính khác cần thiết
				};

				// Cập nhật dữ liệu của roomID và Date từ form
				var roomID = updatedShowtime.RoomID;
				var date = updatedShowtime.Date;
				var existingShowtimes = await _unitOfWork.Showtime.GetAllAsync(u => u.RoomID == roomID && u.Date == date);

				// Biến để kiểm tra xem xung đột về thời gian 
				bool hasTimeConflict = false;

				foreach (var existingShowtime in existingShowtimes)
				{
					if (existingShowtime.ShowtimeID != updatedShowtime.ShowtimeID) // Bỏ qua so sánh với chính suất chiếu đang cập nhật
					{
						// Tính thời gian bắt đầu và kết thúc của suất chiếu đã tồn tại
						var startTimeOfExistingShowtime = existingShowtime.Time * 60 + existingShowtime.Minute;
						var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == existingShowtime.MovieID);
						var movie_duration = movie.Duration;
						var endTimeOfExistingShowtime = startTimeOfExistingShowtime + movie_duration;

						// Tính thời gian bắt đầu của suất chiếu mới
						var startTimeOfNewShowtime = updatedShowtime.Time * 60 + updatedShowtime.Minute;
						var endTimeOfNewShowtime = startTimeOfNewShowtime + movie_duration;

						// Kiểm tra xem thời gian bắt đầu của suất chiếu mới có bị chồng chéo với bất kỳ suất chiếu nào đã tồn tại không
						if (startTimeOfNewShowtime < endTimeOfExistingShowtime && endTimeOfNewShowtime > startTimeOfExistingShowtime)
						{
							// Thời gian bắt đầu của suất chiếu mới bị chồng chéo với suất chiếu đã tồn tại
							hasTimeConflict = true;
							break; // Thoát khỏi vòng lặp khi gặp xung đột
						}
					}
				}

				if (!hasTimeConflict)
				{
					// Không gặp xung đột về thời gian, tiến hành cập nhật dữ liệu
					_unitOfWork.Showtime.Update(updatedShowtime);
					_unitOfWork.Save();
					return RedirectToAction("Index");
				}
				else
				{
					// Phục hồi dữ liệu cũ của suất chiếu
					// Lưu ý: Đây chỉ là ví dụ, bạn cần điều chỉnh tùy theo cách thức bạn lưu trữ và phục hồi dữ liệu
					//_unitOfWork.Showtime.Update(oldShowtime);
					//_unitOfWork.Save();

					// Xuất thông báo lỗi khi gặp xung đột về thời gian
					ViewData["ErrorMessage"] = "Start time is not valid.";

					var cinemas = await _unitOfWork.Cinema.GetAllAsync();
					var movies = await _unitOfWork.Movie.GetAllAsync();

					showtimeVM.CinemaList = cinemas.Select(u => new SelectListItem
					{
						Text = u.CinemaName,
						Value = u.CinemaID.ToString(),
					});

					showtimeVM.MovieList = movies.Select(u => new SelectListItem
					{
						Text = u.MovieName,
						Value = u.MovieID.ToString(),
					});

					return View(showtimeVM);
				}
			}

			return RedirectToAction("Index");
		}


		[HttpPost]
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
				var movieID = showtime.Room.CinemaID;

				var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinemaID);
				var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movieID);

				var showtimeData = new
				{
					showtimeID = showtime.ShowtimeID,
					date = showtime.Date,
					time = showtime.Time,
					minute = showtime.Minute,
					movie = showtime.Movie,
					movie_duration = showtime.Movie.Duration,
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
		[HttpDelete]
		public async Task<IActionResult> Delete(Guid showtime_id)
		{
			var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id);
			if (showtime == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			_unitOfWork.Showtime.Delete(showtime);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Delete showtime successfully! " });
		}

		//[HttpGet]
		//public async Task<string> GetDuration(Guid movie_id)
		//{
		//	var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie_id);
		//	var movieDuration = movie.Duration;
		//	return movieDuration.ToString();
		//}

	}
	#endregion
}



