using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Timer = System.Timers.Timer;

namespace TestTimer.Android
{
    public enum CountdownTimerStates
    {
        Started,
        Stopped
    }

    public enum QuestionTimeRemainingStates
    {
        Positive,
        Negative
    }

    public class CountdownTimerEventArgs : EventArgs
    {
        public string QuestionsRemaining { get; }
        public string TotalTimeRemainingText { get; }
        public string TimeRemainingForCurrentQuestionText { get; }

        public CountdownTimerEventArgs(string totalTimeRemainingText, string timeRemainingForCurrentQuestionText, string questionsRemaining)
        {
            TotalTimeRemainingText = totalTimeRemainingText;
            TimeRemainingForCurrentQuestionText = timeRemainingForCurrentQuestionText;
            QuestionsRemaining = questionsRemaining;
        }
    }

    public class CountdownTimerStopped : EventArgs
    {
        public TimeSpan TotalTimeRemainingBeforeStopped { get; }
        public bool CountdownComplete { get; }
        public int QuestionsRemaining { get; }

        public CountdownTimerStopped(TimeSpan totalTimeRemaining, int questionsRemaining)
        {
            TotalTimeRemainingBeforeStopped = totalTimeRemaining;
            CountdownComplete = totalTimeRemaining.TotalSeconds < 1;
            QuestionsRemaining = questionsRemaining;
        }
    }

    public class TestCoundownTimer
    {
        private const string TimeFormat = "{0}:{1}:{2}";
        private const string TimeFormatNegative = "-{0}:{1}:{2}";

        public int Hours { get; }
        public int Minutes { get; }
        public int TotalQuestions { get; }
        public int QuestionsRemaining { get; private set; }

        private double _questionsRemainingDigitCount;

        public TimeSpan TotalTimeRemaining { get; private set; }
        public TimeSpan TimeLeftForCurrentQuestion { get; private set; }

        public bool IsOverTimeForCurrentQuestion { get; private set; }

        public CountdownTimerStates State { get; private set; }

        public QuestionTimeRemainingStates QuestionTimeRemainingState { get; private set; }

        private readonly Timer _countdownTimer = new Timer(1000);

        public event EventHandler<CountdownTimerEventArgs> TimerUpdated;
        public event EventHandler<CountdownTimerStopped> TimerStopped;
        public event EventHandler<EventArgs> QuestionTimeRemainingNegative;
        public event EventHandler<EventArgs> QuestionTimeRemainingPositive;

        public TestCoundownTimer(int hours, int minutes, int totalQuestions, ISynchronizeInvoke screen)
        {
            Hours = hours;
            Minutes = minutes;
            QuestionsRemaining = TotalQuestions = totalQuestions;

            _questionsRemainingDigitCount = Math.Floor(Math.Log10(QuestionsRemaining) + 1);


            TotalTimeRemaining = new TimeSpan(Hours, Minutes, 0);

            TimeLeftForCurrentQuestion = CalculateTimeForQuestion(TotalTimeRemaining, QuestionsRemaining);

            //setting this property to the instance of the currently running screen will make the Elapsed event run on the UI Thread.
            _countdownTimer.SynchronizingObject = screen;

            _countdownTimer.Elapsed += CountdownTimerOnElapsed;
        }

