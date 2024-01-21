using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.Models
{
	public class Voucher
	{
        public Guid VoucherID { get; set; }

        public string? VoucherName { get; set; } = string.Empty;

        public double Value { get; set; }

        public int Quantity { get; set; }

        //public virtual Ticket Ticket { get; set; }
    }
}
