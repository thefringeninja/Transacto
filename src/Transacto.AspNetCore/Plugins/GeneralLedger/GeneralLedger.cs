using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Messages;
using Transacto.Modules;

namespace Transacto.Plugins.GeneralLedger {
	internal class GeneralLedger : IPlugin {
		public string Name { get; } = nameof(GeneralLedger);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapBusinessTransaction<JournalEntry>("/entries")
			.MapCommands(string.Empty,
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod));

		public void ConfigureServices(IServiceCollection services) => services
			.AddSingleton<CommandHandlerModule>(provider => new GeneralLedgerEntryModule(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredService<AccountIsDeactivated>()))
			.AddSingleton<CommandHandlerModule>(provider => new GeneralLedgerModule(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredService<AccountIsDeactivated>()))
			.AddSingleton<AccountIsDeactivated>(provider => {
				var readModel = provider.GetRequiredService<DeactivatedAccounts>();

				return accountNumber => readModel.Contains(accountNumber.ToInt32());
			})
			.AddInMemoryProjection<DeactivatedAccounts>(new DeactivatedAccountsProjection())
			.AddInMemoryProjection<UnclosedGeneralLedgerEntries>(new UnclosedGeneralLedgerEntriesProjection());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class DeactivatedAccountsProjection : InMemoryProjection<DeactivatedAccounts> {
			public DeactivatedAccountsProjection() {
				When<AccountDeactivated>((readModel, e) => readModel.Deactivated(e.AccountNumber));
				When<AccountReactivated>((readModel, e) => readModel.Reactivated(e.AccountNumber));
			}
		}

		private class DeactivatedAccounts : IMemoryReadModel {
			private ImmutableHashSet<int> _inner;
			public Optional<Position> Checkpoint { get; set; }

			public DeactivatedAccounts() {
				_inner = ImmutableHashSet<int>.Empty;
			}

			public void Deactivated(int accountNumber) => Interlocked.Exchange(ref _inner, _inner.Add(accountNumber));

			public void Reactivated(int accountNumber) =>
				Interlocked.Exchange(ref _inner, _inner.Remove(accountNumber));

			public bool Contains(int accountNumber) => _inner.Contains(accountNumber);
		}

		private class UnclosedGeneralLedgerEntriesProjection : InMemoryProjection<UnclosedGeneralLedgerEntries> {
			public UnclosedGeneralLedgerEntriesProjection() {
				When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.GeneralLedgerEntryPosted(e.Period, e.GeneralLedgerEntryId));
				When<AccountingPeriodClosed>((readModel, e) =>
					readModel.AccountingPeriodClosed(e.Period, e.GeneralLedgerEntryIds, e.ClosingGeneralLedgerEntryId));
			}
		}

		private class UnclosedGeneralLedgerEntries : IMemoryReadModel {
			public Optional<Position> Checkpoint { get; set; }

			private readonly ConcurrentDictionary<string, ImmutableHashSet<Guid>> _entriesByPeriod;
			private ImmutableHashSet<Guid> _notClosed;

			public UnclosedGeneralLedgerEntries() {
				_entriesByPeriod = new ConcurrentDictionary<string, ImmutableHashSet<Guid>>();
				_notClosed = ImmutableHashSet<Guid>.Empty;
			}

			public void GeneralLedgerEntryPosted(string period, Guid generalLedgerEntryId) => _entriesByPeriod
				.AddOrUpdate(period, _ => ImmutableHashSet<Guid>.Empty.Add(generalLedgerEntryId),
					(_, previous) => previous.Add(generalLedgerEntryId));

			public void AccountingPeriodClosed(string period, IEnumerable<Guid> generalLedgerEntryIds, Guid closingGeneralLedgerEntryId) {
				if (!_entriesByPeriod.TryRemove(period, out var entries)) {
					return;
				}

				var notClosed = entries.Except(generalLedgerEntryIds).Remove(closingGeneralLedgerEntryId);

				if (notClosed.IsEmpty) {
					return;
				}

				Interlocked.Exchange(ref _notClosed, _notClosed.Union(notClosed).Compact());
			}
		}
	}
}
