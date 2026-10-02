using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Production;

namespace Game.Domain.Materials
{
    /// <summary>Danh mục nguyên liệu, sản phẩm, công thức và xưởng đã kiểm tra tính hợp lệ.</summary>
    public sealed class MaterialCatalog
    {
        private readonly IReadOnlyList<MaterialDefinition> materials;
        private readonly IReadOnlyList<ProductDefinition> products;
        private readonly IReadOnlyList<Recipe> recipes;
        private readonly IReadOnlyList<ProducerDefinition> producers;
        public IReadOnlyList<MaterialDefinition> Materials => materials;
        public IReadOnlyList<ProductDefinition> Products => products;
        public IReadOnlyList<Recipe> Recipes => recipes;
        public IReadOnlyList<ProducerDefinition> Producers => producers;

        public static MaterialCatalog Default { get; } = CreateDefault();

        public MaterialCatalog(IEnumerable<MaterialDefinition> materials,
            IEnumerable<ProductDefinition> products = null,
            IEnumerable<Recipe> recipes = null,
            IEnumerable<ProducerDefinition> producers = null)
        {
            this.materials = (materials ?? throw new ArgumentNullException(nameof(materials))).ToArray();
            this.products = (products ?? Enumerable.Empty<ProductDefinition>()).ToArray();
            this.recipes = (recipes ?? Enumerable.Empty<Recipe>()).ToArray();
            this.producers = (producers ?? Enumerable.Empty<ProducerDefinition>()).ToArray();
            Validate();
        }

        private void Validate()
        {
            EnsureUnique(materials.Select(x => x.Id.Value), "material");
            EnsureUnique(products.Select(x => x.Id.Value), "product");
            EnsureUnique(recipes.Select(x => x.Id.Value), "recipe");
            EnsureUnique(producers.Select(x => x.Id.Value), "producer");
            foreach (var material in materials)
            {
                if (!MaterialId.IsValidFamily(material.Family)) throw new ArgumentException("Nhóm nguyên liệu không hợp lệ.");
                if (material.Tier < 1 || material.Tier > 5) throw new ArgumentException("Tier nguyên liệu phải từ 1 đến 5.");
                if (material.Id != MaterialId.For(material.Family, material.Tier)) throw new ArgumentException("Mã nguyên liệu không khớp nhóm và tier.");
                if (string.IsNullOrWhiteSpace(material.Name)) throw new ArgumentException("Thiếu tên nguyên liệu.");
            }
            foreach (var product in products)
            {
                if (string.IsNullOrWhiteSpace(product.Name)) throw new ArgumentException("Thiếu tên sản phẩm.");
                if (product.Producer.HasValue && !producers.Any(x => x.Id == product.Producer.Value)) throw new ArgumentException("Xưởng của sản phẩm không tồn tại.");
            }
            foreach (var recipe in recipes)
            {
                if (recipe.Inputs == null || recipe.Inputs.Count == 0) throw new ArgumentException("Công thức phải có đầu vào.");
                if (recipe.Outputs == null || recipe.Outputs.Count == 0) throw new ArgumentException("Công thức phải có đầu ra.");
                if (!producers.Any(x => x.Id == recipe.Producer)) throw new ArgumentException("Xưởng của công thức không tồn tại.");
                foreach (var input in recipe.Inputs) ValidateItem(input.Material, input.Product, input.Quantity);
                foreach (var output in recipe.Outputs) ValidateItem(output.Material, output.Product, output.Quantity);
            }
        }

        private void ValidateItem(MaterialId? material, ProductId? product, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Số lượng công thức phải lớn hơn 0.");
            if (material.HasValue == product.HasValue) throw new ArgumentException("Mỗi dòng công thức phải tham chiếu đúng một loại vật phẩm.");
            if (material.HasValue && !materials.Any(x => x.Id == material.Value)) throw new ArgumentException("Nguyên liệu trong công thức không tồn tại.");
            if (product.HasValue && !products.Any(x => x.Id == product.Value)) throw new ArgumentException("Sản phẩm trong công thức không tồn tại.");
        }

