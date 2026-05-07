using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //קבוצת ניהול
    public class ManagementGroup
    {
        // קוד קבוצת ניהול - מפתח ראשי
        public int ManagementGroupId { get; set; }

        // קוד מנהל - מפתח זר
        public int ManagerId { get; set; }

        // שם קבוצת ניהול
        public string ManagementGroupName { get; set; }
    }
}
