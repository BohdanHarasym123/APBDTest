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

    public async Task AddVisitAsync(CreateVisitDTO visit)
    {
        //checking if ids are greater than 0
        if(visit.clientId <= 0 || visit.visitId <= 0) throw new Exception("Ids must be greater than 0");
            
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Connection = connection;
        
        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            //checking if visit with given id already exists
            command.Parameters.Clear();

            command.CommandText = "SELECT 1 FROM Visit WHERE visit_id = @id";
            command.Parameters.AddWithValue("@id", visit.visitId);
            var visitCheck = await command.ExecuteScalarAsync();
            if (visitCheck != null) throw new Exception("Visit with such id already exists");

            command.Parameters.Clear();

            //checking if customer with given id exists
            command.CommandText = "SELECT 1 FROM Client WHERE client_id = @id";
            command.Parameters.AddWithValue("@id", visit.clientId);
            var clientCheck = await command.ExecuteScalarAsync();
            if (clientCheck == null) throw new Exception("Client with such id does not exist");

            command.Parameters.Clear();

            //checking if mechanic with given license number exists
            command.CommandText = "SELECT mechanic_id FROM Mechanic WHERE licence_number = @licenseNumber";
            command.Parameters.AddWithValue("@licenseNumber", visit.mechanicLicenceNumber);
            var mechanicId = (int?)await command.ExecuteScalarAsync();
            if (mechanicId == null) throw new Exception("Mechanic with such license number does not exist");

            command.Parameters.Clear();

            //checking if services with given names exist
            foreach (var service in visit.services)
            {
                
                //checking if fees are greater than 0 
                if(service.serviceFee <= 0) throw new Exception("Service fee must be greater than 0");
                
                command.CommandText = "SELECT 1 FROM Service WHERE name = @name";
                command.Parameters.AddWithValue("@name", service.serviceName);
                
                var serviceCheck = await command.ExecuteScalarAsync();
                if (serviceCheck == null) throw new Exception($"Service with {service.serviceName} name does not exist");

                command.Parameters.Clear();

            }

            command.CommandText =
                "INSERT INTO Visit VALUES (@id, @client_id, @mechanic_id, @date)";
            command.Parameters.AddWithValue("@id", visit.visitId);
            command.Parameters.AddWithValue("@client_id", visit.clientId);
            command.Parameters.AddWithValue("@mechanic_id", mechanicId);
            command.Parameters.AddWithValue("@date", DateTime.Now);

            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();

            foreach (var service in visit.services)
            {
                command.CommandText = "SELECT service_id FROM Service WHERE name = @name";
                command.Parameters.AddWithValue("@name", service.serviceName);
                var serviceId = (int?)await command.ExecuteScalarAsync();

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO Visit_Service VALUES (@visit_id, @service_id, @service_fee)";
                command.Parameters.AddWithValue("@visit_id", visit.visitId);
                command.Parameters.AddWithValue("@service_id", serviceId);
                command.Parameters.AddWithValue("@service_fee", service.serviceFee);

                await command.ExecuteNonQueryAsync();

                command.Parameters.Clear();
            }

            transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            transaction.RollbackAsync();
            throw;
        }
    }
}