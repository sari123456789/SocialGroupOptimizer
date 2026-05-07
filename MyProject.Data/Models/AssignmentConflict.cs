using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //קונפליקטים ובעיות
    public class AssignmentConflict
    {
        // קוד שיבוץ - מפתח זר
        public int AssignmentId { get; set; }

        // סוג קונפליקט (למשל: סתירת העדפות / אילוץ קשיח)
        public string ConflictType { get; set; }

        // תיאור הקונפליקט בפירוט
        public string Description { get; set; }

        // חומרת הבעיה (אזהרה / קריטי)
        public string Severity { get; set; }

        // המלצת מערכת לפתרון
        public string SystemRecommendation { get; set; }
    }
}
