using System.Collections.Generic;
using Neodroid.Utilities.Interfaces;
using Neodroid.Utilities.Sensors;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR

#endif

namespace Neodroid.Utilities.Unsorted {
  /// <summary>
  ///
  /// </summary>
  public static class NeodroidUtilities {
    /// <summary>
    ///
    /// </summary>
    /// <param name="rb"></param>
    /// <returns></returns>
    public static float KineticEnergy(Rigidbody rb) {
      return
          0.5f
          * rb.mass
          * Mathf.Pow(
              rb.velocity.magnitude,
              2); // mass in kg, velocity in meters per second, result is joules
    }

    public static Vector3 Vector3Clamp(ref Vector3 vec, Vector3 min_point, Vector3 max_point) {
      vec.x = Mathf.Clamp(vec.x, min_point.x, max_point.x);
      vec.y = Mathf.Clamp(vec.y, min_point.y, max_point.y);
      vec.z = Mathf.Clamp(vec.z, min_point.z, max_point.z);
      return vec;
    }

    public static void DrawLine(Vector3 p1, Vector3 p2, float width) {
      var count = Mathf.CeilToInt(width); // how many lines are needed.
      if (count == 1) {
        Gizmos.DrawLine(p1, p2);
      } else {
        var c = Camera.current;
        if (c == null) {
          Debug.LogError("Camera.current is null");
          return;
        }

        var v1 = (p2 - p1).normalized; // line direction
        var v2 = (c.transform.position - p1).normalized; // direction to camera
        var n = Vector3.Cross(v1, v2); // normal vector
        for (var i = 0; i < count; i++) {
          //Vector3 o = n * width ((float)i / (count - 1) - 0.5f);
          var o = n * width * ((float)i / (count - 1) - 0.5f);
          Gizmos.DrawLine(p1 + o, p2 + o);
        }
      }
    }

