namespace Transacto.Framework;

public delegate MessageHandler<TReturn> MessageHandlerResolver<TReturn>(object command);
