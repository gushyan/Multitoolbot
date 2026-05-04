using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Multitoolbot
{
    public interface IStopLogic
    {
        public List<ExtStopPlace> SearchStops(string text);

        public string EditNamesStops(string cleanNote, string stopName);

        public string FormatArrivalMessage(ArrivalResponse response, string stopName);
    }
}
