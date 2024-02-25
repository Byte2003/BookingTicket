using SWP_BookingTicket.Models;
using Microsoft.AspNetCore.Mvc;
using PayPal.Api;
using SWP_BookingTicket.DataAccess.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SWP_BookingTicket.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class PaymentController : Controller
    {
        private Payment payment;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Payment(string Cancel = null)
        {
            try
            {
                APIContext apiContext = PaypalConfiguration.GetAPIContext();

                if (!string.IsNullOrEmpty(Cancel))
                {
                    return View("CancelledPayment");
                }

                string payerId = HttpContext.Request.Query["PayerID"].ToString();
                if (string.IsNullOrEmpty(payerId))
                {
                    var requestUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.Path;
                    var guid = Convert.ToString(new Random().Next(100000));
                    requestUrl = requestUrl + "?guid=" + guid;
                    var createdPayment = CreatePayment(apiContext, requestUrl);

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
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var guid = HttpContext.Request.Query["guid"];
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

            return View("SuccessPayment");
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

            return payment.Execute(aPIContext, paymentExecution);
        }

        private Payment CreatePayment(APIContext aPIContext, string redirectUrl)
        {

            var itemList = new ItemList()
            {
                items = new List<Item>(),
            };

            itemList.items.Add(new Item()
            {
                name = "Item 1",
                currency = "USD",
                price = "1",
                quantity = "1",
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
                total = "1",
            };

            var transactionList = new List<Transaction>();

            transactionList.Add(new Transaction()
            {
                description = "Transaction Description",
                invoice_number = Guid.NewGuid().ToString(),
                amount = amount,
                item_list = itemList
            });

            payment = new Payment()
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
