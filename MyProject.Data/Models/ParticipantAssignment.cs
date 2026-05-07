using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //שיבוץ למשתתף
    public class ParticipantAssignment
    {
        // קוד משתתף לשיבוץ - מפתח ראשי
        public int ParticipantAssignmentId { get; set; }

        // קוד משתתף - מפתח זר
        public int ParticipantId { get; set; }

        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // קוד מנהל - מפתח זר
        public int ManagerId { get; set; }
        // תרומת המשתתף לאיזון הקבוצה (חיובי/שלילי)
        public double BalanceContribution { get; set; }

    }
}
