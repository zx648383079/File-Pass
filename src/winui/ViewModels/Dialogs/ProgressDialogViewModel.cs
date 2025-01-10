using System;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ProgressDialogViewModel: BindableBase
    {
        private DateTime _beginTime = DateTime.Now;

        private int _elapsedTime;

        public int ElapsedTime {
            get => _elapsedTime;
            set => Set(ref _elapsedTime, value);
        }

        private int _timeLeft;

        public int TimeLeft {
            get => _timeLeft;
            set => Set(ref _timeLeft, value);
        }

        private bool _progressUnknow = true;

        public bool ProgressUnknow {
            get => _progressUnknow;
            set => Set(ref _progressUnknow, value);
        }


        private double _progress;

        public double Progress {
            get => _progress;
            set {
                Set(ref _progress, value);
                if (value > 0)
                {
                    ProgressUnknow = false;
                    Computed();
                }
            }
        }

        private void Computed()
        {
            if (ProgressUnknow)
            {
                return;
            }
            var diff = DateTime.Now - _beginTime;
            ElapsedTime = (int)diff.TotalSeconds;
            TimeLeft = (int)(diff.TotalSeconds * 100 / Progress - diff.TotalSeconds);
        }
    }
}
