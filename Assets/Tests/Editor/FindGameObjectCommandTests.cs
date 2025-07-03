using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP.Tests
{
    public class FindGameObjectCommandTests
    {
        private FindGameObjectCommand command;
        private GameObject testRoot;
        
        [SetUp]
        public void SetUp()
        {
            command = new FindGameObjectCommand();
            testRoot = new GameObject("TestGameObject");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
                Object.DestroyImmediate(testRoot);
        }
        
        [Test]
        public void CommandName_ReturnsCorrectName()
        {
            Assert.That(command.CommandName, Is.EqualTo("findgameobject"));
        }
        
        [Test]
        public void Description_ReturnsNonEmptyString()
        {
            Assert.That(command.Description, Is.Not.Null);
            Assert.That(command.Description, Is.Not.Empty);
        }
        
        [Test]
        public async Task ExecuteAsync_WithNonExistentPath_ReturnsNotFound()
        {
            // Arrange
            JObject paramsJson = new JObject
            {
                ["Path"] = "NonExistent/GameObject"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectResponse response = baseResponse as FindGameObjectResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.found, Is.False);
            Assert.That(response.errorMessage, Is.Not.Null);
        }
        
        [Test]
        public async Task ExecuteAsync_WithExistingGameObject_ReturnsCorrectInfo()
        {
            // Arrange
            JObject paramsJson = new JObject
            {
                ["Path"] = "TestGameObject"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectResponse response = baseResponse as FindGameObjectResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.found, Is.True);
            Assert.That(response.name, Is.EqualTo("TestGameObject"));
            Assert.That(response.path, Is.EqualTo("TestGameObject"));
            Assert.That(response.isActive, Is.True);
        }
        
        [Test]
        public async Task ExecuteAsync_WithGameObjectWithComponents_ReturnsComponentInfo()
        {
            // Arrange
            testRoot.AddComponent<BoxCollider>();
            testRoot.AddComponent<Rigidbody>();
            
            JObject paramsJson = new JObject
            {
                ["Path"] = "TestGameObject"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectResponse response = baseResponse as FindGameObjectResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.found, Is.True);
            Assert.That(response.components, Is.Not.Null);
            Assert.That(response.components.Length, Is.GreaterThan(2)); // At least Transform + BoxCollider + Rigidbody
            
            ComponentInfo transformComponent = System.Array.Find(response.components, c => c.type == "Transform");
            Assert.That(transformComponent, Is.Not.Null);
            
            ComponentInfo boxColliderComponent = System.Array.Find(response.components, c => c.type == "BoxCollider");
            Assert.That(boxColliderComponent, Is.Not.Null);
        }
        
        [Test]
        public async Task ExecuteAsync_WithTransformComponent_ReturnsPositionProperties()
        {
            // Arrange
            testRoot.transform.position = new Vector3(1.5f, 2.5f, 3.5f);
            testRoot.transform.rotation = Quaternion.Euler(45f, 90f, 180f);
            testRoot.transform.localScale = new Vector3(2f, 3f, 4f);
            
            JObject paramsJson = new JObject
            {
                ["Path"] = "TestGameObject"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectResponse response = baseResponse as FindGameObjectResponse;
            
            // Assert
            ComponentInfo transformComponent = System.Array.Find(response.components, c => c.type == "Transform");
            Assert.That(transformComponent, Is.Not.Null);
            Assert.That(transformComponent.properties, Is.Not.Null);
            Assert.That(transformComponent.properties.Length, Is.GreaterThan(0));
            
            // Check for position property
            ComponentPropertyInfo positionProp = System.Array.Find(transformComponent.properties, p => p.name == "position");
            Assert.That(positionProp, Is.Not.Null);
            Assert.That(positionProp.type, Does.Contain("Vector3"));
        }
        
        [Test]
        public async Task ExecuteAsync_WithHierarchicalPath_FindsNestedGameObject()
        {
            // Arrange
            GameObject parent = new GameObject("Parent");
            GameObject child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            
            JObject paramsJson = new JObject
            {
                ["Path"] = "Parent/Child"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectResponse response = baseResponse as FindGameObjectResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.found, Is.True);
            Assert.That(response.name, Is.EqualTo("Child"));
            Assert.That(response.path, Is.EqualTo("Parent/Child"));
            
            // Cleanup
            Object.DestroyImmediate(parent);
        }
    }
}