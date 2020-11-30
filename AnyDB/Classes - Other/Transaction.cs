/********************************************************************************************************************
 * 
 * Transaction.cs
 * 
 * A wrapper for a DbTransaction and its associated DbConnection. This IDisposable class is to be used in a using 
 * construct to ensure the connection is closed after use.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace AnyDB
{
    /// <summary>
    /// </summary>
    public class Transaction : IDisposable
    {
        /*===========================================================================================================
         * 
         * Internal fields
         * 
         * These are to be used only by Database.BeginTransaction() and inside this Transaction class.
         */

        internal DbConnection TheConnection;

        internal DbTransaction TheTransaction
        {
            get
            {
                if (state == TransactionState.disposed)
                    throw new AnyDbException("Transaction #" + Counter + " is " + state.ToString().Replace('_', ' ') + ". Stop using it!");
                if (state != TransactionState.start)
                    throw new AnyDbException("Transaction #" + Counter + " is " + state.ToString().Replace('_', ' ') + ". Dispose it!");
                else return _theTransaction;
            }
            private set
            {
                _theTransaction = value;
            }
        }

        internal readonly short Counter;

        /*===========================================================================================================
         * 
         * Public readonly.
         */

        private Database TheDatabase;

        /// <summary>
        /// A back reference to the Database object that this transaction is being used with. This property allows you
        /// to pass a Transaction without having to also pass the Database object.
        /// </summary>

        public Database Database
        {
            get
            {
                return TheDatabase;
            }
        }

        /*===========================================================================================================
         * 
         * Private fields
         * 
         * Only used inside this class.
         */

        private DbTransaction _theTransaction;
        private Database db;
        private Exception up = null;
        private TransactionState state;

        private enum TransactionState
        {
            start,
            committed,
            rolled_back,
            disposed
        }

        private static short _Counter;

        /*===========================================================================================================
         * 
         * Constructor
         * 
         * This is marked internal because only Database.BeginTransaction() should be able to create a new 
         * AnyDB.Transaction instance.
         */

        internal Transaction(Database Database, DbConnection Connection, IsolationLevel IsolationLevel)
        {
            Counter = unchecked(++_Counter);
            db = Database;
            TheDatabase = Database;
            TheConnection = Connection;
            TheTransaction = Connection.BeginTransaction(IsolationLevel);
            state = TransactionState.start;
            Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " " + state.ToString().Replace('_', ' ') + " (" + db.ProviderInvariantName + ")");
        }

        /// <summary>
        /// Finishes the current transaction and makes your changes permanent. At this point the connection will also 
        /// be closed, after which you should Dispose() the instance.
        /// </summary>
        /// <exception cref="AnyDbException">Transaction already closed.</exception>
        /// <exception cref="DbException">Any other database problem.</exception>

        public void Commit()
        {
            up = null;

            /*
             * Do the commit.
             */

            if (state == TransactionState.start)
            {
                try
                {
                    Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " commit");
                    _theTransaction.Commit();
                    _theTransaction.Dispose();
                }
                catch (Exception ex)
                {
                    up = ex;
                }
                finally
                {
                    _theTransaction = null;
                    state = TransactionState.committed;
                }
            }
            else
            {
                up = new AnyDbException("Transaction #" + Counter + " is already " + state.ToString().Replace('_', ' '));
            }

            /*
             * Close the connection.
             */

            CloseConnection();

            /*
             * Throw any error we may have encountered.
             */

            if (up != null) throw up;
            Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " " + state.ToString().Replace('_', ' '));
        }

        /// <summary>
        /// Abandons the current transaction, "undoing" your changes. At this point the connection will also be closed, 
        /// after which you should Dispose() the instance.
        /// </summary>
        /// <exception cref="AnyDbException">Transaction already closed.</exception>
        /// <exception cref="DbException">Any other database problem.</exception>

        public void Rollback()
        {
            up = null;

            /*
             * Do the rollback.
             */

            if (state == TransactionState.start)
            {
                try
                {
                    Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " rollback");
                    _theTransaction.Rollback();
                    _theTransaction.Dispose();
                }
                catch (Exception ex)
                {
                    up = ex;
                }
                finally
                {
                    _theTransaction = null;
                    state = TransactionState.rolled_back;
                }
            }
            else
            {
                up = new AnyDbException("Transaction #" + Counter + " is already " + state.ToString().Replace('_', ' '));
            }

            /*
             * Close the connection.
             */

            CloseConnection();

            /*
             * Throw any error we may have encountered.
             */

            if (up != null) throw up;
            Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " " + state.ToString().Replace('_', ' '));
        }

        /// <summary>
        /// Releases resources associated with the Transaction, i.e. DbTransaction and DbConnection. If at this point 
        /// the transaction has not been committed or rolled back, we'll do a rollback, for two reasons: (1) you must 
        /// Commit() if you want your changes to become permanent and (2) if you didn't explicitly Commit() or 
        /// Rollback(), you probably got here through an Exception, in which case you have an incomplete transaction.
        /// </summary>

        public void Dispose()
        {
            if (state != TransactionState.disposed)
            {
                up = null;
                Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " disposing");
                if (_theTransaction != null)
                {
                    try
                    {
                        Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " not committed");
                        Rollback();
                    }
                    catch
                    {
                        // Don't let Dispose() throw
                    }
                    finally
                    {
                        _theTransaction = null;
                    }
                }
                if (db != null)
                {
                    Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " being removed from database #" + db.Counter);
                    db.Transaction = null;
                    db = null;
                }
                state = TransactionState.disposed;
                Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " " + state.ToString().Replace('_', ' '));
            }
            else
                Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " " + state.ToString().Replace('_', ' ') + " twice");
        }

        /*===========================================================================================================
         * 
         * CloseConnection()
         * 
         * Does what it says on the tin.
         */

        private void CloseConnection()
        {
            if (TheConnection != null)
            {
                Debug.WriteLineIf(Database.Trace, "Transaction #" + Counter + " connection closing");
                try
                {
                    TheConnection.Close();
                    TheConnection.Dispose();
                }
                catch (Exception ex)
                {
                    // This is unlikely
                    if (up != null) up = ex;
                }
                finally
                {
                    TheConnection = null;
                }
            }
        }
    }
}