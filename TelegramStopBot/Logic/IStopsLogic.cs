using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramStopBot.Logic
{
    public interface IStopsLogic
    {
        public string FormatArrivalMessage(ArrivalResponse response, string stopName, string note);
    }
}
