using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class StaffManagementController(DB db) : Controller
    {
        // Access: StaffManagement/Index
        public IActionResult Index()
        {
            return View();
        }

        // Access: StaffManagement/Assign
        public IActionResult Assign()
        {
            return View();
        }

        //Access: StaffManagement/List
        public IActionResult List()
        {
            var staffs = db.Staffs.Include(s => s.Account)
                                  .ThenInclude(a => a.AccountDetail)
                                  .ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Staff ID", "Staff Name", "Email", "Actions" }
            };
            foreach (var s in staffs) 
            {
                var row = new List<string>
                {
                    s.Id.ToString(),
                    s.Account?.AccountDetail?.Username ?? "N/A",
                    s.Account?.Email ?? "N/A",
                    "TODO"
                };
                tableData.Rows.Add(row);
            }
            return View(tableData);
        }

    }
}
