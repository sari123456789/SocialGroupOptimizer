// =============================================================================

// Program.cs — נקודת הכניסה של שכבת ה-API.

// =============================================================================

// תפקיד הקובץ:

// 1) לקרוא הגדרות מ-appsettings.json

// 2) לרשום שירותים (Dependency Injection)

// 3) להגדיר אימות JWT, CORS ו-Swagger

// 4) לבנות את pipeline של ASP.NET Core ולהפעיל את השרת

//

// תחביר "var builder = WebApplication.CreateBuilder(args)":

// יוצר אובייקט בנייה ל-WebApplication. args מגיע משורת הפקודה dotnet run.

// =============================================================================



using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using MyProject.API.Auth;

using MyProject.API.Configuration;

using MyProject.API.Placement;

using MyProject.API.Placement.Excel;

using MyProject.BL.Algorithm.InitialPlacement.Orchestration;

using MyProject.BL.Algorithm.LocalSearch.Engine;
using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.Execution;
using MyProject.BL.Algorithm.LocalSearch.Generation;
using MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;
using MyProject.BL.Algorithm.LocalSearch.Selection;

using MyProject.BL.Logic.Configuration;

using MyProject.BL.Logic.Constraints;

using MyProject.BL.Logic.Scoring;

using MyProject.Core.Domain.Services;

using MyProject.Data;

using MyProject.Data.Import;



var builder = WebApplication.CreateBuilder(args);



// ===== שלב 1: חיבור למסד =====

// GetConnectionString קורא מ-Configuration את המפתח "DefaultConnection".

// תחביר "?? throw" אומר: אם החיבור חסר — עוצרים מיד עם חריגה ברורה.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")

    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");



// AddApplicationPersistence מוגדר ב-MyProject.Data/ServiceCollectionExtensions.cs

// ורושם ApplicationDbContext עם SQL Server.

builder.Services.AddApplicationPersistence(connectionString);



// ===== שלב 2: רישום שירותי Placement (Scoped) =====

// Scoped = מופע חדש לכל בקשת HTTP. מתאים לשירותים שמשתמשים ב-DbContext.

builder.Services.AddScoped<AssignmentPlacementLoader>();

builder.Services.AddScoped<AssignmentParticipantsLoader>();

builder.Services.AddScoped<AssignmentDetailLoader>();

builder.Services.AddScoped<AssignmentEditorService>();

builder.Services.AddScoped<AssignmentInitialPlacementValidator>();

builder.Services.AddScoped<AssignmentSingleSheetImporter>();

builder.Services.AddScoped<ParticipantExcelImporter>();

builder.Services.AddScoped<ParticipantsExcelWorkbookReader>();

builder.Services.AddScoped<ParticipantsExcelValidator>();

builder.Services.AddScoped<ParticipantsExcelToInitialPlacementMapper>();



// ===== שלב 3: אימות =====

// AuthService — בודק שם+סיסמה מול המסד (BCrypt).

// JwtTokenService — מנפיק JWT לאחר התחברות מוצלחת.

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<JwtTokenService>();



// Configure<T> קושר קטע הגדרות מ-appsettings ל-JwtSettings (Key, Issuer, Audience...).

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));



// ===== שלב 4: רישום שכבת BL (Singleton) =====

// Singleton = מופע אחד לכל חיי האפליקציה. מתאים למנועים stateless.

// AlgorithmSettingsRegistration קורא את קטע "Algorithm" מהגדרות.

builder.Services.AddSingleton(AlgorithmSettingsRegistration.BindFromConfiguration(builder.Configuration));



// IAssignmentValidator הוא ממשק Core; ConstraintEngine הוא המימוש ב-BL.

// כך האלגוריתם מאמת אילוצים דרך ממשק אחיד — לא ישירות מול API.

builder.Services.AddSingleton<IAssignmentValidator, ConstraintEngine>();

// IAssignmentScorer — מימוש ScoringManager; נדרש ל-MoveEvaluator ול-Local Search עתידי.

builder.Services.AddSingleton<IAssignmentScorer, ScoringManager>();

// Local Search — הערכת מועמדי move (לא מתזמר / לא SearchStrategy).

builder.Services.AddSingleton<IMoveEvaluator, MoveEvaluator>();

