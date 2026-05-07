using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //אילוצי סיווגים לשיבוץ
    public class AssignmentClassificationConstraint
    {
        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // קוד סיווג - מפתח זר
        public int ClassificationId { get; set; }

        // סיווג איזון או הפרדה
        public bool IsBalanceOrSeparation { get; set; }
    }
}
