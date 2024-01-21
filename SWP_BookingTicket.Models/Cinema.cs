using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWP_BookingTicket.Models
{
	public class Cinema
	{
        [Key]
        public Guid CinemaID { get; set; }

        [Required]
        public string CinemaName { get; set; }

        [Required]
        public string Address { get; set; }

        
    }
}
