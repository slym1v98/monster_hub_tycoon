using Game.Domain.Materials;

namespace Game.Domain.Production
{
    /// <summary>Nguyên liệu hoặc sản phẩm cần cho một công thức.</summary>
    public sealed class RecipeInput
    {
        public MaterialId? Material { get; }
        public ProductId? Product { get; }
        public int Quantity { get; }
        public RecipeInput(MaterialId material, int quantity) { Material = material; Quantity = quantity; }
        public RecipeInput(ProductId product, int quantity) { Product = product; Quantity = quantity; }
    }

    /// <summary>Nguyên liệu hoặc sản phẩm do một công thức tạo ra.</summary>
    public sealed class RecipeOutput
    {
        public MaterialId? Material { get; }
        public ProductId? Product { get; }
        public int Quantity { get; }
        public RecipeOutput(MaterialId material, int quantity) { Material = material; Quantity = quantity; }
        public RecipeOutput(ProductId product, int quantity) { Product = product; Quantity = quantity; }
    }

    /// <summary>Công thức sản xuất; thời gian và chi phí chờ có số liệu trong GDD.</summary>
    public sealed class Recipe
    {
        public RecipeId Id { get; }
        public string Name { get; }
        public ProducerId Producer { get; }
        public System.Collections.Generic.IReadOnlyList<RecipeInput> Inputs { get; }
        public System.Collections.Generic.IReadOnlyList<RecipeOutput> Outputs { get; }
        public int? DurationMinutes { get; }
        public long? OperatingCost { get; }
        public Recipe(RecipeId id, string name, ProducerId producer,
            System.Collections.Generic.IReadOnlyList<RecipeInput> inputs,
            System.Collections.Generic.IReadOnlyList<RecipeOutput> outputs,
            int? durationMinutes = null, long? operatingCost = null)
        {
            if (durationMinutes.HasValue && durationMinutes.Value <= 0) throw new System.ArgumentOutOfRangeException(nameof(durationMinutes));
            if (operatingCost.HasValue && operatingCost.Value < 0) throw new System.ArgumentOutOfRangeException(nameof(operatingCost));
            Id = id; Name = name; Producer = producer;
            Inputs = inputs == null ? null : System.Array.AsReadOnly(new System.Collections.Generic.List<RecipeInput>(inputs).ToArray());
            Outputs = outputs == null ? null : System.Array.AsReadOnly(new System.Collections.Generic.List<RecipeOutput>(outputs).ToArray());
            DurationMinutes = durationMinutes; OperatingCost = operatingCost;
        }
    }
}
