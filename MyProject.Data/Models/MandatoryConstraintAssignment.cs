using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //שיבוץ באילוצי חובה או אסור
    public class MandatoryConstraintAssignment
    {
        // קוד אילוץ חובה/אסור - מפתח זר
        public int MandatoryConstraintId { get; set; }

        // קוד משתתף משובץ לאילוץ - מפתח זר
        public int ParticipantAssignmentId { get; set; }
    }
}