        private void CountdownTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            System.Diagnostics.Debug.WriteLine("Timer Elasped Thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
            _countdownTimer.SynchronizingObject.Invoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("Timer Elasped SynchronizedObject.Invoke Thread ID: {0}", Thread.CurrentThread.ManagedThreadId);

                TotalTimeRemaining = TotalTimeRemaining.Subtract(new TimeSpan(0, 0, 1));

                TimeLeftForCurrentQuestion = TimeLeftForCurrentQuestion.Subtract(new TimeSpan(0, 0, 1));

                IsOverTimeForCurrentQuestion = TimeLeftForCurrentQuestion.TotalSeconds < 0;

                if (IsOverTimeForCurrentQuestion)
                {
                    OnQuestionTimeRemainingNegative();
                }
                else
                {
                    OnQuestionTimeRemainingPositive();
                }

                if (TotalTimeRemaining.TotalSeconds < 1)
                {
                    Stop();
                }
                else
                {
                    OnTimerUpdated(new CountdownTimerEventArgs(GetTotalTimeRemaining(), GetTimeRemainingForCurrentQuestion(), GetNumberOfQuestionsRemaining()));
                }
            }), null);
        }

        private TimeSpan CalculateTimeForQuestion(TimeSpan totalTimeRemaining, int questionsRemaining)
        {
            var ticksRemaining = totalTimeRemaining.Ticks;
            var ticksForQuestion = questionsRemaining > 0 ? ticksRemaining / questionsRemaining : 0;
            var timeForQuestion = TimeSpan.FromTicks(ticksForQuestion);
            if (totalTimeRemaining < timeForQuestion)
            {
                return totalTimeRemaining;
            }
            else
            {
                return timeForQuestion;
            }
        }

        public void Start()
        {
            _countdownTimer.Start();
            State = CountdownTimerStates.Started;
        }

        public void Stop(bool fireEvent = true)
        {
            _countdownTimer.Stop();
            State = CountdownTimerStates.Stopped;
            if (fireEvent)
            {
                OnTimerStopped();
            }
        }

        public void NextQuestion()
        {
            QuestionsRemaining--;
            TimeLeftForCurrentQuestion = CalculateTimeForQuestion(TotalTimeRemaining, QuestionsRemaining);

            if (QuestionsRemaining == 0)
            {
                Stop();
            }
        }

        public void PreviousQuestion()
        {
            QuestionsRemaining++;
            TimeLeftForCurrentQuestion = CalculateTimeForQuestion(TotalTimeRemaining, QuestionsRemaining);
        }

        public string GetTimeRemainingForCurrentQuestion()
        {
            if (TimeLeftForCurrentQuestion.TotalSeconds < 0)
            {
                return string.Format(TimeFormatNegative, Math.Abs(TimeLeftForCurrentQuestion.Hours).ToString("D2"), Math.Abs(TimeLeftForCurrentQuestion.Minutes).ToString("D2"), Math.Abs(TimeLeftForCurrentQuestion.Seconds).ToString("D2"));
            }
            else
            {
                return string.Format(TimeFormat, TimeLeftForCurrentQuestion.Hours.ToString("D2"), TimeLeftForCurrentQuestion.Minutes.ToString("D2"), TimeLeftForCurrentQuestion.Seconds.ToString("D2"));
            }
        }

        public string GetTotalTimeRemaining()
        {
            if (TotalTimeRemaining.TotalSeconds < 0)
            {
                return string.Format(TimeFormatNegative, Math.Abs(TotalTimeRemaining.Hours).ToString("D2"), Math.Abs(TotalTimeRemaining.Minutes).ToString("D2"), Math.Abs(TotalTimeRemaining.Seconds).ToString("D2"));
            }
            else
            {
                return string.Format(TimeFormat, TotalTimeRemaining.Hours.ToString("D2"), TotalTimeRemaining.Minutes.ToString("D2"), TotalTimeRemaining.Seconds.ToString("D2"));
            }
        }

        public string GetNumberOfQuestionsRemaining()
        {
            return QuestionsRemaining.ToString("D" + _questionsRemainingDigitCount);
        }

        protected virtual void OnTimerUpdated(CountdownTimerEventArgs args)
        {
            TimerUpdated?.Invoke(this, args);
        }

        protected virtual void OnTimerStopped()
        {
            TimerStopped?.Invoke(this, new CountdownTimerStopped(TotalTimeRemaining, QuestionsRemaining));
        }

        protected virtual void OnQuestionTimeRemainingNegative()
        {
            if (QuestionTimeRemainingState == QuestionTimeRemainingStates.Positive)
            {
                QuestionTimeRemainingState = TimeLeftForCurrentQuestion.CompareTo(TimeSpan.Zero) < 0
                    ? QuestionTimeRemainingStates.Negative
                    : QuestionTimeRemainingStates.Positive;

                QuestionTimeRemainingNegative?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void OnQuestionTimeRemainingPositive()
        {
            if (QuestionTimeRemainingState == QuestionTimeRemainingStates.Negative)
            {
                QuestionTimeRemainingState = TimeLeftForCurrentQuestion.CompareTo(TimeSpan.Zero) < 0
                    ? QuestionTimeRemainingStates.Negative
                    : QuestionTimeRemainingStates.Positive;

                QuestionTimeRemainingPositive?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}