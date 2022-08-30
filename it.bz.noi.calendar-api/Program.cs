using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Graph;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using Microsoft.Graph.Extensions;

dotenv.net.DotEnv.Load();

static string GetEnv(string key)
{
    return Environment.GetEnvironmentVariable(key) ?? throw new Exception($"Environment variable {key} not set.");
}

static string? TryGetEnv(string key)
{
    return Environment.GetEnvironmentVariable(key);
}

string username = GetEnv("USERNAME");
string password = GetEnv("PASSWORD");
string tenantId = GetEnv("TENANT_ID");
string clientId = GetEnv("CLIENT_ID");
string openIdAuthority = GetEnv("OPENID_AUTHORITY");
var meetingRooms = GetEnv("MEETING_ROOMS").Split(',', ';');
int countOfEvents = int.Parse(TryGetEnv("NUMBER_OF_EVENTS") ?? "5");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwtBearerOptions =>
{
    jwtBearerOptions.Authority = openIdAuthority;
    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("noi-auth", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

var credential = new UsernamePasswordCredential(
    username,
    password,
    tenantId,
    clientId
);
var client = new GraphServiceClient(credential);

async Task<IEnumerable<Event>> GetEventsFromCalendar(
    GraphServiceClient client,
    string meetingRoom,
    DateOnly start,
    DateOnly end
)
{
    // Only query for the next `COUNT_OF_EVENTS` number of events
    var result = await client.Users[meetingRoom]
        .CalendarView
        .Request(new[] {
            new QueryOption("startDateTime", start.ToString("yyyy-MM-dd")),
            new QueryOption("endDateTime", end.ToString("yyyy-MM-dd"))
        })
        .OrderBy("start/dateTime desc")
        .Top(countOfEvents)
        .GetAsync();
    return result
        .Select(@event =>
            new Event(
                meetingRoom,
                @event.Subject,
                @event.Body.Content,
                @event.Start.ToDateTimeOffset(),
                @event.End.ToDateTimeOffset()
            )
        );
}

async Task<IEnumerable<Event>> GetAllCalendarEvents()
{
    // Get events for the next sixty days.
    var start = DateOnly.FromDateTime(DateTime.Today);
    var end = start.AddDays(60);
    var eventss = await Task.WhenAll(
        meetingRooms.Select(meetingRoom =>
            GetEventsFromCalendar(client, meetingRoom, start, end)
        )
    );
    var events =
        eventss
            .SelectMany(events => events)
            .OrderBy(e => e.StartDateTime)
            .Take(countOfEvents);
    return events;
}

app.MapGet("/", GetAllCalendarEvents);

app.Run();

record Event(string MeetingRoom, string Subject, string Body, DateTimeOffset StartDateTime, DateTimeOffset EndDateTime);

