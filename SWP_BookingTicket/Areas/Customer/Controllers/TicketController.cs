using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SWP_BookingTicket.DataAccess.Data;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Models.ViewModels;
using SWP_BookingTicket.Services;
using System.Security.Claims;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize(Roles = "customer,admin,cinemaManager")]
	public class TicketController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly int _pendingLockDuration = 15 * 1000; // 3 minutes
		public TicketVM TicketVM;
		private readonly object _lockObject = new object();
		private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
		private readonly IUnlockASeatService _unlockASeatService;
		private readonly IBackgroundJobClient _backgroundJobClient;
		private enum PaymentStatus
		{
			Pending,
			Success
		}
		public TicketController(IUnitOfWork unitOfWork, IDbContextFactory<AppDbContext> dbContextFactory, IUnlockASeatService unlockASeatService, IBackgroundJobClient backgroundJobClient)
		{
			_unlockASeatService = unlockASeatService;
			_unitOfWork = unitOfWork;
			_dbContextFactory = dbContextFactory;
			_backgroundJobClient = backgroundJobClient;
		}
		public async Task<IActionResult> Index(Guid? movie_id = null)
		{
			IEnumerable<Movie> movieList = await _unitOfWork.Movie.GetAllAsync();
			ViewData["MovieList"] = movieList;
			return View();
		}
		[HttpGet]
		public async Task<IActionResult> ChooseSeat(Guid showtime_id)
		{
			var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id, includeProperties: "Room,Movie");
			if (showtime is not null)
			{
				var room = showtime.Room;
				var seats = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room.RoomID);
				ViewData["ShowtimeID"] = showtime_id.ToString();
				ViewData["Seats"] = seats;
			}
			return View();
		}
		public async Task HandleLockAndUnlock(Guid[] seats, Guid showtime_id)
		{
			try
			{
				using var dbContext = _dbContextFactory.CreateDbContext();
				IUnitOfWork uow = new UnitOfWork(dbContext);
				List<Seat> seatList = new List<Seat>();
				// Handle payment settings -- On developing
				var payment_status = PaymentStatus.Success; // Temporaly paid


				if (payment_status == PaymentStatus.Pending)
				{
					foreach (var seatID in seats)
					{
						var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
						if (seat != null)
						{
							LockASeat(seat, showtime_id);
							_backgroundJobClient.Schedule(() =>
								_unlockASeatService.UnlockASeat(seatID, showtime_id), TimeSpan.FromSeconds(15));
						}

					}
				}
				if (payment_status == PaymentStatus.Success)
				{

					if (payment_status == PaymentStatus.Success)
					{

						foreach (var seatID in seats)
						{
							var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
							LockASeat(seat, showtime_id);
							_backgroundJobClient.Schedule(() => _unlockASeatService.UnlockASeat(seatID, showtime_id), TimeSpan.FromSeconds(15));
						}
					}

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				throw;
			}

		}
		private void LockASeat(Seat seat, Guid showtime_id)
		{
			using var dbContext = _dbContextFactory.CreateDbContext();
			IUnitOfWork uow = new UnitOfWork(dbContext);
			if (!seat.SeatStatus.ToLower().Contains("locked"))
			{
				seat.SeatStatus = "LOCKED_";
			}
			seat.SeatStatus += showtime_id;
			uow.Seat.Update(seat);
			uow.Save();
			Console.WriteLine($"Lock : {showtime_id.ToString().ToUpper()}");
		}
		[HttpGet]
		public async Task<IActionResult> BookingConfirmation(Guid[] seatIDList, Guid showtime_id)
		{

			using var dbContext = _dbContextFactory.CreateDbContext();
			IUnitOfWork uow = new UnitOfWork(dbContext);
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			var showtime = await uow.Showtime
					.GetFirstOrDefaultAsync(x => x.ShowtimeID == showtime_id, includeProperties: "Room");
			var movie = await uow.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);

			List<Seat> seatList = new List<Seat>();
			HandleLockAndUnlock(seatIDList, showtime_id);
			foreach (var seatID in seatIDList)
			{
				var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
				seatList.Add(seat);
			}
			TicketVM = new TicketVM();
			foreach (var seat in seatList)
			{
				Ticket ticket = new Ticket();
				ticket.SeatID = seat.SeatID;
				ticket.ShowtimeID = showtime_id;
				ticket.Total = movie.Price;
				ticket.AppUserID = claim.Value;
				TicketVM.Ticket.Add(ticket);
			}
			TicketVM.Quantity = seatList.Count();
			foreach (var ticket in TicketVM.Ticket)
			{
				_unitOfWork.Ticket.Add(ticket);
				TicketVM.Total += ticket.Total;
			}
			_unitOfWork.Save();

			ViewData["TicketVM"] = TicketVM;

			var Time = $"Date: {showtime.Date.ToString()} Time: {showtime.Time}h{showtime.Minute}";
			ViewData["Time"] = Time;

			ViewData["Movie"] = movie.MovieName;
			ViewData["Duration"] = movie.Duration.ToString();

			List<string> seatsName = new List<string>();
			foreach (var ticket in TicketVM.Ticket)
			{
				var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == ticket.SeatID);
				if (seat != null)
				{
					seatsName.Add(seat.SeatName);
				}
			}
			ViewData["Seats"] = seatsName;
			var room = showtime.Room;
			ViewData["Room"] = room.RoomName;
			var cinema = await uow.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);
			ViewData["Cinema"] = cinema.CinemaName;
			//ViewData["Price"] = TicketVM.Ticket.FirstOrDefault().Total.ToString();
			return Json(new { success = true });
			return View();
		}
		#region API Call
		[HttpGet]
		public async Task<IActionResult> GetShowtimeForAMovieWithinADay(Guid movie_id, string? date, string? address)
		{
			string[] dateComponents = date.Split('-');
			if (dateComponents.Length != 3)
			{
				throw new FormatException("Invalid date format");
			}

			int year = int.Parse(dateComponents[2]);
			int month = int.Parse(dateComponents[1]);
			int day = int.Parse(dateComponents[0]);
			DateOnly showtimeDate = new DateOnly(year, month, day);
			var showtimesForMovie = await _unitOfWork.Showtime.GetAllAsync(u => u.MovieID == movie_id, includeProperties: "Room,Movie");

			// Get showtime for a specific day for of a movie
			var showtimesWithinDay = showtimesForMovie.Where(u => u.Date == showtimeDate);
			// Get Available room for it
			var roomsForShowtime = showtimesWithinDay.Select(u => u.Room).DistinctBy(u => u.RoomID);
			// Retrive all cinema address available for it
			var cinemas = await _unitOfWork.Cinema.GetAllAsync();

			var cinemasAddressForThisShowtime = from room in roomsForShowtime
												join cinema in cinemas on room.CinemaID equals cinema.CinemaID
												select cinema.Address;
			cinemasAddressForThisShowtime = cinemasAddressForThisShowtime.Distinct();
			if (address != null)
			{
				var showtimesForAllCondition = from room in roomsForShowtime
											   join cinema in cinemas on room.CinemaID equals cinema.CinemaID
											   join showtime in showtimesWithinDay on room.RoomID equals showtime.RoomID
											   where cinema.Address == address
											   select new { name = cinema.CinemaName, time = showtime.Time, minute = showtime.Minute, showtimeID = showtime.ShowtimeID };
				return Json(new { showtimes = showtimesWithinDay, addresses = cinemasAddressForThisShowtime, showtimesForAllCondition = showtimesForAllCondition });
			}
			return Json(new { showtimes = showtimesWithinDay, addresses = cinemasAddressForThisShowtime });
		}
		#endregion
	}
}
