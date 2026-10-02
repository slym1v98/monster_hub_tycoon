using System;

namespace Game.Domain
{
    /// <summary>Chỉ số bất biến, sinh một lần bằng SimRandom theo thứ tự Khéo léo, May mắn, Sức bền, Lãnh đạo.</summary>
    public sealed record TrainerAttributes
    {
        public double Dexterity { get; }
        public double Luck { get; }
        public double Endurance { get; }
        public double Leadership { get; }

        public TrainerAttributes(double dexterity, double luck, double endurance, double leadership)
        {
            Validate(dexterity, nameof(dexterity));
            Validate(luck, nameof(luck));
            Validate(endurance, nameof(endurance));
            Validate(leadership, nameof(leadership));
            Dexterity = dexterity;
            Luck = luck;
            Endurance = endurance;
            Leadership = leadership;
        }

        public static TrainerAttributes Generate(SimRandom random, TrainerAttributeConfig config)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new TrainerAttributes(Draw(random, config.Dexterity), Draw(random, config.Luck),
                Draw(random, config.Endurance), Draw(random, config.Leadership));
        }

        static double Draw(SimRandom random, TrainerAttributeRange range) =>
            range.Minimum + (range.Maximum - range.Minimum) * random.NextDouble();

        static void Validate(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
