using System;
using System.Diagnostics;

namespace AnyDB
{
    public partial class Database : IDisposable
    {
        /// <summary>
        /// Disposes a Database object, releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (Transaction != null)
            {
                try
                {
                    Debug.WriteLineIf(Database.Trace, "Database #" + Counter + " has open Transaction #" + Transaction.Counter);
                    Transaction.Dispose();
                }
                finally
                {
                    Transaction = null;
                }
            }
            Debug.WriteLineIf(Database.Trace, "Database #" + Counter + " disposed");
        }
    }
}