using System.ComponentModel;
using MediatR;

namespace Cellm.Tools.FileReader;

[Description("Reads the contents of file and returns plain text.")]
internal record FileReaderRequest([Description("The absolute path to the file.")] string FilePath) : IRequest<FileReaderResponse>;