using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed record MonsterRecoveryStarted(int Minute, int TrainerId, string MonsterId, int RecoveryId,
        int CompleteAtMinute, long Fee, bool IsCapturedMonster) : IDomainEvent;
    public sealed record MonsterRecoveryCompleted(int Minute, int TrainerId, string MonsterId,
        bool IsCapturedMonster, bool AddedToStorage) : IDomainEvent;
    public sealed record GeneBankOwnershipChanged(int Minute, int TrainerId, string MonsterId,
        MonsterCustody From, MonsterCustody To) : IDomainEvent;
    public sealed record GeneBankMonsterResold(int Minute, int BuyerId, string MonsterId, long Price) : IDomainEvent;
}
