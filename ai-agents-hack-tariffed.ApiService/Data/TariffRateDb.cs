using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_agents_hack_tariffed.ApiService.Data
{
    public class TariffRateDb : DbContext
    {
        public TariffRateDb(DbContextOptions<TariffRateDb> options)
            : base(options) { }

        public DbSet<TariffRate> TariffRates => Set<TariffRate>();
    }
}
