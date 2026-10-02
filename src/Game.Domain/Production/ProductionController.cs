using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Supply;

namespace Game.Domain.Production
{
    public enum ProductionJobState { Running, Completed, Cancelled }

    /// <summary>Tham số chạy xưởng; mọi số hiện tại là prototype, chưa cân bằng.</summary>
    public sealed class ProductionConfig
    {
        private readonly decimal[] refineryEfficiencyByLevel;
        private readonly decimal[] jobDurationMultiplierByLevel;
        public int DefaultJobDurationMinutes { get; }
        public long DefaultOperatingCost { get; }
        public IReadOnlyList<decimal> RefineryEfficiencyByLevel => Array.AsReadOnly(refineryEfficiencyByLevel);
        public IReadOnlyList<decimal> JobDurationMultiplierByLevel => Array.AsReadOnly(jobDurationMultiplierByLevel);
        public int DefaultConcurrentJobsPerProducer { get; }

        public ProductionConfig(int defaultJobDurationMinutes = 60, long defaultOperatingCost = 0,
            decimal[] refineryEfficiencyByLevel = null, int defaultConcurrentJobsPerProducer = 1)
            : this(defaultJobDurationMinutes, defaultOperatingCost, refineryEfficiencyByLevel,
                defaultConcurrentJobsPerProducer, null)
        { }

        public ProductionConfig(decimal[] jobDurationMultiplierByLevel)
            : this(60, 0, null, 1, jobDurationMultiplierByLevel)
        { }

        public ProductionConfig(int defaultJobDurationMinutes, long defaultOperatingCost,
            decimal[] refineryEfficiencyByLevel, int defaultConcurrentJobsPerProducer,
            decimal[] jobDurationMultiplierByLevel)
        {
            if (defaultJobDurationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(defaultJobDurationMinutes));
            if (defaultOperatingCost < 0) throw new ArgumentOutOfRangeException(nameof(defaultOperatingCost));
            if (defaultConcurrentJobsPerProducer <= 0) throw new ArgumentOutOfRangeException(nameof(defaultConcurrentJobsPerProducer));
            this.refineryEfficiencyByLevel = refineryEfficiencyByLevel == null
                ? new[] { 0.85m, 0.885m, 0.92m, 0.955m, 0.99m }
                : (decimal[])refineryEfficiencyByLevel.Clone();
            if (this.refineryEfficiencyByLevel.Length != 5 || this.refineryEfficiencyByLevel.Any(x => x <= 0 || x > 1))
                throw new ArgumentException("Hiệu suất Tinh chế cần đúng 5 giá trị trong khoảng (0..1].", nameof(refineryEfficiencyByLevel));
            this.jobDurationMultiplierByLevel = jobDurationMultiplierByLevel == null
                ? new[] { 1.0m, 0.9m, 0.8m, 0.7m, 0.6m }
                : (decimal[])jobDurationMultiplierByLevel.Clone();
            if (this.jobDurationMultiplierByLevel.Length != 5 || this.jobDurationMultiplierByLevel.Any(x => x <= 0 || x > 1))
                throw new ArgumentException("Multiplier thời lượng cần đúng 5 giá trị trong khoảng (0..1].", nameof(jobDurationMultiplierByLevel));
            DefaultJobDurationMinutes = defaultJobDurationMinutes;
            DefaultOperatingCost = defaultOperatingCost;
            DefaultConcurrentJobsPerProducer = defaultConcurrentJobsPerProducer;
        }
    }

    /// <summary>Một công việc xưởng đã giữ nguyên liệu và có giờ hoàn thành cố định.</summary>
    public sealed class ProductionJob
    {
        public long Id { get; }
        public Recipe Recipe { get; }
        public int ProducerLevel { get; }
        public int StartMinute { get; }
        public int FinishMinute { get; }
        public ProductionJobState State { get; internal set; }
        public IReadOnlyList<InventoryItemQuantity> Inputs { get; }
        public IReadOnlyList<InventoryItemQuantity> BaseOutputs { get; }
        public IReadOnlyList<InventoryItemQuantity> ActualOutputs { get; internal set; }

        internal ProductionJob(long id, Recipe recipe, int producerLevel, int startMinute, int finishMinute,
            IReadOnlyList<InventoryItemQuantity> inputs, IReadOnlyList<InventoryItemQuantity> outputs)
        {
            Id = id; Recipe = recipe; ProducerLevel = producerLevel; StartMinute = startMinute; FinishMinute = finishMinute;
            Inputs = Array.AsReadOnly(inputs.ToArray()); BaseOutputs = Array.AsReadOnly(outputs.ToArray());
            ActualOutputs = Array.Empty<InventoryItemQuantity>();
            State = ProductionJobState.Running;
        }
    }

