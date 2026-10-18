using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace PartyQOL.UI.Widgets
{
    /// <summary>
    /// Dual-handle range slider — same behavior as CAE <c>CaeRangeSliderWidget</c>.
    /// Values run 0..1000 along the track; pressing the track grabs the nearer handle.
    /// Handles and filler must not accept events, and are expected left-aligned.
    /// </summary>
    public class PartyQolRangeSliderWidget : Widget
    {
        public const float Resolution = 1000f;

        private float _lowValue;
        private float _highValue = Resolution;
        private int _dragged;

        public PartyQolRangeSliderWidget(UIContext context)
            : base(context)
        {
        }

        [Editor(false)]
        public Widget LowHandle { get; set; }

        [Editor(false)]
        public Widget HighHandle { get; set; }

        [Editor(false)]
        public Widget Filler { get; set; }

        [Editor(false)]
        public float LowValue
        {
            get => _lowValue;
            set
            {
                if (value != _lowValue)
                {
                    _lowValue = value;
                    OnPropertyChanged(value, nameof(LowValue));
                }
            }
        }

        [Editor(false)]
        public float HighValue
        {
            get => _highValue;
            set
            {
                if (value != _highValue)
                {
                    _highValue = value;
                    OnPropertyChanged(value, nameof(HighValue));
                }
            }
        }

        protected override bool OnPreviewMouseScroll() => true;

        protected override void OnMouseScroll()
        {
            float delta = EventManager.DeltaMouseScroll;
            if (delta != 0f)
            {
                EventFired(delta > 0f ? "ScrollUp" : "ScrollDown");
            }
        }

        protected override void OnMousePressed()
        {
            if (!IsEnabled)
            {
                return;
            }

            float value = ValueAtMouse();
            // Equal handles: grab the one that can move toward the click.
            _dragged = System.Math.Abs(value - _lowValue) < System.Math.Abs(value - _highValue)
                       || (_lowValue == _highValue && value < _lowValue)
                ? 1
                : 2;
            Drag();
        }

        protected override void OnMouseReleased(bool isFromInput)
        {
            _dragged = 0;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (_dragged != 0)
            {
                // The release can land outside the widget; the button state ends the drag either way.
                if (Input.IsKeyDown(InputKey.LeftMouseButton))
                {
                    Drag();
                }
                else
                {
                    _dragged = 0;
                }
            }

            Layout();
        }

        private void Drag()
        {
            float value = ValueAtMouse();
            if (_dragged == 1)
            {
                LowValue = System.Math.Min(value, _highValue);
            }
            else
            {
                HighValue = System.Math.Max(value, _lowValue);
            }
        }

        private float ValueAtMouse()
        {
            float handle = LowHandle?.Size.X ?? 0f;
            float track = Size.X - handle;
            if (track <= 0f)
            {
                return 0f;
            }

            float t = (EventManager.MousePosition.X - GlobalPosition.X - handle / 2f) / track;
            return System.Math.Max(0f, System.Math.Min(1f, t)) * Resolution;
        }

        private void Layout()
        {
            float handle = LowHandle?.Size.X ?? 0f;
            float track = Size.X - handle;
            if (track <= 0f)
            {
                return;
            }

            float low = track * _lowValue / Resolution;
            float high = track * _highValue / Resolution;
            if (LowHandle != null)
            {
                LowHandle.ScaledPositionXOffset = low;
            }

            if (HighHandle != null)
            {
                HighHandle.ScaledPositionXOffset = high;
            }

            if (Filler != null)
            {
                Filler.ScaledPositionXOffset = low + handle / 2f;
                Filler.ScaledSuggestedWidth = System.Math.Max(0f, high - low);
            }
        }
    }
}
