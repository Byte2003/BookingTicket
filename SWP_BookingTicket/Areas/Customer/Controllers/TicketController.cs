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
using System;
using System.Security.Claims;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

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
        // private static List<string> scheduledJobIds;
        private enum PaymentStatus
        {
            Pending,
            Success
        }
        public TicketController(IUnitOfWork unitOfWork, IDbContextFactory<AppDbContext> dbContextFactory, IUnlockASeatService unlockASeatService, IBackgroundJobClient backgroundJobClient)
        {
            // scheduledJobIds = new List<string>();
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
        public async Task HandleLockAndUnlock(List<Guid> seats, Guid showtime_id, string? status)
        {
            

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                IUnitOfWork uow = new UnitOfWork(dbContext);
                List<Seat> seatList = new List<Seat>();
                // Handle payment settings -- On developing

                double timeSpan = 0;
                if (status == "pending")
                {
                    timeSpan = 3 * 60; // 3 min
                }

                if (status == "success")
                {
                    //Showtime showtime = await uow.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id);

                    //DateTime currentTime = DateTime.Now;
                    //DateOnly showTimeDate = showtime.Date;
                    //DateTime endShow = new DateTime(showTimeDate.Year, showTimeDate.Month, showTimeDate.Day, showtime.Time, showtime.Minute, 0);

                    //TimeSpan difference = endShow - currentTime;
                    //// Get the difference in seconds
                    //double timeUntilShowtimeEnd = difference.TotalSeconds;
                    //timeSpan = timeUntilShowtimeEnd;


                    timeSpan = 3 * 60; // test 
                }


                foreach (var seatID in seats)
                {
                    var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
                    LockASeat(seat, showtime_id, status);
                    string jobId = _backgroundJobClient.Schedule(() =>
                        _unlockASeatService.UnlockASeat(seatID, showtime_id, status), TimeSpan.FromSeconds(timeSpan));

                    //jobId = jobId + "_seatID=" + seatID.ToString() + "_showtimeID=" + showtime_id.ToString();
                    //scheduledJobIds.Add(jobId);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }


        private void LockASeat(Seat seat, Guid showtime_id, string? status)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            IUnitOfWork uow = new UnitOfWork(dbContext);
            if (!seat.SeatStatus.ToLower().Contains("locked"))
            {
                seat.SeatStatus = "LOCKED_";
            }

            if (status == "pending")
                seat.SeatStatus += showtime_id.ToString() + "_status=pending";

            if (status == "success")
            {
                seat.SeatStatus = seat.SeatStatus.Replace(showtime_id.ToString() + "_status=pending", showtime_id.ToString() + "_status=success");
            }

            uow.Seat.Update(seat);
            uow.Save();
            Console.WriteLine($"Lock : {showtime_id.ToString().ToUpper()}");
        }

        public async Task<IActionResult> BookingProcess(string seatIDs, Guid showtime_id, string? status = null)
        {
            Console.WriteLine(TicketVM);
            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>(); // Use List<Guid> instead of Guid[]

            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID); // Use Add method to append to the list
            }

            switch (status)
            {
                case null:
                    {

                        HandleLockAndUnlock(seatIDList, showtime_id, "pending"); // pending in 3 min

                        return RedirectToAction("AuthorizePayment", "Payment", new
                        {
                            seatIDs = seatIDs,
                            showtime_id = showtime_id,
                            status = status
                        });
                        // break;
                    }
                case "success":
                    {
                        // Stop the current HandleLockAndUnlock function
                        // TODO: 

                        // Set another HandleLockAndUnlock

                        HandleLockAndUnlock(seatIDList, showtime_id, "success");

                        // Add Ticket to db
                        var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id);
                        var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
                        var claimsIdentity = (ClaimsIdentity)User.Identity;
                        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


                        List<Seat> seatList = new List<Seat>();
                        foreach (var seatID in seatIDList)
                        {
                            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);

                            Ticket ticket = new Ticket();
                            ticket.SeatID = seat.SeatID;
                            ticket.ShowtimeID = showtime_id;
                            ticket.Total = movie.Price;
                            ticket.AppUserID = claim.Value;

                            //_unitOfWork.Ticket.Add(ticket);

                        }

                        //_unitOfWork.Save();
                        // redirect to the sucess payment page  
                        break;
                    }
                case "cancel":
                    {

                        break;
                    }
                case "fail":
                    {

                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BookingConfirmation(string seatIDs, Guid showtime_id)
        {

            // TODO: handle 2 user choose seats at the same time before purchase
            // TODO: Create View Booked Tickets page
            // CRUD Voucher Apply voucher for the total amount 
            // Increase User Point after booking ticket
            // Using point for payment option
            
            ViewData["seatIDs"] = seatIDs;
            ViewData["showtime_id"] = showtime_id;

            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>(); // Use List<Guid> instead of Guid[]
            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID); // Use Add method to append to the list
            }

            var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(x => x.ShowtimeID == showtime_id, includeProperties: "Room");
            var room = showtime.Room;
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
            var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);

            List<Seat> seatList = new List<Seat>();
            List<string> seatsName = new List<string>();
            var total = 0.0;
            foreach (var seatID in seatIDList)
            {
                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
                seatList.Add(seat);
                seatsName.Add(seat.SeatName);
                total += movie.Price;
            }

            var Time = $"Date: {showtime.Date.ToString()} Time: {showtime.Time}h{showtime.Minute}";
            ViewData["Time"] = Time;
            ViewData["Movie"] = movie.MovieName;
            ViewData["Duration"] = movie.Duration.ToString();
            ViewData["Seats"] = seatsName;
            ViewData["Room"] = room.RoomName;
            ViewData["Cinema"] = cinema.CinemaName;
            ViewData["Price"] = movie.Price.ToString();
            ViewData["Total"] = total.ToString();
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
