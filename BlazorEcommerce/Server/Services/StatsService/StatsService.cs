namespace BlazorEcommerce.Server.Services.StatsService
{
    public class StatsService : IStatsService
    {
        private DataContext _context;

        public StatsService(DataContext context)
        {
            _context = context;
        }

        public async Task<int> GetVisits()
        {
            Stats? stats = await _context.Stats.FirstOrDefaultAsync();
            if (stats == null)
            {
                return 0;
            }
            return stats.Visits;
        }

        public async Task IncrementVisits()
        {
            Stats? stats = await _context.Stats.FirstOrDefaultAsync();
            if (stats == null)
            {
                _context.Stats.Add(new Stats() { lastVisit = DateTime.Now, Visits = 1 });
            }
            else
            {
                stats.Visits++;
                stats.lastVisit = DateTime.Now;

            }
            await _context.SaveChangesAsync();
        }
    }
}
