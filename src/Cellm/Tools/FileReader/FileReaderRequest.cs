using System.ComponentModel;
using MediatR;

namespace Cellm.Tools.FileReader;

internal record FileReaderRequest([Description("The absolute path to the file.")] string FilePath) : IRequest<FileReaderResponse>;