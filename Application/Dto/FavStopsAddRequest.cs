using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto
{
    public record class FavStopsAddRequest(long ChatId, int StopId);
}
