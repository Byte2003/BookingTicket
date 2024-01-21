using SWP_BookingTicket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.DataAccess.Repositories
{
	public interface IUnitOfWork
	{
		IRepository<Cinema> Cinema { get; }
		IRepository<Movie> Movie { get; }
		IRepository<Seat> Seat { get; }
		IRepository<Room> Room { get; }
		IRepository<Ticket> Ticket { get; }
		IRepository<Showtime> Showtime { get; }
		IRepository<Promotion> Promotion { get; }
		IRepository<Comment> Comment { get; }
		void Save();
	}
}
