using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Auth;

public interface ISessionTokenService
{
    string CreateToken(User user);
}
