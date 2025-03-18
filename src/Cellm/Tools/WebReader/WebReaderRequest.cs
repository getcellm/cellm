using MediatR;

namespace Cellm.Tools.WebReader;

internal record WebReaderRequest(string URL) : IRequest<WebReaderResponse>;
