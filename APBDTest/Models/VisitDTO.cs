namespace APBDTest.Models;

public class VisitDTO
{
    public DateTime date { get; set; }
    public ClientDTO client { get; set; }
    public MechanicDTO mechanic { get; set; }
    public List<VisitServiceDTO> visitServices { get; set; }
}