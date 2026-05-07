using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Data.Models
{
    //ארכיון שיאים
    public class BestSolutionArchive
    {
        // קוד ריצה - מזהה ייחודי לכל הרצת אלגוריתם
        public int RunId { get; set; }

        // מספר איטרציה - באיזה שלב נמצא השיא
        public int IterationNumber { get; set; }

        // ציון שיא - הציון הטוב ביותר שהושג
        public double BestScore { get; set; }

        // מצב פתרון - שמירת כל מבנה החלוקה (למשל JSON / BLOB)
        public string SolutionStateBlob { get; set; }

        // האם בוצע ניעור (Diversification) לפני מציאת הפתרון
        public bool WasDiversified { get; set; }
    }
}
