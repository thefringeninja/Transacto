using System;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
    public class GeneralLedgerEntry : AggregateRoot {
        public static readonly Func<GeneralLedgerEntry> Factory = () => new GeneralLedgerEntry();
        private GeneralLedgerEntryIdentifier _identifier;
        private bool _posted;
        private Money _balance;

        public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier => _identifier;

        private GeneralLedgerEntry() {
            Register<GeneralLedgerEntryCreated>(e => _identifier = new GeneralLedgerEntryIdentifier(e.GeneralLedgerEntryId));
            Register<CreditApplied>(e => _balance += new Money(e.Amount));
            Register<DebitApplied>(e => _balance -= new Money(e.Amount));
            Register<GeneralLedgerEntryPosted>(_ => _posted = true);
        }

        public static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
            GeneralLedgerEntryNumber number, PeriodIdentifier period, DateTimeOffset createdOn) {
            if (!period.Contains(createdOn)) {
                throw new InvalidOperationException();
            }

            var entry = Factory();

            entry.Apply(new GeneralLedgerEntryCreated {
                GeneralLedgerEntryId = identifier.ToGuid(),
                Number = number.ToString(),
                CreatedOn = createdOn,
                Period = period.ToDto()
            });

            return entry;
        }

        public void ApplyCredit(Credit credit) {
            MustNotBePosted();

            Apply(new CreditApplied {
                GeneralLedgerEntryId = _identifier.ToGuid(),
                Amount = credit.Amount.ToDecimal(),
                AccountNumber = credit.AccountNumber.ToInt32()
            });
        }

        public void ApplyDebit(Debit debit) {
            MustNotBePosted();

            Apply(new DebitApplied {
                GeneralLedgerEntryId = _identifier.ToGuid(),
                Amount = debit.Amount.ToDecimal(),
                AccountNumber = debit.AccountNumber.ToInt32()
            });
        }

        public void ApplyTransaction(IBusinessTransaction transaction) {
            MustNotBePosted();

            foreach (var x in transaction.Transaction) {
                Apply(x);
            }
        }

        public void Post() {
            MustBeInBalance();

            Apply(new GeneralLedgerEntryPosted {
                GeneralLedgerEntryId = _identifier.ToGuid()
            });
        }

        private void MustNotBePosted() {
            if (!_posted)
                return;

            throw new InvalidOperationException();
        }

        private void MustBeInBalance() {
            if (_balance != Money.Zero)
                return;

            throw new InvalidOperationException();
        }
    }
}
