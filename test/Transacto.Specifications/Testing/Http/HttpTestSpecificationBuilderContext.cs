namespace Transacto.Testing.Http;

internal class HttpTestSpecificationBuilderContext {
	private readonly HttpMessage[] _messages;

	public HttpTestSpecificationBuilderContext() {
		_messages = [];
	}

	private HttpTestSpecificationBuilderContext(HttpMessage[] messages) {
		_messages = messages;
	}

	public HttpTestSpecificationBuilderContext Append(params HttpMessage[] messages) =>
		new([.. _messages, .. messages]);

	public HttpCentricTestSpecification ToHttpCentricSpecification() => new(_messages);
}
