using MediatR;

namespace Cellm.Tools.FileReader;

internal class FileReaderRequestHandler : IRequestHandler<FileReaderRequest, FileReaderResponse>
{
	private readonly FileReaderFactory _fileReaderFactory;

    public FileReaderRequestHandler(FileReaderFactory fileReaderFactory)
    {
        _fileReaderFactory = fileReaderFactory;
    }

    public async Task<FileReaderResponse> Handle(FileReaderRequest request, CancellationToken cancellationToken)
    {
        try {
            var reader = _fileReaderFactory.GetReader(request.FilePath);
            var content = await reader.ReadContent(request.FilePath, cancellationToken);
            return new FileReaderResponse(content);
        }
        catch(ArgumentException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
        catch(FileNotFoundException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
        catch(NotSupportedException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
    }
}