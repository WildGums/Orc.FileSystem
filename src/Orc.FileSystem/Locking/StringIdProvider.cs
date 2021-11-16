namespace Orc.FileSystem
{
    using System;
    using System.Linq;
    using System.Text;

    public class StringIdProvider : IStringIdProvider
    {
        public string NewStringId()
        {
            var builder = new StringBuilder();
            var symbols = Enumerable.Empty<char>();
            symbols = symbols.Concat(Enumerable.Range('A', 26).Select(e => (char)e));
            symbols = symbols.Concat(Enumerable.Range('0', 10).Select(e => (char)e));

            symbols.OrderBy(e => Guid.NewGuid())
                .Take(8)
                .ToList().ForEach(e => builder.Append(e));

            return builder.ToString();
        }
    }
}