    public static AnimationCurve DefaultAnimationCurve() {
      return new AnimationCurve(new Keyframe(1, 1), new Keyframe(0, 0));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public static Gradient DefaultGradient() {
      var gradient = new Gradient {
          // The number of keys must be specified in this array initialiser
          colorKeys = new[] {
              // Add your colour and specify the stop point
              new GradientColorKey(new Color(1, 1, 1), 0),
              new GradientColorKey(new Color(1, 1, 1), 1f),
              new GradientColorKey(new Color(1, 1, 1), 0)
          },
          // This sets the alpha to 1 at both ends of the gradient
          alphaKeys = new[] {
              new GradientAlphaKey(1, 0),
              new GradientAlphaKey(1, 1),
              new GradientAlphaKey(1, 0)
          }
      };

      return gradient;
    }

    public static Texture2D RenderTextureImage(Camera camera) {
      // From unity documentation, https://docs.unity3d.com/ScriptReference/Camera.Render.html
      var current_render_texture = RenderTexture.active;
      RenderTexture.active = camera.targetTexture;
      camera.Render();
      var texture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
      texture.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
      texture.Apply();
      RenderTexture.active = current_render_texture;
      return texture;
    }

    public static void RegisterCollisionTriggerCallbacksOnChildren(
        Component caller,
        Transform parent,
        ChildSensor.OnChildCollisionEnterDelegate on_collision_enter_child,
        ChildSensor.OnChildTriggerEnterDelegate on_trigger_enter_child = null,
        ChildSensor.OnChildCollisionExitDelegate on_collision_exit_child = null,
        ChildSensor.OnChildTriggerExitDelegate on_trigger_exit_child = null,
        ChildSensor.OnChildCollisionStayDelegate on_collision_stay_child = null,
        ChildSensor.OnChildTriggerStayDelegate on_trigger_stay_child = null,
        bool debug = false) {
      var children_with_colliders = parent.GetComponentsInChildren<Collider>();

      foreach (var child in children_with_colliders) {
        var child_sensors = child.GetComponents<ChildSensor>();
        ChildSensor sensor = null;
        foreach (var child_sensor in child_sensors) {
          if (child_sensor.Caller != null && child_sensor.Caller == caller) {
            sensor = child_sensor;
            break;
          }

          if (child_sensor.Caller == null) {
            child_sensor.Caller = caller;
            sensor = child_sensor;
            break;
          }
        }

        if (sensor == null) {
          sensor = child.gameObject.AddComponent<ChildSensor>();
          sensor.Caller = caller;
        }

        if (on_collision_enter_child != null) {
          sensor.OnCollisionEnterDelegate = on_collision_enter_child;
        }

        if (on_trigger_enter_child != null) {
          sensor.OnTriggerEnterDelegate = on_trigger_enter_child;
        }

        if (on_collision_exit_child != null) {
          sensor.OnCollisionExitDelegate = on_collision_exit_child;
        }

        if (on_trigger_exit_child != null) {
          sensor.OnTriggerExitDelegate = on_trigger_exit_child;
        }

        if (on_trigger_stay_child != null) {
          sensor.OnTriggerStayDelegate = on_trigger_stay_child;
        }

        if (on_collision_stay_child != null) {
          sensor.OnCollisionStayDelegate = on_collision_stay_child;
        }

        if (debug) {
          Debug.Log(
              caller.name
              + " has created "
              + sensor.name
              + " on "
              + child.name
              + " under parent "
              + parent.name);
        }
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="colors"></param>
    /// <returns></returns>
    public static string ColorArrayToString(IEnumerable<Color> colors) {
      var s = "";
      foreach (var color in colors) {
        s += color.ToString();
      }

      return s;
    }

    public static TRecipient MaybeRegisterComponent<TRecipient, TCaller>(TRecipient r, TCaller c)
        where TRecipient : Object, IHasRegister<TCaller> where TCaller : Component, IRegisterable {
      TRecipient component;
      if (r != null) {
        component = r; //.GetComponent<Recipient>();
      } else if (c.GetComponentInParent<TRecipient>() != null) {
        component = c.GetComponentInParent<TRecipient>();
      } else {
        component = Object.FindObjectOfType<TRecipient>();
      }

      if (component != null) {
        component.Register(c);
      } else {
        Debug.Log($"Could not find a {typeof(TRecipient)} recipient during registeration");
      }

      return component;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="r"></param>
    /// <param name="c"></param>
    /// <param name="identifier"></param>
    /// <typeparam name="TRecipient"></typeparam>
    /// <typeparam name="TCaller"></typeparam>
    /// <returns></returns>
    public static TRecipient MaybeRegisterNamedComponent<TRecipient, TCaller>(
        TRecipient r,
        TCaller c,
        string identifier)
        where TRecipient : Object, IHasRegister<TCaller> where TCaller : Component, IRegisterable {
      TRecipient component;
      if (r != null) {
        component = r;
      } else if (c.GetComponentInParent<TRecipient>() != null) {
        component = c.GetComponentInParent<TRecipient>();
      } else {
        component = Object.FindObjectOfType<TRecipient>();
      }

      if (component != null) {
        component.Register(c, identifier);
      } else {
        #if NEODROID_DEBUG
        Debug.Log($"Could not find a {typeof(TRecipient)} recipient during registeration");
        #endif
      }

      return component;
    }

    /// Use this method to get all loaded objects of some type, including inactive objects.
    /// This is an alternative to Resources.FindObjectsOfTypeAll (returns project assets, including prefabs), and GameObject.FindObjectsOfTypeAll (deprecated).
    public static T[] FindAllObjectsOfTypeInScene<T>() {
      //(Scene scene) {
      var results = new List<T>();
      for (var i = 0; i < SceneManager.sceneCount; i++) {
        var s = SceneManager.GetSceneAt(i); // maybe EditorSceneManager
        if (!s.isLoaded) {
          continue;
        }

        var all_game_objects = s.GetRootGameObjects();
        foreach (var go in all_game_objects) {
          results.AddRange(go.GetComponentsInChildren<T>(true));
        }
      }

      return results.ToArray();
    }

    public static GameObject[] FindAllGameObjectsExceptLayer(int layer) {
      var goa = Object.FindObjectsOfType<GameObject>();
      var gol = new List<GameObject>();
      foreach (var go in goa) {
        if (go.layer != layer) {
          gol.Add(go);
        }
      }

      return gol.Count == 0 ? null : gol.ToArray();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static GameObject[] RecursiveChildGameObjectsExceptLayer(Transform parent, int layer) {
      var gol = new List<GameObject>();
      foreach (Transform go in parent) {
        if (go) {
          if (go.gameObject.layer != layer) {
            gol.Add(go.gameObject);
            var children = RecursiveChildGameObjectsExceptLayer(go, layer);
            if (children != null && children.Length > 0) {
              gol.AddRange(children);
            }
          }
        }
      }

      return gol.Count == 0 ? null : gol.ToArray();
    }
    #if UNITY_EDITOR
    public static void DrawString(
        string text,
        Vector3 world_pos,
        Color? color = null,
        float o_x = 0,
        float o_y = 0) {
      Handles.BeginGUI();

      var restore_color = GUI.color;

      if (color.HasValue) {
        GUI.color = color.Value;
      }

      var view = SceneView.currentDrawingSceneView;
      var screen_pos = view.camera.WorldToScreenPoint(world_pos);

      if (screen_pos.y < 0
          || screen_pos.y > Screen.height
          || screen_pos.x < 0
          || screen_pos.x > Screen.width
          || screen_pos.z < 0) {
        GUI.color = restore_color;
        Handles.EndGUI();
        return;
      }

      Handles.Label(TransformByPixel(world_pos, o_x, o_y), text);

      GUI.color = restore_color;
      Handles.EndGUI();
    }

    public static Vector3 TransformByPixel(Vector3 position, float x, float y) {
      return TransformByPixel(position, new Vector3(x, y));
    }

    /// <summary>
    ///
    /// </summary>
    enum GeneratorState {
      Expand_x_ = 0,
      Expand_y_ = 1,

      Inc_x_ = 2,
      Dec_x_ = 3,

      Inc_y_ = 4,
      Dec_y_ = 5
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static IEnumerable<Vector3> SnakeSpaceFillingGenerator(int length = 100) {
      var x = 0;
      var y = 0;
      var state = GeneratorState.Expand_x_;

      var out_vectors = new Vector3[length];
      if (length == 0) {
        return out_vectors;
      }

      //out_vectors[0] = new Vector3(x, 0, y);

      for (var i = 0; i < length; i++) {
        switch (state) {
          case GeneratorState.Expand_x_:
            x += 1;
            state = GeneratorState.Inc_y_;
            break;

          case GeneratorState.Inc_x_:
            x += 1;
            if (y == x) {
              state = GeneratorState.Dec_y_;
            }

            break;
          case GeneratorState.Dec_x_:
            x -= 1;
            if (x == 0) {
              state = GeneratorState.Expand_y_;
            }

            break;
          case GeneratorState.Expand_y_:
            y += 1;
            state = GeneratorState.Inc_x_;
            break;
          case GeneratorState.Inc_y_:
            y += 1;
            if (y == x) {
              state = GeneratorState.Dec_x_;
            }

            break;
          case GeneratorState.Dec_y_:
            y -= 1;
            if (y == 0) {
              state = GeneratorState.Expand_x_;
            }

            break;
          default: throw new System.ArgumentOutOfRangeException();
        }

        out_vectors[i] = new Vector3(x, 0, y);
      }

      return out_vectors;
    }

    public static Vector3 TransformByPixel(Vector3 position, Vector3 translate_by) {
      var cam = SceneView.currentDrawingSceneView.camera;
      return cam ? cam.ScreenToWorldPoint(cam.WorldToScreenPoint(position) + translate_by) : position;
    }
    #endif

    /** Contains logic for coverting a camera component into a Texture2D. */
    /*public Texture2D ObservationToTex(Camera camera, int width, int height)
        {
          Camera cam = camera;
          Rect oldRec = camera.rect;
          cam.rect = new Rect(0f, 0f, 1f, 1f);
          bool supportsAntialiasing = false;
          bool needsRescale = false;
          var depth = 24;
          var format = RenderTextureFormat.Default;
          var readWrite = RenderTextureReadWrite.Default;
          var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

          var finalRT =
            RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
          var renderRT = (!needsRescale) ? finalRT :
            RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
          var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

          var prevActiveRT = RenderTexture.active;
          var prevCameraRT = cam.targetTexture;

          // render to offscreen texture ( from CPU side)
          RenderTexture.active = renderRT;
          cam.targetTexture = renderRT;

          cam.Render();

          if (needsRescale)
          {
            RenderTexture.active = finalRT;
            Graphics.Blit(renderRT, finalRT);
            RenderTexture.ReleaseTemporary(renderRT);
          }

          tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
          tex.Apply();
          cam.targetTexture = prevCameraRT;
          cam.rect = oldRec;
          RenderTexture.active = prevActiveRT;
          RenderTexture.ReleaseTemporary(finalRT);
          return tex;
        }

        /// Contains logic to convert the agent's cameras into observation list
        ///  (as list of float arrays)
        public List<float[,,,]> GetObservationMatrixList(List<int> agent_keys)
        {
          List<float[,,,]> observation_matrix_list = new List<float[,,,]>();
          Dictionary<int, List<Camera>> observations = CollectObservations();
          for (int obs_number = 0; obs_number < brainParameters.cameraResolutions.Length; obs_number++)
          {
            int width = brainParameters.cameraResolutions[obs_number].width;
            int height = brainParameters.cameraResolutions[obs_number].height;
            bool bw = brainParameters.cameraResolutions[obs_number].blackAndWhite;
            int pixels = 0;
            if (bw)
              pixels = 1;
            else
              pixels = 3;
            float[,,,] observation_matrix = new float[agent_keys.Count
              , height
              , width
              , pixels];
            int i = 0;
            foreach (int k in agent_keys)
            {
              Camera agent_obs = observations[k][obs_number];
              Texture2D tex = ObservationToTex(agent_obs, width, height);
              for (int w = 0; w < width; w++)
              {
                for (int h = 0; h < height; h++)
                {
                  Color c = tex.GetPixel(w, h);
                  if (!bw)
                  {
                    observation_matrix[i, tex.height - h - 1, w, 0] = c.r;
                    observation_matrix[i, tex.height - h - 1, w, 1] = c.g;
                    observation_matrix[i, tex.height - h - 1, w, 2] = c.b;
                  }
                  else
                  {
                    observation_matrix[i, tex.height - h - 1, w, 0] = (c.r + c.g + c.b) / 3;
                  }
                }
              }
              UnityEngine.Object.DestroyImmediate(tex);
              Resources.UnloadUnusedAssets();
              i++;
            }
            observation_matrix_list.Add(observation_matrix);
          }
          return observation_matrix_list;
        }*/
  }
}
