using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// Intentionally failing PlayMode tests to verify error reporting accuracy
    /// </summary>
    public class FailingPlayModeTests
    {
        [Test]
        public void MathTest_ShouldFail()
        {
            // Arrange
            int expected = 10;
            int actual = 2 + 3; // This will be 5, not 10
            
            // Act & Assert - This will fail intentionally
            Assert.AreEqual(expected, actual, "This test is designed to fail - math result is wrong");
        }

        [Test]
        public void StringTest_ShouldFail()
        {
            // Arrange
            string expected = "Expected String";
            string actual = "Actual String"; // Different string
            
            // Act & Assert - This will fail intentionally
            Assert.AreEqual(expected, actual, "This test is designed to fail - strings don't match");
        }

        [UnityTest]
        public IEnumerator TimeTest_ShouldFail()
        {
            // Arrange
            float startTime = Time.time;
            
            // Act - Wait for a very short time
            yield return new WaitForSeconds(0.1f);
            
            // Assert - This will fail because we expect too much time to pass
            float endTime = Time.time;
            float elapsed = endTime - startTime;
            Assert.Greater(elapsed, 5.0f, "This test is designed to fail - not enough time elapsed");
        }

        [Test]
        public void NullTest_ShouldFail()
        {
            // Arrange
            GameObject nullObject = null;
            
            // Act & Assert - This will fail with null reference
            Assert.IsNotNull(nullObject, "This test is designed to fail - object is null");
        }

        [Test]
        public void ExceptionTest_ShouldFail()
        {
            // This test will fail by throwing an exception
            throw new System.InvalidOperationException("This test is designed to fail with an exception");
        }

        [Test, Ignore("This test is intentionally ignored")]
        public void IgnoredTest_ShouldBeSkipped()
        {
            // This test should be skipped/ignored
            Assert.IsTrue(true, "This test should never run because it's ignored");
        }

        [Test]
        public void BooleanTest_ShouldFail()
        {
            // Arrange
            bool expected = true;
            bool actual = false;
            
            // Act & Assert - This will fail
            Assert.AreEqual(expected, actual, "This test is designed to fail - boolean values don't match");
        }
    }
}