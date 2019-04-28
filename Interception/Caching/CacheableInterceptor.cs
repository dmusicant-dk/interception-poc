using AopAlliance.Intercept;
using Interception.ApiContract;
using Interception.Interception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace Interception.Caching
{
    [Interceptor(new Type[] { typeof(Cacheable) })]
    public class CacheableInterceptor : IMethodInterceptor, MethodInterceptor
    {
        private static Type cacheableType = typeof(Cacheable);

        private Dictionary<MethodInfo, CacheKey> cacheableMethods = new Dictionary<MethodInfo, CacheKey>();
        //private Dictionary<MethodInfo, CacheEvict> evictingMethods = new Dictionary<MethodInfo, CacheEvict>();
        public CacheableInterceptor()
        {
        }

        public void addMethod(InterceptableCall attribute, MethodInfo method)
        {
            //Cacheable
            if (attribute is Cacheable)
            {
                cacheableMethods.Add(method, new CacheKey((Cacheable)attribute, method));
            }

            //We don't know this type, shouldn't be called
            else
            {
                throw new ArgumentException("Unknown attribute: " + attribute);
            }
        }

        public object Invoke(IMethodInvocation invocation)
        {
            string value = null;

            //This is a cacheable method
            if (cacheableMethods.ContainsKey(invocation.Method))
            {
                System.Diagnostics.Debug.WriteLine("Called 2!");
                string cacheKey = cacheableMethods[invocation.Method].getKeyValue(invocation.Arguments);

                //Try to first get the key (might not exist)
                value = RedisCacheManager.getStringValue(cacheKey);

                //It didn't exist
                if (value == null)
                {
                    System.Diagnostics.Debug.WriteLine("Didn't exist, creating for: " + cacheKey);
                    //TODO: Use serializer here to handle more than strings
                    value = (string) invocation.Proceed();

                    //Save it
                    RedisCacheManager.setStringValue(cacheKey, value);
                }
            }

            //Default (we should not be getting called!)
            else
            {
                throw new ArgumentException("Incorrectly called for a non-cacheable method: " + invocation.Method.Name);
            }

            return value;
        }

        class CacheKey
        {
            private static String delim = ":";
            private String prefix { get; set; }
            private List<int> keyParamIndexes = new List<int>();

            public CacheKey(Cacheable cacheable, MethodInfo method)
            {
                this.prefix = cacheable.cacheName + delim;
                setParams(cacheable, method);
            }

            public String getKeyValue(object[] args)
            {
                StringBuilder builder = new StringBuilder(prefix);

                //TODO: Add a serializer for these key items (protobuf extension?)
                foreach (int index in keyParamIndexes)
                {
                    builder.Append(args[index]);
                    builder.Append(delim);
                }

                return builder.ToString();
            }

            private void setParams(Cacheable cacheable, MethodInfo method)
            {
                //They specified the params to use
                if (cacheable.keyFromArgs != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    foreach (string name in cacheable.keyFromArgs)
                    {
                        ParameterInfo param = parameters.Where(parameter => parameter.Name.Equals(name)).First();
                        ContractEnforcer.Requires(param != null, "The cache key expected a parameter named: " + name);

                        //Add this index
                        keyParamIndexes.Add(param.Position);
                    }
                }

                //We're using the params from the method
                else
                {
                    for (int i = 0; i < method.GetParameters().Length; i++)
                    {
                        keyParamIndexes.Add(i);
                    }
                }
            }
        }
    }
}