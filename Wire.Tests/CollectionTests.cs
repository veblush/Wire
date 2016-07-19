﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// using System.Collections.Immutable;

namespace Wire.Tests
{
    [TestClass]
    public class CollectionTests : TestBase
    {
#if !NET35
        [TestMethod]
        public void CanSerializeImmutableDictionary()
        {
            var map = ImmutableDictionary<string, object>.Empty;
            var serializer = new Wire.Serializer();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(map, stream);
                stream.Position = 0;
                var map2 = serializer.Deserialize(stream);  // exception
            }
        }
#endif

        [TestMethod]
        public void CanSerializeSet()
        {
            var expected = new HashSet<Something>
            {
                new Something
                {
                    BoolProp = true,
                    Else = new Else
                    {
                        Name = "Yoho"
                    },
                    Int32Prop = 999,
                    StringProp = "Yesbox!"
                },
                new Something(),
                new Something(),
                null
            };

            Serialize(expected);
            Reset();
            var actual = Deserialize<HashSet<Something>>();
            CollectionAssert.AreEqual(expected.ToList(), actual.ToList());
        }

        [TestMethod]
        public void CanSerializeStack()
        {
            var expected = new Stack<Something>();
            expected.Push(new Something
            {
                BoolProp = true,
                Else = new Else
                {
                    Name = "Yoho"
                },
                Int32Prop = 999,
                StringProp = "Yesbox!"
            });


            expected.Push(new Something());

            expected.Push(new Something());

            Serialize(expected);
            Reset();
            var actual = Deserialize<Stack<Something>>();
            CollectionAssert.AreEqual(expected.ToList(), actual.ToList());
        }

        [TestMethod]
        public void CanSerializeDictionary()
        {
            var expected = new Dictionary<string, string>
            {
                ["abc"] = "def",
                ["ghi"] = "jkl,"
            };

            Serialize(expected);
            Reset();
            var actual = Deserialize<Dictionary<string, string>>();
            CollectionAssert.AreEqual(expected.ToList(), actual.ToList());
        }

        [TestMethod]
        public void CanSerializeList()
        {
            var expected = new[]
            {
                new Something
                {
                    BoolProp = true,
                    Else = new Else
                    {
                        Name = "Yoho"
                    },
                    Int32Prop = 999,
                    StringProp = "Yesbox!"
                },
                new Something(), new Something(), null
            }.ToList();

            Serialize(expected);
            Reset();
            var actual = Deserialize<List<Something>>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanSerializeArray()
        {
            var expected = new[]
            {
                new Something
                {
                    BoolProp = true,
                    Else = new Else
                    {
                        Name = "Yoho"
                    },
                    Int32Prop = 999,
                    StringProp = "Yesbox!"
                },
                new Something(),
                new Something(), null
            };
            Serialize(expected);
            Reset();
            var actual = Deserialize<Something[]>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanSerializeByteArray()
        {
            var expected = new byte[]
            {
                1,2,3,4
            };
            Serialize(expected);
            Reset();
            var actual = Deserialize<byte[]>();
            CollectionAssert.AreEqual(expected, actual);
        }

#if !NET35
        [TestMethod]
        public void Issue18()
        {
            var msg = new byte[] { 1, 2, 3, 4 };
            var serializer = new Serializer(new SerializerOptions(versionTolerance: true, preserveObjectReferences: true));

            byte[] serialized;
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(msg, ms);
                serialized = ms.ToArray();
            }

            byte[] deserialized;
            using (var ms = new MemoryStream(serialized))
            {
                deserialized = serializer.Deserialize<byte[]>(ms);
            }

            Assert.IsTrue(msg.SequenceEqual(deserialized));
        }
#endif

        [TestMethod]
        public void CanSerializeArrayOfTuples()
        {
            var expected = new[]
            {
                Tuple.Create(1,2,3),
                Tuple.Create(4,5,6),
                Tuple.Create(7,8,9),
            };
            Serialize(expected);
            Reset();
            var actual = Deserialize<Tuple<int,int,int>[]>();
            CollectionAssert.AreEqual(expected, actual);
        }


        //TODO: add support for multi dimentional arrays
        [TestMethod,Ignore]
        public void CanSerializeMultiDimentionalArray()
        {
            var expected = new double[3, 3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        expected[i, j, k] = i + j + k;
                    }
                }
            }
            Serialize(expected);
            Reset();
            var actual = Deserialize<double[,,]>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanSerializePrimitiveArray()
        {
            var expected = new[] {DateTime.MaxValue, DateTime.MinValue, DateTime.Now, DateTime.Today};
            Serialize(expected);
            Reset();
            var actual = Deserialize<DateTime[]>();
            CollectionAssert.AreEqual(expected, actual);
        }        
    }
}