using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;

namespace SWP_BookingTicket.Areas.CinemaManager.Controllers
{
	[Area("CinemaManager")]
	[Authorize(Roles = "cinemaManager")]
	public class CinemaController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		public CinemaController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			//IEnumerable<Cinema> cinemas = await _unitOfWork.Cinema.GetAllAsync();
			//return View(cinemas);
			return View();
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(Cinema cinema)
		{
			if (ModelState.IsValid)
			{
				_unitOfWork.Cinema.Add(cinema);
				_unitOfWork.Save();
			}			
			return RedirectToAction("Index");
		}
		[HttpGet]
		public async Task<IActionResult> Update(Guid cinema_id)
		{
			var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinema_id);
			if (cinema == null)
			{
				return NotFound();
			}
			else
			{
				return View(cinema);
			}
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Update(Cinema cinema)
		{
			if (ModelState.IsValid)
			{
				_unitOfWork.Cinema.Update(cinema);
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
		}
		#region API Calls
		[HttpGet]
		public async Task<IActionResult> GetAllCinemas()
		{
			var cinemas = await _unitOfWork.Cinema.GetAllAsync();
			return Json(new { data = cinemas });
		}
		[HttpDelete]
		public async Task<IActionResult> Delete(Guid cinema_id)
		{
			var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync( u=> u.CinemaID ==cinema_id);
			if (cinema == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			_unitOfWork.Cinema.Delete(cinema);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Delete cinema successfully! " });
		}
		#endregion
	}
}
