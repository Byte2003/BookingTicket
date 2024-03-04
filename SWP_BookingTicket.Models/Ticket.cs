using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.Models
{
	public class Ticket
	{
		[Key]
        public Guid TicketID { get; set; }
       
		public Guid ShowtimeID { get; set; }
		[ForeignKey(nameof(ShowtimeID))]
		[ValidateNever]
		public Showtime Showtime { get; set; }

		public Guid SeatID { get; set; }
		[ForeignKey(nameof(SeatID))] 
		[ValidateNever]
		public Seat Seat { get; set; }

		public string AppUserID {  get; set; }
		[ForeignKey(nameof(AppUserID))]
		[ValidateNever]
		public AppUser AppUser { get; set; }

		public double Total { get; set; }

		public Guid? VoucherID { get; set; }
		[ForeignKey(nameof(VoucherID))]
		[ValidateNever]
		public Voucher Voucher { get; set; }

		public DateTime? BookedDate { get; set; }
	}
}
