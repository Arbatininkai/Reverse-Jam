using API.Models;

namespace API.Stores;

public class UserStore : IUserStore //laikina vartotoju "duomenu baze"
{
    public List<User> Users { get; } = new List<User>();
} // saugos kaip id, email, password
