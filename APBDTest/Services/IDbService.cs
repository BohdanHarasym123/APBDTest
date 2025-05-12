using APBDTest.Models;

namespace APBDTest.Services;

public interface IDbService
{
    public Task<VisitDTO> GetVisitByIdAsync(int id);
}