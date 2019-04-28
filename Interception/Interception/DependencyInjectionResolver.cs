using AopAlliance.Intercept;
using Interception.ApiContract;
using Microsoft.Extensions.DependencyInjection;
using Spring.Aop.Framework;
using Spring.Aop.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;

namespace Interception.Interception
{
    public class DependencyInjectionResolver : IDependencyResolver
    {
        protected IServiceProvider provider { get; set; }
        private static Dictionary<Type, MethodInterceptor> interceptors = new Dictionary<Type, MethodInterceptor>();

        public DependencyInjectionResolver(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public IDependencyScope BeginScope()
        {
            return new DependencyInjectionResolver(this.provider.CreateScope().ServiceProvider);
        }

        public object GetService(Type serviceType)
        {
            return this.provider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.provider.GetServices(serviceType);
        }

        public void Dispose()
        {

        }

        public static IServiceProvider createProvider(Type baseType)
        {
            //This will hold all services that can take injections
            ServiceCollection services = new ServiceCollection();

            //For now we'll just add controllers as taking dependencies
            services.AddControllersAsServices(
                baseType.Assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => typeof(IHttpController).IsAssignableFrom(t) || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            );

            //Get all the interceptors so we can apply them (as advice) on the proper methods
            findInterceptors(baseType);

            //Add all classes with interceptable methods as injectable services (with their proxy)
            addInterceptables(services, baseType);

            //Build it from the collection
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private static void findInterceptors(Type baseType)
        {
            //First find all the types that are marked as interceptors internally
            List<Type> internalInterceptorTypes = typeof(DependencyInjectionResolver).Assembly
                .GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(Interceptor), true).Length > 0)
                .ToList();

            //Now find all the ones from their project
            List<Type> externalInterceptorTypes = baseType.Assembly
               .GetTypes()
               .Where(type => type.GetCustomAttributes(typeof(Interceptor), true).Length > 0)
               .ToList();

            //Add them together
            internalInterceptorTypes.AddRange(externalInterceptorTypes);

            //Make into an array
            Type[] interceptorTypes = internalInterceptorTypes.ToArray();

            //Now for each one, let's add it for all of its types
            foreach (Type interceptorType in interceptorTypes)
            {
                //Get the interceptor attribute
                Interceptor descriptor = (Interceptor)interceptorType.GetCustomAttributes(typeof(Interceptor), true)[0];

                //Create it (we require a no-arg constructor and for it to be a MethodInterceptor and IMethodInterceptor)
                ContractEnforcer.Requires(typeof(MethodInterceptor).IsAssignableFrom(interceptorType));
                ContractEnforcer.Requires(typeof(IMethodInterceptor).IsAssignableFrom(interceptorType));
                MethodInterceptor interceptor = (MethodInterceptor)Activator.CreateInstance(interceptorType);

                //Go through and add all the types
                Array.ForEach(
                    descriptor.types,
                    type => interceptors.Add(type, interceptor)
                );
            }
        }

        private static void addInterceptables(IServiceCollection services, Type baseType)
        {
            /*
             * Am here:
             * x1. Create attribute Interceptor( types = { Cacheable } ) //type must be interceptablecall
             * x2. Find all the interceptors and create a map with the key being the type(s)
             * x3. Find all classes with methods that take InterceptableCall
             * x4. Filter by each Interceptor and pass them to that
             * x5. Add cacheable and cacheevict handling
             * 6. Add unit tests (code coverage?)
             *      xa. Unit test project
             *      xb. Code coverage (try to get to 90%)
             *      c. Include mocking
             * 7. Turn into a library and import that
             * 8. Generate code at runtime (instead of Spring AOP) - do first in new simple project?
             */

            //Get all classes with loggable methods
            Type[] types = baseType.Assembly
                .GetTypes()
                .Where(
                    type =>
                    {
                        return type.GetMethods()
                            .Where(method => method.GetCustomAttributes(typeof(InterceptableCall), false).Length > 0)
                            .Count() > 0;
                    }
                )
                .ToArray();

            //Now add them each but as proxied
            foreach (Type type in types)
            {
                //Make a proxy of it (we require a no-args constructor for now)
                ProxyFactory factory = new ProxyFactory(Activator.CreateInstance(type));

                //Add any methods that have interceptors
                addInterceptors(factory, type);

                //Save the proxy as the service
                services.AddSingleton(getInterfaceType(type), factory.GetProxy());
            }
        }

        private static void addInterceptors(ProxyFactory factory, Type type)
        {
            //Get all the methods
            MethodInfo[] methods = type.GetMethods();
            List<Attribute> handledAttributes = new List<Attribute>();

            foreach (MethodInfo method in methods)
            {
                //Get the custom attributes
                InterceptableCall[] attributes = (InterceptableCall[])method.GetCustomAttributes(typeof(InterceptableCall), true);

                foreach (InterceptableCall attribute in attributes)
                {
                    //Get the interceptor
                    MethodInterceptor interceptor = interceptors[attribute.GetType()];

                    //It must exist
                    ContractEnforcer.Requires(interceptor != null, string.Format("The interceptable call attribute [{0}] has no interceptor.", attribute));

                    //Add the method
                    interceptor.addMethod(attribute, method);

                    System.Diagnostics.Debug.WriteLine("Before advisor");
                    //Now add it as advice
                    if (!handledAttributes.Contains(attribute))
                    {
                        System.Diagnostics.Debug.WriteLine("Adding advisor for: " + attribute.GetType());

                        //Add our method wrapper/advice/interceptor
                        factory.AddAdvice((IMethodInterceptor)interceptor);

                        //We need an advisor that tells it to only be called for 
                        //methods with this particular attribute
                        factory.AddAdvisor(
                            new DefaultPointcutAdvisor(
                                new AttributeMatchingPointcut(attribute.GetType()),
                                (IMethodInterceptor)interceptor
                            )
                        );

                        //Mark it added
                        handledAttributes.Add(attribute);
                    }
                }
            }
        }

        private static Type getInterfaceType(Type type)
        {
            //It has at least one interface
            if (type.GetInterfaces().Length > 0)
            {
                return type.GetInterfaces()[0];
            }

            return type;
        }
    }
}