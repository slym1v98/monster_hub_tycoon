using Game.Domain;
using Game.Domain.Monsters;

/// <summary>Tạo Trainer tối giản cho test đơn vị.</summary>
public static class TestTrainers
{
    public static Trainer Make(int id = 0, Personality personality = Personality.Timid, long wage = 1000)
    {
        var trainer = new Trainer
        {
            Id = id, Rarity = Rarity.Common, Personality = personality,
            BackpackCapacity = 30, ContractWage = wage,
        };
        trainer.Roster.Add(Monster.Create(new MonsterId("test_starter"), MonsterCatalog.Default.Definitions[0],
            trainer.Rarity, MonsterIvGrade.B, 1, id, isSoulBound: true));
        return trainer;
    }
}
