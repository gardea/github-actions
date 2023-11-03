using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paschi.GitHubActions.AzureStorageToFtp
{
    internal static class Helpers
    {
        internal static string Dump(this Exception ex)
        {
            StringBuilder sb = new();
            if (ex != null)
            {
                sb.AppendLine(ex.Message);
            }
            var current = ex.InnerException;
            var level = 0;
            while (current != null)
            {
                sb.AppendLine($"{new string('\t', level++)}{current.Message}");
                current = current.InnerException;
            }
            return sb.ToString();
        }
    }
}
