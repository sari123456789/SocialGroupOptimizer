// =============================================================================

// Program.cs Γאפ ╫á╫º╫ץ╫ף╫¬ ╫פ╫¢╫á╫ש╫í╫פ ╫⌐╫£ ╫⌐╫¢╫ס╫¬ ╫פ-API.

// =============================================================================

// ╫¬╫ñ╫º╫ש╫ף ╫פ╫º╫ץ╫ס╫Ñ:

// 1) ╫£╫º╫¿╫ץ╫נ ╫פ╫ע╫ף╫¿╫ץ╫¬ ╫₧-appsettings.json

// 2) ╫£╫¿╫⌐╫ץ╫¥ ╫⌐╫ש╫¿╫ץ╫¬╫ש╫¥ (Dependency Injection)

// 3) ╫£╫פ╫ע╫ף╫ש╫¿ ╫נ╫ש╫₧╫ץ╫¬ JWT, CORS ╫ץ-Swagger

// 4) ╫£╫ס╫á╫ץ╫¬ ╫נ╫¬ pipeline ╫⌐╫£ ASP.NET Core ╫ץ╫£╫פ╫ñ╫ó╫ש╫£ ╫נ╫¬ ╫פ╫⌐╫¿╫¬

//

// ╫¬╫ק╫ס╫ש╫¿ "var builder = WebApplication.CreateBuilder(args)":

// ╫ש╫ץ╫ª╫¿ ╫נ╫ץ╫ס╫ש╫ש╫º╫ר ╫ס╫á╫ש╫ש╫פ ╫£-WebApplication. args ╫₧╫ע╫ש╫ó ╫₧╫⌐╫ץ╫¿╫¬ ╫פ╫ñ╫º╫ץ╫ף╫פ dotnet run.

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

using MyProject.BL.Algorithm.LocalSearch.Evaluation;

using MyProject.BL.Logic.Configuration;

using MyProject.BL.Logic.Constraints;

using MyProject.BL.Logic.Scoring;

using MyProject.Core.Domain.Services;

using MyProject.Data;

using MyProject.Data.Import;



var builder = WebApplication.CreateBuilder(args);



// ===== ╫⌐╫£╫ס 1: ╫ק╫ש╫ס╫ץ╫¿ ╫£╫₧╫í╫ף =====

// GetConnectionString ╫º╫ץ╫¿╫נ ╫₧-Configuration ╫נ╫¬ ╫פ╫₧╫ñ╫¬╫ק "DefaultConnection".

// ╫¬╫ק╫ס╫ש╫¿ "?? throw" ╫נ╫ץ╫₧╫¿: ╫נ╫¥ ╫פ╫ק╫ש╫ס╫ץ╫¿ ╫ק╫í╫¿ Γאפ ╫ó╫ץ╫ª╫¿╫ש╫¥ ╫₧╫ש╫ף ╫ó╫¥ ╫ק╫¿╫ש╫ע╫פ ╫ס╫¿╫ץ╫¿╫פ.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")

    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");



// AddApplicationPersistence ╫₧╫ץ╫ע╫ף╫¿ ╫ס-MyProject.Data/ServiceCollectionExtensions.cs

// ╫ץ╫¿╫ץ╫⌐╫¥ ApplicationDbContext ╫ó╫¥ SQL Server.

builder.Services.AddApplicationPersistence(connectionString);



// ===== ╫⌐╫£╫ס 2: ╫¿╫ש╫⌐╫ץ╫¥ ╫⌐╫ש╫¿╫ץ╫¬╫ש Placement (Scoped) =====

// Scoped = ╫₧╫ץ╫ñ╫ó ╫ק╫ף╫⌐ ╫£╫¢╫£ ╫ס╫º╫⌐╫¬ HTTP. ╫₧╫¬╫נ╫ש╫¥ ╫£╫⌐╫ש╫¿╫ץ╫¬╫ש╫¥ ╫⌐╫₧╫⌐╫¬╫₧╫⌐╫ש╫¥ ╫ס-DbContext.

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



// ===== ╫⌐╫£╫ס 3: ╫נ╫ש╫₧╫ץ╫¬ =====

// AuthService Γאפ ╫ס╫ץ╫ף╫º ╫⌐╫¥+╫í╫ש╫í╫₧╫פ ╫₧╫ץ╫£ ╫פ╫₧╫í╫ף (BCrypt).

// JwtTokenService Γאפ ╫₧╫á╫ñ╫ש╫º JWT ╫£╫נ╫ק╫¿ ╫פ╫¬╫ק╫ס╫¿╫ץ╫¬ ╫₧╫ץ╫ª╫£╫ק╫¬.

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<JwtTokenService>();



