using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayPal.Api;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Models.ViewModels;
using System.Security.Claims;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "customer,admin,cinemaManager")]
    public class PaymentController : Controller
    {
        private Payment payment;
        private readonly IUnitOfWork _unitOfWork;

        private TicketVM TicketVM { get; set; }

        public PaymentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> AuthorizePaymentAsync(string seatIDs, Guid showtime_id, string? voucherCode, string? status = null, string Cancel = null)
        {
            var voucher_value = 0.0;
            var voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherName == voucherCode);
            if (voucher != null)
            {
                voucher_value = voucher.Value;
            }

            try
            {
                APIContext apiContext = PaypalConfiguration.GetAPIContext();

                if (!string.IsNullOrEmpty(Cancel))
                {
                    return View("CancelledPayment");
                }

                //HttpContext.Session.SetString("seatIDs", seatIDs);
                //HttpContext.Session.SetString("showtime_id", showtime_id.ToString());

                string payerId = HttpContext.Request.Query["PayerID"].ToString();
                if (string.IsNullOrEmpty(payerId))
                {
                    var requestUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.Path;
                    var guid = Convert.ToString((new Random()).Next(100000));
                    requestUrl = requestUrl + "?guid=" + guid;
                    var createdPayment = await CreatePayment(apiContext, requestUrl, seatIDs, showtime_id, voucher_value);

                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = "";
                    while (links.MoveNext())
                    {
                        Links link = links.Current;
                        if (link.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = link.href;
                            break;
                        }
                    }

                    // Store payment ID in session
                    HttpContext.Session.SetString("payment", createdPayment.id);
                    HttpContext.Session.SetString("seatIDs", seatIDs);
                    HttpContext.Session.SetString("showtime_id", showtime_id.ToString());
                    HttpContext.Session.SetString("voucher_code", voucherCode + "");
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var guid = Request.Query["guid"];
                    var paymentId = HttpContext.Session.GetString("payment");

                    // Execute payment
                    var executedPayment = ExecutePayment(apiContext, payerId, paymentId);
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailedPayment");
                    }
                }
            }
            catch (PayPal.PaymentsException ex)
            {
                return View("FailedPayment");
            }
            catch (Exception ex)
            {
                return View("FailedPayment");
            }

            HttpContext.Session.Remove("payment");

            return RedirectToAction("BookingProcess", "Ticket", new
            {
                seatIDs = HttpContext.Session.GetString("seatIDs"),
                showtime_id = HttpContext.Session.GetString("showtime_id"),
                payment_method = "paypal",
                status = "success",
                voucherCode = HttpContext.Session.GetString("voucher_code"),
            });
            // return View("SuccessPayment");
        }

        private Payment ExecutePayment(APIContext aPIContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };

            payment = new Payment()
            {
                id = paymentId,
            };

            return this.payment.Execute(aPIContext, paymentExecution);
        }

        private async Task<Payment> CreatePayment(APIContext aPIContext, string redirectUrl, string seatIDs, Guid showtime_id, double? voucher_value)
        {

            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>(); // Use List<Guid> instead of Guid[]


            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID); // Use Add method to append to the list
            }

            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var showtime = await _unitOfWork.Showtime
                    .GetFirstOrDefaultAsync(x => x.ShowtimeID == showtime_id, includeProperties: "Room");
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
            var room = showtime.Room;
            var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);

            var total = 0.0;
            string seat_names = "";

            foreach (var seatID in seatIDList)
            {
                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
                seat_names += seat.SeatName;
                total += movie.Price;
            }
            var Time = $"Date: {showtime.Date.ToString()} Time: {showtime.Time}h{showtime.Minute}";


            var itemList = new ItemList()
            {
                items = new List<Item>(),
            };

            itemList.items.Add(new Item()
            {
                name = cinema.CinemaName + " " + movie.MovieName,
                currency = "USD",
                price = (movie.Price * (1 - voucher_value / 100)).ToString(),
                quantity = seatIDList.Count().ToString(),
                sku = "sku"
            });

            var payer = new Payer()
            {
                payment_method = "paypal",
            };

            var redirUrl = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = (total * (1 - voucher_value / 100)).ToString(),
            };

            var transactionList = new List<Transaction>();

            transactionList.Add(new Transaction()
            {
                description = "Seat: " + seat_names + "Room: " + room.RoomName,
                invoice_number = Guid.NewGuid().ToString(),
                amount = amount,
                item_list = itemList
            });

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrl,
            };

            return payment.Create(aPIContext);
        }
    }
}
