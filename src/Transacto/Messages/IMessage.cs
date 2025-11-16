namespace Transacto.Messages;

public interface IMessage {
	(string template, object[] args) GetMessage();

	string ToString() {
		var (template, args) = GetMessage();
		return string.Format(template, args);
	}
}
