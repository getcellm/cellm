using System.ComponentModel;

namespace Cellm.Tools.FileReader;

internal record FileReaderResponse([Description("The content of the file")] string FileContent);
