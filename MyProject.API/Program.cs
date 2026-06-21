// 
// Program.cs — נקודת הכניסה של שכבת ה-API.
// 
// תפקיד הקובץ:
// 1) לקרוא הגדרות מ-appsettings.json
// 2) לרשום שירותים (Dependency Injection)
// 3) להגדיר אימות JWT ו-CORS
// 4) לבנות את pipeline של ASP.NET Core ולהפעיל את השרת
// 

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyProject.API.Auth;
using MyProject.API.Configuration;
using MyProject.API.Placement;
using MyProject.API.Placement.Excel;
using MyProject.BL.Algorithm.Improvement;
using MyProject.BL.Algorithm.InitialPlacement.Orchestration;
using MyProject.BL.Algorithm.LocalSearch.Engine;
using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.Execution;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Strategies;
using MyProject.BL.Algorithm.LocalSearch.Generation;
using MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;
using MyProject.BL.Algorithm.LocalSearch.Selection;
using MyProject.BL.Logic.Configuration;
using MyProject.BL.Logic.Constraints;
using MyProject.BL.Logic.Scoring;
using MyProject.BL.ManualMoves;
using MyProject.BL.Transparency;
using MyProject.Core.Domain.Services;
using MyProject.Data;
using MyProject.Data.Import;

// var — משתנה עם הסקת טיפוס; WebApplication.CreateBuilder יוצר אובייקט בנייה; args הוא מערך ארגומנטים משורת הפקודה
var builder = WebApplication.CreateBuilder(args);

//   שלב 1: חיבור למסד  
// var — קורא מחרוזת חיבור מה-Configuration לפי המפתח "DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    // ?? throw — אם התוצאה null, זורק InvalidOperationException במקום להמשיך עם ערך חסר
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

// builder.Services — רושם ApplicationDbContext ו-SQL Server דרך הרחבה מ-MyProject.Data
builder.Services.AddApplicationPersistence(connectionString);

//   שלב 2: רישום שירותי Placement (Scoped)  
// AddScoped<T> — מופע חדש של T לכל בקשת HTTP (מתאים לשירותים עם DbContext)
builder.Services.AddScoped<AssignmentPlacementLoader>();
// AddScoped — טוען תוצאות שיבוץ קיימות מהמסד
builder.Services.AddScoped<AssignmentPlacementRunner>();
// AddScoped — טוען רשימת משתתפים לשיבוץ
builder.Services.AddScoped<AssignmentParticipantsLoader>();
// AddScoped — טוען פרטי שיבוץ בודד
builder.Services.AddScoped<AssignmentDetailLoader>();
// AddScoped — שירות עריכת שיבוץ (CRUD על משתתפים וקבוצות)
builder.Services.AddScoped<AssignmentEditorService>();
// AddScoped — מאמת נתונים לפני חלוקה ראשונית
builder.Services.AddScoped<AssignmentInitialPlacementValidator>();
// AddScoped — טוען הסברי שיבוץ ל-API
builder.Services.AddScoped<AssignmentExplanationLoader>();
// AddScoped — מטפל בהעברות/החלפות ידניות
builder.Services.AddScoped<AssignmentManualMoveService>();
// AddScoped — מייבא גיליון Excel יחיד לשיבוץ
builder.Services.AddScoped<AssignmentSingleSheetImporter>();
// AddScoped — קורא חוברת Excel של משתתפים
builder.Services.AddScoped<ParticipantsExcelWorkbookReader>();
// AddScoped — מאמת מבנה ותוכן Excel
builder.Services.AddScoped<ParticipantsExcelValidator>();

