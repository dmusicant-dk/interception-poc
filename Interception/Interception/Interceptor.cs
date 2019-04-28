using Interception.ApiContract;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;

namespace Interception.Interception
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class Interceptor : Attribute
    {
        private static Type REQUIRED_BASE_TYPE = typeof(InterceptableCall);

        public Type[] types { get; private set; }

        public Interceptor(Type[] types)
        {
            validateTypes(types);
            this.types = types;
        }

        private void validateTypes(Type[] types)
        {
            //An interceptor must apply to at least one type
            ContractEnforcer.Requires(types != null && types.Length > 0, "Types must not be null or empty");

            //Since C# doesn't allow generics on Type, we do the check ourselves
            foreach (Type type in types)
            {
                ContractEnforcer.Requires(
                    type.IsSubclassOf(REQUIRED_BASE_TYPE),
                    string.Format("[{0}] is the wrong Type. It must inherit from InterceptableCall", type)
                );
            }
        }
    }
}