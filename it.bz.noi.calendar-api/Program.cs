// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Graph;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    dotenv.net.DotEnv.Load();

    static string GetEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key) ??
            throw new Exception($"Environment variable {key} not set.");
    }

    static string? TryGetEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

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

builder.Services
    .ConfigureOptions<CalendarApiODataOptions>()
    .AddControllers();

builder.Services
    .ConfigureOptions<ConfigureJwtBearerOptions>()
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("noi-auth", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddScoped(serviceProvider =>
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

app.MapControllers();

app.Run();

public record Organizer(string Name, string Address);
public record Location(string Id, string Name, string Uri);
public record Event(string? Id, string MeetingRoom, Organizer? Organizer, Location? Location, string? Subject, string? Body, DateTimeOffset StartDateTime, DateTimeOffset EndDateTime);

public record Settings(string Username, string Password, string TenantId, string ClientId, string OpenIdAuthority, string[] MeetingRooms, int NumberOfEvents);

class CalendarApiODataOptions : IConfigureNamedOptions<ODataOptions>
{
    private readonly Settings _settings;

    public CalendarApiODataOptions(Settings settings)
    {
        _settings = settings;
    }

    public void Configure(string name, ODataOptions options)
    {
        Configure(options);
    }

    public void Configure(ODataOptions options)
    {
        options.Select().Filter().SetMaxTop(_settings.NumberOfEvents).OrderBy().Count();
    }
}

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

public class CalendarController : ODataController
{
    private readonly GraphServiceClient _client;
    private readonly Settings _settings;

    public CalendarController(GraphServiceClient client, Settings settings)
    {
        _client = client;
        _settings = settings;
    }

#if !DEBUG
    [Authorize("noi-auth")]
#endif
    [EnableQuery]
    [HttpGet("/")]
    public async Task<List<Event>> Get()
    {
        return (await GetAllCalendarEvents(_client, _settings)).ToList();
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
            .GetAsync(config =>
            {
                config.QueryParameters.StartDateTime = start.ToString("yyyy-MM-dd");
                config.QueryParameters.EndDateTime = end.ToString("yyyy-MM-dd");
                config.QueryParameters.Top = 5;
                config.QueryParameters.Orderby = new[] { "start/dateTime" };
            });
        return result?.Value != null ? result.Value
            .Select((Func<Microsoft.Graph.Models.Event, Event>)(@event =>
                new Event(
                    Id: @event.Id,
                    MeetingRoom: meetingRoom,
                    Organizer: ExtractOrganizer(@event),
                    Location: ExtractLocation(@event),
                    Subject: ExtractSubject(@event.Subject),
                    Body: @event.Body?.Content,
                    StartDateTime: @event.Start.ToDateTimeOffset(),
                    EndDateTime: @event.End.ToDateTimeOffset()
                ))
            ) : Enumerable.Empty<Event>();
    }

    private static Organizer? ExtractOrganizer(Microsoft.Graph.Models.Event @event)
    {
        if (@event.Organizer?.EmailAddress != null) {
            return new Organizer(
                Name: @event.Organizer.EmailAddress?.Name ?? "",
                Address: @event.Organizer.EmailAddress?.Address ?? ""
            );
        }
        return null;
    }

    private static Location? ExtractLocation(Microsoft.Graph.Models.Event @event)
    {
        if (@event.Location != null) {
            return new Location(
                Id: @event.Location.UniqueId ?? "",
                Name: @event.Location.DisplayName ?? "",
                Uri: @event.Location.LocationUri ?? ""
            );
        }
        return null;
    }

    /// <summary>
    /// Remove the first chunk before a comma, as it contains the user name
    /// </summary>
    private string? ExtractSubject(string? subject)
    {
        var chunks = subject?.Split(',');
        if (chunks?.Length > 2) {
            return chunks[1].Trim();
        }
        return subject;
    }
}

