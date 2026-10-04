using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LAC.Director
{
    // UTF-8 có BOM để Excel đọc đúng tiếng Việt; số luôn dùng dấu chấm bất kể locale máy.
    internal sealed class TelemetryCsv : IDisposable
    {
        private readonly StreamWriter _writer;

        public TelemetryCsv(string path, params object[] header)
        {
            _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                FileShare.Read), new UTF8Encoding(true));
            try
            {
                Row(header);
                _writer.Flush();
            }
            catch
            {
                try { _writer.Dispose(); } catch (IOException) { }
                throw;
            }
        }

        public void Row(params object[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) _writer.Write(',');
                string value = cells[i] is IFormattable number
                    ? number.ToString(null, CultureInfo.InvariantCulture)
                    : cells[i]?.ToString() ?? "";
                _writer.Write('"');
                _writer.Write(value.Replace("\"", "\"\""));
                _writer.Write('"');
            }
            _writer.Write("\r\n");
        }

        public void Flush() => _writer.Flush();
        public void Dispose() => _writer.Dispose();
    }
}
