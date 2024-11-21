using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

internal class PdfReader : IFileReader
{
    public bool CanRead(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return Path.GetExtension(filePath).ToLower() is ".pdf";
    }

    public Task<string> ReadFile(string filePath, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        using (PdfDocument document = PdfDocument.Open(filePath))
        {
            foreach (Page page in document.GetPages())
            {
                var pageSegmenter = DocstrumBoundingBoxes.Instance;
                var textBlocks = pageSegmenter.GetBlocks(page.GetWords());

                var readingOrder = UnsupervisedReadingOrderDetector.Instance;
                var orderedTextBlocks = readingOrder.Get(textBlocks);

                foreach (var orderedTextBlock in orderedTextBlocks)
                {
                    var text = orderedTextBlock.Text.Normalize(NormalizationForm.FormKC);
                    stringBuilder.Append(text);
                    stringBuilder.AppendLine();
                }
            }
        }

        return Task.FromResult(stringBuilder.ToString());
    }
}