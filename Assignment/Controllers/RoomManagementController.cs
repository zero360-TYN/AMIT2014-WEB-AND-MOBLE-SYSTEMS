using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
namespace Assignment.Controllers
{
    public class RoomManagementController(DB db) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult List()
        {
            var roomData = db.Rooms
                .Where(r => r.IsDeleted != true)
                .Select(r => new
                {
                    r.Id,
                    r.RoomNumber,
                    RoomTypeName = r.RoomType.Name,
                    r.RoomType.BasePrice
                })
                .ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Id", "Room Number", "Room Type", "Base Price" },
                Rows = roomData.Select(r => new List<string>
                {
                    r.Id.ToString(),
                    r.RoomNumber,
                    r.RoomTypeName,
                    r.BasePrice.ToString("C", new System.Globalization.CultureInfo("en-MY"))
                }).ToList()
            };

            return View(tableData);
        }
        public IActionResult Create()
        {
            return View();
        }
    }
}
