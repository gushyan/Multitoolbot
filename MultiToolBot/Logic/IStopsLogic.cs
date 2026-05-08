using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Multitoolbot.Logic
{
    public interface IStopsLogic
    {
        public List<ExtStopPlace> SearchStops(string text);

        public List<IGrouping<string, ExtStopPlace>> SearchGroupStops(string text);

        public string FormatArrivalMessage(ArrivalResponse response, string stopName, string note);
    }
}
