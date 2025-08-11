using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace VitalScan.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;

    public HomeController(IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _httpFactory = httpFactory;
        _cfg = cfg;
    }

    // ----- View models -----

    public class ServiceVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DurationMinutes { get; set; }
        public decimal PriceAud { get; set; }
    }

    public class SlotVm
    {
        public DateTime startLocal { get; set; }
        public DateTime endLocal { get; set; }
        public bool isAvailable { get; set; }
    }

    public class BookVm
    {
        // inputs
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public DateTime StartLocal { get; set; }
        public int DurationMinutes { get; set; } = 60;

        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string? Notes { get; set; }

        // UI helpers
        public string DateStr { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
        public List<SlotVm> Slots { get; set; } = new();
    }

    public record BookingResponse(int Id, string Status);


    // ----- Actions -----

    public async Task<IActionResult> Index()
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");
        var services = await http.GetFromJsonAsync<List<ServiceVm>>($"{api}/api/services") ?? new();
        return View(services);
    }

    [HttpGet]
    public async Task<IActionResult> BookNow()
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");
        var services = await http.GetFromJsonAsync<List<ServiceVm>>($"{api}/api/services") ?? new();
        var s = services.FirstOrDefault();
        if (s is null) return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Book), new { id = s.Id, name = s.Name });
    }


    // GET: /Home/Book?id=1&name=Meta%20Hunter%20Scan&date=2025-08-11&durationMinutes=60
    [HttpGet]
    public async Task<IActionResult> Book(int id, string name, DateTime? date = null, int durationMinutes = 60)
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");

        var d = date ?? DateTime.Today;
        var url = $"{api}/api/availability?serviceId={id}&date={d:yyyy-MM-dd}&durationMinutes={durationMinutes}";
        var slots = await http.GetFromJsonAsync<List<SlotVm>>(url) ?? new();

        var vm = new BookVm
        {
            ServiceId = id,
            ServiceName = name ?? "",
            DateStr = d.ToString("yyyy-MM-dd"),
            DurationMinutes = durationMinutes,
            StartLocal = d.Date.AddHours(10),
            Slots = slots.Where(s => s.isAvailable).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");

        var payload = new
        {
            serviceOfferingId = vm.ServiceId,
            practitionerId = 1, // TODO: make selectable
            startLocal = vm.StartLocal,
            durationMinutes = vm.DurationMinutes,
            customerName = vm.CustomerName,
            customerEmail = vm.CustomerEmail,
            customerPhone = vm.CustomerPhone,
            notes = vm.Notes
        };

        var resp = await http.PostAsJsonAsync($"{api}/api/bookings", payload);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, err);
            // Rehydrate slots so the page can be re-rendered with errors
            return await ReloadSlotsAndReturn(vm);
        }

        var ok = await resp.Content.ReadFromJsonAsync<BookingResponse>();
        return RedirectToAction(nameof(Thanks), new { id = ok?.Id ?? 0 });
    }

    [HttpGet]
    public async Task<IActionResult> Thanks(int id)
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");

        var detail = await http.GetFromJsonAsync<BookingDetailVm>($"{api}/api/bookings/{id}");
        if (detail is null) return RedirectToAction(nameof(Index));

        return View(detail);
    }



    // ----- Helpers -----

    private async Task<IActionResult> ReloadSlotsAndReturn(BookVm vm)
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");

        if (!DateTime.TryParse(vm.DateStr, out var d))
            d = DateTime.Today;

        var url = $"{api}/api/availability?serviceId={vm.ServiceId}&date={d:yyyy-MM-dd}&durationMinutes={vm.DurationMinutes}";
        var slots = await http.GetFromJsonAsync<List<SlotVm>>(url) ?? new();

        vm.Slots = slots.Where(s => s.isAvailable).ToList();
        vm.ServiceName = vm.ServiceName ?? "";
        vm.StartLocal = vm.StartLocal == default ? d.Date.AddHours(10) : vm.StartLocal;

        return View("Book", vm);
    }

    // Services / Pricing
    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");
        var services = await http.GetFromJsonAsync<List<ServiceVm>>($"{api}/api/services") ?? new();
        return View(services);
    }

    // About
    [HttpGet]
    public IActionResult About() => View();

    // Contact (GET form)
    [HttpGet]
    public IActionResult Contact() => View(new ContactVm());

    // Contact (POST form -> API email)
    public class ContactVm
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Message { get; set; } = "";
        public bool Sent { get; set; }
    }
    [HttpGet] public IActionResult Privacy() => View();
    [HttpGet] public IActionResult Terms() => View();
    [HttpGet] public IActionResult Cancellation() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name) || string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Message))
        {
            ModelState.AddModelError("", "Please fill in name, email and your message.");
            return View(vm);
        }

        var api = _cfg.GetValue<string>("ApiBaseUrl") ?? "";
        var http = _httpFactory.CreateClient("api");
        var resp = await http.PostAsJsonAsync($"{api}/api/contact", new { vm.Name, vm.Email, vm.Phone, vm.Message });

        vm.Sent = resp.IsSuccessStatusCode;
        if (!vm.Sent) ModelState.AddModelError("", "We couldn’t send your message. Please try again.");
        return View(vm);
    }



    public class BookingDetailVm
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public int PractitionerId { get; set; }
        public string PractitionerName { get; set; } = "";
        public DateTime StartLocal { get; set; }
        public DateTime EndLocal { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string? Notes { get; set; }
    }

}
