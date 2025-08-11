namespace VitalScan.Domain;

public class ServiceOffering
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int DurationMinutes { get; set; } = 60;
    public decimal PriceAud { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Practitioner
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Bio { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class Clinic
{
    public int Id { get; set; }
    public string Name { get; set; } = "VitalScan";
    public string Address { get; set; } = "";
    public string Timezone { get; set; } = "Australia/Melbourne";
}

public enum BookingStatus { Pending = 0, Confirmed = 1, Cancelled = 2 }

public class Booking
{
    public int Id { get; set; }
    public int ServiceOfferingId { get; set; }
    public int PractitionerId { get; set; }
    public DateTime StartLocal { get; set; }
    public DateTime EndLocal { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string Notes { get; set; } = "";
}
