using EventEase.Models;
using EventEasePOE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEasePOE.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking
        public async Task<IActionResult> Index(int? eventTypeId, DateTime? startDate, DateTime? endDate, bool? availableOnly)
        {
            var bookingsQuery = _context.Booking
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventType)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Venue)
                .AsQueryable();

            // Filter by EventType
            if (eventTypeId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.Event.EventTypeId == eventTypeId);
            }

            // Filter by date range
            if (startDate.HasValue && endDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate);
            }

            // Filter for available venues (if required)
            if (availableOnly == true)
            {
                // Get all venue IDs that are already booked in that range
                var bookedVenueIds = await _context.Booking
                    .Where(b =>
                        (!startDate.HasValue || b.BookingDate >= startDate) &&
                        (!endDate.HasValue || b.BookingDate <= endDate))
                    .Select(b => b.VenueId)
                    .Distinct()
                    .ToListAsync();

                bookingsQuery = bookingsQuery.Where(b => !bookedVenueIds.Contains(b.VenueId));
            }

            // Populate EventType dropdown
            ViewBag.EventTypeId = new SelectList(_context.EventType, "EventTypeId", "Name", eventTypeId);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.AvailableOnly = availableOnly ?? false;

            return View(await bookingsQuery.ToListAsync());
        }

        // Other methods (Create, Edit, Delete, Details) stay the same as previously posted
        // ...
    }
}