        private static void EnsureUnique(IEnumerable<string> ids, string kind)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) throw new ArgumentException($"Mã {kind} trùng hoặc rỗng.");
        }

        private static MaterialCatalog CreateDefault()
        {
            var familyNames = new[] { "Khoáng", "Gỗ", "Vải / Da", "Đá quý", "Thảo dược", "Thực phẩm" };
            var list = new List<MaterialDefinition>();
            foreach (MaterialFamily family in Enum.GetValues(typeof(MaterialFamily)))
                for (var tier = 1; tier <= 5; tier++)
                    list.Add(new MaterialDefinition(MaterialId.For(family, tier), family, tier,
                        $"{familyNames[(int)family]} Tier {tier}"));

            var producers = new[]
            {
                new ProducerDefinition(new ProducerId("refinery"), "Nhà máy Tinh chế"),
                new ProducerDefinition(new ProducerId("reactor"), "Lò Phản Ứng"),
                new ProducerDefinition(new ProducerId("monster_forge"), "Lò Rèn"),
                new ProducerDefinition(new ProducerId("trainer_textile_workshop"), "Xưởng Dệt"),
                new ProducerDefinition(new ProducerId("aura_jeweler"), "Tiệm Kim Hoàn"),
                new ProducerDefinition(new ProducerId("hospital"), "Bệnh Viện Thú Y"),
                new ProducerDefinition(new ProducerId("restaurant"), "Nhà Hàng"),
                new ProducerDefinition(new ProducerId("bar"), "Quán Bar"),
                new ProducerDefinition(new ProducerId("tool_workshop"), "Xưởng Công Cụ"),
                new ProducerDefinition(new ProducerId("soda_factory"), "Nhà Máy Nước Ngọt"),
                new ProducerDefinition(new ProducerId("inn"), "Nhà Trọ"),
                new ProducerDefinition(new ProducerId("academy"), "Học Viện"),
                new ProducerDefinition(new ProducerId("evolution_lab"), "Phòng Thí Nghiệm Tiến Hóa")
            };
            var products = new[]
            {
                Product("blank", "Phôi liệu", "refinery"), Product("enhancement_stone", "Đá Cường hóa", "reactor"),
                Product("distilled_water", "Nước Cất", "reactor"), Product("evolution_stone", "Đá Tiến Hóa", "reactor"),
                Product("potion", "Thuốc", "hospital"), Product("vaccine", "Vắc-xin", "hospital"),
                Product("tranquilizer", "Thuốc An Thần", "hospital"), Product("food_drink", "Đồ ăn, nước uống", "restaurant"),
                Product("reward_cake", "Bánh thưởng", "restaurant"), Product("liquor", "Rượu", "bar"),
                Product("raincoat", "Áo mưa", "tool_workshop"), Product("gas_mask", "Mặt nạ phòng độc", "tool_workshop"),
                Product("capture_ball", "Bóng bắt thú", "tool_workshop"), Product("trap", "Bẫy", "tool_workshop"),
                Product("tactics_book", "Sách Chiến Thuật", "tool_workshop"), Product("monster_buff_bottle", "Bình nước buff", "soda_factory"),
                Product("pet_communication_lock", "Khóa Giao Tiếp Thú Cưng", "academy"),
                Product("overclock_coffee", "Cà phê ép xung", "inn"),
                Product("monster_gear", "Trang bị Monster", "monster_forge"),
                Product("trainer_utility_gear", "Trang bị Tiện ích Trainer", "trainer_textile_workshop"),
                Product("aura_gear", "Trang bị Hào quang", "aura_jeweler"),
                Product("mutation_core", "Lõi Đột Biến", null), Product("gene_fragment", "Gene Fragments", null),
                Product("world_boss_crystal", "Tinh Thể Boss Thế Giới", null), Product("broken_relic", "Cổ vật vỡ", null)
            };
            return new MaterialCatalog(list, products, producers: producers);
        }

        private static ProductDefinition Product(string id, string name, string producer)
            => new ProductDefinition(new ProductId(id), name, producer == null ? (ProducerId?)null : new ProducerId(producer));
    }
}
