using SWP_BookingTicket.Models;

namespace SWP_BookingTicket.Services
{
	public interface IUnlockASeatService
	{
		void UnlockASeat(Guid seat_id, Guid showtime_id, string? status);
		
	}
}
