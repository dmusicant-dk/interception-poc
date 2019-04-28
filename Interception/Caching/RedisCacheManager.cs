using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Interception.Caching
{
    public class RedisCacheManager
    {
        private static Lazy<IConnectionMultiplexer> redisConnection;

        static RedisCacheManager()
        {
            redisConnection = new Lazy<IConnectionMultiplexer>(
                () =>
                {
                    return ConnectionMultiplexer.Connect(
                        System.Configuration.ConfigurationManager.AppSettings["RedisConnection"]
                    );
                }
            );
        }

        public static IConnectionMultiplexer getConnection()
        {
            return redisConnection.Value;
        }

        public static string getStringValue(string cacheKey)
        {
            IDatabase database = redisConnection.Value.GetDatabase();
            return database.StringGet(cacheKey);
        }

        public static void setStringValue(string cacheKey, string value)
        {
            IDatabase database = redisConnection.Value.GetDatabase();
            database.StringSet(cacheKey, value);
        }
    }
}