// Configure<T> ╫º╫ץ╫⌐╫¿ ╫º╫ר╫ó ╫פ╫ע╫ף╫¿╫ץ╫¬ ╫₧-appsettings ╫£-JwtSettings (Key, Issuer, Audience...).

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));



// ===== ╫⌐╫£╫ס 4: ╫¿╫ש╫⌐╫ץ╫¥ ╫⌐╫¢╫ס╫¬ BL (Singleton) =====

// Singleton = ╫₧╫ץ╫ñ╫ó ╫נ╫ק╫ף ╫£╫¢╫£ ╫ק╫ש╫ש ╫פ╫נ╫ñ╫£╫ש╫º╫ª╫ש╫פ. ╫₧╫¬╫נ╫ש╫¥ ╫£╫₧╫á╫ץ╫ó╫ש╫¥ stateless.

// AlgorithmSettingsRegistration ╫º╫ץ╫¿╫נ ╫נ╫¬ ╫º╫ר╫ó "Algorithm" ╫₧╫פ╫ע╫ף╫¿╫ץ╫¬.

builder.Services.AddSingleton(AlgorithmSettingsRegistration.BindFromConfiguration(builder.Configuration));



// IAssignmentValidator ╫פ╫ץ╫נ ╫₧╫₧╫⌐╫º Core; ConstraintEngine ╫פ╫ץ╫נ ╫פ╫₧╫ש╫₧╫ץ╫⌐ ╫ס-BL.

// ╫¢╫ת ╫פ╫נ╫£╫ע╫ץ╫¿╫ש╫¬╫¥ ╫₧╫נ╫₧╫¬ ╫נ╫ש╫£╫ץ╫ª╫ש╫¥ ╫ף╫¿╫ת ╫₧╫₧╫⌐╫º ╫נ╫ק╫ש╫ף Γאפ ╫£╫נ ╫ש╫⌐╫ש╫¿╫ץ╫¬ ╫₧╫ץ╫£ API.

builder.Services.AddSingleton<IAssignmentValidator, ConstraintEngine>();

// IAssignmentScorer — מימוש ScoringManager; נדרש ל-MoveEvaluator ול-Local Search עתידי.

builder.Services.AddSingleton<IAssignmentScorer, ScoringManager>();

// Local Search — הערכת מועמדי move (לא מתזמר / לא SearchStrategy).

builder.Services.AddSingleton<IMoveEvaluator, MoveEvaluator>();



// InitialPlacementOrchestrator ╫₧╫¬╫צ╫₧╫¿ ╫נ╫¬ ╫¬╫פ╫£╫ש╫ת ╫פ╫ק╫£╫ץ╫º╫פ ╫פ╫¿╫נ╫⌐╫ץ╫á╫ש╫¬ ╫ס-BL.

builder.Services.AddSingleton<InitialPlacementOrchestrator>();



// ===== ╫⌐╫£╫ס 5: ╫פ╫ע╫ף╫¿╫¬ JWT Bearer =====

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()

    ?? throw new InvalidOperationException("Jwt settings are not configured.");



builder.Services

    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        // TokenValidationParameters ╫₧╫ע╫ף╫ש╫¿ ╫₧╫פ ╫£╫ס╫ף╫ץ╫º ╫ס╫¢╫£ ╫ס╫º╫⌐╫פ ╫ó╫¥ ╫¢╫ץ╫¬╫¿╫¬ Authorization: Bearer ...

        options.TokenValidationParameters = new TokenValidationParameters

        {

            ValidateIssuer = true,           // ╫₧╫ש ╫פ╫á╫ñ╫ש╫º ╫נ╫¬ ╫פ╫ר╫ץ╫º╫ƒ

            ValidateAudience = true,         // ╫£╫₧╫ש ╫פ╫ר╫ץ╫º╫ƒ ╫₧╫ש╫ץ╫ó╫ף (╫פ╫£╫º╫ץ╫ק)

            ValidateLifetime = true,         // ╫פ╫נ╫¥ ╫ñ╫ע ╫¬╫ץ╫º╫ú

            ValidateIssuerSigningKey = true, // ╫פ╫נ╫¥ ╫פ╫ק╫¬╫ש╫₧╫פ ╫¬╫º╫ש╫á╫פ

            ValidIssuer = jwtSettings.Issuer,

            ValidAudience = jwtSettings.Audience,

            // SymmetricSecurityKey Γאפ ╫₧╫ñ╫¬╫ק ╫í╫ץ╫ף╫ש ╫₧╫⌐╫ץ╫¬╫ú ╫£╫ק╫¬╫ש╫₧╫פ ╫ץ╫£╫נ╫ש╫₧╫ץ╫¬ (HMAC).

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

        };

    });



