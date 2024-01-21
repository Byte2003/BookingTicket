using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.Models
{
	public class AppUser : IdentityUser
	{
        public DateOnly DOB { get; set; }
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string? Avatar { get; set; } = string.Empty;
		public decimal Point { get; set; }
    }
}
