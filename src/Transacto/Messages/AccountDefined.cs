namespace Transacto.Messages {
    public class AccountDefined {
	    public string AccountName { get; set; } = null!;
        public int AccountNumber { get; set; }

        public override string ToString() => $"Account {AccountNumber} - {AccountName} was defined.";
    }
}
