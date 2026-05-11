using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{

    public class FavStops
    {
        public long ChatId { get; set; }
        public int StopId { get; set; }
    }
}
