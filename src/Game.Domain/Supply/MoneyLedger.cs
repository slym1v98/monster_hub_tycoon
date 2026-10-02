using System;
using System.Collections.Generic;

namespace Game.Domain.Supply
{
    /// <summary>Một khoản chuyển tiền cân bằng giữa bên trả, bên nhận và thuế.</summary>
    public sealed record MoneyTransaction(long Sequence, string Payer, string Payee, string TaxAccount,
        long Gross, long Tax, string Reason);

    /// <summary>Sổ kép bất biến theo giao dịch, trong đó thuế được ghi thành một khoản riêng.</summary>
    public sealed class MoneyLedger
    {
        private readonly Dictionary<string, long> balances = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly List<MoneyTransaction> transactions = new List<MoneyTransaction>();
        public IReadOnlyList<MoneyTransaction> Transactions => transactions.AsReadOnly();
        public long TotalBalance
        {
            get
            {
                long total = 0;
                checked { foreach (var value in balances.Values) total += value; }
                return total;
            }
        }

        public long BalanceOf(string account)
        {
            RequireAccount(account, nameof(account));
            return balances.TryGetValue(account, out var balance) ? balance : 0;
        }

        public MoneyTransaction Record(string payer, string payee, string taxAccount,
            long gross, long tax, string reason)
        {
            RequireAccount(payer, nameof(payer)); RequireAccount(payee, nameof(payee));
            RequireAccount(taxAccount, nameof(taxAccount)); RequireAccount(reason, nameof(reason));
            if (gross < 0) throw new ArgumentOutOfRangeException(nameof(gross));
            if (tax < 0 || tax > gross) throw new ArgumentOutOfRangeException(nameof(tax));
            if (payer == payee || payer == taxAccount || (tax > 0 && payee == taxAccount))
                throw new ArgumentException("Các tài khoản trong giao dịch phải phân biệt.");

            var net = checked(gross - tax);
            var payerBalance = checked(BalanceOf(payer) - gross);
            var payeeBalance = checked(BalanceOf(payee) + net);
            var taxBalance = checked(BalanceOf(taxAccount) + tax);
            balances[payer] = payerBalance;
            balances[payee] = payeeBalance;
            balances[taxAccount] = taxBalance;
            var transaction = new MoneyTransaction(transactions.Count + 1L, payer, payee, taxAccount, gross, tax, reason);
            transactions.Add(transaction);
            return transaction;
        }

        private static void RequireAccount(string value, string parameter)
        { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Tài khoản/lý do không được rỗng.", parameter); }
    }
}
