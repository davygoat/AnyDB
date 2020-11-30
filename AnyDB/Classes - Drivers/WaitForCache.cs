using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace AnyDB
{
    partial class DriverBase
    {
        static readonly string lockme = "LockedUpInaCupboardWithAniceWarmWoman";

        static internal Dictionary<string, DataTable> ProceduresCache = new Dictionary<string, DataTable>();
        static internal Dictionary<string, DataTable> ProcParamsCache = new Dictionary<string, DataTable>();

        static Dictionary<string, ManualResetEvent> WaitForProcParams = new Dictionary<string, ManualResetEvent>();

        /// <summary>
        /// </summary>
        protected delegate void ParameterlessLambda();

        /// <summary>
        /// Called by the driver constructor to start cacheing the Procedures and ProcParams collections. See 
        /// Drivers.Oracle for usage. The first instance for each connection string will start a thread. Subsequent
        /// instances will go straight through, so they can start querying tables without having to wait for
        /// stored procedures. If they want to call a stored procedure, they will call WaitForBackgroundProcParams()
        /// to wait for the first thread to finish. More importantly, if you're not actually going to use any
        /// stored procedures, you won't have to wait forever to get your database connection.
        /// </summary>
        /// <returns></returns>
        protected void BackgroundProcParamsNeeded(string ConnectionString, ParameterlessLambda stuffToDo)
        {
            lock(lockme)
            {
                if (!WaitForProcParams.ContainsKey(ConnectionString))
                {
                    WaitForProcParams[ConnectionString] = new ManualResetEvent(false);
                    new Thread(() =>
                    {
                        try
                        {
                            stuffToDo();
                            WaitForProcParams[ConnectionString].Set();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }).Start();
                }
            }
        }

        internal void WaitForBackgroundProcParams()
        {
            if (WaitForProcParams.ContainsKey(ConnectionString))
            {
                WaitForProcParams[ConnectionString].WaitOne();
                if (ProceduresCache.ContainsKey(ConnectionString)) dtProcedures = ProceduresCache[ConnectionString];
                if (ProcParamsCache.ContainsKey(ConnectionString)) dtProcParams = ProcParamsCache[ConnectionString];
            }
        }
    }
}