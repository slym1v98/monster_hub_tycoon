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
        public bool Fainted => CurrentHp == 0;
        public RebellionOutcome Rebellion { get; }
        public IReadOnlyDictionary<string, int> Cooldowns { get; }

        internal BattleMonsterState(MonsterId id, BattleSide side, long currentHp, IDictionary<string, int> cooldowns, RebellionOutcome rebellion = RebellionOutcome.None)
        {
            Id = id;
            Side = side;
            CurrentHp = currentHp;
            Rebellion = rebellion;
            Cooldowns = new ReadOnlyDictionary<string, int>(new SortedDictionary<string, int>(cooldowns, StringComparer.Ordinal));
        }
    }

    /// <summary>Kết quả cùng log có thứ tự, ảnh chụp cuối và trạng thái RNG để lưu/tiếp tục mô phỏng.</summary>
    public sealed class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public MonsterId? ActiveId { get; }
        /// <summary>True only when the team has exactly three Monsters and all three have fainted.</summary>
        public bool TeamDown { get; }
        public int CompletedRounds { get; }
        public IReadOnlyList<BattleAction> Actions { get; }
        public IReadOnlyList<BattleMonsterState> FinalMonsters { get; }
        public ulong InitialRandomState { get; }
        public ulong FinalRandomState { get; }

        internal BattleResult(BattleOutcome outcome, int completedRounds, IEnumerable<BattleAction> actions,
            IEnumerable<BattleMonsterState> finalMonsters, ulong initialRandomState, ulong finalRandomState, MonsterId? activeId)
        {
            Outcome = outcome;
            CompletedRounds = completedRounds;
            Actions = Array.AsReadOnly(actions.ToArray());
            FinalMonsters = Array.AsReadOnly(finalMonsters.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray());
            ActiveId = activeId;
            TeamDown = FinalMonsters.Count(x => x.Side == BattleSide.Team) == 3
                && FinalMonsters.Where(x => x.Side == BattleSide.Team).All(x => x.Fainted);
            InitialRandomState = initialRandomState;
            FinalRandomState = finalRandomState;
        }
    }
}
