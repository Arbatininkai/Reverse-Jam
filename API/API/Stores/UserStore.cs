using API.Models;

namespace API.Stores;

public static class UserStore //laikina vartotoju "duomenu baze"
{
    public static List<User> Users { get; set; } = new List<User>();
} // saugos kaip id, email, password
