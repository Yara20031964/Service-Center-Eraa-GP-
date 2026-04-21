namespace KHDMA.Application.DTOs.Profile;

public class AddressDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddresssLine { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class CreateAddressDto
{
    public string Label { get; set; } = string.Empty;
    public string AddresssLine { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
