using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Graph;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using Microsoft.Graph.Extensions;
using Microsoft.Extensions.Options;

dotenv.net.DotEnv.Load();

static string GetEnv(string key)
{
    return Environment.GetEnvironmentVariable(key) ?? throw new Exception($"Environment variable {key} not set.");
}

static string? TryGetEnv(string key)
{
    return Environment.GetEnvironmentVariable(key);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    return new Settings(
        Username: GetEnv("USERNAME"),
        Password: GetEnv("PASSWORD"),
        TenantId: GetEnv("TENANT_ID"),
        ClientId: GetEnv("CLIENT_ID"),
        OpenIdAuthority: GetEnv("OPENID_AUTHORITY"),
        MeetingRooms: GetEnv("MEETING_ROOMS").Split(',', ';'),
        NumberOfEvents: int.Parse(TryGetEnv("NUMBER_OF_EVENTS") ?? "5")
    );
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer();

builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("noi-auth", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetService<Settings>()!;
    var credential = new UsernamePasswordCredential(
        username: settings.Username,
        password: settings.Password,
        tenantId: settings.TenantId,
        clientId: settings.ClientId
    );
    return new GraphServiceClient(credential);
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

async Task<IEnumerable<Event>> GetEventsFromCalendar(
    GraphServiceClient client,
    Settings settings,
    string meetingRoom,
    DateOnly start,
    DateOnly end
)
{
    // Only query for the next `NUMBER_OF_EVENTS` number of events
    var result = await client.Users[meetingRoom]
        .CalendarView
        .Request(new[] {
            new QueryOption("startDateTime", start.ToString("yyyy-MM-dd")),
            new QueryOption("endDateTime", end.ToString("yyyy-MM-dd"))
        })
        .OrderBy("start/dateTime")
        .Top(settings.NumberOfEvents)
        .GetAsync();
    return result
        .Select(@event =>
            new Event(
                MeetingRoom: meetingRoom,
                Subject: @event.Subject,
                Body: @event.Body.Content,
                StartDateTime: @event.Start.ToDateTimeOffset(),
                EndDateTime: @event.End.ToDateTimeOffset()
            )
        );
}

async Task<IEnumerable<Event>> GetAllCalendarEvents(GraphServiceClient client, Settings settings)
{
    // Get events for the next sixty days.
    var start = DateOnly.FromDateTime(DateTime.Today);
    var end = start.AddDays(60);
    var eventss = await Task.WhenAll(
        settings.MeetingRooms.Select(meetingRoom =>
            GetEventsFromCalendar(client, settings, meetingRoom, start, end)
        )
    );
    var events =
        eventss
            .SelectMany(events => events)
            .OrderBy(e => e.StartDateTime)
            .Take(settings.NumberOfEvents);
    return events;
}

app.MapGet("/", GetAllCalendarEvents).RequireAuthorization("noi-auth");

app.Run();

record Event(string MeetingRoom, string Subject, string Body, DateTimeOffset StartDateTime, DateTimeOffset EndDateTime);

record Settings(string Username, string Password, string TenantId, string ClientId, string OpenIdAuthority, string[] MeetingRooms, int NumberOfEvents);

class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly Settings _settings;

    public ConfigureJwtBearerOptions(Settings settings)
    {
        _settings = settings;
    }

    public void Configure(string name, JwtBearerOptions options)
    {
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        options.Authority = _settings.OpenIdAuthority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            ValidateAudience = false
        };
    }
}
