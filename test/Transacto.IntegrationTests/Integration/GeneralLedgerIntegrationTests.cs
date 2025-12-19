using System.Diagnostics;
using System.Globalization;
using System.Net;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Messages;
using Transacto.Plugins.GeneralLedger;
using Transacto.Plugins.GeneralLedger.LedgerEntryIndex;

namespace Transacto.Integration;

public class GeneralLedgerIntegrationTests : IntegrationTests {
	private readonly LocalDateTime _openedOn;
	private string Period { get; }

	public GeneralLedgerIntegrationTests(TextWriter logOutput) : base(logOutput) {
		_openedOn = LocalDateTime.FromDateTime(DateTime.Now);
		Period = LocalDateTimePattern.Create("yyyyMM", CultureInfo.InvariantCulture).Format(_openedOn);
	}

	public async Task period() {
		using var response1 = await HttpClient.GetAsync("/general-ledger/period");
		Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);

		var position = await HttpClient.SendCommand("/general-ledger", new OpenGeneralLedger {
			OpenedOn = _openedOn.ToDateTimeUnspecified()
		}, TransactoHttpSerializerOptions.Commands);

		using var response2 = await HttpClient.ConditionalGetAsync("/general-ledger/period", position);
		await Assert.HalResponseEqual<CurrentPeriod, CurrentPeriodRepresentation>(
			new HalResponse<CurrentPeriod>(new CurrentPeriod(
				Period), []),
			response2);
	}

	public async Task searching_entries() {
		await OpenBooks(_openedOn).ToArrayAsync();

		var generalLedgerEntryIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid())
			.ToArray();
		Checkpoint checkpoint = default;
		for (var i = 0; i < 10; i++) {
			checkpoint = await HttpClient.SendCommand("/general-ledger/entries",
				new PostGeneralLedgerEntry {
					BusinessTransaction = new BusinessTransaction().ToJournalEntry(),
					GeneralLedgerEntryId = generalLedgerEntryIds[i],
					Period = Period,
					CreatedOn = _openedOn.ToDateTimeUnspecified()
				},
				TransactoHttpSerializerOptions.Commands);
		}

		var searchResults = await HttpClient.ConditionalGetAsync2<SearchResults>("/general-ledger/search", checkpoint);
		Assert.Equal(10, searchResults.State.Results.Length);
		var links = searchResults.Links.Where(link => link.Rel == "t:ledger").ToArray();
		Assert.Equal(10, links.Length);
		var entries = await Task.WhenAll(Array.ConvertAll(links, link => HttpClient.ConditionalGetAsync2<JournalLedgerEntry>(
			$"/general-ledger/entries/{link.Href}",
			checkpoint)));	
	}

	private class BusinessTransaction() : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("b");
		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems() {
			yield return new Debit(new AccountNumber(1000), new Money(10m));
			yield return new Credit(new AccountNumber(1200), new Money(10m));
		}
	}
}
