using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.BL.Algorithm.InitialPlacement.Solver.Wire;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: לקוח HTTP לשירות OR-Tools CP-SAT החיצוני — שולח job, poll סטטוס, וממפה תשובה ל-<see cref="SolverResponse"/>.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="InitialPlacementOrchestrator"/> ו-<see cref="ExternalSolverFallback"/>.
/// </remarks>
public sealed class ExternalSolverClient : IExternalSolverClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// תפקיד: מאתחל לקוח HTTP — יוצר HttpClient פנימי אם לא סופק מבחוץ.
    /// </summary>
    /// <param name="httpClient">לקוח HTTP קיים; null יוצר instance חדש.</param>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, <see cref="ExternalSolverFallback"/>, בדיקות _solver_validation.</remarks>
    public ExternalSolverClient(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            // יוצרים HttpClient פנימי — נצטרך Dispose בסוף.
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
        }
    }

    /// <summary>
    /// תפקיד: שולח job לפותר, poll עד סיום/timeout, ומחזיר תשובה מנורמלת.
    /// </summary>
    /// <param name="request">בקשת הפותר הפנימית.</param>
    /// <param name="settings">הגדרות — כתובת בסיס, timeout, מרווח polling.</param>
    /// <param name="cancellationToken">אסימון ביטול.</param>
    /// <returns>תשובת פותר; Failed עם IsTimeout=true במקרה timeout.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public async Task<SolverResponse> SolveAsync(
        SolverRequest request,
        AlgorithmSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var timeoutMs = settings.SolverTimeoutMs;
        var submitWire = SolverJobWireMapper.ToSubmitWire(request, timeoutMs);
        var baseUrl = settings.SolverBaseUrl.TrimEnd('/');
        var submitUri = new Uri($"{baseUrl}/api/v1/solver/jobs");

        HttpResponseMessage submitResponse;
        try
        {
            // שלב 1: שליחת job לפותר (POST JSON).
            submitResponse = await _httpClient.PostAsJsonAsync(submitUri, submitWire, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // timeout של HttpClient — לא ביטול מכוון.
            return CreateFailedResponse(isTimeout: true, "External solver request timed out before job submission.");
        }
        catch (HttpRequestException ex)
        {
            return CreateFailedResponse(isTimeout: false, $"External solver communication failed: {ex.Message}");
        }

        if (submitResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await submitResponse.Content.ReadFromJsonAsync<SolverErrorWire>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return CreateFailedResponse(
                isTimeout: false,
                error?.Errors.Count > 0 ? error.Errors : new[] { "External solver rejected the request." });
        }

        if (!submitResponse.IsSuccessStatusCode)
        {
            return CreateFailedResponse(
                isTimeout: false,
                new[] { $"External solver returned HTTP {(int)submitResponse.StatusCode} on job submission." });
        }

        var created = await submitResponse.Content.ReadFromJsonAsync<SolverJobCreatedWire>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (created is null)
        {
            return CreateFailedResponse(isTimeout: false, new[] { "External solver returned an empty job creation response." });
        }

        // שלב 2: polling — ממתינים עד Success/Infeasible/Timeout או עד deadline.
        var pollDeadline = DateTime.UtcNow.AddMilliseconds(timeoutMs + settings.SolverPollIntervalMs);
        var statusUri = new Uri($"{baseUrl}/api/v1/solver/jobs/{created.JobId}");

        while (DateTime.UtcNow < pollDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SolverJobStatusResponseWire? statusResponse;
            try
            {
                statusResponse = await _httpClient.GetFromJsonAsync<SolverJobStatusResponseWire>(statusUri, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return CreateFailedResponse(isTimeout: true, "External solver polling timed out.");
            }
            catch (HttpRequestException ex)
            {
                return CreateFailedResponse(isTimeout: false, $"External solver polling failed: {ex.Message}");
            }

            if (statusResponse is null)
            {
                return CreateFailedResponse(isTimeout: false, new[] { "External solver returned an empty status response." });
            }

            // Pending/Running — ממתינים ומנסים שוב.
            if (statusResponse.Status is SolverJobStatusWire.Pending or SolverJobStatusWire.Running)
            {
                await Task.Delay(settings.SolverPollIntervalMs, cancellationToken).ConfigureAwait(false);
                continue;
            }

            return MapWireStatus(statusResponse);
        }

        return CreateFailedResponse(isTimeout: true, "External solver polling deadline exceeded.");
    }

    private static SolverResponse MapWireStatus(SolverJobStatusResponseWire wire)
    {
        // switch על סטטוס wire — מנרמל ל-SolverResponseStatus פנימי.
        return wire.Status switch
        {
            SolverJobStatusWire.Success => new SolverResponse(
                SolverResponseStatus.Success,
                wire.UnitGroupAssignments,
                wire.Errors,
                false),
            SolverJobStatusWire.Infeasible => new SolverResponse(
                SolverResponseStatus.Infeasible,
                new Dictionary<int, int>(),
                wire.Errors.Count > 0 ? wire.Errors : new List<string> { "Solver reported infeasible problem." },
                false),
            SolverJobStatusWire.Timeout => new SolverResponse(
                SolverResponseStatus.Failed,
                new Dictionary<int, int>(),
                wire.Errors.Count > 0 ? wire.Errors : new List<string> { "External solver timed out." },
                true),
            SolverJobStatusWire.InvalidInput => new SolverResponse(
                SolverResponseStatus.Failed,
                new Dictionary<int, int>(),
                wire.Errors.Count > 0 ? wire.Errors : new List<string> { "External solver reported invalid input." },
                false),
            _ => new SolverResponse(
                SolverResponseStatus.Failed,
                new Dictionary<int, int>(),
                wire.Errors.Count > 0 ? wire.Errors : new List<string> { "External solver failed." },
                wire.IsTimeout),
        };
    }

    private static SolverResponse CreateFailedResponse(bool isTimeout, string error) =>
        CreateFailedResponse(isTimeout, new[] { error });

    private static SolverResponse CreateFailedResponse(bool isTimeout, IReadOnlyList<string> errors) =>
        new(
            SolverResponseStatus.Failed,
            new Dictionary<int, int>(),
            errors,
            isTimeout);

    /// <summary>
    /// תפקיד: משחרר HttpClient פנימי אם נוצר ע"י הבנאי.
    /// </summary>
    /// <remarks>נקרא מ- using/dispose של הלקוח בבדיקות _solver_validation.</remarks>
    public void Dispose()
    {
        // משחררים רק HttpClient שיצרנו בעצמנו — לא כזה שהוזרק מבחוץ.
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
