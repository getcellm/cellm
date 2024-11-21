using MediatR;

namespace Cellm.Tools.FileReader;

internal class FileReaderRequestHandler(FileReaderFactory fileReaderFactory) : IRequestHandler<FileReaderRequest, FileReaderResponse>
{
    public async Task<FileReaderResponse> Handle(FileReaderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reader = fileReaderFactory.GetFileReader(request.FilePath);
            var content = await reader.ReadFile(request.FilePath, cancellationToken);
            return new FileReaderResponse(content);
        }
        catch (ArgumentException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
        catch (FileNotFoundException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new FileReaderException($"Failed to read file: {request.FilePath}", ex);
        }
    }
}