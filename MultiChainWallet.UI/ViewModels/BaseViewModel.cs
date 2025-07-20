using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultiChainWallet.UI.ViewModels
{
    /// <summary>
    /// 基础ViewModel类，提供属性更改通知
    /// Base ViewModel class providing property change notifications
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;

        /// <summary>
        /// 指示ViewModel是否正在执行操作
        /// Indicates whether the ViewModel is currently performing an operation
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 设置属性值并触发属性更改通知
        /// Sets property value and raises property changed notification
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发属性更改事件
        /// Raises the property changed event
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 