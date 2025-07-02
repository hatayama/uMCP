using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    public sealed class TestSessionManager : ScriptableSingleton<TestSessionManager>
    {
        [SerializeField] private bool testBoolValue;
        [SerializeField] private int testIntValue;
        [SerializeField] private string testStringValue = "";
        [SerializeField] private float testFloatValue;

        public bool TestBoolValue
        {
            get => testBoolValue;
            set
            {
                testBoolValue = value;
                Save(true);
            }
        }

        public int TestIntValue
        {
            get => testIntValue;
            set
            {
                testIntValue = value;
                Save(true);
            }
        }

        public string TestStringValue
        {
            get => testStringValue;
            set
            {
                testStringValue = value ?? "";
                Save(true);
            }
        }

        public float TestFloatValue
        {
            get => testFloatValue;
            set
            {
                testFloatValue = value;
                Save(true);
            }
        }

        public void ResetAllValues()
        {
            testBoolValue = false;
            testIntValue = 0;
            testStringValue = "";
            testFloatValue = 0.0f;
            Save(true);
        }

        public string GetAllValuesAsString()
        {
            return $"Bool: {testBoolValue}, Int: {testIntValue}, String: '{testStringValue}', Float: {testFloatValue}";
        }
    }
}