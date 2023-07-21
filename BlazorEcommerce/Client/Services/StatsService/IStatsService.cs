namespace BlazorEcommerce.Client.Services.StatsService
{
    public interface IStatsService
    {
        event Action OnChange;
        Task<int> GetVisits();
        Task IncrementVisits();
    }
}