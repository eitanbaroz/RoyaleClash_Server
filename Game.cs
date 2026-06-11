using System.Text;
using System.Text.Json;

public class Game
{
    public const int GRID_WIDTH = 6;
    public const int GRID_HEIGHT = 5;
    public const int START_HEALTH = 100;
    public const int START_GOLD = 500;

    public Player player;
    public Player enemy;

    public Game()
    {
        List<Card> PlayerDeck = [CardDatabase.GetCard(1), CardDatabase.GetCard(2), CardDatabase.GetCard(3), CardDatabase.GetCard(4), CardDatabase.GetCard(5), CardDatabase.GetCard(6), CardDatabase.GetCard(7), CardDatabase.GetCard(8), CardDatabase.GetCard(9)];
        List<Card> EnemyDeck = [CardDatabase.GetCard(1), CardDatabase.GetCard(2), CardDatabase.GetCard(3), CardDatabase.GetCard(4), CardDatabase.GetCard(5), CardDatabase.GetCard(6), CardDatabase.GetCard(7), CardDatabase.GetCard(8), CardDatabase.GetCard(9)];

        // creates shop for player and enemy as they are console players
        player = new ConsolePlayer(START_HEALTH, PlayerDeck, GRID_WIDTH, GRID_HEIGHT, START_GOLD);
        enemy = new ConsolePlayer(START_HEALTH, EnemyDeck, GRID_WIDTH, GRID_HEIGHT, START_GOLD);
    }

    public Game(OnlinePlayer player)
    {
        this.player = player;

        List<Card> EnemyDeck = [CardDatabase.GetCard(1), CardDatabase.GetCard(2), CardDatabase.GetCard(3), CardDatabase.GetCard(4), CardDatabase.GetCard(5), CardDatabase.GetCard(6), CardDatabase.GetCard(7), CardDatabase.GetCard(8), CardDatabase.GetCard(9)];
        this.enemy = new AiPlayer(START_HEALTH, EnemyDeck, GRID_WIDTH, GRID_HEIGHT, START_GOLD); ;
    }

    public Game(OnlinePlayer player, Player eplayer)
    {
        this.player = player;
        this.enemy = eplayer;
    }

    public void DisplayBoard(GameUnit?[,] board)
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
                if (board[row, col] == null || board[row, col]!.currhealth <= 0)
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

    public async Task Run()
    {
        //timer
        for (int i = 0; i < 2; i++)
        {
            System.Console.WriteLine("Playing player 1");
            await player.Play();
            System.Console.WriteLine("Playing player 2");
            await enemy.Play();
            System.Console.WriteLine("Done");
        }


        System.Console.WriteLine(GameUtils.GetLiveUnitCount(enemy.board, enemy).ToString());

        GameUnit?[,] tempBoard = new GameUnit[GRID_HEIGHT * 2, GRID_WIDTH];

        Func<string, Task> sendToOnlinePlayers = async (string type) =>
        {
            List<GameUnit> units = new();

            foreach (GameUnit g in tempBoard)
            {
                if (g != null && g.currhealth > 0)
                {
                    units.Add(g);
                }
            }

            ServerResponse res = new()
            {
                type = type,
                data = units,
            };

            byte[] responseBytes;

            if (player is OnlinePlayer p)
            {
                responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res) + "\n");
                await p.stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }

            if (enemy is OnlinePlayer e)
            {
                responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res) + "\n");
                await e.stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }

            Console.WriteLine("sent : \n" + JsonSerializer.Serialize(res) + "\n");
        };


        // Copy units from player & enemy to the temp board 
        foreach (GameUnit? u in player.board)
        {
            if (u == null) continue;
            tempBoard[u.startY, u.startX] = u.Clone();
        }
        foreach (GameUnit? u in enemy.board)
        {
            if (u == null) continue;
            tempBoard[u.startY + GRID_HEIGHT, u.startX] = u.Clone();
            tempBoard[u.startY + GRID_HEIGHT, u.startX]!.startY += GRID_HEIGHT;
        }

        if (!GameUtils.BothHaveUnits(tempBoard, player, enemy))
        {
            System.Console.WriteLine("No units on one team.");
            return;
        }

        foreach (GameUnit g in tempBoard)
        {
            g.gameX = g.startX;
            g.gameY = g.startY;
        }

        await sendToOnlinePlayers("ongoing");
        await Task.Delay(5000); // give a momonet for the players to see the boards

        CancellationTokenSource source = new();

        foreach (GameUnit g in tempBoard)
        {
            if (g != null) Task.Run(() => g.Attack(tempBoard, source.Token));
        }

        Task gameStatusUpdate = Task.Run(async () =>
        {
            while (!source.IsCancellationRequested)
            {
                await sendToOnlinePlayers("ongoing");
                await Task.Delay(20);
            }

            await sendToOnlinePlayers("end");
        });

        // wait for player loss 
        while (GameUtils.BothHaveUnits(tempBoard, player, enemy))
        {
            // System.Console.WriteLine(GameUtils.GetLiveUnitCount(tempBoard, player) + " ," + GameUtils.GetLiveUnitCount(tempBoard, enemy));
        }
        source.Cancel();

        if (GameUtils.GetLiveUnitCount(tempBoard, player) != 0)
        {
            System.Console.WriteLine("victory");
            enemy.health -= 20;
        }
        else
        {
            System.Console.WriteLine("defeat");
            player.health -= 20;
        }

        DisplayBoard(tempBoard);

        // _ _ _ _ _ _    } 
        // _ _ _ _ _ _    }  Player
        // _ _ _ _ _ _    }
        // _ _ _ _ _ _  }
        // _ _ _ _ _ _  } Enemy 
        // _ _ _ _ _ _  }




        System.Console.WriteLine("player health:" + player.health + "," + "enemy health:" + enemy.health);
    }
}