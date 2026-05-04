using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto
{
    public record class StopPlaceAddRequest(int id, string Name, long Note);
}
