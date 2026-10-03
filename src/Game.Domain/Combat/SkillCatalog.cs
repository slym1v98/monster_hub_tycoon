using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Danh mục kỹ năng đã kiểm tra; mã và thứ tự duyệt dùng so sánh ordinal.</summary>
    public sealed class SkillCatalog
    {
        private readonly Dictionary<string, SkillDefinition> byId;
        public IReadOnlyList<SkillDefinition> Definitions { get; }
        /// <summary>Các mã hiệu ứng do dữ liệu khai báo; tác vụ giải trận sẽ định nghĩa cách áp dụng.</summary>
        public IReadOnlyList<string> EffectIds { get; }
        public static SkillCatalog Prototype { get; } = new SkillCatalog(new[]
        {
            // Bộ đánh đơn Prototype; sức mạnh 10 và hồi chiêu 0 là dữ liệu tạm, không phải giá trị GDD.
            new SkillDefinition("fire_strike", MonsterElement.Fire, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("water_strike", MonsterElement.Water, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("grass_strike", MonsterElement.Grass, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("electric_strike", MonsterElement.Electric, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("ice_strike", MonsterElement.Ice, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("poison_strike", MonsterElement.Poison, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("ground_strike", MonsterElement.Ground, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("light_strike", MonsterElement.Light, 10, 0, SkillTargetRule.SingleEnemy),
            new SkillDefinition("dark_strike", MonsterElement.Dark, 10, 0, SkillTargetRule.SingleEnemy)
        });

        public SkillCatalog(IEnumerable<SkillDefinition> definitions, IEnumerable<string> effectIds = null)
        {
            var entries = (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray();
            if (entries.Length == 0) throw new ArgumentException("Danh mục kỹ năng không được rỗng.", nameof(definitions));
            var effects = new HashSet<string>(StringComparer.Ordinal);
            foreach (var effectId in effectIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Mã hiệu ứng không được rỗng.", nameof(effectIds));
                if (!effects.Add(effectId)) throw new ArgumentException("Mã hiệu ứng bị trùng.", nameof(effectIds));
            }
            byId = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
            foreach (var definition in entries)
            {
                if (definition == null) throw new ArgumentException("Định nghĩa kỹ năng không được null.", nameof(definitions));
                definition.Validate();
                if (byId.ContainsKey(definition.Id)) throw new ArgumentException("Mã kỹ năng bị trùng.", nameof(definitions));
                if (definition.EffectId != null && !effects.Contains(definition.EffectId))
                    throw new ArgumentException("Kỹ năng tham chiếu mã hiệu ứng chưa khai báo.", nameof(definitions));
                byId.Add(definition.Id, definition);
            }
            Definitions = Array.AsReadOnly(entries.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
            EffectIds = Array.AsReadOnly(effects.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }

        public SkillDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Mã kỹ năng không được rỗng.", nameof(id));
            if (!byId.TryGetValue(id, out var definition)) throw new KeyNotFoundException("Mã kỹ năng chưa được khai báo: " + id);
            return definition;
        }

        /// <summary>Kiểm tra mọi tham chiếu kỹ năng và giữ thứ tự bộ kỹ năng do dữ liệu quy định.</summary>
        public IReadOnlyList<SkillDefinition> ResolveLoadout(IEnumerable<string> skillIds)
        {
            if (skillIds == null) throw new ArgumentNullException(nameof(skillIds));
            return Array.AsReadOnly(skillIds.Select(Get).ToArray());
        }
    }
}
