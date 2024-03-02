using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Services;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace SWP_BookingTicket.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager")]
    public class PromotionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly UploadImageService _uploadImageService;
		public PromotionController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, UploadImageService uploadImageService)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
			_uploadImageService = uploadImageService;

		}
		[HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Promotion promotion, IFormFile? fileImage)
		{
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tập tin hình ảnh đã được tải lên hay chưa
                if (fileImage != null && fileImage.Length > 0)
                {
                    // Nếu có tập tin được tải lên, tiến hành tải lên và lưu trữ đường dẫn
                    promotion.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\promotion");
                }
                else
                {
                    // Nếu không có tập tin được tải lên, thiết lập ImageUrl thành null
                    promotion.ImageUrl = null;
                }

                // Tiến hành thêm promotion vào cơ sở dữ liệu
                _unitOfWork.Promotion.Add(promotion);
                _unitOfWork.Save();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Update(Guid promotion_id)
        {
            var promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion_id);
            if (promotion == null)
            {
                return NotFound();
            }
            else
            {
                return View(promotion);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(Promotion promotion, IFormFile? fileImage)
        {
			var _promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion.PromotionID);
			var oldImg = _promotion.ImageUrl;
			if (ModelState.IsValid)
			{
				if (fileImage is not null)
				{
					promotion.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\promotion", oldImg);

				}
				else
				{
					promotion.ImageUrl = oldImg;
				}
				
				_unitOfWork.Promotion.Update(promotion);
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
        }

        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _unitOfWork.Promotion.GetAllAsync();
            return Json(new { data = promotions });
        }
        
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid promotion_id)
        {
            var promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion_id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Promotion.Delete(promotion);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete promotion successfully! " });
        }
        #endregion
    }
}
