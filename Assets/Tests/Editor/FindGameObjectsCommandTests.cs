using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP.Tests
{
    public class FindGameObjectsCommandTests
    {
        private FindGameObjectsCommand command;
        private GameObject testObject1;
        private GameObject testObject2;
        private GameObject testObject3;
        
        [SetUp]
        public void SetUp()
        {
            command = new FindGameObjectsCommand();
            
            // Create test GameObjects
            testObject1 = new GameObject("TestObject1");
            testObject2 = new GameObject("TestObject2");
            testObject3 = new GameObject("AnotherObject");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject1 != null) Object.DestroyImmediate(testObject1);
            if (testObject2 != null) Object.DestroyImmediate(testObject2);
            if (testObject3 != null) Object.DestroyImmediate(testObject3);
        }
        
        [Test]
        public void CommandName_ReturnsCorrectName()
        {
            Assert.That(command.CommandName, Is.EqualTo("findgameobjects"));
        }
        
        [Test]
        public void Description_ReturnsNonEmptyString()
        {
            Assert.That(command.Description, Is.Not.Null);
            Assert.That(command.Description, Is.Not.Empty);
        }
        
        [Test]
        public async Task ExecuteAsync_WithNamePattern_FindsMatchingObjects()
        {
            // Arrange
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "TestObject"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.results, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(2));
            Assert.That(response.results.Length, Is.EqualTo(2));
            
            // Check that both TestObject1 and TestObject2 are found
            string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
            Assert.That(foundNames, Does.Contain("TestObject1"));
            Assert.That(foundNames, Does.Contain("TestObject2"));
            Assert.That(foundNames, Does.Not.Contain("AnotherObject"));
        }
        
        [Test]
        public async Task ExecuteAsync_WithEmptyParameters_ReturnsError()
        {
            // Arrange
            JObject paramsJson = new JObject();
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(0));
            Assert.That(response.errorMessage, Is.Not.Null);
            Assert.That(response.errorMessage, Does.Contain("At least one search criterion"));
        }
        
        [Test]
        public async Task ExecuteAsync_WithComponentSearch_FindsObjectsWithSpecificComponent()
        {
            // Arrange
            testObject1.AddComponent<BoxCollider>();
            testObject2.AddComponent<Rigidbody>();
            testObject3.AddComponent<BoxCollider>();
            
            JObject paramsJson = new JObject
            {
                ["RequiredComponents"] = new JArray { "BoxCollider" }
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.GreaterThanOrEqualTo(2)); // Scene might have other objects with BoxCollider
            
            string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
            Assert.That(foundNames, Does.Contain("TestObject1"));
            Assert.That(foundNames, Does.Contain("AnotherObject"));
            Assert.That(foundNames, Does.Not.Contain("TestObject2"));
        }
        
        [Test]
        public async Task ExecuteAsync_WithMultipleComponentSearch_FindsObjectsWithAllComponents()
        {
            // Arrange
            testObject1.AddComponent<BoxCollider>();
            testObject1.AddComponent<Rigidbody>();
            testObject2.AddComponent<BoxCollider>();
            testObject3.AddComponent<Rigidbody>();
            
            JObject paramsJson = new JObject
            {
                ["RequiredComponents"] = new JArray { "BoxCollider", "Rigidbody" }
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(1));
            Assert.That(response.results[0].name, Is.EqualTo("TestObject1"));
            
            // Verify components are returned
            ComponentInfo boxCollider = System.Array.Find(response.results[0].components, c => c.type == "BoxCollider");
            ComponentInfo rigidbody = System.Array.Find(response.results[0].components, c => c.type == "Rigidbody");
            Assert.That(boxCollider, Is.Not.Null);
            Assert.That(rigidbody, Is.Not.Null);
        }
        
        [Test]
        public async Task ExecuteAsync_WithTagSearch_FindsObjectsWithSpecificTag()
        {
            // Arrange
            // Using tags that don't require pre-definition in Unity
            // All GameObjects start with "Untagged" by default
            testObject1.tag = "Untagged";
            testObject2.tag = "Untagged";
            testObject3.tag = "Untagged";
            
            JObject paramsJson = new JObject
            {
                ["Tag"] = "Untagged"
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.GreaterThanOrEqualTo(3)); // At least our 3 test objects
            
            string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
            Assert.That(foundNames, Does.Contain("TestObject1"));
            Assert.That(foundNames, Does.Contain("TestObject2"));
            Assert.That(foundNames, Does.Contain("AnotherObject"));
            
            // Verify tag is returned in results
            foreach (var result in response.results)
            {
                if (result.name == "TestObject1" || result.name == "TestObject2" || result.name == "AnotherObject")
                {
                    Assert.That(result.tag, Is.EqualTo("Untagged"));
                }
            }
        }
        
        [Test]
        public async Task ExecuteAsync_WithLayerSearch_FindsObjectsOnSpecificLayer()
        {
            // Arrange
            int enemyLayer = 8; // Assuming layer 8 is "Enemy" layer
            testObject1.layer = 0; // Default layer
            testObject2.layer = enemyLayer;
            testObject3.layer = enemyLayer;
            
            JObject paramsJson = new JObject
            {
                ["Layer"] = enemyLayer
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(2));
            
            string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
            Assert.That(foundNames, Does.Contain("TestObject2"));
            Assert.That(foundNames, Does.Contain("AnotherObject"));
            Assert.That(foundNames, Does.Not.Contain("TestObject1"));
            
            // Verify layer is returned in results
            Assert.That(response.results[0].layer, Is.EqualTo(enemyLayer));
        }
        
        [Test]
        public async Task ExecuteAsync_WithRegexSearch_FindsObjectsMatchingPattern()
        {
            // Arrange
            GameObject enemy1 = new GameObject("Enemy1");
            GameObject enemy2 = new GameObject("Enemy2");
            GameObject player = new GameObject("Player1");
            
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "Enemy\\d+",
                ["UseRegex"] = true
            };
            
            try
            {
                // Act
                BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
                FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
                
                // Assert
                Assert.That(response, Is.Not.Null);
                Assert.That(response.totalFound, Is.GreaterThanOrEqualTo(2));
                
                string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
                Assert.That(foundNames, Does.Contain("Enemy1"));
                Assert.That(foundNames, Does.Contain("Enemy2"));
                Assert.That(foundNames, Does.Not.Contain("Player1"));
            }
            finally
            {
                // Cleanup
                Object.DestroyImmediate(enemy1);
                Object.DestroyImmediate(enemy2);
                Object.DestroyImmediate(player);
            }
        }
        
        [Test]
        public async Task ExecuteAsync_WithIncludeInactive_FindsInactiveObjects()
        {
            // Arrange
            testObject1.SetActive(true);
            testObject2.SetActive(false);
            testObject3.SetActive(false);
            
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "Object",
                ["IncludeInactive"] = true
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(3)); // Should find all 3 objects including inactive
            
            string[] foundNames = System.Array.ConvertAll(response.results, r => r.name);
            Assert.That(foundNames, Does.Contain("TestObject1"));
            Assert.That(foundNames, Does.Contain("TestObject2"));
            Assert.That(foundNames, Does.Contain("AnotherObject"));
        }
        
        [Test]
        public async Task ExecuteAsync_WithoutIncludeInactive_ExcludesInactiveObjects()
        {
            // Arrange
            testObject1.SetActive(true);
            testObject2.SetActive(false);
            testObject3.SetActive(false);
            
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "Object",
                ["IncludeInactive"] = false
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(1)); // Should only find active object
            Assert.That(response.results[0].name, Is.EqualTo("TestObject1"));
            Assert.That(response.results[0].isActive, Is.True);
        }
        
        [Test]
        public async Task ExecuteAsync_WithComplexSearch_CombinesMultipleCriteria()
        {
            // Arrange
            testObject1.AddComponent<BoxCollider>();
            testObject1.layer = 0;
            testObject2.AddComponent<BoxCollider>();
            testObject2.layer = 8;
            testObject3.layer = 8;
            
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "Object",
                ["RequiredComponents"] = new JArray { "BoxCollider" },
                ["Layer"] = 8
            };
            
            // Act
            BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
            FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.totalFound, Is.EqualTo(1)); // Only TestObject2 matches all criteria
            Assert.That(response.results[0].name, Is.EqualTo("TestObject2"));
            
            // Verify all criteria are met
            ComponentInfo boxCollider = System.Array.Find(response.results[0].components, c => c.type == "BoxCollider");
            Assert.That(boxCollider, Is.Not.Null);
            Assert.That(response.results[0].layer, Is.EqualTo(8));
        }
        
        [Test]
        public async Task ExecuteAsync_WithMaxResults_LimitsReturnedObjects()
        {
            // Arrange
            // Create many GameObjects
            GameObject[] manyObjects = new GameObject[20];
            for (int i = 0; i < 20; i++)
            {
                manyObjects[i] = new GameObject($"ManyObject{i}");
            }
            
            JObject paramsJson = new JObject
            {
                ["NamePattern"] = "ManyObject",
                ["MaxResults"] = 5
            };
            
            try
            {
                // Act
                BaseCommandResponse baseResponse = await command.ExecuteAsync(paramsJson);
                FindGameObjectsResponse response = baseResponse as FindGameObjectsResponse;
                
                // Assert
                Assert.That(response, Is.Not.Null);
                Assert.That(response.results.Length, Is.EqualTo(5)); // Should be limited to 5
                Assert.That(response.totalFound, Is.EqualTo(5)); // Total found should also be 5
                
                // Verify all results match the pattern
                foreach (var result in response.results)
                {
                    Assert.That(result.name, Does.StartWith("ManyObject"));
                }
            }
            finally
            {
                // Cleanup
                foreach (var obj in manyObjects)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }
    }
}