using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;

namespace SWP_BookingTicket.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager")]
    public class SeatController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public SeatController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            IEnumerable<Room> RoomList = await _unitOfWork.Room.GetAllAsync();
            IEnumerable<SelectListItem> RoomListSelect = RoomList.Select(u => new SelectListItem
            {
                Text = u.RoomName,
                Value = u.RoomID.ToString(),
            });
            ViewData["RoomList"] = RoomListSelect;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid room_id)
        {
            Room r = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == room_id);
            ViewData["room_id"] = room_id;
            ViewData["room_name"] = r.RoomName;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Seat seat)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Seat.Add(seat);
                _unitOfWork.Save();
                //ViewData["msg"] = "Seat created successfully.";
                return RedirectToAction("Index");
            }
            return View(seat.RoomID);
        }
        [HttpGet]
        public async Task<IActionResult> Update(Guid seat_id)
        {
            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seat_id, includeProperties:nameof(Room));
            if (seat == null)
            {
                return NotFound();
            }
            else
            {
                return View(seat);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Seat seat)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Seat.Update(seat);
                _unitOfWork.Save();
               // ViewData["msg"] = "Seat updated successfully.";
                return RedirectToAction("Index");
            }
            return View(seat);
        }

        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetSeatList(Guid? room_id)
        {
            IEnumerable<Seat> seatList;
            if (string.IsNullOrEmpty(room_id.ToString()))
            {
                seatList = await _unitOfWork.Seat.GetAllAsync(includeProperties: nameof(Room));
            }
            else
            {
                seatList = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room_id, includeProperties: nameof(Room));
            }
            return Json(new { data = seatList });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid seat_id)
        {
            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seat_id);
            if (seat == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Seat.Delete(seat);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Seat successfully! " });
        }
        #endregion
    }
}
