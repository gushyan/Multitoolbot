using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto
{
    public record StopPlaceUpdateNameRequest(string OldName, string NewName);
}
