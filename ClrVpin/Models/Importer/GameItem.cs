using System;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Models.Importer
{
    public class GameItem
    {
        public GameItem(OnlineGame onlineGame)
        {
            OnlineGame = onlineGame;
            GameDetail = onlineGame.Hit?.GameDetail;
        }

        public GameItem(GameDetail localGame)
        {
            GameDetail = localGame;
        }

        public string Id { get; set; }
        public string Name => OnlineGame != null ? OnlineGame.Name : GameDetail.Game.Name;
        public string Manufacturer => OnlineGame != null ? OnlineGame.Name : GameDetail.Game.Name;
        public string Year => OnlineGame != null ? OnlineGame.YearString : GameDetail.Game.Year;
        public string Type => OnlineGame != null ? OnlineGame.Type : GameDetail.Game.Type;
        public DateTime? UpdatedAt => OnlineGame?.UpdatedAt;    // not supported in local DB GameDetail

        public GameDetail GameDetail { get; }
        public OnlineGame OnlineGame { get; }
    }
}
