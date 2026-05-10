using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramStopBot.Logic
{
    public interface IStopsTelegramFormatter
    {
        public string FormatArrivalMessage(ArrivalResponse response, string stopName, string note);
    }
}
