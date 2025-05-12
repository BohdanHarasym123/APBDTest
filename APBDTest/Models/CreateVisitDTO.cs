namespace APBDTest.Models;

public class CreateVisitDTO
{
    public int visitId { get; set; }
    public int clientId { get; set; }
    public string mechanicLicenceNumber { get; set; }
    public List<CreateServiceDTO> services { get; set; }
}