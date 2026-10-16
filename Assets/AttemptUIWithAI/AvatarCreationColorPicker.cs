using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AvatarCreationColorPicker : MonoBehaviour
    {
        [SerializeField] private Color initialColor = new Color(1f, 0.28f, 0.08f);
        public Color SelectedColor => Color.HSVToRGB(hue, saturation, brightness);
        public float Hue => hue;
        public float Saturation => saturation;
        public float Brightness => brightness;
        public event Action<Color> ColorChanged;

        private float hue, saturation = 1f, brightness = 1f;
        private VisualElement panel, wheel, swatch, saturationTrack, brightnessTrack;
        private PickerDrag hueDrag, saturationDrag, brightnessDrag;
        private bool initialized;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q<VisualElement>("colorPanel");
            wheel = root.Q<VisualElement>("hueWheel");
            swatch = root.Q<VisualElement>("colorSwatch");
            saturationTrack = root.Q<VisualElement>("saturationSlider");
            brightnessTrack = root.Q<VisualElement>("brightnessSlider");
            if (panel == null || wheel == null || swatch == null || saturationTrack == null || brightnessTrack == null)
            {
                Debug.LogError("Avatar color picker requires the named elements in AvatarCreation.uxml.", this);
                return;
            }
            if (!initialized)
            {
                Color.RGBToHSV(initialColor, out hue, out saturation, out brightness);
                initialized = true;
            }
            panel.AddToClassList("runtime-picker");
            swatch.pickingMode = PickingMode.Ignore;
            wheel.generateVisualContent += DrawHueHandle;
            swatch.generateVisualContent += DrawSwatch;
            saturationTrack.generateVisualContent += DrawSaturation;
            brightnessTrack.generateVisualContent += DrawBrightness;
            hueDrag = new PickerDrag(UpdateHue);
            saturationDrag = new PickerDrag(p => UpdateTrack(p, saturationTrack, true));
            brightnessDrag = new PickerDrag(p => UpdateTrack(p, brightnessTrack, false));
            wheel.AddManipulator(hueDrag);
            saturationTrack.AddManipulator(saturationDrag);
            brightnessTrack.AddManipulator(brightnessDrag);
            Repaint();
        }

        private void OnDisable()
        {
            if (hueDrag == null) return;
            wheel.RemoveManipulator(hueDrag);
            saturationTrack.RemoveManipulator(saturationDrag);
            brightnessTrack.RemoveManipulator(brightnessDrag);
            wheel.generateVisualContent -= DrawHueHandle;
            swatch.generateVisualContent -= DrawSwatch;
            saturationTrack.generateVisualContent -= DrawSaturation;
            brightnessTrack.generateVisualContent -= DrawBrightness;
            panel.RemoveFromClassList("runtime-picker");
            hueDrag = null;
        }

        public void SetColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            // Preserve the previous hue when importing an achromatic color.
            SetHSV(s > 0f ? h : hue, s, v);
        }

        public void SetHSV(float h, float s, float v)
        {
            if (float.IsNaN(h) || float.IsInfinity(h) || float.IsNaN(s) || float.IsInfinity(s) || float.IsNaN(v) || float.IsInfinity(v)) return;
            h = Mathf.Repeat(h, 1f);
            s = Mathf.Clamp01(s);
            v = Mathf.Clamp01(v);
            if (h == hue && s == saturation && v == brightness) return;
            hue = h;
            saturation = s;
            brightness = v;
            initialized = true;
            Repaint();
            ColorChanged?.Invoke(SelectedColor);
        }

        private bool UpdateHue(Vector2 point)
        {
            var delta = point - wheel.contentRect.center;
            float radius = Mathf.Min(wheel.contentRect.width, wheel.contentRect.height) * 0.5f;
            // Initial presses must hit the ring. Captured drags may move off the ring.
            if (!wheel.HasPointerCapture(activePointer) && (delta.magnitude < radius * 0.76f || delta.magnitude > radius + 12f)) return false;
            float angle = Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg;
            SetHSV((150f - angle) / 360f, saturation, brightness);
            return true;
        }

        private int activePointer => hueDrag == null ? -1 : hueDrag.PointerId;

        private bool UpdateTrack(Vector2 point, VisualElement track, bool isSaturation)
        {
            float inset = track.contentRect.width * 0.5f;
            float value = 1f - Mathf.InverseLerp(inset, track.contentRect.height - inset, point.y);
            SetHSV(hue, isSaturation ? value : saturation, isSaturation ? brightness : value);
            return true;
        }

        private void Repaint()
        {
            wheel?.MarkDirtyRepaint();
            swatch?.MarkDirtyRepaint();
            saturationTrack?.MarkDirtyRepaint();
            brightnessTrack?.MarkDirtyRepaint();
        }

        private static void Circle(Painter2D painter, Vector2 center, float radius, Color color)
        {
            if (radius <= 0f) return;
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            painter.ClosePath();
            painter.Fill();
        }

        private void DrawSwatch(MeshGenerationContext ctx)
        {
            var rect = swatch.contentRect;
            Circle(ctx.painter2D, rect.center, Mathf.Min(rect.width, rect.height) * 0.22f, SelectedColor);
        }

        private void DrawHueHandle(MeshGenerationContext ctx)
        {
            var rect = wheel.contentRect;
            float angle = (hue * 360f - 150f) * Mathf.Deg2Rad;
            float radius = Mathf.Min(rect.width, rect.height) * 0.445f;
            var point = rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Circle(ctx.painter2D, point, 23f, Color.white);
            Circle(ctx.painter2D, point, 15f, Color.HSVToRGB(hue, 1f, 1f));
        }

        private void DrawSaturation(MeshGenerationContext ctx) => DrawTrack(ctx, saturationTrack, true);
        private void DrawBrightness(MeshGenerationContext ctx) => DrawTrack(ctx, brightnessTrack, false);

        private void DrawTrack(MeshGenerationContext ctx, VisualElement track, bool isSaturation)
        {
            var rect = track.contentRect;
            if (rect.width <= 0f || rect.height <= rect.width) return;
            var painter = ctx.painter2D;
            float radius = rect.width * 0.5f;
            float length = rect.height - rect.width;
            // Short horizontal strokes produce a smooth gradient on Unity 6.0.
            painter.lineWidth = length / 128f + 1f;
            for (int i = 0; i < 128; i++)
            {
                float t = i / 127f;
                painter.strokeColor = Color.HSVToRGB(hue, isSaturation ? 1f - t : saturation, isSaturation ? brightness : 1f - t);
                float y = radius + length * t;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }
            Circle(painter, new Vector2(radius, radius), radius, Color.HSVToRGB(hue, isSaturation ? 1f : saturation, isSaturation ? brightness : 1f));
            Circle(painter, new Vector2(radius, rect.height - radius), radius, Color.HSVToRGB(hue, isSaturation ? 0f : saturation, isSaturation ? brightness : 0f));
            float value = isSaturation ? saturation : brightness;
            var handle = new Vector2(radius, radius + length * (1f - value));
            Circle(painter, handle, 23f, Color.white);
            Circle(painter, handle, 15f, SelectedColor);
        }

        private sealed class PickerDrag : PointerManipulator
        {
            private readonly Func<Vector2, bool> update;
            public int PointerId { get; private set; } = -1;
            public PickerDrag(Func<Vector2, bool> update) { this.update = update; }
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(Down);
                target.RegisterCallback<PointerMoveEvent>(Move);
                target.RegisterCallback<PointerUpEvent>(Up);
                target.RegisterCallback<PointerCancelEvent>(Cancel);
                target.RegisterCallback<PointerCaptureOutEvent>(Lost);
            }
            protected override void UnregisterCallbacksFromTarget()
            {
                if (PointerId >= 0 && target.HasPointerCapture(PointerId)) target.ReleasePointer(PointerId);
                PointerId = -1;
                target.UnregisterCallback<PointerDownEvent>(Down);
                target.UnregisterCallback<PointerMoveEvent>(Move);
                target.UnregisterCallback<PointerUpEvent>(Up);
                target.UnregisterCallback<PointerCancelEvent>(Cancel);
                target.UnregisterCallback<PointerCaptureOutEvent>(Lost);
            }
            private void Down(PointerDownEvent evt)
            {
                if (evt.button != 0 || PointerId >= 0) return;
                PointerId = evt.pointerId;
                if (!update(target.WorldToLocal(evt.position))) { PointerId = -1; return; }
                target.CapturePointer(PointerId);
                evt.StopPropagation();
            }
            private void Move(PointerMoveEvent evt)
            {
                if (evt.pointerId != PointerId || !target.HasPointerCapture(PointerId)) return;
                update(target.WorldToLocal(evt.position));
                evt.StopPropagation();
            }
            private void Up(PointerUpEvent evt)
            {
                if (evt.pointerId != PointerId) return;
                update(target.WorldToLocal(evt.position));
                target.ReleasePointer(PointerId);
                PointerId = -1;
                evt.StopPropagation();
            }
            private void Cancel(PointerCancelEvent evt)
            {
                if (evt.pointerId != PointerId) return;
                target.ReleasePointer(PointerId);
                PointerId = -1;
            }
            private void Lost(PointerCaptureOutEvent evt) { if (evt.pointerId == PointerId) PointerId = -1; }
        }
    }
}
