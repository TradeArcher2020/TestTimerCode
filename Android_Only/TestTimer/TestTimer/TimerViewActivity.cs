using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TestTimer.Android.Controls;

namespace TestTimer.Android
{
    [Activity(Label = "TimerViewActivity", ScreenOrientation = ScreenOrientation.Portrait)]
    public class TimerViewActivity : Activity
    {
        private TestCoundownTimer _timer;
        private AutoScaleToWidthTextView _questionTimeText;
        private AutoScaleToWidthTextView _testTimeText;
        private AutoScaleToWidthTextView _questionsRemainingText;
        private Button _prevButton;
        private Button _nextButton;
        private Drawable _questionTimeTextDefaultBackground;

        protected override void OnCreate(Bundle bundle)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);

            base.OnCreate(bundle);

            SetContentView(Resource.Layout.TimerView);

            //Get the data passed into this view.
            var hours = Intent.GetIntExtra("hours", 0);
            var minutes = Intent.GetIntExtra("minutes", 0);
            var questions = Intent.GetIntExtra("questions", 0);

            //Validate the data.  If it doesn't pass, navigate back.
            if (hours == 0 && minutes == 0 || questions == 0)
            {
                OnBackPressed();
            }

            //Keep the screen alive so it doesn't go into sleep mode.  
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            //Get references to our UI controls
            _questionTimeText = FindViewById<AutoScaleToWidthTextView>(Resource.Id.questionTimeRemainingTextView);
            _testTimeText = FindViewById<AutoScaleToWidthTextView>(Resource.Id.testTimeRemainingTextView);
            _questionsRemainingText = FindViewById<AutoScaleToWidthTextView>(Resource.Id.questionsRemainingTextView);
            _prevButton = FindViewById<Button>(Resource.Id.prevQuestionButton);
            _nextButton = FindViewById<Button>(Resource.Id.nextQuestionButton);

            //Store the default background for the TextView that shows the amount of time left for the current question.  
            _questionTimeTextDefaultBackground = _questionTimeText.Background;

            //Start the countdown timer object using the data passed into this Activity.  
            _timer = new TestCoundownTimer(hours, minutes, questions, new SynchronizeInvoke() { Activity = this });

            //Update the relevant TextView controls every second.
            _timer.TimerUpdated += (sender, args) =>
            {
                _questionTimeText.Text = args.TimeRemainingForCurrentQuestionText;
                _testTimeText.Text = args.TotalTimeRemainingText;
                _questionsRemainingText.Text = args.QuestionsRemaining;
            };

            //Listen for the stopped event of the timer.
            _timer.TimerStopped += OnTimerOnTimerStopped;

            //Change the background color of the TextView containing the time remaining for the current question to red when it is negative.  
            _timer.QuestionTimeRemainingNegative += (sender, args) =>
            {
                _questionTimeText.Background = new ColorDrawable(Color.Red);
            };

            //Change the background color of the TextView containing the time remaining for the current question back to its default when it is positive.  
            _timer.QuestionTimeRemainingPositive += (sender, args) =>
            {
                _questionTimeText.Background = _questionTimeTextDefaultBackground;
            };

            //Start our timer object.
            _timer.Start();

            //Update our Next and Previous buttons so they are properly enabled/disabled.
            UpdateButtons();

            //When the next button is clicked, alert the countdown timer object so it can update its calculations.
            _nextButton.Click += (sender, args) =>
            {
                _timer.NextQuestion();
                UpdateButtons();
            };

            //When the previous button is clicked, alert the countdown timer object so it can update its calculations.
            _prevButton.Click += (sender, args) =>
            {
                _timer.PreviousQuestion();
                UpdateButtons();
            };
        }

        private void OnTimerOnTimerStopped(object sender, CountdownTimerStopped stopped)
        {
            //To avoid problems, detach this event so it can only be fired once.
            _timer.TimerStopped -= OnTimerOnTimerStopped;

            //Display a dialog to the user that the test has ended.  
            var dlgBuilder = new AlertDialog.Builder(this);
            dlgBuilder.SetTitle("Test Complete!");
            dlgBuilder.SetMessage("The test has ended.");
            //After the user dismisses the dialog, navigate to the prvious Activity.  
            dlgBuilder.SetPositiveButton("OK", (s, args) => { OnBackPressed(); });
            dlgBuilder.Show();
        }

        private void UpdateButtons()
        {
            _prevButton.Enabled = _timer.TotalQuestions != _timer.QuestionsRemaining;
            _nextButton.Enabled = _timer.QuestionsRemaining <= _timer.TotalQuestions;
        }

        //Cleanup code.  
        protected override void OnStop()
        {
            if (_timer != null && _timer.State != CountdownTimerStates.Stopped)
            {
                _timer.Stop(false);
            }
            _timer = null;
            base.OnStop();
        }

        protected override void OnPause()
        {
            OnBackPressed();
            
            base.OnPause();
        }
    }
}