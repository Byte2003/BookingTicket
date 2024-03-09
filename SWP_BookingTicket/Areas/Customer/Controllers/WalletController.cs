﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using System.Security.Claims;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "customer,admin,cinemaManager")]
    public class WalletController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        public WalletController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);
            if (claim == null || user == null)
            {
                return NotFound();
            }
            else
            {
                return View(user);
            }
        }
    }
}