    /// <summary>Thiếu hụt đầu vào sau khi truy ngược công thức tới nguyên liệu thô.</summary>
    public sealed record ProductionRestockDemand(InventoryItem Item, int Quantity);

    /// <summary>Điều phối mục tiêu tồn kho và job sản xuất theo thời điểm mô phỏng.</summary>
    public sealed class ProductionController
    {
        private sealed class ProducerRuntime
        {
            public ProducerDefinition Definition;
            public int Level = 1;
            public int Capacity;
        }

        private readonly Dictionary<ProductId, int> targets = new Dictionary<ProductId, int>();
        private readonly HashSet<ProductId> pausedTargets = new HashSet<ProductId>();
        private readonly Dictionary<ProducerId, ProducerRuntime> producers;
        private readonly Recipe[] recipes;
        private readonly HashSet<ProductId> knownProducts;
        private readonly Inventory inventory;
        private readonly MoneyLedger ledger;
        private readonly ProductionConfig config;
        private readonly TreasuryAccount treasury;
        private readonly List<ProductionJob> jobs = new List<ProductionJob>();
        private readonly Dictionary<string, decimal> yieldRemainders = new Dictionary<string, decimal>(StringComparer.Ordinal);
        private ProductionRestockDemand[] restockDemands = Array.Empty<ProductionRestockDemand>();
        private long nextJobId = 1;

        public int CurrentMinute { get; private set; }
        public IReadOnlyList<ProductionJob> Jobs => jobs.AsReadOnly();
        public IReadOnlyList<ProductionJob> ActiveJobs => jobs.Where(x => x.State == ProductionJobState.Running)
            .OrderBy(x => x.FinishMinute).ThenBy(x => x.Id).ToArray();
        public Inventory Inventory => inventory;
        public IReadOnlyList<ProductionRestockDemand> RestockDemands => restockDemands;
        public IReadOnlyDictionary<ProductId, int> Targets => new Dictionary<ProductId, int>(targets);

        public ProductionController(MaterialCatalog catalog, Inventory inventory, MoneyLedger ledger, ProductionConfig config)
            : this(catalog, inventory, ledger, config, null)
        { }

        public ProductionController(MaterialCatalog catalog, Inventory inventory, MoneyLedger ledger, ProductionConfig config,
            TreasuryAccount treasury)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.treasury = treasury;
            recipes = catalog.Recipes.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray();
            knownProducts = new HashSet<ProductId>(catalog.Products.Select(x => x.Id));
            producers = catalog.Producers.ToDictionary(x => x.Id, x => new ProducerRuntime
            {
                Definition = x, Capacity = x.MaximumConcurrentJobs ?? config.DefaultConcurrentJobsPerProducer
            });
        }

        public void SetProducerState(ProducerId producer, int level, int concurrentJobs)
        {
            if (!producers.TryGetValue(producer, out var runtime)) throw new ArgumentException("Xưởng không tồn tại.", nameof(producer));
            if (level < 1 || level > 5) throw new ArgumentOutOfRangeException(nameof(level));
            if (concurrentJobs <= 0) throw new ArgumentOutOfRangeException(nameof(concurrentJobs));
            runtime.Level = level; runtime.Capacity = concurrentJobs;
            Reconcile();
        }

        public void SetTarget(ProductId product, int target, int now)
        {
            if (!knownProducts.Contains(product)) throw new ArgumentException("Sản phẩm không có trong catalog.", nameof(product));
            if (target < 0) throw new ArgumentOutOfRangeException(nameof(target));
            AdvanceTo(now);
            pausedTargets.Remove(product);
            targets[product] = target;
            Reconcile();
        }

        public void AdvanceTo(int minute)
        {
            if (minute < CurrentMinute) throw new ArgumentOutOfRangeException(nameof(minute), "Thời điểm mô phỏng không thể lùi.");
            while (true)
            {
                var dueMinute = ActiveJobs.Where(x => x.FinishMinute <= minute).Select(x => (int?)x.FinishMinute).Min();
                if (!dueMinute.HasValue) break;
                CurrentMinute = dueMinute.Value;
                var due = ActiveJobs.Where(x => x.FinishMinute == CurrentMinute).OrderBy(x => x.Id).ToArray();
                foreach (var job in due) CompleteJob(job.Id);
                Reconcile();
            }
            CurrentMinute = minute;
            Reconcile();
        }

