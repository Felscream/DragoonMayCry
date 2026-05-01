using System.Collections.Generic;

namespace DragoonMayCry.Record.Model
{
    public class JobRecord
    {
        public JobRecord()
        {
            Record = [];
            EmdRecord = [];
        }
        public Dictionary<uint, DutyRecord> Record { get; private set; }
        public Dictionary<uint, DutyRecord> EmdRecord { get; private set; }

        public void UpdateEmdRecord(ushort dutyId, DutyRecord dutyRecord)
        {
            if (EmdRecord.ContainsKey(dutyId))
            {
                EmdRecord[dutyId] = dutyRecord;
            }
            else
            {
                EmdRecord.Add(dutyId, dutyRecord);
            }
        }

        public void UpdateRecord(ushort dutyId, DutyRecord dutyRecord)
        {
            if (Record.ContainsKey(dutyId))
            {
                Record[dutyId] = dutyRecord;
            }
            else
            {
                Record.Add(dutyId, dutyRecord);
            }
        }
    }
}
