using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //אילוצי גודל קבוצה
    public class GroupSizeConstraint
    {
        // קוד קבוצה - מפתח זר
        public int GroupId { get; set; }

        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // גודל מינימלי לקבוצה
        public int MinGroupSize { get; set; }

        // גודל מקסימלי לקבוצה
        public int MaxGroupSize { get; set; }
    }
}
