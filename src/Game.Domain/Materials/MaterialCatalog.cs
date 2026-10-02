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
            if (materials.Any(x => x == null) || products.Any(x => x == null) || recipes.Any(x => x == null) || producers.Any(x => x == null))
                throw new ArgumentException("Danh mục không được chứa định nghĩa null.");
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
                if (string.IsNullOrWhiteSpace(product.Unit)) throw new ArgumentException("Thiếu đơn vị sản phẩm.");
                if (product.Producer.HasValue && !producers.Any(x => x.Id == product.Producer.Value)) throw new ArgumentException("Xưởng của sản phẩm không tồn tại.");
            }
            foreach (var producer in producers)
                if (string.IsNullOrWhiteSpace(producer.Name)) throw new ArgumentException("Thiếu tên xưởng.");
            foreach (var recipe in recipes)
            {
                if (string.IsNullOrWhiteSpace(recipe.Name)) throw new ArgumentException("Thiếu tên công thức.");
                if (recipe.Inputs == null || recipe.Inputs.Count == 0) throw new ArgumentException("Công thức phải có đầu vào.");
                if (recipe.Outputs == null || recipe.Outputs.Count == 0) throw new ArgumentException("Công thức phải có đầu ra.");
                if (recipe.DurationMinutes.HasValue && recipe.DurationMinutes.Value <= 0) throw new ArgumentException("Thời lượng recipe phải lớn hơn 0.");
                if (recipe.OperatingCost.HasValue && recipe.OperatingCost.Value < 0) throw new ArgumentException("Chi phí recipe không được âm.");
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
            var products = new List<ProductDefinition>
            {
                Product("enhancement_stone", "Đá Cường hóa", "reactor"),
                Product("distilled_water", "Nước Cất", "reactor"), Product("evolution_stone", "Đá Tiến Hóa", "reactor"),
                Product("potion", "Thuốc", "hospital"), Product("vaccine", "Vắc-xin", "hospital"),
                Product("tranquilizer", "Thuốc An Thần", "hospital"), Product("food_drink", "Đồ ăn, nước uống", "restaurant"),
                Product("reward_cake", "Bánh thưởng", "restaurant"), Product("liquor", "Rượu", "bar"),
                Product("raincoat", "Áo mưa", "tool_workshop"), Product("gas_mask", "Mặt nạ phòng độc", "tool_workshop"),
                Product("capture_ball", "Bóng bắt thú", "tool_workshop"), Product("trap", "Bẫy", "tool_workshop"),
                Product("tactics_book", "Sách Chiến Thuật", "tool_workshop"), Product("monster_buff_bottle", "Bình nước buff", "soda_factory", ProductEffectKind.TemporaryMonsterStatBuff),
                Product("pet_communication_lock", "Khóa Giao Tiếp Thú Cưng", "academy"),
                Product("overclock_coffee", "Cà phê ép xung", "inn"),
                Product("monster_gear", "Trang bị Monster", "monster_forge"),
                Product("trainer_utility_gear", "Trang bị Tiện ích Trainer", "trainer_textile_workshop"),
                Product("aura_gear", "Trang bị Hào quang", "aura_jeweler"),
                Product("mutation_core", "Lõi Đột Biến", null), Product("gene_fragment", "Gene Fragments", null),
                Product("world_boss_crystal", "Tinh Thể Boss Thế Giới", null), Product("broken_relic", "Cổ vật vỡ", null)
            };
            var blanks = new List<ProductDefinition>();
            var recipes = new List<Recipe>();
            foreach (var material in list.Where(x => x.Family == MaterialFamily.Ore || x.Family == MaterialFamily.ClothLeather || x.Family == MaterialFamily.Gem))
            {
                var blankId = new ProductId($"blank_{MaterialId.FamilyKey(material.Family)}_tier_{material.Tier}");
                var name = $"Phôi {familyNames[(int)material.Family]} Tier {material.Tier}";
                blanks.Add(new ProductDefinition(blankId, name, new ProducerId("refinery")));
                recipes.Add(new Recipe(new RecipeId($"refine_{material.Id.Value}_to_{blankId.Value}"),
                    $"Tinh chế {material.Name} thành {name}", new ProducerId("refinery"),
                    new[] { new RecipeInput(material.Id, 1) }, new[] { new RecipeOutput(blankId, 1) }));
            }
            products.AddRange(blanks);
            var reactorOutputs = new[]
            {
                (new ProductId("enhancement_stone"), "Đá Cường hóa"),
                (new ProductId("distilled_water"), "Nước Cất"),
                (new ProductId("evolution_stone"), "Đá Tiến Hóa")
            };
            foreach (var blank in blanks)
                foreach (var output in reactorOutputs)
                    recipes.Add(new Recipe(new RecipeId($"reactor_{blank.Id.Value}_to_{output.Item1.Value}"),
                        $"Chế {output.Item2} từ {blank.Name}", new ProducerId("reactor"),
                        new[] { new RecipeInput(blank.Id, 1) }, new[] { new RecipeOutput(output.Item1, 1) }));
            AddConsumableRecipe(recipes, "hospital", "potion", "Chế Thuốc từ Thảo dược", MaterialFamily.Herb);
            AddConsumableRecipe(recipes, "hospital", "vaccine", "Chế Vắc-xin từ Thảo dược", MaterialFamily.Herb);
            AddConsumableRecipe(recipes, "hospital", "tranquilizer", "Chế Thuốc An Thần từ Thảo dược", MaterialFamily.Herb);
            AddConsumableRecipe(recipes, "restaurant", "food_drink", "Chế Đồ ăn, nước uống từ Thực phẩm", MaterialFamily.Food);
            AddConsumableRecipe(recipes, "restaurant", "reward_cake", "Chế Bánh thưởng từ Thực phẩm", MaterialFamily.Food);
            AddConsumableRecipe(recipes, "bar", "liquor", "Chế Rượu từ Thực phẩm", MaterialFamily.Food);
            AddConsumableRecipe(recipes, "soda_factory", "monster_buff_bottle", "Chế Bình nước buff từ Thực phẩm", MaterialFamily.Food);
            AddConsumableRecipe(recipes, "inn", "overclock_coffee", "Chế Cà phê ép xung từ Thực phẩm", MaterialFamily.Food);
            AddProductInputRecipe(recipes, "tool_workshop", "raincoat", "Chế Áo mưa từ Phôi", new ProductId("blank_ore_tier_1"));
            AddProductInputRecipe(recipes, "tool_workshop", "gas_mask", "Chế Mặt nạ phòng độc từ Phôi", new ProductId("blank_ore_tier_1"));
            AddProductInputRecipe(recipes, "tool_workshop", "tactics_book", "Chế Sách Chiến Thuật từ Phôi", new ProductId("blank_ore_tier_1"));
            AddToolRecipe(recipes, "capture_ball", "Bóng bắt thú");
            AddToolRecipe(recipes, "trap", "Bẫy");
            return new MaterialCatalog(list, products, recipes, producers);
        }

        private static void AddConsumableRecipe(List<Recipe> recipes, string producer, string product, string name, MaterialFamily family)
            => recipes.Add(new Recipe(new RecipeId($"{producer}_{product}"), name, new ProducerId(producer),
                new[] { new RecipeInput(MaterialId.For(family, 1), 1) },
                new[] { new RecipeOutput(new ProductId(product), 1) }));

        private static void AddProductInputRecipe(List<Recipe> recipes, string producer, string product, string name, ProductId input)
            => recipes.Add(new Recipe(new RecipeId($"{producer}_{product}"), name, new ProducerId(producer),
                new[] { new RecipeInput(input, 1) }, new[] { new RecipeOutput(new ProductId(product), 1) }));

        private static void AddToolRecipe(List<Recipe> recipes, string product, string name)
            => recipes.Add(new Recipe(new RecipeId($"tool_workshop_{product}"), name, new ProducerId("tool_workshop"),
                new[] { new RecipeInput(new ProductId("blank_ore_tier_1"), 1),
                    new RecipeInput(MaterialId.For(MaterialFamily.Wood, 1), 1) },
                new[] { new RecipeOutput(new ProductId(product), 1) }));

        private static ProductDefinition Product(string id, string name, string producer,
            ProductEffectKind effectKind = ProductEffectKind.None)
            => new ProductDefinition(new ProductId(id), name,
                producer == null ? (ProducerId?)null : new ProducerId(producer), effectKind: effectKind);
    }
}
