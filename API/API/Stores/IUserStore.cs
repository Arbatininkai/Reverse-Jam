using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public interface IUserStore
    {
        List<User> Users { get; }
    }
}