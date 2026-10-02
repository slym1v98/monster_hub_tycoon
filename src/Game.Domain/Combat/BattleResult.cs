using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public enum BattleSide { Team, Opponents }
    public enum BattleOutcome { TeamWon, OpponentsWon, Draw, RoundLimit }

    /// <summary>Trạng thái cuối độc lập theo Monster; không đổi HP của ảnh chụp đầu vào.</summary>
    public sealed class BattleMonsterState
    {
        public MonsterId Id { get; }
        public BattleSide Side { get; }
        public long CurrentHp { get; }
        public IReadOnlyDictionary<string, int> Cooldowns { get; }

        internal BattleMonsterState(MonsterId id, BattleSide side, long currentHp, IDictionary<string, int> cooldowns)
        {
            Id = id;
            Side = side;
            CurrentHp = currentHp;
            Cooldowns = new ReadOnlyDictionary<string, int>(new SortedDictionary<string, int>(cooldowns, StringComparer.Ordinal));
        }
    }

    /// <summary>Kết quả cùng log có thứ tự, ảnh chụp cuối và trạng thái RNG để lưu/tiếp tục mô phỏng.</summary>
    public sealed class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public int CompletedRounds { get; }
        public IReadOnlyList<BattleAction> Actions { get; }
        public IReadOnlyList<BattleMonsterState> FinalMonsters { get; }
        public ulong InitialRandomState { get; }
        public ulong FinalRandomState { get; }

        internal BattleResult(BattleOutcome outcome, int completedRounds, IEnumerable<BattleAction> actions,
            IEnumerable<BattleMonsterState> finalMonsters, ulong initialRandomState, ulong finalRandomState)
        {
            Outcome = outcome;
            CompletedRounds = completedRounds;
            Actions = Array.AsReadOnly(actions.ToArray());
            FinalMonsters = Array.AsReadOnly(finalMonsters.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray());
            InitialRandomState = initialRandomState;
            FinalRandomState = finalRandomState;
        }
    }
}
