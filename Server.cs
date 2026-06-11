using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class TcpServer
{
    private TcpListener server;

    public TcpServer(string ip, int port)
    {
        server = new TcpListener(IPAddress.Parse(ip), port);
    }

    public async Task StartAsync()
    {
        server.Start();
        Console.WriteLine("Async server started...");

        while (true)
        {
            // NON-BLOCKING accept
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected!");

            // Handle client without blocking
            _ = Task.Run(async () =>
            {
                // try
                // {
                await HandleClientAsync(client);
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine($"Client error: {ex.Message}");
                // }
            });
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        int joinBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
        string joinMsg = Encoding.UTF8.GetString(buffer, 0, joinBytes); ;
        ClientRequest joinReq = JsonSerializer.Deserialize<ClientRequest>(joinMsg)!;

        // always will be a start_deck
        int[] deckids = ((JsonElement)joinReq.data).Deserialize<int[]>();


        const int health = 100;
        // TODO : get deck from user
        List<Card> PlayerDeck = deckids.Select(x => CardDatabase.GetCard(x)).ToList();
        const int width = Game.GRID_WIDTH;
        const int height = Game.GRID_HEIGHT;
        const int gold = Game.START_GOLD;

        OnlinePlayer player = new OnlinePlayer(health, PlayerDeck, width, height, gold, stream);

        byte[] responseBytes;


        Console.WriteLine("Created client thread");

        // send the first shop to the user
        responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ServerResponse()
        {
            type = "shop",
            data = new AfterBuy()
            {
                shop = player.GetShopRotation(),
                gold = player.gold,
                units = player.board.Cast<GameUnit>().Where(x => x != null).ToArray() // convert to 1D array from 2D
            }
        }) + "\n");

        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

        System.Console.WriteLine("Send join msg.");

        List<Card> EnemyDeck = [CardDatabase.GetCard(1), CardDatabase.GetCard(2), CardDatabase.GetCard(3), CardDatabase.GetCard(4), CardDatabase.GetCard(5), CardDatabase.GetCard(6), CardDatabase.GetCard(7), CardDatabase.GetCard(8), CardDatabase.GetCard(9)];
        var enemy = new AiPlayer(100, EnemyDeck, 6, 5, 100);

        try
        {
            while (true)
            {
                // await get data from the client
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead); // UTF8- a form of encryption....TD
                Console.WriteLine($"Received: {message}");

                // process the user request
                ClientRequest? req = JsonSerializer.Deserialize<ClientRequest>(message);

                if (req == null) continue;

                else if (req.type == "buy")
                {
                    int unitId = ((JsonElement)req.data).GetInt32();

                    Console.WriteLine($"Buying unit index: {unitId}");

                    player!.shop!.Buy(unitId);

                    // send the next shop 
                    ServerResponse res = new ServerResponse()
                    {
                        type = "shop",
                        data = new AfterBuy()
                        {
                            shop = player.GetShopRotation(),
                            gold = player.gold,
                            units = player.board.Cast<GameUnit>().Where(x => x != null).ToArray() // convert to 1D array from 2D
                        }
                    };

                    responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res) + "\n");
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }

                else if (req.type == "start_battle")
                {
                    Game game = new Game(player, enemy);
                    await game.Run();
                }

                else if (req.type == "RecieveAfterCombat")
                {
                    ServerResponse res = new ServerResponse()
                    {
                        type = "aftercombat",
                        data = new AfterCombat()
                        {
                            playerhealth = player.health,
                            enemyhealth = enemy.health
                        }
                    };

                    responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res) + "\n");
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }

                else continue;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            Console.WriteLine("Client disconnected.");
            client.Close();
        }
    }
}

public class ClientRequest
{
    // buy, start_battle, start_deck 
    public string? type { get; set; }

    // buy -> int (expect after_buy)
    // start_battle -> null (expect ongoing until end)
    // start_deck -> int[] (ids)
    public object? data { get; set; }
}

public class PreCombatUnitData
{
    public int id { get; set; }
    public int x { get; set; }
    public int y { get; set; }
}

public class ServerResponse
{
    public string type { get; set; } // after_buy, ongoing, end

    // shop -> ShopOffer[]?
    // ongoing -> InCombatUnitData[]?
    // end -> AfterCombat[]?
    public object data { get; set; }
}

public class AfterCombat
{
    public int playerhealth { get; set; }
    public int enemyhealth { get; set; }

}

public class InCombatUnitData
{
    public int id { get; set; }
    public double x { get; set; }
    public double y { get; set; }
    public int currhealth { get; set; }
    public int maxhealth { get; set; }
}

public class AfterBuy
{
    public int[]? shop { get; set; } // unit ids
    public int gold { get; set; }
    public GameUnit[]? units { get; set; }
}