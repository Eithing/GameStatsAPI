namespace GameStatsAPI.Models
{
    public class TopUsersResponse
    {
        public ulong UserId { get; set; }

        public string MostPlayedGameName { get; set; }

        public double Duration1Game { get; set; }

        public double TotalDuration { get; set; }

        public List<UserGameActivityDetail> GameList { get; set; }
    }

    public class UserGameActivityDetail
    {
        public string GameName { get; set; }
        public double Duration { get; set; }
    }
}
