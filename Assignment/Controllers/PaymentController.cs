using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Assignment.Controllers
{
    public class PaymentController(DB db, IWebHostEnvironment en, IConfiguration cf) : Controller
    {

        public IActionResult Make_Payment(int? bookingId)
        {
            if (bookingId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var price = db.Bookings.Where(b => b.Id == bookingId)
                                   .Select(b => new PaymentVM
                                   {
                                       Id = b.Id,
                                       Price = b.TotalPrice,

                                   }).FirstOrDefault();

            if (price == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TotalAmount = price.Price * 1.06m;


            return View(price);
        }

        [HttpPost]
        public IActionResult Make_Payment(PaymentVM vm)
        {
            if (vm.paymentDetail == null)
            {
                ViewBag.ErrorMessage = "Invalid Payment Details";
                return View(vm);
            }

            if (string.IsNullOrEmpty(vm.paymentDetail.Name) 
                || string.IsNullOrEmpty(vm.paymentDetail.CardNumber) 
                || string.IsNullOrEmpty(vm.paymentDetail.ExpiryDate) 
                || string.IsNullOrEmpty(vm.paymentDetail.CVV))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View(vm);
            }

            if (vm.paymentDetail.Name.Any(char.IsDigit))
            {
                ViewBag.ErrorMessage = "Wrong Name Format";
                return View(vm);
            }

            if (vm.paymentDetail.CardNumber.Length != 16)
            {
                ViewBag.ErrorMessage = "Wrong Card Number Format.";
                return View(vm);
            }

            if (vm.paymentDetail.CVV.Length != 3)
            {
                ViewBag.ErrorMessage = "Wrong CVV Format.";
                return View(vm);
            }

            var booking = db.Bookings.FirstOrDefault(b => b.Id == vm.Id);

            var price = booking.TotalPrice;
            var tax = price * 0.06m;
            var totalPrice = price + tax;

            try
            {
                Payment payment = new Payment
                {
                    BookingId = booking.Id,
                    PaymentStatus = PaymentStatus.Processing,
                    Amount = totalPrice,
                    CreatedAt = DateTime.UtcNow,

                    PaymentDetail = vm.paymentDetail
                };
                db.Payments.Add(payment);
                db.SaveChanges();
            } catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Failed to Create an Account, Please Try Again.\n" + ex.ToString();
                return View(vm);
            }

            return RedirectToAction("Verification", "Payment", new { bookingId = vm.Id });
        }


        public IActionResult Verification(int? bookingId)
        {
            if (bookingId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = db.Bookings
                .Include(b => b.Service)
                .Include(b => b.Account)
                .Include(b => b.Payments)
                    .ThenInclude(b => b.PaymentDetail)
    
                .FirstOrDefault(b => b.Id == bookingId);

            if (booking == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        public IActionResult Success(int? bookingId)
        {
            if (bookingId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = db.Bookings
                .Include(b => b.Service)
                .Include(b => b.Account)
                .Include(b => b.Payments)
                .ThenInclude (p => p.PaymentDetail)
                .FirstOrDefault(b => b.Id == bookingId);


            if (booking == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var payment = booking.Payments.FirstOrDefault();

            if (payment != null)
            {
                payment.PaymentStatus = PaymentStatus.Succeeded;
                db.SaveChanges();
            }

            return View(booking);
        }

        public IActionResult Receipt(int? bookingId)
        {
            if (bookingId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = db.Bookings
                .Include(b => b.Service)
                .Include(b => b.Account)
                .Include(b => b.Payments)
                    .ThenInclude(p => p.PaymentDetail)
                .FirstOrDefault(b => b.Id == bookingId);

            if (booking == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        public IActionResult Pricing()
        {
            return View();
        }
    }
}
