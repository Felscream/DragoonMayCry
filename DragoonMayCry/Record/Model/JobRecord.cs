using System.Collections.Generic;

namespace DragoonMayCry.Record.Model
{
    public class JobRecord
    {
        public Dictionary<ushort, DutyRecord> Record { get; private set; }
        public Dictionary<ushort, DutyRecord> EmdRecord { get; private set; }
        public JobRecord()
        {
            Record = [];
            EmdRecord = [];
        }

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
