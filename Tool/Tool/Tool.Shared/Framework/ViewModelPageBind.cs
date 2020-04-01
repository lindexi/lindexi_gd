using System;
using Tool.Shared.View;
using Tool.Shared.ViewModel;

namespace Tool.Shared.Framework
{
    public class ViewModelPageBind
    {
        public IViewModel CreateViewModel(string name)
        {
            if (name == "NavigatePage")
            {
                return new NavigateModel();
            }

            return null;
        }

        public Type GetPageType(string name)
        {
            if (name == "NavigatePage")
            {
                return typeof(NavigatePage);
            }

            return null;
        }
    }
}