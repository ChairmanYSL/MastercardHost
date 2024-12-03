using System.ComponentModel;
using System.Configuration;
using MvvmHelpers;

namespace MastercardHost
{
    public class MainModel:INotifyPropertyChanged
    {
        private Config _config;


        public Config Config
        {
            get => _config;
            set => SettingsPropertyValue(ref _config, value);
        }

        public ServerSettings ServerSettings
        {
            get => _serverSettings;
            set
            {
                if (_serverSettings != value)
                {
                    _serverSettings = value;
                    OnPropertyChanged(nameof(ServerSettings));
                }
            }
        }

        public ClientSettings ClientSettings
        {
            get => _clientSettings;
            set
            {
                if (_clientSettings != value)
                {
                    _clientSettings = value;
                    OnPropertyChanged(nameof(ClientSettings));
                }
            }
        }

        public string RespCode
        {
            get => _respCode;
            set
            {
                if (_respCode != value)
                {
                    _respCode = value;
                    OnPropertyChanged(nameof(RespCode));
                }
            }
        }

        public string Iad
        {
            get => _iad;
            set
            {
                if (_iad != value)
                {
                    _iad = value;
                    OnPropertyChanged(nameof(Iad));
                }
            }
        }

        public string Script
        {
            get => _script;
            set
            {
                if (_script != value)
                {
                    _script = value;
                    OnPropertyChanged(nameof(Script));
                }
            }
        }

        public ObservableRangeCollection<string> OutcomeText
        {
            get => _outcomeText;
            set
            {
                if (_outcomeText != value)
                {
                    _outcomeText = value;
                    OnPropertyChanged(nameof(OutcomeText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
