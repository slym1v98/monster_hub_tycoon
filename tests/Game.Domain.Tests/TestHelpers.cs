using Game.Domain;

/// <summary>Tạo Trainer tối giản cho test đơn vị.</summary>
public static class TestTrainers
{
    public static Trainer Make(int id = 0, Personality personality = Personality.Timid, long wage = 1000) => new Trainer
    {
        Id = id, Rarity = Rarity.Common, Personality = personality,
        TeamHp = 300, TeamHpMax = 300, BackpackCapacity = 30, ContractWage = wage,
    };
}
