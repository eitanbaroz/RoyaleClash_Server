public class Shop
{
    const int size = 3;
    public List<Card> currentRotation;
    List<Card> deck;
    Player owner;

    public Shop(Player p)
    {
        this.currentRotation = new List<Card>();
        this.deck = p.deck;
        owner = p;
    }


    public void BuildShop()
    {
        currentRotation.Clear();
        for (int i = 0; i < size; i++)
        {
            Random rng = new Random();
            int rnd = rng.Next(0, deck.Count);
            currentRotation.Add(deck[rnd]);
        }
    }

    /// <summary>
    /// Description
    /// </summary>
    /// <param name="num">insert the number of te card you want to buy </param>
    /// <param name="p">insert the player</param>
    public void Buy(int num)
    {
        bool stop = false;
        if (num == -1)
        {
            if (owner.gold >= 1)
            {
                BuildShop();
                owner.gold -= 1;
            }
            else
            {
                Console.WriteLine("SHOP: not enough gold");
            }

        }
        else if (owner.gold < currentRotation[num].cost)
        {
            Console.WriteLine("SHOP: not enough gold");
        }

        else if (owner.gold >= currentRotation[num].cost)
        {
            owner.gold -= currentRotation[num].cost;

            //adding the unit of the card- shop[num] to the board.
            while (!stop)
            {

                Random rng = new Random();
                int row = rng.Next(0, owner.board.GetLength(0));
                rng = new Random();
                int col = rng.Next(0, owner.board.GetLength(1));

                if (owner.board[row, col] == null)
                {
                    owner.board[row, col] = currentRotation[num].unit.Clone();
                    owner.board[row, col]!.startX = col;
                    owner.board[row, col]!.startY = row;
                    owner.board[row, col]!.SetOwner(owner);
                    stop = true;
                }
            }

            // p.units[1, 0] = null; // 2nd row (1) first column (0)

            BuildShop();
        }




    }
}