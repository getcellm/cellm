using MediatR;

namespace Cellm.Tools.FileReader;

internal record FileReaderRequest(string FilePath) : IRequest<FileReaderResponse>;
