using Blazored.LocalStorage;

namespace BlazorEcommerce.Client.Services.StatsService
{


    public class StatsService : IStatsService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorageService;

        public StatsService(HttpClient http, ILocalStorageService localStorageService)
        {
            _http = http;
            _localStorageService = localStorageService;
        }

        public event Action OnChange;

        public async Task<int> GetVisits()
        {
            var visits = int.Parse(await _http.GetStringAsync("api/Stats"));
            OnChange.Invoke();
            return visits;
        }

        public async Task IncrementVisits()
        {
            DateTime? lastVisit = await _localStorageService.GetItemAsync<DateTime?>("lastVisit");

            if (lastVisit == null || ((DateTime)lastVisit).Date != DateTime.Now.Date)
            {
                await _http.PostAsync("api/Stats", null);
                await _localStorageService.SetItemAsync<DateTime>("lastVisit", DateTime.Now);
            }
        }
    }
}
