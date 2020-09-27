namespace Transacto.Messages {
    public class AccountRenamed {
	    public string NewAccountName { get; set; } = null!;
        public int AccountNumber { get; set; }

        public override string ToString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
    }
}
