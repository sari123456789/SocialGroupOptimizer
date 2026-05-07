using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //מנהל
    public class Manager
    {
        // קוד מנהל - מפתח ראשי
        public int ManagerId { get; set; }

        // שם מנהל
        public string ManagerName { get; set; }
    }
}
