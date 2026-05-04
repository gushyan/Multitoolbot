using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{

    public class StopPlace
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Note { get; set; }
    }
}
