using System;
using Game.Domain.Materials;

namespace Game.Domain.Production
{
    /// <summary>Dữ liệu định nghĩa một xưởng sản xuất.</summary>
    public sealed class ProducerDefinition
    {
        public ProducerId Id { get; }
        public string Name { get; }
        public int? MaximumConcurrentJobs { get; }
        public ProducerDefinition(ProducerId id, string name, int? maximumConcurrentJobs = null)
        { Id = id; Name = name; MaximumConcurrentJobs = maximumConcurrentJobs; }
    }
}
