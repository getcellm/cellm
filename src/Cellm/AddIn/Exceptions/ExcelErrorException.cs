using ExcelDna.Integration;

namespace Cellm.AddIn.Exceptions
{
    [Serializable]
    internal class ExcelErrorException(ExcelError excelError) : Exception
    {
        public ExcelError GetExcelError() => excelError;
    }
}