// AddAuthorization ╫₧╫נ╫ñ╫⌐╫¿ ╫⌐╫ש╫₧╫ץ╫⌐ ╫ס-[Authorize] ╫ó╫£ ╫ס╫º╫¿╫ש╫¥.

builder.Services.AddAuthorization();



builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();



// ===== ╫⌐╫£╫ס 6: CORS =====

// CORS (Cross-Origin Resource Sharing) Γאפ ╫₧╫נ╫ñ╫⌐╫¿ ╫£╫£╫º╫ץ╫ק React (╫ñ╫ץ╫¿╫ר 5173/5174)

// ╫£╫⌐╫£╫ץ╫ק ╫ס╫º╫⌐╫ץ╫¬ ╫£-API (╫ñ╫ץ╫¿╫ר 5256) ╫₧╫פ╫ף╫ñ╫ף╫ñ╫ƒ.

// WithoutOrigins Γאפ ╫¿╫º ╫¢╫¬╫ץ╫ס╫ץ╫¬ localhost ╫₧╫ñ╫ץ╫¿╫⌐╫ץ╫¬ ╫₧╫ץ╫¿╫⌐╫ץ╫¬ (╫£╫נ "*" ╫ס-production).

builder.Services.AddCors(options =>

{

    options.AddDefaultPolicy(policy =>

    {

        policy.WithOrigins(

                "http://localhost:5173",

                "https://localhost:5173",

                "http://localhost:5174",

                "https://localhost:5174")

            .AllowAnyHeader()   // ╫₧╫נ╫ñ╫⌐╫¿ ╫¢╫ץ╫¬╫¿╫¬ Authorization

            .AllowAnyMethod();  // GET, POST, PUT, DELETE...

    });

});



// Build() ╫í╫ץ╫ע╫¿ ╫נ╫¬ ╫⌐╫£╫ס ╫פ╫¿╫ש╫⌐╫ץ╫¥ ╫ץ╫ש╫ץ╫ª╫¿ WebApplication ╫₧╫ץ╫¢╫ƒ ╫£╫פ╫¿╫ª╫פ.

var app = builder.Build();



// ===== ╫⌐╫£╫ס 7: ╫נ╫¬╫ק╫ץ╫£ Development ╫ס╫£╫ס╫ף =====

if (app.Environment.IsDevelopment())

{

    // CreateScope ╫ש╫ץ╫ª╫¿ Scope DI ╫צ╫₧╫á╫ש Γאפ DbContext ╫ק╫ש ╫¿╫º ╫ס╫¬╫ץ╫ת ╫פ╫ס╫£╫ץ╫º.

    using (var scope = app.Services.CreateScope())

    {

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Migrate() ╫₧╫¿╫ש╫Ñ ╫₧╫ש╫ע╫¿╫ª╫ש╫ץ╫¬ EF ╫⌐╫£╫נ ╫פ╫ץ╫ק╫£╫ץ ╫ó╫ף╫ש╫ש╫ƒ.

        db.Database.Migrate();

        // SeedAsync ╫ר╫ץ╫ó╫ƒ ╫á╫¬╫ץ╫á╫ש ╫ף╫₧╫ץ (╫₧╫á╫פ╫£, ╫₧╫⌐╫¬╫¬╫ñ╫ש╫¥ ╫£╫ף╫ץ╫ע╫₧╫פ) ╫נ╫¥ ╫פ╫₧╫í╫ף ╫¿╫ש╫º.

        await DevelopmentDataSeeder.SeedAsync(db);

    }



    app.UseSwagger();

    app.UseSwaggerUI();

}



// ===== ╫⌐╫£╫ס 8: Middleware pipeline =====

// ╫פ╫í╫ף╫¿ ╫ק╫⌐╫ץ╫ס: CORS ╫£╫ñ╫á╫ש Auth, Auth ╫£╫ñ╫á╫ש MapControllers.

app.UseHttpsRedirection(); // ╫₧╫ñ╫á╫פ HTTPΓזעHTTPS ╫¢╫⌐╫₧╫ץ╫ע╫ף╫¿

app.UseCors();

app.UseAuthentication();   // ╫º╫ץ╫¿╫נ Bearer token ╫ץ╫₧╫₧╫£╫נ HttpContext.User

app.UseAuthorization();    // ╫ס╫ץ╫ף╫º [Authorize]



// MapControllers ╫₧╫ק╫ס╫¿ ╫נ╫¬ ╫¢╫£ ╫פ╫ס╫º╫¿╫ש╫¥ (Controllers) ╫£╫á╫¬╫ש╫ס╫ש URL.

app.MapControllers();



app.Run();


