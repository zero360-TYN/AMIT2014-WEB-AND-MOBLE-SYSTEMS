using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class RoomManagementController(DB db) : Controller
    {
        // Access: RoomManagement/Index
        public IActionResult Index()
        {
            return View();
        }

        // Access: RoomManagement/List
        public IActionResult List(string? search, string? searchBy)
        {
            var query = db.Rooms
                .Include(r => r.RoomType)
                .Where(r => !r.IsDeleted)
                .SearchBy(search, searchBy);

            var rooms = query.ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Room ID", "Room Number", "Room Type", "Base Price", "Actions" }
            };

            foreach (var r in rooms)
            {
                var row = new List<string>
                {
                    r.Id.ToString(),
                    r.RoomNumber,
                    r.RoomType?.Name ?? "N/A",
                    r.RoomType != null ? r.RoomType.BasePrice.ToString("C", new System.Globalization.CultureInfo("en-MY")) : "N/A",
                    $"<a href='/RoomManagement/Details/{r.Id}'>Details</a> | <a href='/RoomManagement/Edit/{r.Id}'>Edit</a> | <button type='button' class='btn-delete' data-id='{r.Id}' data-name='{r.RoomNumber}'>Delete</button>"
                };
                tableData.Rows.Add(row);
            }

            if (Request.IsAjax())
            {
                return PartialView("_TableList", tableData);
            }

            return View(tableData);
        }

        // Access: RoomManagement/Create
        public IActionResult Create()
        {
            ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name");
            return View();
        }

        // POST: RoomManagement/Create
        [HttpPost]
        public IActionResult Create(RoomCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            // Validate RoomType existence
            var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == model.RoomTypeId);
            if (roomType == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Selected room type does not exist.";
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            // Check duplicate RoomNumber
            var isDuplicate = db.Rooms.Any(r => r.RoomNumber == model.RoomNumber && !r.IsDeleted);
            if (isDuplicate)
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists.");
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            var room = new Room
            {
                RoomNumber = model.RoomNumber,
                RoomTypeId = model.RoomTypeId,
                IsDeleted = false
            };

            db.Rooms.Add(room);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Room {room.RoomNumber.ToString()} created successfully.";
            return RedirectToAction("List");
        }

        // Access: RoomManagement/Edit/{id}
        public IActionResult Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Room ID.";
                return RedirectToAction("List");
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == id && !r.IsDeleted);
            if (room == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room not found.";
                return RedirectToAction("List");
            }

            var vm = new RoomEditViewModel
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                RoomTypeId = room.RoomTypeId
            };

            ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", room.RoomTypeId);
            return View(vm);
        }

        // POST: RoomManagement/Edit
        [HttpPost]
        public IActionResult Edit(RoomEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == model.Id && !r.IsDeleted);
            if (room == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room not found or already deleted.";
                return RedirectToAction("List");
            }

            // Validate RoomType existence
            var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == model.RoomTypeId);
            if (roomType == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Selected room type does not exist.";
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            // Check duplicate RoomNumber
            var isDuplicate = db.Rooms.Any(r => r.RoomNumber == model.RoomNumber && r.Id != model.Id && !r.IsDeleted);
            if (isDuplicate)
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists.");
                ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
                return View(model);
            }

            room.RoomNumber = model.RoomNumber;
            room.RoomTypeId = model.RoomTypeId;
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Room {room.RoomNumber} updated successfully.";
            return RedirectToAction("Details", new { id = model.Id });
        }

        // Access: RoomManagement/Details/{id}
        public IActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Room ID.";
                return RedirectToAction("List");
            }

            var room = db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (room == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room not found.";
                return RedirectToAction("List");
            }

            var vm = new RoomDetailsViewModel
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                RoomTypeName = room.RoomType?.Name ?? "N/A",
                BasePrice = room.RoomType?.BasePrice ?? 0
            };

            return View(vm);
        }

        // POST: RoomManagement/Delete
        [HttpPost]
        public IActionResult Delete(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Room ID.";
                return RedirectToAction("List");
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == id && !r.IsDeleted);
            if (room == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room not found or already deleted.";
                return RedirectToAction("List");
            }

            room.IsDeleted = true;
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Room {room.RoomNumber} deleted successfully.";
            return RedirectToAction("List");
        }

        // Access: RoomManagement/CreateRoomType
        public IActionResult CreateRoomType()
        {
            ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name");
            return View();
        }

        // POST: RoomManagement/CreateRoomType
        [HttpPost]
        public IActionResult CreateRoomType(RoomTypeCreateViewModel model)
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

            // Check duplicate RoomType Name
            var isDuplicate = db.RoomTypes.Any(rt => rt.Name == model.Name);
            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Room type name already exists.");
                ViewBag.ServiceCategories = new SelectList(db.ServiceCategories, "Id", "Name", model.ServiceCategoryId);
                return View(model);
            }

            var roomType = new RoomType
            {
                Name = model.Name,
                Description = model.Description,
                BasePrice = model.BasePrice,
                ServiceCategoryId = model.ServiceCategoryId,
                CreatedAt = DateTime.Now
            };

            db.RoomTypes.Add(roomType);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Room Type {roomType.Name} created successfully.";
            return RedirectToAction("RoomTypeList");
        }

        // Access: RoomManagement/RoomTypeList
        public IActionResult RoomTypeList(string? search, string? searchBy)
        {
            var query = db.RoomTypes
                .Include(rt => rt.ServiceCategory)
                .SearchBy(search, searchBy);

            var roomTypes = query.ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Room Type ID", "Name", "Description", "Base Price", "Service Category", "Actions" }
            };

            foreach (var rt in roomTypes)
            {
                var row = new List<string>
                {
                    rt.Id.ToString(),
                    rt.Name,
                    rt.Description ?? "N/A",
                    rt.BasePrice.ToString("C", new System.Globalization.CultureInfo("en-MY")),
                    rt.ServiceCategory?.Name ?? "N/A",
                    $"<a href='/RoomManagement/RoomTypeDetails/{rt.Id}'>Details</a> | <a href='/RoomManagement/EditRoomType/{rt.Id}'>Edit</a>"
                };
                tableData.Rows.Add(row);
            }

            if (Request.IsAjax())
            {
                return PartialView("_TableList", tableData);
            }

            return View(tableData);
        }

        // Access: RoomManagement/EditRoomType/{id}
        public IActionResult EditRoomType(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Room Type ID.";
                return RedirectToAction("RoomTypeList");
            }

            var roomType = db.RoomTypes
                .Include(rt => rt.ServiceCategory)
                .FirstOrDefault(rt => rt.Id == id);

            if (roomType == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room Type not found.";
                return RedirectToAction("RoomTypeList");
            }

            var vm = new RoomTypeEditViewModel
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                BasePrice = roomType.BasePrice,
                ServiceCategoryName = roomType.ServiceCategory?.Name ?? "N/A"
            };

            return View(vm);
        }

        // POST: RoomManagement/EditRoomType
        [HttpPost]
        public IActionResult EditRoomType(RoomTypeEditViewModel model)
        {
            var roomType = db.RoomTypes
                .Include(rt => rt.ServiceCategory)
                .FirstOrDefault(rt => rt.Id == model.Id);

            if (roomType == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room Type not found.";
                return RedirectToAction("RoomTypeList");
            }

            if (!ModelState.IsValid)
            {
                model.ServiceCategoryName = roomType.ServiceCategory?.Name ?? "N/A";
                return View(model);
            }

            // Check duplicate RoomType Name
            var isDuplicate = db.RoomTypes.Any(rt => rt.Name == model.Name && rt.Id != model.Id);
            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Room type name already exists.");
                model.ServiceCategoryName = roomType.ServiceCategory?.Name ?? "N/A";
                return View(model);
            }

            roomType.Name = model.Name;
            roomType.Description = model.Description;
            roomType.BasePrice = model.BasePrice;
            // ServiceCategory remains locked/unchanged
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Room Type {roomType.Name} updated successfully.";
            return RedirectToAction("RoomTypeList");
        }

        // Access: RoomManagement/RoomTypeDetails/{id}
        public IActionResult RoomTypeDetails(int? id, DateTime? date)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Room Type ID.";
                return RedirectToAction("RoomTypeList");
            }

            var roomType = db.RoomTypes
                .Include(rt => rt.ServiceCategory)
                .Include(rt => rt.Rooms.Where(r => !r.IsDeleted))
                .FirstOrDefault(rt => rt.Id == id);

            if (roomType == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Room Type not found.";
                return RedirectToAction("RoomTypeList");
            }

            var selectedDate = date ?? DateTime.Today;

            // Map active rooms into columns for the Resources timetable view
            var resources = roomType.Rooms
                .OrderBy(r => r.RoomNumber)
                .Select(r => new SlotResourceData
                {
                    Id = r.Id.ToString(),
                    Name = $"Room {r.RoomNumber}"
                }).ToList();

            // Fetch bookings for all rooms belonging to this room type
            var roomIds = roomType.Rooms.Select(r => r.Id).ToList();
            var bookings = db.Bookings
                .Include(b => b.Service)
                .Include(b => b.Account).ThenInclude(a => a.AccountDetail)
                .Include(b => b.Staff).ThenInclude(s => s.Account).ThenInclude(a => a.AccountDetail)
                .Include(b => b.BookingDetail)
                .Include(b => b.Room)
                .Where(b => roomIds.Contains(b.RoomId))
                .ToList();

            // Map bookings to slot event items with structured metadata payload
            var events = bookings.Select(b => new SlotEventData
            {
                Id = b.Id.ToString(),
                Resource = b.RoomId.ToString(),
                Text = $"{b.Service?.Name ?? "Service"} ({b.BookingDetail?.PokemonName ?? "Pet"})",
                Start = b.StartTime,
                End = b.EndTime,
                Status = b.Status.ToString(),
                BackColor = b.Status switch
                {
                    BookingStatus.confirmed => "#0d6efd", // Blue
                    BookingStatus.pending => "#ffc107",   // Amber
                    BookingStatus.completed => "#198754", // Green
                    BookingStatus.cancelled => "#dc3545", // Red
                    _ => "#6c757d"
                },
                Data = new
                {
                    bookingId = b.Id,
                    customerName = b.Account?.AccountDetail?.Username ?? "N/A",
                    customerEmail = b.Account?.Email ?? "N/A",
                    pokemonName = b.BookingDetail?.PokemonName ?? "N/A",
                    serviceName = b.Service?.Name ?? "N/A",
                    roomNumber = b.Room != null ? $"Room {b.Room.RoomNumber}" : "N/A",
                    staffName = b.Staff?.Account?.AccountDetail?.Username ?? "N/A",
                    totalPrice = b.TotalPrice.ToString("C", new System.Globalization.CultureInfo("en-MY")),
                    status = b.Status.ToString()
                }
            }).ToList();

            var vm = new RoomTypeDetailsViewModel
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                BasePrice = roomType.BasePrice,
                ServiceCategoryName = roomType.ServiceCategory?.Name ?? "N/A",
                TotalRooms = roomType.Rooms.Count,
                SelectedDate = selectedDate,
                Resources = resources,
                Events = events
            };

            return View(vm);
        }
    }
}
