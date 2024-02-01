﻿using Microsoft.AspNetCore.Authorization;
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
	public class UserAccountController : Controller
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly IEmailSender _emailSender;
        public UserAccountController(UserManager<AppUser> userManager, IEmailSender emailSender)
        {
           _userManager = userManager;
			_emailSender = emailSender;
        }
        [HttpGet]
		public IActionResult Index()
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
		#endregion
	}
}