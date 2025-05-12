using System.Data.SqlClient;
using APBDTest.Models;

namespace APBDTest.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService()
    {
        _connectionString =
            "Data Source=localhost, 1433; User=SA; Password=yourStrong()Password; Initial Catalog = master; Integrated Security=False;Connect Timeout=30;Encrypt=False";
    }

    public async Task<VisitDTO> GetVisitByIdAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        //checking if visit exists
        await using var checkVisitCommand = new SqlCommand("SELECT 1 FROM Visit WHERE visit_id = @id", connection);
        checkVisitCommand.Parameters.AddWithValue("@id", id);
        var check = await checkVisitCommand.ExecuteScalarAsync();
        if(check == null) throw new Exception("Visit with such id does not exist");

        VisitDTO visit = null;
        
        await using var getVisitCommand = new SqlCommand(@"SELECT v.date,
            c.first_name, c.last_name, c.date_of_birth,
            m.mechanic_id, m.licence_number 
            FROM Visit v JOIN Client c ON v.client_id = c.client_id 
            JOIN Mechanic m ON v.mechanic_id = m.mechanic_id 
            WHERE visit_id = @id", connection);
        getVisitCommand.Parameters.AddWithValue("@id", id);
        
        var visitReader = await getVisitCommand.ExecuteReaderAsync();

        while (await visitReader.ReadAsync())
        {
            visit = new VisitDTO();
            visit.date = visitReader.GetDateTime(0);

            visit.client = new ClientDTO
            {
                firstName = visitReader.GetString(1),
                lastName = visitReader.GetString(2),
                dateOfBirth = visitReader.GetDateTime(3),
            };

            visit.mechanic = new MechanicDTO
            {
                mechanicId = visitReader.GetInt32(4),
                licenseNumber = visitReader.GetString(5),
            };
            
            visit.visitServices = new List<VisitServiceDTO>();
        }
        visitReader.Close();

        await using var getServicesCommand = new SqlCommand(
            "SELECT s.name, vs.service_fee FROM Visit_Service vs JOIN Service s ON s.service_id = vs.service_id WHERE vs.visit_id = @id", connection);
        getServicesCommand.Parameters.AddWithValue("@id", id);
        
        var servicesReader = await getServicesCommand.ExecuteReaderAsync();
        while (await servicesReader.ReadAsync())
        {
            var service = new VisitServiceDTO
            {
                name = servicesReader.GetString(0),
                serviceFee = servicesReader.GetDecimal(1),
            };
            
            visit.visitServices.Add(service);
        }
        servicesReader.Close();
        
        return visit;
    }
}