        public void CompleteJob(long jobId)
        {
            var job = FindRunningJob(jobId);
            if (CurrentMinute < job.FinishMinute) throw new InvalidOperationException("Chưa tới thời điểm hoàn thành job.");
            var nextRemainders = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var actualOutputs = CalculateOutputs(job, nextRemainders);
            inventory.ValidateProductionCompletion(job.Inputs, actualOutputs);
            if (treasury == null)
            {
                var cost = job.Recipe.OperatingCost ?? config.DefaultOperatingCost;
                if (cost > 0) ledger.Record("hub:treasury", "world:production-cost", "world:tax-sink", cost, 0, "production operating cost");
            }
            inventory.CompleteProductionMany(job.Inputs, actualOutputs);
            job.ActualOutputs = Array.AsReadOnly(actualOutputs.ToArray());
            foreach (var remainder in nextRemainders) yieldRemainders[remainder.Key] = remainder.Value;
            job.State = ProductionJobState.Completed;
        }

        public bool CancelJob(long jobId)
        {
            var job = jobs.FirstOrDefault(x => x.Id == jobId);
            if (job == null || job.State != ProductionJobState.Running) return false;
            inventory.CancelProductionMany(job.Inputs, returnInputs: true);
            job.State = ProductionJobState.Cancelled;
            foreach (var output in job.Recipe.Outputs)
                if (output.Product.HasValue) pausedTargets.Add(output.Product.Value);
            Reconcile();
            return true;
        }

        private void Reconcile()
        {
            var demands = new Dictionary<InventoryItem, int>();
            var madeProgress = true;
            while (madeProgress)
            {
                madeProgress = false;
                foreach (var target in targets.OrderBy(x => x.Key.Value, StringComparer.Ordinal).ToArray())
                {
                    if (pausedTargets.Contains(target.Key)) continue;
                    var matching = recipes.Where(x => x.Outputs.Any(o => o.Product == target.Key))
                        .OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray();
                    if (matching.Length == 0) continue;
                    var recipe = matching.FirstOrDefault(x => HasInputs(x.Inputs.Select(ToInventoryQuantity).ToArray())) ?? matching[0];
                    var runtime = producers[recipe.Producer];
                    while (ProjectedOutput(target.Key) < target.Value && ActiveCount(recipe.Producer) < runtime.Capacity)
                    {
                        var inputNeeds = recipe.Inputs.Select(ToInventoryQuantity).ToArray();
                        if (!HasInputs(inputNeeds))
                        {
                            foreach (var need in inputNeeds)
                                AddExpandedDemand(need.Item, Math.Max(0, need.Quantity - inventory.Get(need.Item).Available), demands, new HashSet<string>(StringComparer.Ordinal));
                            break;
                        }
                        if (!StartJob(recipe, runtime)) break;
                        madeProgress = true;
                    }
                }
            }
            restockDemands = demands.OrderBy(x => x.Key.Value, StringComparer.Ordinal)
                .Select(x => new ProductionRestockDemand(x.Key, x.Value)).ToArray();
        }

        private bool StartJob(Recipe recipe, ProducerRuntime producer)
        {
            var cost = recipe.OperatingCost ?? config.DefaultOperatingCost;
            if (treasury != null && cost > treasury.Balance) return false;
            var inputs = recipe.Inputs.Select(ToInventoryQuantity).ToArray();
            var outputs = recipe.Outputs.Select(ToInventoryQuantity).ToArray();
            var baseDuration = recipe.DurationMinutes ?? config.DefaultJobDurationMinutes;
            var scaledDuration = decimal.Ceiling(baseDuration * config.JobDurationMultiplierByLevel[producer.Level - 1]);
            var duration = Math.Max(1, decimal.ToInt32(scaledDuration));
            var finish = checked(CurrentMinute + duration);
            if (treasury != null && cost > 0)
            {
                if (!treasury.TrySpend(cost)) return false;
                ledger.Record("hub:treasury", "world:production-cost", "world:tax-sink", cost, 0, "production operating cost");
            }
            inventory.ReserveMany(inputs);
            inventory.BeginProductionMany(inputs);
            jobs.Add(new ProductionJob(nextJobId++, recipe, producer.Level, CurrentMinute, finish, inputs, outputs));
            return true;
        }

        private decimal ProjectedOutput(ProductId product)
        {
            var balance = inventory.Get(new InventoryItem(product));
            decimal projected = balance.Available;
            foreach (var job in ActiveJobs)
                foreach (var output in job.BaseOutputs.Where(x => x.Item == new InventoryItem(product)))
                    projected += (decimal)output.Quantity * Efficiency(job.Recipe, job.ProducerLevel);
            foreach (var recipe in recipes.Where(x => x.Outputs.Any(o => o.Product == product)))
                projected += yieldRemainders.TryGetValue(YieldKey(recipe, product), out var remainder) ? remainder : 0m;
            return projected;
        }

