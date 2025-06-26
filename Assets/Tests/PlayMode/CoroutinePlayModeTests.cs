using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests that focus on coroutines and time-based functionality
    /// </summary>
    public class CoroutinePlayModeTests
    {
        [UnityTest]
        public IEnumerator WaitForSeconds_ShouldWork()
        {
            // Arrange
            float startTime = Time.time;
            float waitTime = 0.5f;
            
            // Act
            yield return new WaitForSeconds(waitTime);
            
            // Assert
            float endTime = Time.time;
            float elapsed = endTime - startTime;
            Assert.GreaterOrEqual(elapsed, waitTime * 0.9f, $"Should wait at least {waitTime * 0.9f} seconds");
            Assert.LessOrEqual(elapsed, waitTime * 1.5f, $"Should not wait more than {waitTime * 1.5f} seconds");
        }

        [UnityTest]
        public IEnumerator WaitForFixedUpdate_ShouldWork()
        {
            // Arrange
            float startTime = Time.fixedTime;
            
            // Act
            yield return new WaitForFixedUpdate();
            
            // Assert
            float endTime = Time.fixedTime;
            Assert.Greater(endTime, startTime, "Fixed time should advance after WaitForFixedUpdate");
        }

        [UnityTest]
        public IEnumerator WaitForEndOfFrame_ShouldWork()
        {
            // Arrange
            int startFrameCount = Time.frameCount;
            
            // Act
            yield return new WaitForEndOfFrame();
            
            // Assert
            int endFrameCount = Time.frameCount;
            Assert.GreaterOrEqual(endFrameCount, startFrameCount, "Frame count should advance after WaitForEndOfFrame");
        }

        [UnityTest]
        public IEnumerator MultipleFrameWait_ShouldWork()
        {
            // Arrange
            int startFrameCount = Time.frameCount;
            int framesToWait = 5;
            
            // Act
            for (int i = 0; i < framesToWait; i++)
            {
                yield return null; // Wait one frame
            }
            
            // Assert
            int endFrameCount = Time.frameCount;
            int framesElapsed = endFrameCount - startFrameCount;
            Assert.GreaterOrEqual(framesElapsed, framesToWait, $"Should wait at least {framesToWait} frames");
        }

        [UnityTest]
        public IEnumerator TimeScale_ShouldAffectTime()
        {
            // Arrange
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 2.0f; // Speed up time
            float startTime = Time.time;
            
            try
            {
                // Act
                yield return new WaitForSeconds(0.5f);
                
                // Assert
                float endTime = Time.time;
                float elapsed = endTime - startTime;
                // With timeScale = 2, waiting 0.5 seconds should take about 0.25 real seconds
                // But Time.time should still show the scaled time
                Assert.Greater(elapsed, 0.4f, "Time should advance with timeScale");
            }
            finally
            {
                // Cleanup
                Time.timeScale = originalTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator GameObject_Movement_OverTime_ShouldWork()
        {
            // Arrange
            GameObject testObject = new GameObject("MovingObject");
            Vector3 startPosition = Vector3.zero;
            Vector3 targetPosition = new Vector3(5, 0, 0);
            float moveSpeed = 10.0f;
            float expectedTime = Vector3.Distance(startPosition, targetPosition) / moveSpeed;
            
            testObject.transform.position = startPosition;
            float startTime = Time.time;
            
            try
            {
                // Act - Move object over time
                while (Vector3.Distance(testObject.transform.position, targetPosition) > 0.1f)
                {
                    testObject.transform.position = Vector3.MoveTowards(
                        testObject.transform.position, 
                        targetPosition, 
                        moveSpeed * Time.deltaTime);
                    yield return null;
                }
                
                // Assert
                float endTime = Time.time;
                float elapsed = endTime - startTime;
                
                Assert.LessOrEqual(Vector3.Distance(testObject.transform.position, targetPosition), 0.1f, 
                    "Object should reach target position");
                Assert.GreaterOrEqual(elapsed, expectedTime * 0.8f, 
                    "Movement should take approximately the expected time");
            }
            finally
            {
                // Cleanup
                if (testObject != null)
                {
                    Object.DestroyImmediate(testObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator Coroutine_Exception_ShouldFail()
        {
            // Arrange
            yield return null; // Wait one frame
            
            // Act & Assert - This will fail with an exception
            throw new System.InvalidOperationException("This coroutine test is designed to fail with an exception");
        }

        [UnityTest]
        public IEnumerator Animation_Simulation_ShouldWork()
        {
            // Arrange
            GameObject animatedObject = new GameObject("AnimatedObject");
            float animationDuration = 1.0f;
            float startScale = 1.0f;
            float endScale = 2.0f;
            float startTime = Time.time;
            
            try
            {
                animatedObject.transform.localScale = Vector3.one * startScale;
                
                // Act - Animate scale over time
                while (Time.time - startTime < animationDuration)
                {
                    float progress = (Time.time - startTime) / animationDuration;
                    float currentScale = Mathf.Lerp(startScale, endScale, progress);
                    animatedObject.transform.localScale = Vector3.one * currentScale;
                    yield return null;
                }
                
                // Final frame
                animatedObject.transform.localScale = Vector3.one * endScale;
                
                // Assert
                float finalScale = animatedObject.transform.localScale.x;
                Assert.AreEqual(endScale, finalScale, 0.1f, "Animation should reach target scale");
                
                float totalTime = Time.time - startTime;
                Assert.GreaterOrEqual(totalTime, animationDuration * 0.9f, "Animation should take expected duration");
            }
            finally
            {
                // Cleanup
                if (animatedObject != null)
                {
                    Object.DestroyImmediate(animatedObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator WaitUntil_Condition_ShouldWork()
        {
            // Arrange
            bool conditionMet = false;
            float startTime = Time.time;
            float delay = 1.0f;
            
            // Simulate a condition that will be met after delay
            float conditionStartTime = Time.time;
            
            // Act
            yield return new WaitUntil(() => {
                if (Time.time - conditionStartTime >= delay)
                {
                    conditionMet = true;
                }
                return conditionMet;
            });
            
            // Assert
            float endTime = Time.time;
            float elapsed = endTime - startTime;
            Assert.IsTrue(conditionMet, "Condition should be met");
            Assert.GreaterOrEqual(elapsed, delay * 0.9f, "Should wait for the condition to be met");
        }
    }
}