builder.Services.AddSingleton<IMoveEvaluationBatch, MoveEvaluationBatch>();

builder.Services.AddSingleton<ISearchStrategy, BestImprovementSearchStrategy>();

builder.Services.AddSingleton<IMoveExecutor, MoveExecutor>();

// Local Search — יצירת מועמדים ומנוע חיפוש.

builder.Services.AddSingleton<IMoveCandidateStrategy, IsolatedParticipantStrategy>();
builder.Services.AddSingleton<IMoveCandidateStrategy, NearMissStrategy>();
builder.Services.AddSingleton<IMoveCandidateStrategy, LowScoreGroupStrategy>();
builder.Services.AddSingleton<IMoveCandidateStrategy, LowContributionStrategy>();
builder.Services.AddSingleton<IMoveCandidateStrategy, ControlledRandomStrategy>();
builder.Services.AddSingleton<MoveGenerationPolicy>();
builder.Services.AddSingleton<SwapMoveGenerator>();
builder.Services.AddSingleton<ILocalSearchEngine, LocalSearchEngine>();



// InitialPlacementOrchestrator מתזמר את תהליך החלוקה הראשונית ב-BL.

builder.Services.AddSingleton<InitialPlacementOrchestrator>();



// ===== שלב 5: הגדרת JWT Bearer =====

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()

    ?? throw new InvalidOperationException("Jwt settings are not configured.");



builder.Services

    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        // TokenValidationParameters מגדיר מה לבדוק בכל בקשה עם כותרת Authorization: Bearer ...

        options.TokenValidationParameters = new TokenValidationParameters

        {

            ValidateIssuer = true,           // מי הנפיק את הטוקן

            ValidateAudience = true,         // למי הטוקן מיועד (הלקוח)

            ValidateLifetime = true,         // האם פג תוקף

            ValidateIssuerSigningKey = true, // האם החתימה תקינה

            ValidIssuer = jwtSettings.Issuer,

            ValidAudience = jwtSettings.Audience,

            // SymmetricSecurityKey — מפתח סודי משותף לחתימה ולאימות (HMAC).

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

        };

    });



// AddAuthorization מאפשר שימוש ב-[Authorize] על בקרים.

builder.Services.AddAuthorization();



builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();



// ===== שלב 6: CORS =====

// CORS (Cross-Origin Resource Sharing) — מאפשר ללקוח React (פורט 5173/5174)

// לשלוח בקשות ל-API (פורט 5256) מהדפדפן.

// WithoutOrigins — רק כתובות localhost מפורשות מורשות (לא "*" ב-production).

builder.Services.AddCors(options =>

{

    options.AddDefaultPolicy(policy =>

    {

        policy.WithOrigins(

                "http://localhost:5173",

                "https://localhost:5173",

                "http://localhost:5174",

                "https://localhost:5174")

            .AllowAnyHeader()   // מאפשר כותרת Authorization

            .AllowAnyMethod();  // GET, POST, PUT, DELETE...

    });

});



// Build() סוגר את שלב הרישום ויוצר WebApplication מוכן להרצה.

var app = builder.Build();



// ===== שלב 7: אתחול Development בלבד =====

if (app.Environment.IsDevelopment())

{

    // CreateScope יוצר Scope DI זמני — DbContext חי רק בתוך הבלוק.

    using (var scope = app.Services.CreateScope())

    {

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Migrate() מריץ מיגרציות EF שלא הוחלו עדיין.

        db.Database.Migrate();

        // SeedAsync טוען נתוני דמו (מנהל, משתתפים לדוגמה) אם המסד ריק.

        await DevelopmentDataSeeder.SeedAsync(db);

    }



    app.UseSwagger();

    app.UseSwaggerUI();

}



// ===== שלב 8: Middleware pipeline =====

// הסדר חשוב: CORS לפני Auth, Auth לפני MapControllers.

app.UseHttpsRedirection(); // מפנה HTTP→HTTPS כשמוגדר

app.UseCors();

app.UseAuthentication();   // קורא Bearer token וממלא HttpContext.User

app.UseAuthorization();    // בודק [Authorize]



// MapControllers מחבר את כל הבקרים (Controllers) לנתיבי URL.

app.MapControllers();



app.Run();


