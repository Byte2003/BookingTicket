using Microsoft.Extensions.Logging;
using SWP_BookingTicket.DataAccess.Data;
using SWP_BookingTicket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.DataAccess.Repositories
{
	public class MovieRepository : Repository<Movie>
	{
		public MovieRepository(AppDbContext db) : base(db)
		{
		}
	}
}
