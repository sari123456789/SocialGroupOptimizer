using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //סיווגים למשתתפים
    public class ParticipantClassification
    {
        // קוד משתתף - מפתח זר
        public int ParticipantId { get; set; }

        // קוד מאפיין הסיווג - מפתח זר
        public int ClassificationAttributeId { get; set; }
    }
}
