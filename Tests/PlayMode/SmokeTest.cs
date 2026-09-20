using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hollow.Tests
{
    /// <summary>
    /// Boots the game in an empty scene, lets it run for a few seconds and fails on any logged
    /// error or exception (UnityTest fails automatically on unexpected Error/Exception logs).
    /// The game is looked up by object name because runtime code lives in Assembly-CSharp.
    /// </summary>
    public class SmokeTest
    {
        [UnityTest]
        public IEnumerator GameBootsAndRuns()
        {
            yield return null;
            yield return null;

            // The bootstrapper (RuntimeInitializeOnLoadMethod) normally creates the game on entering
            // Play Mode. If the test runner's scene swap removed it, create it the same way.
            if (GameObject.Find("Game") == null)
            {
                var type = System.Type.GetType("Hollow.Game, Assembly-CSharp");
                Assert.IsNotNull(type, "Hollow.Game type not found in Assembly-CSharp");
                new GameObject("Game").AddComponent(type);
                yield return null;
            }

            Assert.IsNotNull(GameObject.Find("Game"), "Game object was not created by the bootstrapper");
            Assert.IsNotNull(GameObject.Find("Player"), "Player missing");
            Assert.IsNotNull(GameObject.Find("Dungeon"), "Dungeon missing");
            Assert.IsNotNull(GameObject.Find("HUD"), "HUD missing");

            // Let AI, physics, coroutines and the HUD run for a while.
            yield return new WaitForSeconds(3f);

            int enemies = 0;
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (go.name.StartsWith("Enemy_")) enemies++;
            Assert.Greater(enemies, 3, "Expected the floor to contain enemies");
        }
    }
}
