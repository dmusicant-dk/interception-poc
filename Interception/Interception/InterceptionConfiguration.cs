using System;
using System.Web.Http;
using System.Web.Http.Dependencies;

namespace Interception.Interception
{
    public class InterceptionConfiguration
    {
        public static void Configure(HttpConfiguration config, Type baseType)
        {
            //Create our provider
            IServiceProvider provider = DependencyInjectionResolver.createProvider(baseType);

            //Create the resolver (and make it use our provider)
            IDependencyResolver resolver = new DependencyInjectionResolver(provider);

            config.DependencyResolver = resolver;
        }
    }
}
