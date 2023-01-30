using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStatsAPI.Models
{
    public class GameActivity
    {
        public int id { get; set; }

        public ulong user_id { get; set; }

        public string game_name { get; set; }

        public DateTime start_date { get; set; }

        public DateTime end_date { get; set; }

        [NotMapped]
        public double DurationMinutes => (end_date - start_date).TotalMinutes;
    }
}
