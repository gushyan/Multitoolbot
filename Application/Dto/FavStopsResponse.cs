using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto
{
    public record FavStopsResponse(long chatId, List<int> stopIds);

}
