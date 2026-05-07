using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //מאפייני סיווגים
    public class ClassificationAttribute
    {
        // קוד מאפיין הסיווג - מפתח ראשי
        public int ClassificationAttributeId { get; set; }

        // קוד סיווג - מפתח זר
        public int ClassificationId { get; set; }

        // מאפיין הסיווג
        public string AttributeName { get; set; }
    }
}
