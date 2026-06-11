using System.Net.Sockets;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// docs : https://ai.google.dev/gemini-api/docs/quickstart#c
/// </summary>
public class AiPlayer : Player
{
    record AiResposne(int index, int x, int y);

    private static string apiKey;
    private static string rules;
    private static string units;

    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    static AiPlayer()
    {
        apiKey = Environment.GetEnvironmentVariable("ApiKey") ?? "";
        rules = File.ReadAllText("rule.json");
        units = File.ReadAllText("units.json");
    }

    public AiPlayer(int health, List<Card> deck, int gridWidth, int gridHeight, int gold)
    : base(health, gridWidth, gridHeight, deck, gold)
    {
        this.shop = new Shop(this);
        shop.BuildShop();
    }

    private async Task<string> PlayTurn()
    {
        using var client = new HttpClient();

        var url = $"{Endpoint}?key={apiKey}";

        while (true)
        {
            try
            {
                Console.WriteLine("Sending request...");

                var shopstr = "[" + string.Join(", ",
                    shop.currentRotation.Select(x =>
                        $"(name: {x.name}, id: {x.id}, cost: {x.cost})"
                    )
                ) + "]";

                var boardstr = "[" + string.Join(", ",
                    board
                        .Cast<GameUnit?>()
                        .Where(x => x != null)
                        .Select(x =>
                            $"(name: {x!.name}, id: {x.id}, x: {x.gameX}, y: {x.gameY})"
                        )
                ) + "]";

                var command = @"
                    You may buy ONLY ONE card per turn.
                    Use 0 for the first shop card, 1 for the second, 2 for the third.

                    You MUST NOT buy more than one card.

                    You MUST NOT place a unit on an occupied position.
                    A position is occupied if another unit already exists at (x, y).

                    Always check board state before placing.
                    If a position is taken, choose another valid empty position.

                    Your goal is to build the strongest possible board using gold efficiently.

                    The enemy is above you, the lower the y value the closer you are to them, meaning range should have lower y value.
                    ";

                var requestBody = new
                {
                    contents = new[]
                    {
        new
        {
            parts = new[]
            {
                new
                {
                    text = $@"
                        RULES:
                        {rules}

                        UNITS:
                        {units}

                        SHOP:
                        {shopstr}

                        BOARD:
                        {boardstr}

                        GOLD: {gold}

                        OBJECTIVE:
                        {command}

                        STRICT OUTPUT FORMAT:
                        Return ONLY valid JSON (no text, no markdown):

                        {{
                        ""index"": 0,
                        ""x"": 0,
                        ""y"": 0
                        }}

                        RULES YOU MUST FOLLOW:
                        - You may buy ONLY ONE card per turn
                        - index must be 0, 1, or 2
                        - You MUST NOT place a unit on an occupied (x,y)
                        - If position is taken, choose a different empty tile
                         "
                            }
                        }
                    }
                }
                };


                System.Console.WriteLine($"Sending : {requestBody.contents[0].parts[0].text}");

                var json = JsonSerializer.Serialize(requestBody);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed: {(int)response.StatusCode}");
                    Console.WriteLine(responseJson);

                    Console.WriteLine("Retrying in 10 seconds...");
                    await Task.Delay(10_000);
                    continue;
                }

                using var doc = JsonDocument.Parse(responseJson);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("Empty response, retrying...");
                    await Task.Delay(10_000);
                    continue;
                }

                string cleaned = text
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                System.Console.WriteLine($"received : {cleaned}");

                return cleaned;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.Message);

                Console.WriteLine("Retrying in 10 seconds...");
                await Task.Delay(10_000);
            }
        }
    }

    public override async Task Play()
    {
        string result = await PlayTurn();

        if (result == null) return;

        System.Console.WriteLine("Found result value");

        var move = JsonSerializer.Deserialize<AiResposne>(result);

        if (move == null)
        {
            System.Console.WriteLine("move is null");
            System.Console.WriteLine(JsonSerializer.Serialize(move));
            return;
        }

        if (move.y < 0 || move.y >= board.GetLength(0) ||
            move.x < 0 || move.x >= board.GetLength(1))
            throw new Exception("Invalid board position");

        if (move.index < 0 || move.index >= shop.currentRotation.Count)
            throw new Exception("Invalid shop index");

        var shopItem = shop.currentRotation[move.index];

        if (shopItem?.unit == null)
            throw new Exception("Shop unit is null");

        var clone = shopItem.unit.Clone();

        if (clone == null)
            throw new Exception("Clone returned null");

        board[move.y, move.x] = clone;
        board[move.y, move.x].startX = move.x;
        board[move.y, move.x].startY = move.y;
        clone.SetOwner(this);

        System.Console.WriteLine("Refresing shop");
        shop.BuildShop();
    }
}

public class ConsolePlayer : Player
{
    public ConsolePlayer(int health, List<Card> deck, int gridWidth, int gridHeight, int gold)
        : base(health, gridWidth, gridHeight, deck, gold)
    {
        this.shop = new Shop(this);
        shop.BuildShop();
    }

    public override async Task Play()
    {
        DisplayShop(shop!);
        DisplayBoard();
    }


    public void DisplayShop(Shop shop)
    {
        int choice = 0;
        while (choice != -1)
        {
            Console.WriteLine("current gold:" + gold);
            Console.WriteLine("---SHOP---");
            for (int i = 0; i < shop.currentRotation.Count(); i++)
            {
                Console.WriteLine($"{i + 1}: {shop.currentRotation[i].name} - Cost: {shop.currentRotation[i].cost}");
            }
            Console.WriteLine("the card do you want to buy?");
            choice = int.Parse(Console.ReadLine()!);
            if (choice == -1) return;
            shop.Buy(choice - 1);
        }
    }

    public void DisplayBoard()
    {
        Console.Write("   ");
        for (int col = 0; col < board.GetLength(1); col++)
        {
            Console.Write(col + " ");
        }
        Console.WriteLine();

        for (int row = 0; row < board.GetLength(0); row++)
        {
            Console.Write(row + "  ");

            for (int col = 0; col < board.GetLength(1); col++)
            {
                if (board[row, col] == null)
                {
                    Console.Write(". ");
                }
                else
                {
                    Console.Write(board[row, col]!.name + " ");
                }
            }

            Console.WriteLine();
        }

    }
}


public class OnlinePlayer : Player
{
    public NetworkStream stream;

    public OnlinePlayer(int health, List<Card> deck, int gridWidth, int gridHeight, int gold, NetworkStream stream)
        : base(health, gridWidth, gridHeight, deck, gold)
    {
        this.stream = stream;
        this.shop = new Shop(this);
        shop.BuildShop();
    }

    public int[] GetShopRotation()
    {
        return this.shop!.currentRotation.Select((e) =>
        {
            return e.id;
        }).ToArray();
    }

    public void UpdateBoard(PreCombatUnitData[] units)
    {
        foreach (var u in units)
        {
            this.board[u.y, u.x] = CardDatabase.GetCard(u.id).unit.Clone();
        }
    }

    public override async Task Play()
    {
        // nothing to do, handle on the phone
        return;
    }
}

public abstract class Player
{
    public int health;

    public List<Card> deck;

    public GameUnit?[,] board;
    public int gold;
    public Shop? shop;

    public Player(int health, int gridWidth, int gridHeight, List<Card> deck, int gold)
    {
        this.health = health;
        this.board = new GameUnit?[gridHeight, gridWidth];
        this.deck = deck;
        this.gold = gold;
    }

    public abstract Task Play();

    public bool HasUnits()
    {
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] != null) return true;
            }
        }
        return false;
    }
}