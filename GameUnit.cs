using System.Security.Cryptography.Xml;

public abstract class GameUnit
{
    public int id { get; set; }
    public string name { get; set; }
    public int startX { get; set; }
    public int startY { get; set; }
    public double gameX { get; set; }
    public double gameY { get; set; }
    public int maxhealth { get; set; }
    public int currhealth { get; set; }
    public int attack { get; set; }
    public int star { get; set; }

    public Action onTurnStart;
    public Action onPlayed;

    public Player? owner;

    public GameUnit(int id, int xStart, int yStart, int maxhealth, int currhealth, int attack, int star, Action onTurnStart, Action onPlayed, string name)
    {
        this.id = id;
        this.startX = xStart;
        this.startY = yStart;
        this.gameX = startX;
        this.gameY = startY;
        this.maxhealth = maxhealth;
        this.currhealth = currhealth;
        this.attack = attack;
        this.star = star;
        this.onTurnStart = onTurnStart;
        this.onPlayed = onPlayed;
        this.name = name;

    }

    public void Upgrade()
    {
        star++;
        maxhealth += 2;
        attack += 2;
    }

    public void SetOwner(Player? owner)
    {
        this.owner = owner;
    }

    public GameUnit Clone()
    {
        GameUnit clone = null;

        if (this is ArcherUnit)
            clone = new ArcherUnit(id, startX, startY, maxhealth, currhealth, attack, ((ArcherUnit)this).attacKSpeed, star, onTurnStart, onPlayed, name);

        else if (this is WarriorUnit)
            clone = new WarriorUnit(id, startX, startY, maxhealth, currhealth, attack, ((WarriorUnit)this).attacKSpeed, ((WarriorUnit)this).moveSpeed, star, onTurnStart, onPlayed, name);

        else if (this is MageUnit)
            clone = new MageUnit(id, startX, startY, maxhealth, currhealth, attack, ((MageUnit)this).attacKSpeed, ((MageUnit)this).splash, star, onTurnStart, onPlayed, name);

        else
            throw new Exception("Add a clone for : " + this.GetType());

        clone.SetOwner(owner);
        return clone;
    }

    public abstract Task Attack(GameUnit?[,] tempBoard, CancellationToken ct);
}

public class ArcherUnit : GameUnit
{
    /// <summary>
    /// Deley between attacks - in seconds
    /// </summary>
    public double attacKSpeed;

    public ArcherUnit(int id, int x, int y, int maxhealth, int currhealth, int attack, double attacKSpeed, int star, Action onTurnStart, Action onPlayed, string name)
        : base(id, x, y, maxhealth, currhealth, attack, star, onTurnStart, onPlayed, name)
    {
        this.attacKSpeed = attacKSpeed;
    }

    public override async Task Attack(GameUnit?[,] tempBoard, CancellationToken ct)
    {
        gameX = startX;
        gameY = startY;
        while (!ct.IsCancellationRequested)
        {
            if (this.currhealth <= 0)
            {
                System.Console.WriteLine($"Unit {this.name} died.");
                return;
            }

            var enemy = GameUtils.FindClosestEnemy(tempBoard, this);

            if (enemy != null && enemy.currhealth > 0)
            {
                enemy.currhealth -= this.attack;
                System.Console.WriteLine($"Unit {this.name} attacked {enemy.name}");
            }

            await Task.Delay(TimeSpan.FromSeconds(attacKSpeed), ct);
        }
    }
}


public class WarriorUnit : GameUnit
{
    /// <summary>
    /// Deley between attacks - in seconds
    /// </summary>
    public double attacKSpeed;
    public double moveSpeed;

    public WarriorUnit(int id, int x, int y, int maxhealth, int currhealth, int attack, double attacKSpeed, double moveSpeed, int star, Action onTurnStart, Action onPlayed, string name)
        : base(id, x, y, maxhealth, currhealth, attack, star, onTurnStart, onPlayed, name)
    {
        this.attacKSpeed = attacKSpeed;
        this.moveSpeed = moveSpeed;
    }

