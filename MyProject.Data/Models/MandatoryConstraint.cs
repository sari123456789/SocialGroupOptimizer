using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //אילוצי חובה ואסור
    public class MandatoryConstraint
    {
        // קוד אילוץ חובה/אסור - מפתח ראשי
        public int MandatoryConstraintId { get; set; }

        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // שם אילוץ
        public string ConstraintName { get; set; }

        // אילוץ חובה או איסור
        public bool IsMandatory { get; set; }
    }
}

