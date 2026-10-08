using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Survivor.io-style camera: fixed top-down, tight on the hero, zooming out a little as the swarm grows.</summary>
    public class CameraRig
    {
        public Camera Camera { get; }
        float shake, zoom;
        Vector2 pos;

        public bool ShakeEnabled { get; set; } = true;

        public CameraRig(Camera cam, GameConfig cfg)
        {
            Camera = cam;
            cam.orthographic = true;
            cam.orthographicSize = cfg.cameraSize;
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0, 1, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.13f, 0.22f);
            zoom = cfg.cameraSize;
        }

        public void Snap(Vector2 target)
        {
            pos = target;
            Apply();
        }

        public void AddShake(float amount)
        {
            if (ShakeEnabled) shake = Mathf.Min(0.6f, shake + amount);
        }

        public void Tick(Vector2 target, int enemies, GameConfig cfg, float dt)
        {
            pos = Vector2.Lerp(pos, target, 1f - Mathf.Exp(-cfg.cameraFollow * dt));
            float wantZoom = Mathf.Lerp(cfg.cameraSize, cfg.cameraSizeSwarm, Mathf.Clamp01(enemies / (float)Mathf.Max(1, cfg.cameraSwarmCount)));
            zoom = Mathf.Lerp(zoom, wantZoom, 1f - Mathf.Exp(-0.8f * dt));
            Camera.orthographicSize = zoom;
            shake = Mathf.Max(0f, shake - dt * 1.8f);
            Apply();
        }

        void Apply()
        {
            Vector2 off = shake > 0 ? Random.insideUnitCircle * shake * 0.5f : Vector2.zero;
            Camera.transform.position = new Vector3(pos.x + off.x, pos.y + off.y, -10f);
            Camera.transform.rotation = Quaternion.identity;
        }

        public Vector2 Position => pos;
    }
}
