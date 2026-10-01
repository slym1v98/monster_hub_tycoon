using System;

namespace Game.Domain
{
    /// <summary>
    /// Thang nang cap tong quat (Nang Sao, Tinh Luyen, Tien hoa): tu bac i len i+1 voi xac suat Success[i],
    /// ton Cost[i] moi lan thu; that bai thi tut FailureDrop[i] bac (0 = o lai). Tinh chi phi ky vong bang he phuong trinh Markov.
    /// </summary>
    public sealed class UpgradeLadder
    {
        public readonly double[] Success;
        public readonly double[] Cost;
        public readonly int[] FailureDrop;

        public UpgradeLadder(double[] success, double[] cost, int[] failureDrop)
        {
            if (success.Length == 0 || success.Length != cost.Length || success.Length != failureDrop.Length)
                throw new ArgumentException("Ba mang phai cung do dai va khac rong.");
            for (int i = 0; i < success.Length; i++)
                if (success[i] <= 0 || success[i] > 1) throw new ArgumentOutOfRangeException(nameof(success));
            Success = success; Cost = cost; FailureDrop = failureDrop;
        }

        public int Steps => Success.Length;

        /// <summary>Chi phi ky vong de len tu bac 'from' toi bac cuoi (Steps).</summary>
        public double ExpectedCost(int from = 0)
        {
            int n = Steps;
            // E_i - p_i*E_{i+1} - (1-p_i)*E_{max(0,i-d_i)} = c_i,  E_n = 0
            var a = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                a[i, i] += 1;
                if (i + 1 < n) a[i, i + 1] -= Success[i];
                int back = Math.Max(0, i - FailureDrop[i]);
                a[i, back] -= 1 - Success[i];
                a[i, n] = Cost[i];
            }
            for (int col = 0; col < n; col++)
            {
                int piv = col;
                for (int r = col + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[piv, col])) piv = r;
                if (Math.Abs(a[piv, col]) < 1e-12) throw new InvalidOperationException("He phuong trinh suy bien.");
                if (piv != col) for (int k = 0; k <= n; k++) { var t = a[col, k]; a[col, k] = a[piv, k]; a[piv, k] = t; }
                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double f = a[r, col] / a[col, col];
                    for (int k = col; k <= n; k++) a[r, k] -= f * a[col, k];
                }
            }
            return a[from, n] / a[from, from];
        }
    }
}
