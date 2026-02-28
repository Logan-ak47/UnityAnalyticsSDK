using System.Collections.Generic;
using NUnit.Framework;
using Ashutosh.AnalyticsSdk.Internal.Validation;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class ValidatorRulesTests
    {
        [Test]
        public void Validator_Truncates_And_Drops_Unsupported()
        {
            var longName = new string('x', 200);
            var longKey = new string('k', 200);
            var longValue = new string('v', 1000);

            var props = new Dictionary<string, object>
            {
                { longKey, longValue },
                { "ok", 1 },
                { "bad", new object() } // unsupported
            };

            var ok = EventValidator.TrySanitize(longName, props, out var sanitizedName, out var sanitizedProps, out var error);

            Assert.IsTrue(ok, error);
            Assert.LessOrEqual(sanitizedName.Length, 64);
            Assert.IsTrue(sanitizedProps.ContainsKey("ok"));

            // key length should be truncated
            bool hasTruncatedKey = false;
            foreach (var k in sanitizedProps.Keys)
            {
                if (k.StartsWith("k"))
                {
                    hasTruncatedKey = true;
                    Assert.LessOrEqual(k.Length, 64);
                }
            }
            Assert.IsTrue(hasTruncatedKey);

            // unsupported should be dropped
            Assert.IsFalse(sanitizedProps.ContainsKey("bad"));

            // value string should be truncated
            foreach (var kvp in sanitizedProps)
            {
                if (kvp.Value is string s && kvp.Key != "ok")
                {
                    Assert.LessOrEqual(s.Length, 256);
                }
            }
        }
    }
}