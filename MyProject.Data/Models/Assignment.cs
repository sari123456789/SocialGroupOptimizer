using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //שיבוצים
    /// <summary>
    /// ישות נתונים המייצגת מסגרת שיבוץ או ריצה בתוך המערכת.
    /// זה אינו פתרון החלוקה לקבוצות מהשכבה הפנימית.
    /// </summary>
    public class Assignment
    {
        // קוד שיבוץ - מפתח ראשי
        public int AssignmentId { get; set; }

        // שם שיבוץ
        public string AssignmentName { get; set; }

        // קוד קבוצת ניהול - מפתח זר
        public int ManagementGroupId { get; set; }
    }
}
