static class CardDatabase
{
    private static Dictionary<int, Card> map = new();
    private static GameUnit unitA = new ArcherUnit(1, 1, 1, maxhealth: 2, currhealth: 2, attack: 3, attacKSpeed: 5, star: 1, null, null, "archy");
    private static GameUnit unitB = new ArcherUnit(2, 1, 1, maxhealth: 2, currhealth: 2, attack: 4, attacKSpeed: 6, star: 1, null, null, "elon");
    private static GameUnit warriorA = new WarriorUnit(3, 1, 1, maxhealth: 8, currhealth: 8, attack: 3, attacKSpeed: 3, moveSpeed: 3, star: 1, null, null, "wolve");
    private static GameUnit warriorB = new WarriorUnit(4, 1, 1, maxhealth: 7, currhealth: 7, attack: 4, attacKSpeed: 4, moveSpeed: 3, star: 1, null, null, "yasuo");

    private static GameUnit MageA = new MageUnit(5, 1, 1, 3, 8, 5, 6, 9, 1, null, null, "naz");

    private static GameUnit heroA = new WarriorUnit(6, 1, 1, maxhealth: 9, currhealth: 9, attack: 5, attacKSpeed: 4, moveSpeed: 4, star: 1, null, null, "bjorn");
    private static GameUnit heroB = new WarriorUnit(7, 1, 1, maxhealth: 8, currhealth: 8, attack: 6, attacKSpeed: 5, moveSpeed: 5, star: 1, null, null, "jagob");
    private static GameUnit heroC = new WarriorUnit(8, 1, 1, maxhealth: 7, currhealth: 7, attack: 5, attacKSpeed: 6, moveSpeed: 6, star: 1, null, null, "zyn");

    private static GameUnit warriorC = new WarriorUnit(9, 1, 1, maxhealth: 6, currhealth: 6, attack: 4, attacKSpeed: 4, moveSpeed: 3, star: 1, null, null, "reen");
    private static GameUnit warriorD = new WarriorUnit(10, 1, 1, maxhealth: 10, currhealth: 10, attack: 5, attacKSpeed: 3, moveSpeed: 2, star: 1, null, null, "pear");

    private static Card card1 = new(id: unitA.id, name: "archy", description: "hello", cost: 1, unit: unitA);
    private static Card card2 = new(id: unitB.id, name: "elon", description: "", cost: 2, unit: unitB);
    private static Card card6 = new(id: warriorA.id, name: "wolve", description: "", cost: 1, unit: warriorA);
    private static Card card7 = new(id: warriorB.id, name: "yasuo", description: "", cost: 2, unit: warriorB);
    private static Card card8 = new(id: MageA.id, name: "naz", description: "", cost: 3, unit: MageA);

    private static Card card9 = new(id: heroA.id, name: "bjorn", description: "", cost: 1, unit: heroA);
    private static Card card10 = new(id: heroB.id, name: "jagob", description: "", cost: 2, unit: heroB);
    private static Card card11 = new(id: heroC.id, name: "zyn", description: "", cost: 1, unit: heroC);
    private static Card card12 = new(id: warriorC.id, name: "reen", description: "", cost: 2, unit: warriorC);
    private static Card card13 = new(id: warriorD.id, name: "pear", description: "", cost: 3, unit: warriorD);

    static CardDatabase()
    {
        // ADD CARDS HERE
        map.Add(card1.id, card1);
        map.Add(card2.id, card2);
        map.Add(card6.id, card6);
        map.Add(card7.id, card7);
        map.Add(card8.id, card8);

        map.Add(card9.id, card9);
        map.Add(card10.id, card10);
        map.Add(card11.id, card11);
        map.Add(card12.id, card12);
        map.Add(card13.id, card13);
    }

    public static Card[] GetAllCards()
    {
        return map.Values.ToArray();
    }

    public static Card GetCard(int id)
    {
        return map[id].Clone();
    }
}