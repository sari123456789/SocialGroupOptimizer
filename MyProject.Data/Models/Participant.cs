using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //משתתף
    public class Participant
    {
        // קוד משתתף - מפתח ראשי
        public int ParticipantId { get; set; }

        /// <summary>
        /// מספר זהות ישראלי (תשע ספרות). משמש למיפוי ל-<see cref="MyProject.Core.Domain.ValueObjects.ParticipantId"/>.
        /// </summary>
        public string IsraeliIdentityNumber { get; set; } = string.Empty;

        // שם משתתף
        public string ParticipantName { get; set; }
    }
}
