using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //אילוצי כמות קבוצות
    public class GroupCountConstraint
    {
        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // כמות קבוצות מינימלית
        public int MinGroups { get; set; }

        // כמות קבוצות מקסימלית
        public int MaxGroups { get; set; }
    }
}
