using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Services;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace SWP_BookingTicket.Areas.CinemaManager.Controllers
{
	[Area("CinemaManager")]
	[Authorize(Roles = "cinemaManager")]
	public class MovieController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly UploadImageService _uploadImageService;

		public MovieController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, UploadImageService uploadImageService)
		{
			_unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
			_uploadImageService = uploadImageService;

        }
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			IEnumerable<Movie> movies = await _unitOfWork.Movie.GetAllAsync();
            //return View(cinemas);
            return View(movies);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(Movie movie, IFormFile? fileImage)
		{
			if (ModelState.IsValid)
			{
				//string wwwRootPath = _webHostEnvironment.WebRootPath;
				//if (fileImage != null)
				//{
				//	string fileNameImage = Guid.NewGuid().ToString() + Path.GetExtension(fileImage.FileName);
				//	string MovieImagePath = Path.Combine(wwwRootPath, @"images\movie");
				//	using (var fileStream = new FileStream(Path.Combine(MovieImagePath, fileNameImage), FileMode.Create))
				//	{
				//		fileImage.CopyTo(fileStream);

				//	}
				//	movie.ImageUrl = @"\images\movie\" + fileNameImage;

				//}
				movie.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\movie");
				_unitOfWork.Movie.Add(movie);
				_unitOfWork.Save();
				TempData["success"] = "Procduct created succesfully";
			}			
			return RedirectToAction("Index");
		}
		[HttpGet]
		public async Task<IActionResult> Update(Guid movie_id)
		{
			var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie_id);
			if (movie == null)
			{
				return NotFound();
			}
			else
			{
				return View(movie);
			}
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(Movie movie, IFormFile? file)
		{
			// Get the old image of movie
			var _movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie.MovieID);
			var oldMovieImg = _movie.ImageUrl;
			if (ModelState.IsValid)
			{
				if(file is not null)
				{
					movie.ImageUrl = _uploadImageService.UploadImage(file, @"images\movie", oldMovieImg);
				} else
				{
					movie.ImageUrl = oldMovieImg;
				}				
				_unitOfWork.Movie.Update(movie);
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
		}
		#region API Calls
		[HttpGet]
		public async Task<IActionResult> GetAllMovies()
		{
			var movies = await _unitOfWork.Movie.GetAllAsync();
			return Json(new { data = movies });
		}
		[HttpDelete]
		public async Task<IActionResult> Delete(Guid movie_id)
		{
			var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync( u=> u.MovieID == movie_id);
			if (movie == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			_unitOfWork.Movie.Delete(movie);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Delete successfully! " });
		}
		#endregion
	}
}