// ===== שלב 3: אימות =====
// AddScoped — שירות התחברות (בדיקת סיסמה מול המסד)
builder.Services.AddScoped<AuthService>();
// AddScoped — הנפקת JWT לאחר התחברות מוצלחת
builder.Services.AddScoped<JwtTokenService>();
// Configure<T> — קושר קטע הגדרות מ-appsettings לאובייקט JwtSettings (Options pattern)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// ===== שלב 4: רישום שכבת BL (Singleton) =====
// AddSingleton — מופע יחיד לכל חיי האפליקציה; BindFromConfiguration קורא קטע "Algorithm"
builder.Services.AddSingleton(AlgorithmSettingsRegistration.BindFromConfiguration(builder.Configuration));
// AddSingleton<I,T> — רושם ConstraintEngine כמימוש של IAssignmentValidator
builder.Services.AddSingleton<IAssignmentValidator, ConstraintEngine>();
// AddSingleton<I,T> — רושם ScoringManager כמימוש של IAssignmentScorer
builder.Services.AddSingleton<IAssignmentScorer, ScoringManager>();
// AddSingleton<I,T> — שירות הסבר שיבוץ (קריאה בלבד)
builder.Services.AddSingleton<IAssignmentExplanationService, AssignmentExplanationService>();
// AddSingleton<I,T> — מעריך מהלכים ידניים (Swap/Transfer)
builder.Services.AddSingleton<IManualMoveEvaluator, ManualMoveEvaluator>();
// AddSingleton<I,T> — מעריך ציון של מהלך בודד ב-Local Search
builder.Services.AddSingleton<IMoveEvaluator, MoveEvaluator>();
// AddSingleton<I,T> — מעריך אצווה של מועמדי מהלכים
builder.Services.AddSingleton<IMoveEvaluationBatch, MoveEvaluationBatch>();
// AddSingleton<I,T> — אסטרטגיית בחירה: המהלך עם השיפור הטוב ביותר
builder.Services.AddSingleton<ISearchStrategy, BestImprovementSearchStrategy>();
// AddSingleton<I,T> — מבצע מהלך על מצב השיבוץ בפועל
builder.Services.AddSingleton<IMoveExecutor, MoveExecutor>();
// AddSingleton<I,T> — אסטרטגיית מועמדים: משתתף מבודד
builder.Services.AddSingleton<IMoveCandidateStrategy, IsolatedParticipantStrategy>();
// AddSingleton<I,T> — אסטרטגיית מועמדים: כמעט-פגיעה באילוץ
builder.Services.AddSingleton<IMoveCandidateStrategy, NearMissStrategy>();
// AddSingleton<I,T> — אסטרטגיית מועמדים: קבוצה עם ציון נמוך
builder.Services.AddSingleton<IMoveCandidateStrategy, LowScoreGroupStrategy>();
// AddSingleton<I,T> — אסטרטגיית מועמדים: תרומה נמוכה לקבוצה
builder.Services.AddSingleton<IMoveCandidateStrategy, LowContributionStrategy>();
// AddSingleton<I,T> — אסטרטגיית מועמדים: אקראי מבוקר
builder.Services.AddSingleton<IMoveCandidateStrategy, ControlledRandomStrategy>();
// AddSingleton — מדיניות שילוב אסטרטגיות יצירת מועמדים
builder.Services.AddSingleton<MoveGenerationPolicy>();
// AddSingleton — מחולל מהלכי החלפה (Swap) בין משתתפים
builder.Services.AddSingleton<SwapMoveGenerator>();
// AddSingleton<I,T> — מנוע Local Search המלא
builder.Services.AddSingleton<ILocalSearchEngine, LocalSearchEngine>();
// AddSingleton — מעריך איזון קבוצות (fallback אחרי Local Search)
builder.Services.AddSingleton<GroupRebalanceEvaluator>();
// AddSingleton — אסטרטגיית פיצול אשכול לאיזון קבוצות
builder.Services.AddSingleton<SplitClusterRebalanceStrategy>();
// AddSingleton עם lambda _ => — יוצר מופע חדש של ScoringWeights ללא תלות ב-DI
builder.Services.AddSingleton(_ => new ScoringWeights());
// AddSingleton<I,T> — מתזמר שיפור שיבוץ (Local Search + GroupRebalance)
builder.Services.AddSingleton<IAssignmentImprovementOrchestrator, AssignmentImprovementOrchestrator>();
// AddSingleton — מתזמר תהליך החלוקה הראשונית
builder.Services.AddSingleton<InitialPlacementOrchestrator>();

