using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //פרוט ציוני השיבוץ
    public class AssignmentScoreBreakdown
    {
        // קוד שיבוץ - מפתח זר לטבלת השיבוצים
        public int AssignmentId { get; set; }

        // ציון חברתי - תוצאה של חישוב העדפות חברתיות
        public double SocialScore { get; set; }

        // קנס איזון - פגיעה באיזון הקבוצות (למשל לפי סיווגים)
        public double BalancePenalty { get; set; }

        // קנס בידוד - עונש על משתתפים מבודדים
        public double IsolationPenalty { get; set; }

        // ציון סופי - שילוב כל המרכיבים לציון אחד
        public double FinalSigma { get; set; }

        // פירוט מילולי - הסבר טקסטואלי לציון
        public string ScoreExplanation { get; set; }
    }
}
