namespace Orc.FileSystem.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class StringIdProviderFacts
    {
        [Test]
        public void CanGenerate100KUniqueIds()
        {
            var stringIdProvider = new StringIdProvider();
            var hashSet = new HashSet<string>();
            const int repsCount = 100000;
            for (int i = 0; i < repsCount; i++)
            {
                var id = stringIdProvider.NewStringId();
                if (!hashSet.Add(id))
                {
                    continue;
                }
            }

            Assert.AreEqual(repsCount, hashSet.Count);
        }
    }
}
