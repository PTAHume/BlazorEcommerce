using BlazorEcommerce.Server.Services.StatsService;
using Microsoft.AspNetCore.Mvc;

namespace BlazorEcommerce.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : Controller
    {
        private IStatsService _statsService;

        public StatsController(IStatsService statsService)
        {
            _statsService = statsService;
        }

        [HttpGet]
        public async Task<ActionResult<int>> GetVisits()
        {
            return await _statsService.GetVisits();
        }

        [HttpPost]
        public async Task IncrementVisits()
        {
            await _statsService.IncrementVisits();
        }
    }
}
