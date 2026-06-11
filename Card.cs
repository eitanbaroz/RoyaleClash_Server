public class Card
{
    public int id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public int cost { get; set; }

    public GameUnit unit { get; set; }

    public Card(int id, string name, string description, int cost, GameUnit unit)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.cost = cost;
        this.unit = unit;
    }

    public Card Clone()
    {
        Card clone = new Card(id, name, description, cost, unit.Clone());
        return clone;
    }
}