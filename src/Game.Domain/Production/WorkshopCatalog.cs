using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;

namespace Game.Domain.Production
{
    public enum EquipmentWorkshopGroup { MonsterCombat, TrainerUtility, Aura }

    /// <summary>Nhóm slot mà một xưởng trang bị chế tạo theo GDD 05.</summary>
    public sealed class EquipmentWorkshopDefinition
    {
        public ProducerId Producer { get; }
        public EquipmentWorkshopGroup Group { get; }
        public int SlotsPerOwner { get; }
        public string OwnerUnit { get; }
        public ProductId EquipmentProduct { get; }
        public EquipmentWorkshopDefinition(ProducerId producer, EquipmentWorkshopGroup group,
            int slotsPerOwner, string ownerUnit, ProductId equipmentProduct)
        {
            if (slotsPerOwner <= 0) throw new ArgumentOutOfRangeException(nameof(slotsPerOwner));
            if (string.IsNullOrWhiteSpace(ownerUnit)) throw new ArgumentException("Thiếu đơn vị chủ sở hữu.", nameof(ownerUnit));
            Producer = producer; Group = group; SlotsPerOwner = slotsPerOwner; OwnerUnit = ownerUnit; EquipmentProduct = equipmentProduct;
        }
    }

    public sealed class EquipmentWorkshopCatalog
    {
        public IReadOnlyList<EquipmentWorkshopDefinition> Workshops { get; }
        private EquipmentWorkshopCatalog(IEnumerable<EquipmentWorkshopDefinition> workshops)
        { Workshops = Array.AsReadOnly(workshops.ToArray()); }

        public static EquipmentWorkshopCatalog Default { get; } = new EquipmentWorkshopCatalog(new[]
        {
            new EquipmentWorkshopDefinition(new ProducerId("monster_forge"), EquipmentWorkshopGroup.MonsterCombat, 6, "Monster", new ProductId("monster_gear")),
            new EquipmentWorkshopDefinition(new ProducerId("trainer_textile_workshop"), EquipmentWorkshopGroup.TrainerUtility, 6, "Trainer", new ProductId("trainer_utility_gear")),
            new EquipmentWorkshopDefinition(new ProducerId("aura_jeweler"), EquipmentWorkshopGroup.Aura, 6, "Trainer", new ProductId("aura_gear"))
        });
    }

    /// <summary>Một điểm bán cho sản phẩm do xưởng được chỉ định tạo ra.</summary>
    public sealed class ConsumableStallDefinition
    {
        public string ShopId { get; }
        public ProducerId Producer { get; }
        public IReadOnlyList<ProductId> Products { get; }
        public ConsumableStallDefinition(string shopId, ProducerId producer, IEnumerable<ProductId> products)
        {
            if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("Thiếu mã quầy.", nameof(shopId));
            if (products == null) throw new ArgumentNullException(nameof(products));
            var list = products.ToArray();
            if (list.Length == 0 || list.Distinct().Count() != list.Length) throw new ArgumentException("Quầy cần sản phẩm phân biệt.", nameof(products));
            ShopId = shopId; Producer = producer; Products = Array.AsReadOnly(list);
        }
    }

    public sealed class ConsumableStallCatalog
    {
        public IReadOnlyList<ConsumableStallDefinition> Stalls { get; }
        private ConsumableStallCatalog(IEnumerable<ConsumableStallDefinition> stalls)
        { Stalls = Array.AsReadOnly(stalls.ToArray()); }

        public static ConsumableStallCatalog Default { get; } = new ConsumableStallCatalog(new[]
        {
            Stall("veterinary_hospital", "hospital", "potion", "vaccine", "tranquilizer"),
            Stall("restaurant", "restaurant", "food_drink", "reward_cake"),
            Stall("bar", "bar", "liquor"),
            Stall("tool_workshop", "tool_workshop", "raincoat", "gas_mask", "capture_ball", "trap", "tactics_book"),
            Stall("inn", "inn", "overclock_coffee"),
            Stall("general_store", "soda_factory", "monster_buff_bottle")
        });

        private static ConsumableStallDefinition Stall(string shop, string producer, params string[] products)
            => new ConsumableStallDefinition(shop, new ProducerId(producer), products.Select(x => new ProductId(x)));
    }
}
