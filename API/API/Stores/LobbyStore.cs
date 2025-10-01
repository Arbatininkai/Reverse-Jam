using API.Models;

namespace API.Stores
{
    public class LobbyStore
    {
        public static List<Lobby> Lobbies { get; set; } = new List<Lobby>(); //laikinas lobiu saugojimas, reiks pakeist duom baze
    }
}
