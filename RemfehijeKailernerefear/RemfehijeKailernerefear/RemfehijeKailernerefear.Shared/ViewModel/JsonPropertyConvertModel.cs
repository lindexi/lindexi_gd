using System;
using System.Collections.Generic;
using System.Text;
using Tool.Shared.Core;
using Tool.Shared.Framework;

namespace Tool.Shared.ViewModel
{
    class JsonPropertyConvertModel : ViewModelBase
    {
        private string _originText;
        private string _jsonProperty;

        private JsonPropertyConvert JsonPropertyConvert { get; } = new JsonPropertyConvert();

        public string OriginText
        {
            get => _originText;
            set
            {
                if (value == _originText) return;
                _originText = value;
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        JsonProperty = JsonPropertyConvert.ConvertJsonProperty(value);
                    }
                    catch (Exception)
                    {
                    }
                }

                OnPropertyChanged();
            }
        }

        public string JsonProperty
        {
            get => _jsonProperty;
            set
            {
                if (value == _jsonProperty) return;
                _jsonProperty = value;
                OnPropertyChanged();
            }
        }


        public override void OnNavigatedTo(object parameter)
        {
        }
    }
}