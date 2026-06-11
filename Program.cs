using System.Text.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=app.db");
});

var app = builder.Build();

app.UseHttpsRedirection();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();
}

app.MapGet("/", () =>
{
    return "OK";
});

app.MapGet("/signup", (string username, string password, AppDbContext db) =>
{
    var existingUser = db.Users.FirstOrDefault(x => x.Username == username);

    if (existingUser != null)
    {
        return Results.BadRequest(new LoginResponse
        {
            success = false,
            message = "User already taken!",
            token = null,
        });
    }

    var user = new User
    {
        Username = username,
        Password = password
    };

    db.Users.Add(user);
    db.SaveChanges();

    var token = new Token
    {
        UserId = user.Id,
        Value = Guid.NewGuid().ToString()
    };

    db.Tokens.Add(token);
    db.SaveChanges();

    return Results.Ok(new LoginResponse
    {
        success = true,
        message = "User created!",
        token = token.Value,
    });
});

app.MapGet("/login", (string username, string password, AppDbContext db) =>
{
    var user = db.Users.FirstOrDefault(x =>
        x.Username == username &&
        x.Password == password);

    if (user == null)
    {
        return Results.BadRequest(new LoginResponse
        {
            success = false,
            message = "User not found!",
            token = null,
        });
    }

    var token = new Token
    {
        UserId = user.Id,
        Value = Guid.NewGuid().ToString()
    };

    db.Tokens.Add(token);
    db.SaveChanges();

    return Results.Ok(new LoginResponse
    {
        success = true,
        message = "Successful login!",
        token = token.Value,
    });
});

app.MapGet("/getcards", (string token, AppDbContext db) =>
{
    var existingToken = db.Tokens.FirstOrDefault(x => x.Value == token);

    if (existingToken == null)
    {
        return Results.BadRequest(new CardResponse()
        {
            success = false,
            message = "Invalid token!",
            cards = []
        });
    }

    return Results.Ok(
        new CardResponse()
        {
            success = true,
            message = "Cards downloaded!",
            cards = CardDatabase.GetAllCards()
        }
    );
});

app.MapGet("/getusercards", (string token, AppDbContext db) =>
{
    var tokenEntry = db.Tokens.FirstOrDefault(t => t.Value == token);

    if (tokenEntry == null)
        return Results.Unauthorized();

    int userId = tokenEntry.UserId;

    var cardIds = db.CardEntries
        .Where(c => c.UserId == userId)
        .Select(c => c.CardId)
        .ToArray() ?? [];

    return Results.Ok(cardIds);
});

app.MapGet("/setusercards", (string token, string cardjson, AppDbContext db) =>
{
    var tokenEntry = db.Tokens.FirstOrDefault(t => t.Value == token);

    if (tokenEntry == null) return Results.Unauthorized();

    int userId = tokenEntry.UserId;

    int[] ids = JsonSerializer.Deserialize<int[]>(cardjson)!;

    // remove all user cards
    var existing = db.CardEntries
        .Where(c => c.UserId == userId)
        .ToList();

    db.CardEntries.RemoveRange(existing);

    // add new user cards
    var newEntries = ids.Select(id => new CardEntry
    {
        UserId = userId,
        CardId = id
    });

    db.CardEntries.AddRange(newEntries);

    db.SaveChanges();

    return Results.Ok("Success");
});


TcpServer server = new TcpServer("0.0.0.0", 5070);

Task.WaitAll(app.RunAsync(), server.StartAsync());

public class LoginResponse
{
    public bool success { get; set; }
    public string? message { get; set; }
    public string? token { get; set; }
}

public class CardResponse
{
    public bool success { get; set; }
    public string? message { get; set; }
    public Card[] cards { get; set; } = [];
}

