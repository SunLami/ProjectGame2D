using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class LiveMapBindingTests
{
    [UnityTest]
    public IEnumerator FindsAdditiveWorldCameraWhileBootstrapIsActive()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene bootstrap = SceneManager.CreateScene("LiveMapTestBootstrap");
        Scene world = SceneManager.CreateScene("MapDuy");

        try
        {
            Assert.IsTrue(SceneManager.SetActiveScene(bootstrap));
            var bootstrapCamera = new GameObject("Bootstrap Camera").AddComponent<Camera>();
            bootstrapCamera.tag = "MainCamera";

            var worldCamera = new GameObject("World Camera").AddComponent<Camera>();
            worldCamera.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(worldCamera.gameObject, world);

            var border = new GameObject("BorderMap").AddComponent<BoxCollider2D>();
            border.size = new Vector2(80f, 40f);
            SceneManager.MoveGameObjectToScene(border.gameObject, world);

            Scene resolved = LiveMapCamera.ResolveWorldScene();
            Assert.AreEqual(world, resolved);
            Assert.IsTrue(LiveMapCamera.TryGetSource(resolved, out Camera source, out Bounds bounds));
            Assert.AreSame(worldCamera, source);
            Assert.AreEqual(new Vector2(80f, 40f), (Vector2)bounds.size);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            SceneManager.UnloadSceneAsync(world);
            SceneManager.UnloadSceneAsync(bootstrap);
        }

        yield return null;
    }
}
