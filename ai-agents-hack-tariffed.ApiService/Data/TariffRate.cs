using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_agents_hack_tariffed.ApiService.Data
{
    [Table("TariffRate")]
    public class TariffRate
    {
        [Column("ID")]
        public int Id { get; set; }
        [Column("Country")]
        public string Country { get; set; } = string.Empty;
        [Column("PercentOfTrade")]
        public string PercentOfTrade { get; set; } = string.Empty;
        [Column("PreviousRate")]
        public string PreviousRate { get; set; } = string.Empty;
        [Column("UpdatedRate")]
        public string UpdatedRate { get; set; } = string.Empty;
        [Column("FlagColor1")]
        public string FlagColor1 { get; set; } = string.Empty;
        [Column("FlagColor2")]
        public string FlagColor2 { get; set; } = string.Empty;
        [Column("FlagColor3")]
        public string FlagColor3 { get; set; } = string.Empty;
    }
}
