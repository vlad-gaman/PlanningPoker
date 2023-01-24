using System.Timers;

namespace PlanningPokerUi.Models
{
    public class MyTimer : Timer
    {
        private ElapsedEventHandler _elapsedEventHandler;

        public int MaxTriggers { get; private set; } = 5;
        public int Countdown { get; private set; }
        

        public MyTimer(double interval) : base(interval)
        {
            Elapsed += MyTimer_Elapsed;
            Countdown = MaxTriggers;
        }

        private void MyTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Countdown--;
        }

        public void Reset()
        {
            Countdown = MaxTriggers;
            Stop();
            Start();
        }

        public void SetElapsed(ElapsedEventHandler elapsedEventHandler)
        {
            ClearElapsed();

            _elapsedEventHandler = elapsedEventHandler;

            Elapsed += _elapsedEventHandler;
            Reset();
        }

        public void ClearElapsed()
        {
            if (_elapsedEventHandler != null)
            {
                Elapsed -= _elapsedEventHandler;
            }
            _elapsedEventHandler = null;
            Stop();
        }
    }
}
