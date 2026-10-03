using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Kết quả đổi cấp có thứ tự; HubWorld sẽ gắn thời điểm khi tích hợp EXP ở tác vụ 9.</summary>
    public sealed record TrainerLevelChanged(int TrainerId, int Rank, int FromLevel, int ToLevel);

    /// <summary>EXP chỉ thuộc Trainer; không tự Rebirth hoặc tạo EXP cho Monster.</summary>
    public static class TrainerProgression
    {
        public static IReadOnlyList<TrainerLevelChanged> AddExperience(Trainer trainer, long amount,
            TrainerProgressionConfig config)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var changes = new List<TrainerLevelChanged>();
            if (amount == 0) return changes.AsReadOnly();
            // decimal cộng chính xác hai long mà không tràn; tối đa 99 bước trước giới hạn Lv100.
            decimal remaining = (decimal)trainer.Experience + amount;
            int level = trainer.Level;
            while (level < 100)
            {
                long required = config.ExperienceToNextLevel(level);
                if (remaining < required) break;
                remaining -= required;
                changes.Add(new TrainerLevelChanged(trainer.Id, trainer.Rank, level, level + 1));
                level++;
            }
            // Đổi cấp có thể báo lỗi tính chỉ số; chỉ ghi EXP sau khi toàn đội đã cập nhật thành công.
            trainer.Level = level;
            trainer.Experience = level == 100 ? 0 : (long)remaining;
            return changes.AsReadOnly();
        }
    }
}
