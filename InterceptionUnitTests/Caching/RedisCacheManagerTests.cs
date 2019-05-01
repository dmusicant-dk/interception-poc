using System;
using Moq;
using StackExchange.Redis;
using System.Reflection;
using NUnit.Framework;

namespace Interception.Caching.Tests
{
    [TestFixture()]
    public class RedisCacheManagerTests
    {
        private Mock<IDatabase> database;

        [SetUp]
        public void setup()
        {
            //Set the connection to be mocked
            Mock<IConnectionMultiplexer> multiplexer = new Mock<IConnectionMultiplexer>();
            database = new Mock<IDatabase>();
            multiplexer.Setup(conn => conn.GetDatabase(-1, null)).Returns(database.Object);

            //Make it return the mocked connection
            Type type = typeof(RedisCacheManager);
            FieldInfo field = type.GetField("redisConnection", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, new Lazy<IConnectionMultiplexer>(() => multiplexer.Object));
        }

        [Test()]
        public void getConnectionTest()
        {
            //Let's mock the Db to give us a specific value so we can guarantee we got the right thing
            database.Setup(db => db.StringGet("Item1", CommandFlags.None)).Returns("FakeString");

            //Get the connection and make sure it exists
            IConnectionMultiplexer multiplexer = RedisCacheManager.getConnection();
            Assert.IsNotNull(multiplexer);

            //Now try and get a string
            String value = multiplexer.GetDatabase().StringGet("Item1");
            Assert.AreEqual("FakeString", value);

        }

        [Test()]
        public void getStringValueTest()
        {
            //Let's mock the Db to give us a specific value so we can guarantee we got the right thing
            database.Setup(db => db.StringGet("Item1", CommandFlags.None)).Returns("FakeString");

            //Get the connection and make sure it exists
            IConnectionMultiplexer multiplexer = RedisCacheManager.getConnection();
            Assert.IsNotNull(multiplexer);

            //Now try and get a string
            Assert.AreEqual("FakeString", RedisCacheManager.getStringValue("Item1"));
        }

        [Test()]
        public void setStringValueTest()
        {
            //Let's mock the Db to give us a specific value first
            database.Setup(db => db.StringGet("Item1", CommandFlags.None)).Returns("FakeString");

            //Now let's set a different value when they call set
            database.Setup(db => db.StringSet("Item1", "New Value", null, When.Always, CommandFlags.None))
                .Callback(
                    () => database.Setup(dBase => dBase.StringGet("Item1", CommandFlags.None)).Returns("New Value")
                );

            //Get the connection and make sure it exists
            IConnectionMultiplexer multiplexer = RedisCacheManager.getConnection();
            Assert.IsNotNull(multiplexer);

            //Now try and get a string
            Assert.AreEqual("FakeString", RedisCacheManager.getStringValue("Item1"));

            //Now set it with a non-matching string to be sure it doesn't set it
            RedisCacheManager.setStringValue("Item2", "New Value");
            Assert.AreEqual("FakeString", RedisCacheManager.getStringValue("Item1"));

            //Now set for real
            RedisCacheManager.setStringValue("Item1", "New Value");
            Assert.AreEqual("New Value", RedisCacheManager.getStringValue("Item1"));

        }
    }
}