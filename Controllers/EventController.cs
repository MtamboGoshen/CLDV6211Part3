using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EventEase.Models;
using EventEasePOE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEasePOE.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EventController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Event
        public async Task<IActionResult> Index(int? eventTypeId, int? venueId, DateTime? startDate, DateTime? endDate)
        {
            var events = _context.Event
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .AsQueryable();

            if (eventTypeId.HasValue)
                events = events.Where(e => e.EventTypeId == eventTypeId.Value);

            if (venueId.HasValue)
                events = events.Where(e => e.VenueId == venueId.Value);

            if (startDate.HasValue)
                events = events.Where(e => e.EventDate >= startDate.Value);

            if (endDate.HasValue)
                events = events.Where(e => e.EventDate <= endDate.Value);

            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name", eventTypeId);
            ViewBag.VenueId = new SelectList(_context.Venue, "Id", "Name", venueId);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(await events.ToListAsync());
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            ViewBag.VenueId = new SelectList(_context.Venue, "Id", "Name");
            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name");
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (@event.ImageFile != null)
                    {
                        var blobUrl = await UploadImageToBlobAzure(@event.ImageFile);
                        // Store blobUrl if needed
                    }

                    _context.Add(@event);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Event created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving event: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "There was a problem saving the event.");
                }
            }

            ViewBag.VenueId = new SelectList(_context.Venue, "Id", "Name", @event.VenueId);
            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name", @event.EventTypeId);
            return View(@event);
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event.FindAsync(id);
            if (@event == null) return NotFound();

            ViewBag.VenueId = new SelectList(_context.Venue, "Id", "Name", @event.VenueId);
            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name", @event.EventTypeId);
            return View(@event);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.EventId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Event updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Event.Any(e => e.EventId == id)) return NotFound();
                    throw;
                }
            }

            ViewBag.VenueId = new SelectList(_context.Venue, "Id", "Name", @event.VenueId);
            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name", @event.EventTypeId);
            return View(@event);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Event
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Event
                .Include(e => e.Booking)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null) return NotFound();

            if (@event.Booking.Any())
            {
                TempData["Error"] = "Cannot delete this event because it has associated booking(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Event.Remove(@event);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Event deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> UploadImageToBlobAzure(IFormFile imageFile)
        {
            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerName = "cldv6211poe";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = imageFile.ContentType
            };

            using var stream = imageFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }
    }
}
