using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    /// <summary>
    /// this just a fake payment controller to simulate the payment process for testing purposes.
    /// this will be abandoned in the future when the real payment gateway is integrated.
    /// </summary>
    public class FakePaymentController(DB db) : Controller
    {
        // GET: /Payment?bookingId=5 or /Payment/Index?bookingId=5
        public IActionResult Index(int bookingId)
        {
            var booking = db.Bookings
                .Include(b => b.BookingDetail)
                .Include(b => b.Service)
                    .ThenInclude(s => s.ServiceCategory)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Staff)
                    .ThenInclude(s => s.Account)
                        .ThenInclude(a => a.AccountDetail)
                .FirstOrDefault(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Booking not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        // POST: /Payment/SimulateSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SimulateSuccess(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Booking not found.";
                return RedirectToAction("Index", "Home");
            }

            booking.Status = BookingStatus.confirmed;

            // Optional: add dummy payment record
            var payment = new Payment
            {
                BookingId = booking.Id,
                PaymentStatus = PaymentStatus.Succeeded,
                Amount = booking.TotalPrice,
                CreatedAt = DateTime.Now,
                PaymentMethodId = "pm_simulated_card",
                PaymentDetail = new PaymentDetail
                {
                    CardHolder = "Demo Pokemon Trainer",
                    CardBrand = "Visa",
                    CardLast4 = "4242"
                }
            };
            db.Payments.Add(payment);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Payment succeeded! Booking #{booking.Id} is now confirmed.";
            return RedirectToAction("Details", "Booking", new { id = booking.Id });
        }
    }
}
