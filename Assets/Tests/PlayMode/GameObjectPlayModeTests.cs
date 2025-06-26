using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests that work with GameObjects and Components
    /// </summary>
    public class GameObjectPlayModeTests
    {
        private GameObject testGameObject;

        [SetUp]
        public void SetUp()
        {
            // Create a test GameObject before each test
            testGameObject = new GameObject("TestGameObject");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void GameObject_Creation_ShouldWork()
        {
            // Assert
            Assert.IsNotNull(testGameObject, "GameObject should be created successfully");
            Assert.AreEqual("TestGameObject", testGameObject.name, "GameObject should have correct name");
            Assert.IsTrue(testGameObject.activeInHierarchy, "GameObject should be active");
        }

        [Test]
        public void Transform_Component_ShouldExist()
        {
            // Act
            Transform transform = testGameObject.GetComponent<Transform>();
            
            // Assert
            Assert.IsNotNull(transform, "GameObject should have a Transform component");
            Assert.AreEqual(Vector3.zero, transform.position, "Transform position should be zero by default");
            Assert.AreEqual(Quaternion.identity, transform.rotation, "Transform rotation should be identity by default");
            Assert.AreEqual(Vector3.one, transform.localScale, "Transform scale should be one by default");
        }

        [Test]
        public void AddComponent_ShouldWork()
        {
            // Act - Use a built-in component that's always available
            Camera camera = testGameObject.AddComponent<Camera>();
            
            // Assert
            Assert.IsNotNull(camera, "Camera component should be added successfully");
            Assert.AreEqual(testGameObject, camera.gameObject, "Component should belong to correct GameObject");
        }

        [Test]
        public void Component_GetComponent_ShouldWork()
        {
            // Arrange
            testGameObject.AddComponent<Camera>();
            
            // Act
            Camera camera = testGameObject.GetComponent<Camera>();
            
            // Assert
            Assert.IsNotNull(camera, "GetComponent should find the added component");
        }

        [UnityTest]
        public IEnumerator GameObject_Transform_Movement_ShouldWork()
        {
            // Arrange
            Vector3 startPosition = new Vector3(0, 0, 0);
            Vector3 targetPosition = new Vector3(5, 0, 0);
            testGameObject.transform.position = startPosition;
            
            // Act - Move object over time
            float moveSpeed = 10.0f;
            while (Vector3.Distance(testGameObject.transform.position, targetPosition) > 0.1f)
            {
                testGameObject.transform.position = Vector3.MoveTowards(
                    testGameObject.transform.position, 
                    targetPosition, 
                    moveSpeed * Time.deltaTime);
                yield return null;
            }
            
            // Assert
            Vector3 finalPosition = testGameObject.transform.position;
            Assert.LessOrEqual(Vector3.Distance(finalPosition, targetPosition), 0.2f, "GameObject should reach target position");
        }

        [Test]
        public void GameObject_SetActive_ShouldWork()
        {
            // Arrange
            Assert.IsTrue(testGameObject.activeInHierarchy, "GameObject should start active");
            
            // Act
            testGameObject.SetActive(false);
            
            // Assert
            Assert.IsFalse(testGameObject.activeInHierarchy, "GameObject should be inactive after SetActive(false)");
            
            // Act
            testGameObject.SetActive(true);
            
            // Assert
            Assert.IsTrue(testGameObject.activeInHierarchy, "GameObject should be active after SetActive(true)");
        }

        [Test]
        public void GameObject_Instantiate_ShouldWork()
        {
            // Act
            GameObject instantiatedObject = Object.Instantiate(testGameObject);
            
            try
            {
                // Assert
                Assert.IsNotNull(instantiatedObject, "Instantiated GameObject should not be null");
                Assert.AreNotEqual(testGameObject, instantiatedObject, "Instantiated GameObject should be different from original");
                Assert.AreEqual(testGameObject.name + "(Clone)", instantiatedObject.name, "Instantiated GameObject should have (Clone) suffix");
            }
            finally
            {
                // Cleanup
                if (instantiatedObject != null)
                {
                    Object.DestroyImmediate(instantiatedObject);
                }
            }
        }

        [Test]
        public void GameObject_FindObjectOfType_ShouldFail()
        {
            // This test is designed to sometimes fail to test error reporting
            // Remove the Camera component if it exists
            Camera existingCamera = testGameObject.GetComponent<Camera>();
            if (existingCamera != null)
            {
                Object.DestroyImmediate(existingCamera);
            }
            
            // Act
            Camera foundCamera = Object.FindObjectOfType<Camera>();
            
            // Assert - This will fail if no Camera exists in the scene
            Assert.IsNotNull(foundCamera, "This test may fail if no Camera exists in the scene");
        }
    }
}