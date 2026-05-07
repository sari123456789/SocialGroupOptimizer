using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //העדפות חברתיות
    public class SocialPreference
    {
        // קוד משתתף לשיבוץ - מכניס ההעדפה (מפתח זר)
        public int FromParticipantAssignmentId { get; set; }

        // קוד משתתף לשיבוץ - משתתף מועדף (מפתח זר)
        public int ToParticipantAssignmentId { get; set; }

        // משקל ההעדפה
        public int PreferenceWeight { get; set; }

        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }
    }
}
