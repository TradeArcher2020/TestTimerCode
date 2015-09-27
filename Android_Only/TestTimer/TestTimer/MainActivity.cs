using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Views.InputMethods;

namespace TestTimer.Android
{
    [Activity(Label = "TestTimer", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait/*, WindowSoftInputMode = SoftInput.AdjustResize*/)]
    public class MainActivity : Activity
    {
        private NumberPicker _hoursPicker;
        private NumberPicker _minutesPicker;
        private EditText _totalQuestionsTextBox;
        private Button _startTimeButton;
        private InputMethodManager _inputManager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _hoursPicker = FindViewById<NumberPicker>(Resource.Id.testHoursNumberPicker);
            _minutesPicker = FindViewById<NumberPicker>(Resource.Id.testMinutesNumberPicker);

            _hoursPicker.ValueChanged += (sender, args) => ValidateFieldsAndEnableButton();
            _minutesPicker.ValueChanged += (sender, args) => ValidateFieldsAndEnableButton();

            _hoursPicker.MaxValue = 12;
            _hoursPicker.MinValue = 0;

            _minutesPicker.MaxValue = 59;
            _minutesPicker.MinValue = 0;

            _inputManager = (InputMethodManager)GetSystemService(Context.InputMethodService);

            EditText hoursEditText = (EditText)_hoursPicker.GetChildAt(0);
            hoursEditText.Focusable = true;
            hoursEditText.FocusableInTouchMode = true;

            hoursEditText.FocusChange += HandleEditTextFocusChangedForKeyboard;

            //_inputManager.ShowSoftInput(hoursEditText, ShowFlags.Implicit);

            EditText minutesEditText = (EditText)_hoursPicker.GetChildAt(0);
            minutesEditText.Focusable = true;
            minutesEditText.FocusableInTouchMode = true;

            minutesEditText.FocusChange += HandleEditTextFocusChangedForKeyboard;

            //_inputManager.ShowSoftInput(minutesEditText, ShowFlags.Implicit);

            _totalQuestionsTextBox = FindViewById<EditText>(Resource.Id.totalQuestionsTextBox);
            _totalQuestionsTextBox.TextChanged += (sender, args) => ValidateFieldsAndEnableButton();
            //_inputManager.ShowSoftInput(_totalQuestionsTextBox, ShowFlags.Implicit);

            _totalQuestionsTextBox.FocusChange += HandleEditTextFocusChangedForKeyboard;

            _startTimeButton = FindViewById<Button>(Resource.Id.startTimerButton);

            _startTimeButton.Click += delegate
            {
                int numberOfQuestions = !string.IsNullOrEmpty(_totalQuestionsTextBox.Text)
                    ? Convert.ToInt32(_totalQuestionsTextBox.Text)
                    : 0;

                if (_hoursPicker.Value == 0 && _minutesPicker.Value == 0 || numberOfQuestions == 0)
                {
                    return;
                }

                var intent = new Intent(this, typeof(TimerViewActivity));
                intent.PutExtra("hours", _hoursPicker.Value);
                intent.PutExtra("minutes", _minutesPicker.Value);
                intent.PutExtra("questions", numberOfQuestions);
                StartActivity(intent);
            };

            ValidateFieldsAndEnableButton();
        }

        private void ValidateFieldsAndEnableButton()
        {
            int numberOfQuestions = !string.IsNullOrEmpty(_totalQuestionsTextBox.Text)
                ? Convert.ToInt32(_totalQuestionsTextBox.Text)
                : 0;

            if (_hoursPicker.Value == 0 && _minutesPicker.Value == 0 || numberOfQuestions == 0)
            {
                _startTimeButton.Enabled = false;
            }
            else
            {
                _startTimeButton.Enabled = true;
            }
        }

        private void HandleEditTextFocusChangedForKeyboard(object sender, View.FocusChangeEventArgs focusChangeEventArgs)
        {
            if (!focusChangeEventArgs.HasFocus)
            {
                HideSoftKeyboard((View)sender);
            }
        }

        private void HideSoftKeyboard(View input)
        {
            _inputManager.HideSoftInputFromWindow(input.WindowToken, HideSoftInputFlags.ImplicitOnly);
        }

        private void ShowSoftKeyboard(View input, bool selectText)
        {
            if (selectText) ((EditText)input).SelectAll();
            ThreadPool.QueueUserWorkItem(s =>
            {
                Thread.Sleep(100); // For some reason, a short delay is required here.
                RunOnUiThread(() => ((InputMethodManager)GetSystemService(InputMethodService)).ShowSoftInput(input, ShowFlags.Implicit));
            });
        }
    }

}

