using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Interception.Interception
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class InterceptableCall : Attribute
    {
    }
}