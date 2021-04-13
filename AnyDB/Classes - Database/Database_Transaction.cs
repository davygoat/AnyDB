/********************************************************************************************************************
 * 
 * Database_Transaction.cs
 * 
 * Methods for starting and ending a transaction.
 */

using System;
using System.Data;
using System.Data.Common;

namespace AnyDB
{
    public partial class Database
    {
        internal Transaction Transaction = null;

        /// <summary>
        /// Starts a transaction, and an associated DbConnection, within which you can issue multiple SQL commands that 
        /// should succeed or fail as a unit. Because of the implied DbConnection, you should take care to always call 
        /// Commit() or Rollback() on the Transaction, and Dispose() of it via a using block. The using block ensures 
        /// that the unfinished transaction is rolled back in the event of an unhandled exception.
        /// </summary>
        /// <returns>An AnyDB.Transaction, which must be disposed after use.</returns>
        /// <exception cref="AnyDbException"></exception>

        public Transaction BeginTransaction()
        {
            return BeginTransaction(Driver.DefaultIsolationLevel);
        }

        /// <summary>
        /// Starts a transaction with the specified isolation level.
        /// </summary>
        /// <param name="IsolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>An AnyDB.Transaction, which must be disposed after use.</returns>
        /// <exception cref="AnyDbException"></exception>

        public Transaction BeginTransaction(IsolationLevel IsolationLevel)
        {
            /*
             * If the database doesn't do transactions, there's no point trying.
             */

            if (!Driver.HasTransactions)
                throw new NotImplementedException(ProductName + " does not have transactions");

            /*
             * Ok, start a transaction.
             */

            if (Transaction == null)
            {
                DbConnection connect = CreateOrReuseConnection(ConnectionString); // create
                return Transaction = new Transaction(this, connect, IsolationLevel);
            }
            else
                throw new AnyDbException("Transaction already in progress. Please make sure you Commit(), Rollback() or Dispose(). Do not nest.");
        }

        /// <summary>
        /// Lambda-based transaction method. Instantiates a Transaction object, using a temporary Database connection,
        /// and calls the specified delegate with the transaction object as a parameter. This is a convenient method 
        /// for doing things in a transaction without needing to manually build up two using blocks.
        /// </summary>
        /// <param name="Provider">
        /// The type of database or database provider that you want to use.
        /// </param>
        /// <param name="ConnectionString">
        /// Provider specific connection string.
        /// </param>
        /// <param name="doThis">
        /// Procedure or lambda expression to call whilst holding the transaction. Don't forget to include a Commit(),
        /// or your transaction will be rolled back by default.
        /// </param>
        public static void BeginTransaction(Providers Provider, string ConnectionString, TransactDelegate doThis)
        {
            using (Database db = new Database(Provider, ConnectionString))
            {
                using (Transaction tx = db.BeginTransaction())
                {
                    doThis(tx);
                }
            }
        }

        /*===========================================================================================================
         * 
         * CreateOrReuseConnection()
         */

        private DbConnection CreateOrReuseConnection(string ConnectionString)
        {
            /*
             * If we're in a transaction, use the connection we opened with BeginTransaction().
             */

            if (Transaction != null)
                return Transaction.TheConnection;
            Transaction = null;

            /*
             * We're not in a transaction, so let's open a new connection.
             */

            DbConnection connect = Factory.CreateConnection();
            connect.ConnectionString = ConnectionString;
            connect.Open();

            /*
             * In some cases, we may need to run a command to set options.
             */

            if (Driver != null && Driver.OnConnect.Count > 0)
            {
                using (DbCommand cmd = Driver.CreateCommand())
                {
                    foreach (string command in Driver.OnConnect)
                    {
                        cmd.CommandText = command;
                        cmd.Connection = connect;
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            /*
             * Return the connection.
             */

            return connect;
        }

        /*===========================================================================================================
         * 
         * PossiblyUseTransaction()
         */

        private DbTransaction PossiblyUseTransaction()
        {
            if (Transaction != null)
                if (Transaction.TheTransaction != null) return Transaction.TheTransaction;
            return null;
        }

        /*===========================================================================================================
         * 
         * DisposeTemporaryConnection()
         */

        private void DisposeTemporaryConnection(DbConnection connect)
        {
            if (connect != null)
            {
                if (Transaction == null)
                {
                    connect.Dispose();
                    connect = null;
                }
            }
        }
    }

    /// <summary>
    /// Callback prototype for lambda-based Transaction constructors.
    /// </summary>
    /// <param name="tx"></param>
    public delegate void TransactDelegate(Transaction tx);
}