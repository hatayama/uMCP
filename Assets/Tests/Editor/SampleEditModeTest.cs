using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class SampleEditModeTest
    {
        [Test]
        public void SampleTest_SimplePasses()
        {
            // Arrange
            int expected = 5;
            int actual = 2 + 3;
            
            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SampleTest_GameObject_CanBeCreated()
        {
            // Arrange & Act
            GameObject testObject = new GameObject("TestObject");
            
            // Assert
            Assert.IsNotNull(testObject);
            Assert.AreEqual("TestObject", testObject.name);
            
            // Cleanup
            GameObject.DestroyImmediate(testObject);
        }
        
        [Test]
        public void SampleTest_Transform_DefaultValues()
        {
            // Arrange
            GameObject testObject = new GameObject("TestObject");
            
            // Act
            Transform transform = testObject.transform;
            
            // Assert
            Assert.AreEqual(Vector3.zero, transform.position);
            Assert.AreEqual(Quaternion.identity, transform.rotation);
            Assert.AreEqual(Vector3.one, transform.localScale);
            
            // Cleanup
            GameObject.DestroyImmediate(testObject);
        }
    }
} 