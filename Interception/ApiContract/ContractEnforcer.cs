using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;

namespace Interception.ApiContract
{
    public class ContractEnforcer
    {
        public static void Requires(bool condition)
        {
#if CONTRACTS_FULL
            Contract.Requires(condition);
#else
            if (!condition)
            {
                throw new ArgumentException();
            }
#endif
        }

        public static void Requires<TException>(bool condition) where TException : Exception, new()
        {
#if CONTRACTS_FULL
            Contract.Requires(condition);
#else
            if (!condition)
            {
                throw new TException();
            }
#endif
        }

        public static void Requires(bool condition, string message)
        {
#if CONTRACTS_FULL
            Contract.Requires(condition, message);
#else
            if (!condition)
            {
                throw new ArgumentException( message );
            }
#endif
        }
    }
}