// ===== שלב 5: הגדרת JWT Bearer =====
// var — קורא הגדרות JWT מה-Configuration וממיר ל-JwtSettings; Get<T>() מבצע deserialization
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    // ?? throw — אם ההגדרות חסרות, עוצרים עם חריגה ברורה
    ?? throw new InvalidOperationException("Jwt settings are not configured.");

// Fluent API — מתחיל רישום אימות; AddAuthentication מגדיר סכימת ברירת מחדל
builder.Services
    // AddAuthentication — בוחר סכימת JWT Bearer כברירת מחדל
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // AddJwtBearer — מגדיר אימות לפי טוקן Bearer בכותרת Authorization
    .AddJwtBearer(options =>
    {
        // options.TokenValidationParameters — אובייקט שמגדיר מה לבדוק בכל בקשה עם טוקן
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // property assignment — חייב לבדוק שה-Issuer בטוקן תואם
            ValidateIssuer = true,
            // property assignment — חייב לבדוק שה-Audience בטוקן תואם
            ValidateAudience = true,
            // property assignment — חייב לבדוק שתוקף הטוקן לא פג
            ValidateLifetime = true,
            // property assignment — חייב לאמת את חתימת הטוקן
            ValidateIssuerSigningKey = true,
            // ValidIssuer — ערך Issuer מותר מההגדרות
            ValidIssuer = jwtSettings.Issuer,
            // ValidAudience — ערך Audience מותר מההגדרות
            ValidAudience = jwtSettings.Audience,
            // new SymmetricSecurityKey — מפתח סימטרי מ-bytes של המפתח הסודי (HMAC)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };
    });

// AddAuthorization — מאפשר שימוש ב-[Authorize] על בקרים ופעולות
builder.Services.AddAuthorization();
// AddControllers — רושם MVC Controllers ומפעיל model binding / validation
builder.Services.AddControllers();

// ===== שלב 6: CORS =====
// AddCors — רושם מדיניות CORS; lambda options => מגדיר את האפשרויות
builder.Services.AddCors(options =>
{
    // AddDefaultPolicy — מדינית ברירת מחדל לכל הבקשות
    options.AddDefaultPolicy(policy =>
    {
        // WithOrigins — רשימת כתובות מקור מורשות (React בפיתוח)
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174")
            // AllowAnyHeader — מאפשר כל כותרת (כולל Authorization)
            .AllowAnyHeader()
            // AllowAnyMethod — מאפשר GET, POST, PUT, DELETE וכו'
            .AllowAnyMethod();
    });
});

// var — Build() סוגר רישום שירותים ויוצר WebApplication מוכן להרצה
var app = builder.Build();

// ===== שלב 7: אתחול Development בלבד =====
// if — בודק אם סביבת הריצה היא Development (מ-appsettings / משתנה סביבה)
if (app.Environment.IsDevelopment())
{
    // using — יוצר Scope DI זמני; var scope — משתנה שיושמד בסוף הבלוק (IDisposable)
    using (var scope = app.Services.CreateScope())
    {
        // var — GetRequiredService מחזיר DbContext מה-Scope; זורק אם לא רשום
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Migrate() — מריץ מיגרציות EF שלא הוחלו על המסד
        db.Database.Migrate();
        // await — ממתין לסיום טעינת נתוני דמו (Seed) באופן אסינכרוני
        await DevelopmentDataSeeder.SeedAsync(db);
    }
}

// ===== שלב 8: Middleware pipeline =====
// UseHttpsRedirection — middleware שמפנה בקשות HTTP ל-HTTPS
app.UseHttpsRedirection();
// UseCors — middleware שמוסיף כותרות CORS לתשובות
app.UseCors();
// UseAuthentication — middleware שקורא Bearer token וממלא HttpContext.User
app.UseAuthentication();
// UseAuthorization — middleware שבודק הרשאות ו-[Authorize]
app.UseAuthorization();
// MapControllers — מחבר את כל ה-Controllers לנתיבי URL
app.MapControllers();
// Run() — מתחיל להאזין לבקשות HTTP וחוסם עד כיבוי השרת
app.Run();