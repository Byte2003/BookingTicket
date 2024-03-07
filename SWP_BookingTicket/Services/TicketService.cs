
using Microsoft.EntityFrameworkCore;
using SWP_BookingTicket.DataAccess.Data;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;

namespace SWP_BookingTicket.Services
{
    public class TicketService : ITicketService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly object _lockObject = new object();
        public TicketService(IDbContextFactory<AppDbContext> dbContextFactory, IUnitOfWork unitOfWork)
        {
            _dbContextFactory = dbContextFactory;
            _unitOfWork = unitOfWork;
        }
        public void ExpriedTicket(Guid ticket_id)
        {
            lock (_lockObject)
            {
                try
                {
                    Ticket ticket = _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticket_id).Result;
                    ticket.TicketStatus = "Expried";
                    _unitOfWork.Ticket.Update(ticket);
                    _unitOfWork.Save();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

        }
    }
}