    public override async Task Attack(GameUnit?[,] tempBoard, CancellationToken ct)
    {
        gameX = startX;
        gameY = startY;
        while (!ct.IsCancellationRequested)
        {
            if (this.currhealth <= 0)
            {
                System.Console.WriteLine($"Unit {this.name} died.");
                return;
            }

            var enemy = GameUtils.FindClosestEnemy(tempBoard, this);

            if (enemy != null && enemy.currhealth > 0)
            {
                if (GameUtils.Dist(this, enemy) <= 1)
                {
                    System.Console.WriteLine("ATTACK !");

                    enemy.currhealth -= this.attack;
                    System.Console.WriteLine($"Unit {this.name} attacked {enemy.name}");
                    await Task.Delay(TimeSpan.FromSeconds(attacKSpeed), ct);
                }
                else
                {
                    System.Console.WriteLine("MOVE !");

                    double dx = enemy.gameX - this.gameX;
                    double dy = enemy.gameY - this.gameY;
                    double len = GameUtils.Dist(enemy, this);

                    double normalX = dx / len;
                    double normaly = dy / len;

                    gameX += normalX * moveSpeed * 0.01666;
                    gameY += normaly * moveSpeed * 0.01666;
                    await Task.Delay(TimeSpan.FromMilliseconds(16.66), ct);
                }
            }
        }
    }
}


public class MageUnit : GameUnit
{
    /// <summary>
    /// Deley between attacks - in seconds
    /// </summary>
    public double attacKSpeed;
    public int splash;

    public MageUnit(int id, int x, int y, int maxhealth, int currhealth, int attack, double attacKSpeed, int splash, int star, Action onTurnStart, Action onPlayed, string name)
        : base(id, x, y, maxhealth, currhealth, attack, star, onTurnStart, onPlayed, name)
    {
        this.attacKSpeed = attacKSpeed;
        this.splash = splash;
    }

    public override async Task Attack(GameUnit?[,] tempBoard, CancellationToken ct)
    {
        gameX = startX;
        gameY = startY;
        while (!ct.IsCancellationRequested)
        {
            if (this.currhealth <= 0)
            {
                System.Console.WriteLine($"Unit {this.name} died.");
                return;
            }

            var enemy = GameUtils.FindClosestEnemy(tempBoard, this);


            if (enemy != null && enemy.currhealth > 0)
            {

                System.Console.WriteLine("ATTACK !");

                enemy.currhealth -= this.attack;
                foreach (GameUnit? unit in tempBoard) // find close enemy
                {
                    if (unit != null && enemy.owner == unit.owner)
                    {
                        if (GameUtils.Dist(enemy, unit) <= splash)
                        {
                            unit.currhealth -= this.attack;
                        }
                    }
                }
                System.Console.WriteLine($"Unit {this.name} attacked {enemy.name}");
                await Task.Delay(TimeSpan.FromSeconds(attacKSpeed), ct);


            }
        }
    }
}

class GameUtils
{
    public static double Dist(GameUnit u1, GameUnit u2)
    {
        double dx = u1.gameX - u2.gameX;
        double dy = u1.gameY - u2.gameY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static GameUnit? FindClosestEnemy(GameUnit?[,] board, GameUnit attacker)
    {
        GameUnit? closestEnemy = null;
        double minDistance = double.MaxValue;

        foreach (GameUnit? unit in board) // find close enemy
        {
            if (unit != null && unit.currhealth > 0 && attacker.owner != unit.owner)
            {
                double distance = Dist(attacker, unit);
                if (distance < minDistance)
                {
                    closestEnemy = unit;
                    minDistance = distance;
                }
            }
        }

        return closestEnemy;
    }

    public static int GetLiveUnitCount(GameUnit?[,] board, Player owner)
    {
        int count = 0;

        foreach (var g in board)
        {
            if (g != null && g.currhealth > 0 && g.owner == owner) count++;
        }

        return count;
    }

    public static bool BothHaveUnits(GameUnit?[,] board, Player u1, Player u2)
    {
        return GetLiveUnitCount(board, u1) > 0 && GetLiveUnitCount(board, u2) > 0;
    }
}