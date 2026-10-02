using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Monsters
{
    /// <summary>Danh mục loài đã kiểm tra, luôn duyệt theo mã loài ordinal.</summary>
    public sealed class MonsterCatalog
    {
        public IReadOnlyList<MonsterDefinition> Definitions { get; }
        public static MonsterCatalog Default { get; } = CreateDefault();

        public MonsterCatalog(IEnumerable<MonsterDefinition> definitions)
        {
            var entries = (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray();
            if (entries.Length == 0) throw new ArgumentException("Danh mục Monster không được rỗng.", nameof(definitions));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in entries)
            {
                if (definition == null) throw new ArgumentException("Định nghĩa Monster không được null.", nameof(definitions));
                definition.Validate();
                if (!ids.Add(definition.Id)) throw new ArgumentException("Mã loài Monster bị trùng.", nameof(definitions));
            }
            Definitions = Array.AsReadOnly(entries.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
        }

        /// <summary>Loài khởi đầu Prototype; HP kế thừa cấu hình tương thích cho đến tác vụ 9.</summary>
        public static MonsterCatalog CreateDefault(long starterHp = 300) => new MonsterCatalog(new[]
        {
            new MonsterDefinition("starter_sprout", "Starter Sprout", MonsterElement.Grass, MonsterRole.Support,
                new MonsterStats(starterHp, 10, 10, 1, 0.05), new MonsterStats(0, 0, 0, 0, 0))
        });
    }
}
