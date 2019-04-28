using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Interception.Interception
{
    public interface MethodInterceptor
    {
        void addMethod(InterceptableCall attribute, MethodInfo method);
    }
}