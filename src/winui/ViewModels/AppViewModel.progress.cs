using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Dialogs;

namespace ZoDream.FileTransfer.ViewModels
{
    internal partial class AppViewModel
    {
        private ProgressDialog? _progress;
        private CancellationTokenSource? _progressToken;

        public CancellationToken ShowProgress(string title = "压缩中...")
        {
            _progressToken = new CancellationTokenSource();
            _ = CreateProgress(title);
            return _progressToken.Token;
        }

        public void CloseProgress()
        {
            if (_progress is null)
            {
                return;
            }
            DispatcherQueue.TryEnqueue(() => {
                _progress.Hide();
                _progress = null;
                _progressToken?.Dispose();
                _progressToken = null;
            });
        }

        private async Task CreateProgress(string title)
        {
            _progress = new ProgressDialog();
            _progress.Title = title;
            await OpenDialogAsync(_progress);
            _progressToken?.Cancel();
        }

        public void UpdateProgress(double progress) 
        {
            if (_progress is null)
            {
                return;
            }
            DispatcherQueue.TryEnqueue(() => {
                _progress.ViewModel.Progress = progress * 100;
            });
        }
    }
}
