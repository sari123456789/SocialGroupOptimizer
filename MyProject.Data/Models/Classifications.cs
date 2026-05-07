using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //סיווגים
    public class Classification
    {
        // קוד סיווג - מפתח ראשי
        public int ClassificationId { get; set; }

        // שם הסיווג
        public string ClassificationName { get; set; }
    }
}
