using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto
{
    public record FavStopsDeleteRequest(long ChatId, int StopId);
}
