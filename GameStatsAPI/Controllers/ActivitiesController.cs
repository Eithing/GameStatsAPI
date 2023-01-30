using GameStatsAPI.DBContexts;
using GameStatsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Timers;

namespace GameStatsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActivitiesController : ControllerBase
    {
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly string activities = "activities";
        private IEnumerable<GameActivity> activitiesCollection;
        private MyDBContext _myDbContext;

        public ActivitiesController(ILogger<ActivitiesController> logger, IMemoryCache memoryCache, MyDBContext context)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _myDbContext = context;

            activitiesCollection = GetActivities();

            var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1));

            _memoryCache.Set(activities, activitiesCollection, cacheOptions);

            System.Timers.Timer aTimer = new System.Timers.Timer(60 * 60 * 1000); //one hour in milliseconds
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Start();
        }

        [HttpGet]
        public IEnumerable<GameActivity> GetAll()
        {
            if (_memoryCache.TryGetValue(activities, out activitiesCollection))
            {
                return activitiesCollection;
            }

            return null;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GameActivity> Create([FromBody]GameActivity gameActivity)
        {
            if(gameActivity != null)
            {
                if (this._myDbContext.GameActivity.Any(a => a.start_date == gameActivity.start_date && a.game_name == gameActivity.game_name && a.user_id == gameActivity.user_id))
                // on vérif si la session n'est pas déjà record (via la start date) si oui on met a jour la end date pour n'avoir qu'une ligne / session
                {
                    var toReplaceActivity = this._myDbContext.GameActivity.Where(a => a.start_date == gameActivity.start_date && a.game_name == gameActivity.game_name && a.user_id == gameActivity.user_id).FirstOrDefault();

                    var activity = this._myDbContext.GameActivity.Find(toReplaceActivity?.id);
                    activity.end_date = gameActivity.end_date;
                    this._myDbContext.SaveChanges();
                }
                else
                {
                    this._myDbContext.GameActivity.Add(gameActivity);
                    this._myDbContext.SaveChanges();
                }
            }
            
            return gameActivity;
        }

        [HttpGet]
        [Route("/top5Games")]
        public IEnumerable<KeyValuePair<string, double>> GetTop5Games(int day)
        {
            if (_memoryCache.TryGetValue(activities, out activitiesCollection))
            {
                Dictionary<string, double> uniqueGameListWithTime = new Dictionary<string, double>();
                foreach(var activities in activitiesCollection.Where(a => a.start_date > DateTime.Now.AddDays(-day)))
                {
                    if(uniqueGameListWithTime.ContainsKey(activities.game_name))
                    {
                        uniqueGameListWithTime[activities.game_name] = uniqueGameListWithTime[activities.game_name] + activities.DurationMinutes;
                    }
                    else
                    {
                        uniqueGameListWithTime.Add(activities.game_name, activities.DurationMinutes);
                    }
                }

                return uniqueGameListWithTime.OrderByDescending(g => g.Value).Take(5);
            }
            return null;
        }

        [HttpGet]
        [Route("/top5Users")]
        public IEnumerable<TopUsersResponse> GetTop5Users(int day)
        {
            if (_memoryCache.TryGetValue(activities, out activitiesCollection))
            {
                List<TopUsersResponse> topUsers = new List<TopUsersResponse>();
                foreach (var activities in activitiesCollection.Where(a => a.start_date > DateTime.Now.AddDays(-day)))
                {
                    if (topUsers.Any(u => u.UserId == activities.user_id)) //Si User existe dans mon cache
                    {
                        var user = topUsers.Where(u => u.UserId == activities.user_id).FirstOrDefault();
                        if(user != null)
                        {
                            user.TotalDuration += activities.DurationMinutes;
                            if (user.GameList.Any(u => u.GameName == activities.game_name)) //Si le jeu existe dans le user (GameList)
                            {
                                var gameList = user.GameList.Where(u => u.GameName == activities.game_name).FirstOrDefault();
                                gameList.Duration += activities.DurationMinutes;
                            }
                            else
                            {
                                user.GameList.Add(new UserGameActivityDetail { GameName = activities.game_name, Duration = activities.DurationMinutes});
                            }
                        }
                    }
                    else
                    {
                        topUsers.Add(new TopUsersResponse { UserId = activities.user_id, TotalDuration = activities.DurationMinutes, GameList = new List<UserGameActivityDetail> { new UserGameActivityDetail { GameName = activities.game_name, Duration = activities.DurationMinutes}}});
                    }
                }
                topUsers = topUsers.OrderByDescending(u => u.TotalDuration).Take(5).ToList();
                foreach (var user in topUsers)
                {
                    Dictionary<string, double> concatGame = new Dictionary<string, double>();
                    foreach(var game in user.GameList)
                    {
                        if (concatGame.ContainsKey(game.GameName))
                        {
                            concatGame[game.GameName] = concatGame[game.GameName] + game.Duration;
                        }
                        else
                        {
                            concatGame.Add(game.GameName, game.Duration);
                        }
                    }
                    var topgame = concatGame.OrderByDescending(g => g.Value).FirstOrDefault();
                    user.Duration1Game = topgame.Value;
                    user.MostPlayedGameName = topgame.Key;
                }
                return topUsers;
            }
            return null;
        }

        private IEnumerable<GameActivity> GetActivities()
        {
            return this._myDbContext.GameActivity.ToList();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            activitiesCollection = GetActivities();

            var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(15));

            _memoryCache.Set(activities, activitiesCollection, cacheOptions);
        }
    }
}
