using Microsoft.Extensions.Logging;
using SWP_BookingTicket.DataAccess.Data;
using SWP_BookingTicket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.DataAccess.Repositories
{
	public class SeatRepository : Repository<Seat>
	{
		public SeatRepository(AppDbContext db) : base(db)
		{
		}
	}
}