        private InventoryItemQuantity[] CalculateOutputs(ProductionJob job, Dictionary<string, decimal> nextRemainders)
        {
            var result = new List<InventoryItemQuantity>();
            foreach (var output in job.BaseOutputs)
            {
                var quantity = output.Quantity;
                if (output.Item.Value.StartsWith("product:blank_", StringComparison.Ordinal))
                {
                    var key = YieldKey(job.Recipe, output.Item);
                    var exact = output.Quantity * Efficiency(job.Recipe, job.ProducerLevel)
                        + (yieldRemainders.TryGetValue(key, out var remainder) ? remainder : 0m);
                    quantity = decimal.ToInt32(decimal.Floor(exact));
                    nextRemainders[key] = exact - quantity;
                }
                if (quantity > 0) result.Add(new InventoryItemQuantity(output.Item, quantity));
            }
            if (result.Count == 0)
            {
                // A fractional refinery yield may consume this batch without creating a whole item yet.
                return Array.Empty<InventoryItemQuantity>();
            }
            return result.ToArray();
        }

        private decimal Efficiency(Recipe recipe, int level)
        {
            if (recipe.Producer.Value != "refinery") return 1m;
            return config.RefineryEfficiencyByLevel[level - 1];
        }

        private static string YieldKey(Recipe recipe, ProductId product) => recipe.Id.Value + "|product:" + product.Value;
        private static string YieldKey(Recipe recipe, InventoryItem item) => recipe.Id.Value + "|" + item.Value;

        private void AddExpandedDemand(InventoryItem item, int shortage,
            Dictionary<InventoryItem, int> demands, HashSet<string> path)
        {
            if (shortage <= 0) return;
            var key = item.Value;
            if (!path.Add(key)) { AddDemand(demands, item, shortage); return; }
            if (key.StartsWith("material:", StringComparison.Ordinal))
            {
                AddDemand(demands, item, shortage);
                path.Remove(key);
                return;
            }
            var productId = new ProductId(key.Substring("product:".Length));
            var recipe = recipes.Where(x => x.Outputs.Any(o => o.Product == productId))
                .OrderBy(x => x.Id.Value, StringComparer.Ordinal).FirstOrDefault();
            if (recipe == null)
            {
                AddDemand(demands, item, shortage);
                path.Remove(key);
                return;
            }
            var outputQuantity = recipe.Outputs.Where(x => x.Product == productId).Sum(x => x.Quantity);
            var batches = Math.Max(1, (int)Math.Ceiling(shortage / (double)outputQuantity));
            foreach (var input in recipe.Inputs)
            {
                var required = checked(input.Quantity * batches);
                var missing = Math.Max(0, required - inventory.Get(ToInventoryQuantity(input).Item).Available);
                if (missing > 0) AddExpandedDemand(ToInventoryQuantity(input).Item, missing, demands, path);
            }
            path.Remove(key);
        }

        private static void AddDemand(Dictionary<InventoryItem, int> demands, InventoryItem item, int quantity)
        { demands[item] = demands.TryGetValue(item, out var current) ? checked(current + quantity) : quantity; }

        private bool HasInputs(IReadOnlyList<InventoryItemQuantity> inputs)
        {
            foreach (var input in inputs)
                if (inventory.Get(input.Item).Available < input.Quantity) return false;
            return true;
        }

        private int ActiveCount(ProducerId producer) => ActiveJobs.Count(x => x.Recipe.Producer == producer);
        private ProductionJob FindRunningJob(long id)
        {
            var job = jobs.FirstOrDefault(x => x.Id == id);
            if (job == null || job.State != ProductionJobState.Running) throw new InvalidOperationException("Job không tồn tại hoặc đã kết thúc.");
            return job;
        }

        private static InventoryItemQuantity ToInventoryQuantity(RecipeInput input)
            => input.Material.HasValue
                ? new InventoryItemQuantity(new InventoryItem(input.Material.Value), input.Quantity)
                : new InventoryItemQuantity(new InventoryItem(input.Product.Value), input.Quantity);
        private static InventoryItemQuantity ToInventoryQuantity(RecipeOutput output)
            => output.Material.HasValue
                ? new InventoryItemQuantity(new InventoryItem(output.Material.Value), output.Quantity)
                : new InventoryItemQuantity(new InventoryItem(output.Product.Value), output.Quantity);

    }
}
