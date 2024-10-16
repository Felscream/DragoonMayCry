using DragoonMayCry.Score.Model;
using Newtonsoft.Json;
using System;

namespace DragoonMayCry.Record.Model
{
    public class DutyRecord
    {
        public StyleType Result { get; private set; }
        public TimeSpan KillTime { get; private set; }
        public DateOnly Date { get; private set; }
        public DutyRecord(StyleType result, TimeSpan killTime)
        {
            Result = result;
            KillTime = killTime;
            Date = DateOnly.FromDateTime(DateTime.Now);
        }

        [JsonConstructor]
        public DutyRecord(StyleType result, TimeSpan killTime, DateOnly date)
        {
            Result = result;
            KillTime = killTime;
            Date = date;
        }
    }
}
