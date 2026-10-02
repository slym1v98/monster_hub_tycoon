using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Các loại sự kiện trong hàng đợi mô phỏng.</summary>
    public enum SimEventKind
    {
        TrainerDecide,      // Trainer chọn việc tiếp theo
        TrainerArriveZone,  // tới Zone, bắt đầu farm
        FarmChunk,          // hết một khúc farm
        TrainerArriveHub,   // về tới HUB
        ServiceDone,        // dùng xong một dịch vụ, nhả chỗ
        WaitTick,           // mỗi giờ chờ tiền: thử gọi Tổng tài
        Dawn,               // 06:00
        Dusk,               // 18:00
        DayStart,           // 00:00: trừ chi phí vận hành, giảm ngày đình công
        PaydayDue,          // 23:59 ngày 30: RunFor dừng tại đây
        MerchantRouteStep,
        ProductionComplete,
        MarketRetry
    }

    /// <summary>Một sự kiện đã hẹn giờ. <see cref="Token"/> dùng để bỏ qua sự kiện cũ khi Trainer bị ngắt giữa chừng.</summary>
    public readonly struct SimEvent
    {
        public readonly int Time;
        public readonly long Seq;      // thứ tự vào hàng, để phá hòa khi cùng thời điểm
        public readonly SimEventKind Kind;
        public readonly int TrainerId; // -1 nếu không gắn với Trainer
        public readonly int Token;
        public readonly int Arg;

        public SimEvent(int time, long seq, SimEventKind kind, int trainerId, int token, int arg)
        {
            Time = time; Seq = seq; Kind = kind; TrainerId = trainerId; Token = token; Arg = arg;
        }
    }

    /// <summary>Hàng đợi ưu tiên (min-heap) theo (Time, Seq). Tất định: cùng thời điểm thì ra theo thứ tự vào.</summary>
    public sealed class EventQueue
    {
        readonly List<SimEvent> heap = new List<SimEvent>();
        long nextSeq;

        public int Count => heap.Count;

        /// <summary>Thời điểm của sự kiện sớm nhất. Ném lỗi nếu hàng đợi rỗng.</summary>
        public int PeekTime
        {
            get
            {
                if (heap.Count == 0) throw new InvalidOperationException("Hàng đợi sự kiện đang rỗng.");
                return heap[0].Time;
            }
        }

        /// <summary>Danh sách sự kiện đang chờ (không theo thứ tự), dùng để kiểm tra bất biến.</summary>
        public IReadOnlyList<SimEvent> Snapshot => heap;

        public void Schedule(int time, SimEventKind kind, int trainerId = -1, int token = 0, int arg = 0)
        {
            heap.Add(new SimEvent(time, nextSeq++, kind, trainerId, token, arg));
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Less(heap[i], heap[parent])) break;
                Swap(i, parent);
                i = parent;
            }
        }

        public SimEvent Dequeue()
        {
            if (heap.Count == 0) throw new InvalidOperationException("Hàng đợi sự kiện đang rỗng.");
            SimEvent top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int left = 2 * i + 1, right = left + 1, smallest = i;
                if (left < heap.Count && Less(heap[left], heap[smallest])) smallest = left;
                if (right < heap.Count && Less(heap[right], heap[smallest])) smallest = right;
                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
            return top;
        }

        static bool Less(SimEvent a, SimEvent b) => a.Time < b.Time || (a.Time == b.Time && a.Seq < b.Seq);

        void Swap(int i, int j) { SimEvent tmp = heap[i]; heap[i] = heap[j]; heap[j] = tmp; }
    }
}
