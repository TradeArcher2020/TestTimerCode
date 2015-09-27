using System;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Text;
using Android.Widget;
using Android.Util;
using Java.Lang;
using Math = System.Math;

namespace TestTimer.Android.Controls
{
    public class TextScaledEventArgs : EventArgs
    {
        public float OldSize { get; set; }
        public float NewSize { get; set; }
    }

    public delegate void TextScaledEventHandler(object sender, TextScaledEventArgs args);

    /// 
    /// TextView that automatically resizes it's content to fit the layout dimensions
    /// 
    public class AutoScaleToWidthTextView : TextView
    {
        public event TextScaledEventHandler TextResized;

        // Flag for text and/or size changes to force a resize
        private bool _needsResize = false;

        // Text size that is set from code. This acts as a starting point for resizing
        private float _textSize;

        // Temporary upper bounds on the starting text size
        private float _maxTextSize = 0;

        // Text view line spacing multiplier
        private float _spacingMult = 1.0f;

        // Text view additional line spacing
        private float _spacingAdd = 0.0f;

        public AutoScaleToWidthTextView(IntPtr a, JniHandleOwnership b) : base(a, b) { }

        // Default constructor override
        public AutoScaleToWidthTextView(Context context) : this(context, null) { }

        // Default constructor when inflating from XML file
        public AutoScaleToWidthTextView(Context context, IAttributeSet attrs) : this(context, attrs, 0) { }

        // Default constructor override
        public AutoScaleToWidthTextView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            _textSize = TextSize;

            TypedArray typeArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.AutoScaleToWidthTextView, 0, 0);
            _maxTextSize = typeArray.GetDimension(Resource.Styleable.AutoScaleToWidthTextView_maxTextSize, 1000);
        }

        //When text changes, set the force resize flag to true and reset the text size.
        protected override void OnTextChanged(ICharSequence text, int start, int before, int after)
        {
            _needsResize = true;
            // Since this view may be reused, it is good to reset the text size
            ResetTextSize();
        }

        // If the text view size changed, set the force resize flag to true
        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            if (w != oldw || h != oldh)
            {
                _needsResize = true;
            }
        }

        public override void SetTextSize(ComplexUnitType unitType, float size)
        {
            base.SetTextSize(unitType, size);
            _textSize = TextSize;
        }

        // Override the set line spacing to update our internal reference values
        public override void SetLineSpacing(float add, float mult)
        {
            base.SetLineSpacing(add, mult);
            _spacingMult = mult;
            _spacingAdd = add;
        }

        public float MaxTextSize
        {
            get { return _maxTextSize; }
            set
            {
                _maxTextSize = value;
                RequestLayout();
                Invalidate();
            }
        }

        //Reset the text to the original size
        public void ResetTextSize()
        {
            if (_textSize > 0)
            {
                base.SetTextSize(ComplexUnitType.Px, _textSize);
            }
        }

        //Resize text after measuring
        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            if (changed || _needsResize)
            {
                int widthLimit = (right - left) - CompoundPaddingLeft - CompoundPaddingRight;
                int heightLimit = (bottom - top) - CompoundPaddingBottom - CompoundPaddingTop;
                ResizeText(widthLimit, heightLimit);
            }
            base.OnLayout(changed, left, top, right, bottom);
        }

        //Resize the text size with default width and height
        public void ResizeText()
        {
            int heightLimit = Height - PaddingBottom - PaddingTop;
            int widthLimit = Width - PaddingLeft - PaddingRight;
            ResizeText(widthLimit, heightLimit);
        }


        // Resize the text size with specified width and height
        public void ResizeText(int width, int height)
        {
            string text = Text;

            // Do not resize if the view does not have dimensions or there is no text
            if (string.IsNullOrEmpty(text) || height <= 0 || width <= 0 || _textSize == 0)
                return;

            // Get the text view's paint object
            TextPaint textPaint = Paint;

            // Store the current text size
            float oldTextSize = textPaint.TextSize;

            // If there is a max text size set, use the lesser of that and the default text size
            float targetTextSize = _maxTextSize > 0 ? Math.Min(_textSize, _maxTextSize) : _textSize;

            // Get the required text height
            int textWidth = GetTextWidth(text, textPaint, targetTextSize);

            // Until we either fit within our text view or we had reached our max text size, incrementally try larger sizes
            while ( (textWidth <= width) && (targetTextSize < _maxTextSize) )
            {
                targetTextSize = Math.Min(targetTextSize + 2, _maxTextSize);
                textWidth = GetTextWidth(text, textPaint, targetTextSize);
            }

            // Some devices try to auto adjust line spacing, so force default line spacing
            // and invalidate the layout as a side effect
            textPaint.TextSize = targetTextSize;
            SetLineSpacing(_spacingAdd, _spacingMult);

            TextResized?.Invoke(this, new TextScaledEventArgs
            {
                OldSize = oldTextSize,
                NewSize = targetTextSize
            });

            // Reset force resize flag
            _needsResize = false;
        }

        // Set the text size of the text paint object and use a static layout to render text off screen before measuring
        private int GetTextHeight(string source, TextPaint paint, int width, float textSize)
        {
            // Update the text paint object
            paint.TextSize = textSize;
            // Measure using a static layout
            StaticLayout layout = new StaticLayout(source, paint, width, Layout.Alignment.AlignNormal, _spacingMult, _spacingAdd, true);
            return layout.Height;
        }

        // Set the text size of the text paint object and use a static layout to render text off screen before measuring
        private int GetTextWidth(string source, TextPaint paint, float textSize)
        {
            // Update the text paint object
            paint.TextSize = textSize;
            //Measure width of text using the Paint object's MeasureText method.  
            //NOTE: the StaticLayout Width property does not return the correct width, so we use this method instead.  
            return Convert.ToInt32( paint.MeasureText(source, 0, source.Length) );
        }
    }
}