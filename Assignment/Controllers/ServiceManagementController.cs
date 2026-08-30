using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class ServiceManagementController(DB db) : Controller
    {
        // Access: ServiceManagement/Index
        public IActionResult Index()
        {
            return View();
        }

        // Access: ServiceManagement/List
        public IActionResult List(string? search, string? searchBy)
        {
            var query = db.Services
                .Include(s => s.ServiceCategory)
                .Where(s => !s.IsDeleted)
                .SearchBy(search, searchBy);

            var services = query.ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Service ID", "Service Name", "Category", "Price", "Duration", "Actions" }
            };

            foreach (var s in services)
            {
                var row = new List<string>
                {
                    s.Id.ToString(),
                    s.Name,
                    s.ServiceCategory?.Name ?? "N/A",
                    s.Price.ToString("C", new System.Globalization.CultureInfo("en-MY")),
                    $"{s.DurationMinutes} mins",
                    $"<a href='/ServiceManagement/Details/{s.Id}'>Details</a> | <a href='/ServiceManagement/Edit/{s.Id}'>Edit</a> | <button type='button' class='btn-delete' data-id='{s.Id}' data-name='{s.Name}'>Delete</button>"
                };
                tableData.Rows.Add(row);
            }

            if (Request.IsAjax())
            {
                return PartialView("_TableList", tableData);
            }

            return View(tableData);
        }

        // Access: ServiceManagement/Create
        public IActionResult Create()
        {
            ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name");
            return View();
        }

        // POST: ServiceManagement/Create
        [HttpPost]
        public IActionResult Create(ServiceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name", model.ServiceCategoryId);
                return View(model);
            }

            // Validate ServiceCategory existence
            var category = db.ServiceCategories.FirstOrDefault(c => c.Id == model.ServiceCategoryId);
            if (category == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Selected service category does not exist.";
                ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name", model.ServiceCategoryId);
                return View(model);
            }

            // Check duplicate service name
            var isDuplicate = db.Services.Any(s => s.Name == model.Name && !s.IsDeleted);
            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Service name already exists.");
                ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name", model.ServiceCategoryId);
                return View(model);
            }

            var service = new Service
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DurationMinutes = model.DurationMinutes,
                ServiceCategoryId = model.ServiceCategoryId,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            db.Services.Add(service);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Service {service.Name} created successfully.";
            return RedirectToAction("List");
        }

        // Access: ServiceManagement/Edit/{id}
        public IActionResult Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Service ID.";
                return RedirectToAction("List");
            }

            var service = db.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (service == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Service not found.";
                return RedirectToAction("List");
            }

            var vm = new ServiceEditViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                ServiceCategoryName = service.ServiceCategory?.Name ?? "N/A"
            };

            return View(vm);
        }

        // POST: ServiceManagement/Edit
        [HttpPost]
        public IActionResult Edit(ServiceEditViewModel model)
        {
            var service = db.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefault(s => s.Id == model.Id && !s.IsDeleted);

            if (service == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Service not found or already deleted.";
                return RedirectToAction("List");
            }

            if (!ModelState.IsValid)
            {
                model.ServiceCategoryName = service.ServiceCategory?.Name ?? "N/A";
                return View(model);
            }

            // Check duplicate service name
            var isDuplicate = db.Services.Any(s => s.Name == model.Name && s.Id != model.Id && !s.IsDeleted);
            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Service name already exists.");
                model.ServiceCategoryName = service.ServiceCategory?.Name ?? "N/A";
                return View(model);
            }

            service.Name = model.Name;
            service.Description = model.Description;
            service.Price = model.Price;
            service.DurationMinutes = model.DurationMinutes;
            // ServiceCategoryId remains locked/unchanged
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Service {service.Name} updated successfully.";
            return RedirectToAction("Details", new { id = model.Id });
        }

        // Access: ServiceManagement/Details/{id}
        public IActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Service ID.";
                return RedirectToAction("List");
            }

            var service = db.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (service == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Service not found.";
                return RedirectToAction("List");
            }

            var vm = new ServiceDetailsViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                ServiceCategoryName = service.ServiceCategory?.Name ?? "N/A",
                CreatedAt = service.CreatedAt
            };

            return View(vm);
        }

        // POST: ServiceManagement/Delete
        [HttpPost]
        public IActionResult Delete(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Service ID.";
                return RedirectToAction("List");
            }

            var service = db.Services.FirstOrDefault(s => s.Id == id && !s.IsDeleted);
            if (service == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Service not found or already deleted.";
                return RedirectToAction("List");
            }

            service.IsDeleted = true;
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Service {service.Name} deleted successfully.";
            return RedirectToAction("List");
        }
    }
}
